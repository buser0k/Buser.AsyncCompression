using System;
using System.Threading;
using System.Threading.Tasks;
using Buser.AsyncCompression.Application.Factories;
using Buser.AsyncCompression.Domain.Entities;
using Buser.AsyncCompression.Domain.Interfaces;
using Buser.AsyncCompression.Domain.ValueObjects;

namespace Buser.AsyncCompression.Application.Services
{
    public class CompressionApplicationService
    {
        private readonly ICompressionService _compressionService;
        private readonly IFileService _fileService;
        private readonly CompressionJobFactory _jobFactory;
        private readonly IProgressReporter _progressReporter;

        public CompressionApplicationService(
            ICompressionService compressionService,
            IFileService fileService,
            CompressionJobFactory jobFactory,
            IProgressReporter progressReporter)
        {
            _compressionService = compressionService ?? throw new ArgumentNullException(nameof(compressionService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _jobFactory = jobFactory ?? throw new ArgumentNullException(nameof(jobFactory));
            _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
        }

        public CompressionJob CreateJob(string inputFilePath, CompressionSettings? settings = null)
        {
            return _jobFactory.CreateJob(inputFilePath, settings);
        }

        public async Task<CompressionResult> CompressFileAsync(CompressionJob job)
        {
            try
            {
                // Validate input file
                if (!await _fileService.ExistsAsync(job.InputFile, job.CancellationToken))
                {
                    return CompressionResult.Failed($"Input file does not exist: {job.InputFile.FullPath}");
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
                return CompressionResult.Failed("Compression was cancelled");
            }
            catch (Exception ex)
            {
                return CompressionResult.Failed($"Compression failed: {ex.Message}");
            }
        }

        public async Task<CompressionResult> CompressFileAsync(string inputFilePath, CompressionSettings? settings = null)
        {
            var job = CreateJob(inputFilePath, settings);
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
