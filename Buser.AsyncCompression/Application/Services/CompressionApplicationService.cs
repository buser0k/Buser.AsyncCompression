using System;
using System.Threading;
using System.Threading.Tasks;
using Buser.AsyncCompression.Application.Factories;
using Buser.AsyncCompression.Domain.Entities;
using Buser.AsyncCompression.Domain.Interfaces;
using Buser.AsyncCompression.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Buser.AsyncCompression.Application.Services
{
    public class CompressionApplicationService
    {
        private readonly ICompressionService _compressionService;
        private readonly IFileService _fileService;
        private readonly CompressionJobFactory _jobFactory;
        private readonly IProgressReporter _progressReporter;
        private readonly ILogger<CompressionApplicationService> _logger;

        public CompressionApplicationService(
            ICompressionService compressionService,
            IFileService fileService,
            CompressionJobFactory jobFactory,
            IProgressReporter progressReporter,
            ILogger<CompressionApplicationService> logger)
        {
            _compressionService = compressionService ?? throw new ArgumentNullException(nameof(compressionService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _jobFactory = jobFactory ?? throw new ArgumentNullException(nameof(jobFactory));
            _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public CompressionJob CreateJob(string inputFilePath, CompressionSettings? settings = null, string? algorithmName = null)
        {
            return _jobFactory.CreateJob(inputFilePath, settings, algorithmName);
        }

        public async Task<CompressionResult> CompressFileAsync(CompressionJob job)
        {
            try
            {
                // Validate input file
                if (!await _fileService.ExistsAsync(job.InputFile, job.CancellationToken))
                {
                    _logger.LogWarning("Input file does not exist: {FilePath}", job.InputFile.FullPath);
                    return CompressionResult.Failed($"Input file does not exist: {job.InputFile.FullPath}");
                }

                _logger.LogInformation("Validating disk space for compression job {JobId}", job.Id);
                // Validate disk space before compression
                var validationResult = await ValidateDiskSpaceAsync(job);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Disk space validation failed for job {JobId}: {Error}", job.Id, validationResult.ErrorMessage);
                    return CompressionResult.Failed(validationResult.ErrorMessage);
                }

                // Delete output file if exists
                if (await _fileService.ExistsAsync(job.OutputFile, job.CancellationToken))
                {
                    await _fileService.DeleteAsync(job.OutputFile, job.CancellationToken);
                }

                // Start compression
                var completedJob = await _compressionService.CompressAsync(job);
                
                return CompressionResult.Success(completedJob);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Compression job {JobId} was cancelled", job.Id);
                return CompressionResult.Failed("Compression was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Compression job {JobId} failed with exception", job.Id);
                job.Fail();
                return CompressionResult.Failed($"Compression failed: {ex.Message}");
            }
        }

        private Task<(bool IsValid, string ErrorMessage)> ValidateDiskSpaceAsync(CompressionJob job)
        {
            try
            {
                job.CancellationToken.ThrowIfCancellationRequested();

                var inputFileSize = job.InputFile.Size;
                if (inputFileSize == 0)
                {
                    return Task.FromResult((true, string.Empty));
                }

                // Estimate compressed size (typically 50-90% of original, use 100% as worst case)
                // For better accuracy, we could do a quick compression test, but that's expensive
                var estimatedOutputSize = inputFileSize; // Conservative estimate

                // Get drive info for output file
                var outputDrive = System.IO.Path.GetPathRoot(job.OutputFile.FullPath);
                if (string.IsNullOrEmpty(outputDrive))
                {
                    return Task.FromResult((true, string.Empty)); // Can't determine drive, skip validation
                }

                var driveInfo = new System.IO.DriveInfo(outputDrive);
                var availableSpace = driveInfo.AvailableFreeSpace;

                // Check if we have enough space (with 10% safety margin)
                var requiredSpace = estimatedOutputSize * 1.1;
                if (availableSpace < requiredSpace)
                {
                    var availableMB = availableSpace / (1024.0 * 1024.0);
                    var requiredMB = requiredSpace / (1024.0 * 1024.0);
                    return Task.FromResult((false, $"Insufficient disk space. Required: {requiredMB:F2} MB, Available: {availableMB:F2} MB"));
                }

                return Task.FromResult((true, string.Empty));
            }
            catch (Exception)
            {
                // If validation fails, log but don't block compression
                // (disk space might change during compression)
                _logger.LogWarning("Disk space validation failed, but continuing with compression");
                return Task.FromResult((true, string.Empty));
            }
        }

        public async Task<CompressionResult> CompressFileAsync(string inputFilePath, CompressionSettings? settings = null, string? algorithmName = null)
        {
            var job = CreateJob(inputFilePath, settings, algorithmName);
            return await CompressFileAsync(job);
        }

        public void PauseCompression(CompressionJob job)
        {
            _compressionService.Pause(job);
        }

        public void ResumeCompression(CompressionJob job)
        {
            _compressionService.Resume(job);
        }

        public void CancelCompression(CompressionJob job)
        {
            _compressionService.Cancel(job);
        }
    }

    public class CompressionResult
    {
        public bool IsSuccess { get; }
        public CompressionJob Job { get; }
        public string ErrorMessage { get; }

        private CompressionResult(bool isSuccess, CompressionJob job, string errorMessage)
        {
            IsSuccess = isSuccess;
            Job = job;
            ErrorMessage = errorMessage;
        }

        public static CompressionResult Success(CompressionJob job)
        {
            return new CompressionResult(true, job, string.Empty);
        }

        public static CompressionResult Failed(string errorMessage)
        {
            return new CompressionResult(false, null!, errorMessage);
        }
    }
}
