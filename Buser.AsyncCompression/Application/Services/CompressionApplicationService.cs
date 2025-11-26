using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        /// Compresses all files within the specified directory recursively.
        /// </summary>
        /// <param name="directoryPath">The directory path to compress.</param>
        /// <param name="settings">Compression settings. If null, default settings are used.</param>
        /// <param name="algorithmName">Compression algorithm name. If null, the default algorithm is used.</param>
        /// <param name="cancellationToken">Token used to cancel the directory compression operation.</param>
        /// <returns>The directory compression result with details for each processed file.</returns>
        public async Task<DirectoryCompressionResult> CompressDirectoryAsync(
            string directoryPath,
            CompressionSettings? settings = null,
            string? algorithmName = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("Directory path cannot be null or empty.", nameof(directoryPath));
            }

            var fullPath = Path.GetFullPath(directoryPath);

            if (!Directory.Exists(fullPath))
            {
                _logger.LogWarning("Directory does not exist: {DirectoryPath}", fullPath);
                return DirectoryCompressionResult.DirectoryNotFound(fullPath);
            }

            var filesToCompress = Directory.EnumerateFiles(fullPath, "*", SearchOption.AllDirectories).ToList();
            if (!filesToCompress.Any())
            {
                _logger.LogInformation("Directory {DirectoryPath} does not contain files to compress", fullPath);
                return DirectoryCompressionResult.Empty(fullPath);
            }

            _logger.LogInformation(
                "Starting directory compression for {DirectoryPath}. Files to compress: {Count}",
                fullPath,
                filesToCompress.Count);

            var fileResults = new List<FileCompressionSummary>(filesToCompress.Count);

            for (var index = 0; index < filesToCompress.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var filePath = filesToCompress[index];
                _logger.LogInformation("Compressing file {Current}/{Total}: {FilePath}", index + 1, filesToCompress.Count, filePath);

                var job = CreateJob(filePath, settings, algorithmName);
                CompressionResult result;

                try
                {
                    _progressReporter.Report(0);
                    result = await CompressFileAsync(job);
                }
                finally
                {
                    job.Dispose();
                }

                fileResults.Add(new FileCompressionSummary(filePath, result));

                var directoryProgress = (double)(index + 1) / filesToCompress.Count;
                _progressReporter.Report(directoryProgress);
            }

            return new DirectoryCompressionResult(fullPath, fileResults);
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

    /// <summary>
    /// Represents the outcome of a recursive directory compression operation.
    /// </summary>
    public class DirectoryCompressionResult
    {
        public DirectoryCompressionResult(string directoryPath, IReadOnlyList<FileCompressionSummary> fileResults, string? errorMessage = null)
        {
            DirectoryPath = directoryPath;
            FileResults = fileResults ?? Array.Empty<FileCompressionSummary>();
            ErrorMessage = errorMessage ?? string.Empty;
        }

        /// <summary>
        /// Gets the directory that was processed.
        /// </summary>
        public string DirectoryPath { get; }

        /// <summary>
        /// Gets the per-file compression results.
        /// </summary>
        public IReadOnlyList<FileCompressionSummary> FileResults { get; }

        /// <summary>
        /// Gets a general error message if the directory could not be processed.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets a value indicating whether the operation encountered a general error (e.g., directory not found).
        /// </summary>
        public bool HasGeneralError => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>
        /// Gets the total number of files discovered in the directory.
        /// </summary>
        public int TotalFiles => FileResults.Count;

        /// <summary>
        /// Gets the number of files that were compressed successfully.
        /// </summary>
        public int SucceededFiles => FileResults.Count(result => result.IsSuccess);

        /// <summary>
        /// Gets the number of files that failed to compress.
        /// </summary>
        public int FailedFiles => FileResults.Count - SucceededFiles;

        /// <summary>
        /// Gets a value indicating whether all files were compressed successfully.
        /// </summary>
        public bool IsSuccess => !HasGeneralError && FailedFiles == 0;

        public static DirectoryCompressionResult DirectoryNotFound(string directoryPath)
            => new DirectoryCompressionResult(directoryPath, Array.Empty<FileCompressionSummary>(), $"Directory does not exist: {directoryPath}");

        public static DirectoryCompressionResult Empty(string directoryPath)
            => new DirectoryCompressionResult(directoryPath, Array.Empty<FileCompressionSummary>());
    }

    /// <summary>
    /// Provides information about a single file that was processed during directory compression.
    /// </summary>
    public class FileCompressionSummary
    {
        public FileCompressionSummary(string filePath, CompressionResult result)
        {
            FilePath = filePath;
            Result = result ?? throw new ArgumentNullException(nameof(result));
        }

        /// <summary>
        /// Gets the path of the file that was compressed.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the compression result for this file.
        /// </summary>
        public CompressionResult Result { get; }

        /// <summary>
        /// Gets a value indicating whether the compression succeeded.
        /// </summary>
        public bool IsSuccess => Result.IsSuccess;

        /// <summary>
        /// Gets the output path for the compressed file, if available.
        /// </summary>
        public string? OutputFilePath => Result.Job?.OutputFile.FullPath;

        /// <summary>
        /// Gets the error message for the compression operation.
        /// </summary>
        public string ErrorMessage => Result.ErrorMessage;
    }
}
