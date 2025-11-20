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
    /// <summary>
    /// Application service that orchestrates compression operations.
    /// This service coordinates between domain services and infrastructure services.
    /// </summary>
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

        /// <summary>
        /// Creates a new compression job for the specified input file.
        /// </summary>
        /// <param name="inputFilePath">The path to the input file to compress.</param>
        /// <param name="settings">The compression settings to use. If null, default settings are used.</param>
        /// <param name="algorithmName">The name of the compression algorithm to use (e.g., "gzip", "brotli"). If null, the default algorithm is used.</param>
        /// <returns>A new compression job instance.</returns>
        public CompressionJob CreateJob(string inputFilePath, CompressionSettings? settings = null, string? algorithmName = null)
        {
            return _jobFactory.CreateJob(inputFilePath, settings, algorithmName);
        }

        /// <summary>
        /// Compresses a file asynchronously using the provided compression job.
        /// </summary>
        /// <param name="job">The compression job to execute.</param>
        /// <returns>A task that represents the asynchronous compression operation. The task result contains the compression result.</returns>
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

                // Delete output file if exists (FileService.DeleteAsync handles non-existent files gracefully)
                await _fileService.DeleteAsync(job.OutputFile, job.CancellationToken);

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

        /// <summary>
        /// Compresses a file asynchronously by creating a job and executing it.
        /// </summary>
        /// <param name="inputFilePath">The path to the input file to compress.</param>
        /// <param name="settings">The compression settings to use. If null, default settings are used.</param>
        /// <param name="algorithmName">The name of the compression algorithm to use. If null, the default algorithm is used.</param>
        /// <returns>A task that represents the asynchronous compression operation. The task result contains the compression result.</returns>
        public async Task<CompressionResult> CompressFileAsync(string inputFilePath, CompressionSettings? settings = null, string? algorithmName = null)
        {
            var job = CreateJob(inputFilePath, settings, algorithmName);
            return await CompressFileAsync(job);
        }

        /// <summary>
        /// Pauses the compression operation for the specified job.
        /// </summary>
        /// <param name="job">The compression job to pause.</param>
        public void PauseCompression(CompressionJob job)
        {
            _compressionService.Pause(job);
        }

        /// <summary>
        /// Resumes a paused compression operation for the specified job.
        /// </summary>
        /// <param name="job">The compression job to resume.</param>
        public void ResumeCompression(CompressionJob job)
        {
            _compressionService.Resume(job);
        }

        /// <summary>
        /// Cancels the compression operation for the specified job.
        /// </summary>
        /// <param name="job">The compression job to cancel.</param>
        public void CancelCompression(CompressionJob job)
        {
            _compressionService.Cancel(job);
        }
    }

    /// <summary>
    /// Represents the result of a compression operation.
    /// </summary>
    public class CompressionResult
    {
        /// <summary>
        /// Gets a value indicating whether the compression operation was successful.
        /// </summary>
        public bool IsSuccess { get; }
        
        /// <summary>
        /// Gets the compression job that was executed. This is null if the operation failed.
        /// </summary>
        public CompressionJob Job { get; }
        
        /// <summary>
        /// Gets the error message if the operation failed, or an empty string if successful.
        /// </summary>
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
