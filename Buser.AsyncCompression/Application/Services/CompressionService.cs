using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Buser.AsyncCompression.Domain.Entities;
using Buser.AsyncCompression.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Buser.AsyncCompression.Application.Services
{
    public class CompressionService : ICompressionService
    {
        private readonly ICompressionAlgorithm _defaultCompressionAlgorithm;
        private readonly IFileService _fileService;
        private readonly IProgressReporter _progressReporter;
        private readonly ILogger<CompressionService> _logger;
        private readonly Application.Factories.CompressionAlgorithmFactory _algorithmFactory;

        public CompressionService(
            ICompressionAlgorithm defaultCompressionAlgorithm,
            IFileService fileService,
            IProgressReporter progressReporter,
            ILogger<CompressionService> logger,
            Application.Factories.CompressionAlgorithmFactory algorithmFactory)
        {
            _defaultCompressionAlgorithm = defaultCompressionAlgorithm ?? throw new ArgumentNullException(nameof(defaultCompressionAlgorithm));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _algorithmFactory = algorithmFactory ?? throw new ArgumentNullException(nameof(algorithmFactory));
        }

        private ICompressionAlgorithm GetAlgorithmForJob(CompressionJob job)
        {
            // Determine algorithm based on output file extension
            var extension = System.IO.Path.GetExtension(job.OutputFile.FullPath).ToLower();
            return extension switch
            {
                ".gz" => _algorithmFactory.CreateGZipAlgorithm(),
                ".br" => _algorithmFactory.CreateBrotliAlgorithm(),
                _ => _defaultCompressionAlgorithm // Default fallback
            };
        }

        public async Task<CompressionJob> CompressAsync(CompressionJob job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            _logger.LogInformation("Starting compression job {JobId} for file {InputFile}", job.Id, job.InputFile.FullPath);
            job.Start();

            try
            {
                using (var inputStream = await _fileService.OpenReadAsync(job.InputFile, job.CancellationToken))
                using (var outputStream = await _fileService.CreateAsync(job.OutputFile, job.CancellationToken))
                {
                    await ProcessCompressionAsync(job, inputStream, outputStream);
                }

                job.Complete();
                _logger.LogInformation("Compression job {JobId} completed successfully. Output: {OutputFile}", job.Id, job.OutputFile.FullPath);
                return job;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Compression job {JobId} was cancelled", job.Id);
                job.Cancel();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Compression job {JobId} failed: {ErrorMessage}", job.Id, ex.Message);
                job.Fail();
                throw new InvalidOperationException($"Compression failed: {ex.Message}", ex);
            }
        }

        private async Task ProcessCompressionAsync(CompressionJob job, Stream inputStream, Stream outputStream)
        {
            var settings = job.Settings;
            var cancellationToken = job.CancellationToken;
            var algorithm = GetAlgorithmForJob(job);
            
            _logger.LogInformation("Using {Algorithm} algorithm for job {JobId}", algorithm.Name, job.Id);
            
            var buffer = new BufferBlock<byte[]>(new DataflowBlockOptions { BoundedCapacity = settings.BoundedCapacity });

            var compressorOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism,
                BoundedCapacity = settings.BoundedCapacity,
                CancellationToken = cancellationToken
            };

            var compressor = new TransformBlock<byte[], byte[]>(
                bytes =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return algorithm.Compress(bytes);
                },
                compressorOptions);

            var writerOptions = new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = settings.BoundedCapacity,
                SingleProducerConstrained = true,
                CancellationToken = cancellationToken
            };

            var writer = new ActionBlock<byte[]>(async bytes =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await outputStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                    job.UpdateProgress(job.ProcessedBytes + bytes.Length);
                    _progressReporter.Report(job.ProgressPercentage);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    // Mark job as failed and rethrow to stop the pipeline
                    _logger.LogError(ex, "Failed to write compressed data for job {JobId}", job.Id);
                    job.Fail();
                    throw new IOException($"Failed to write compressed data: {ex.Message}", ex);
                }
            }, writerOptions);

            // Link the pipeline
            buffer.LinkTo(compressor);
            _ = buffer.Completion.ContinueWith(task => compressor.Complete(), cancellationToken);

            compressor.LinkTo(writer);
            _ = compressor.Completion.ContinueWith(task => writer.Complete(), cancellationToken);

            var readBuffer = new byte[settings.BufferSize];

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Wait for pause event (non-blocking check)
                    job.PauseEvent.Wait(cancellationToken);

                    int readCount = await inputStream.ReadAsync(readBuffer, 0, settings.BufferSize, cancellationToken);

                    if (readCount > 0)
                    {
                        var postData = new byte[readCount];
                        Buffer.BlockCopy(readBuffer, 0, postData, 0, readCount);

                        // Use SendAsync for efficient asynchronous posting to buffer
                        // This is more efficient than polling with Post() and Task.Delay
                        await buffer.SendAsync(postData, cancellationToken);
                    }

                    if (readCount == 0) // End of stream
                    {
                        buffer.Complete();
                        break;
                    }
                }

                await writer.Completion;
            }
            catch (OperationCanceledException)
            {
                buffer.Complete();
                throw;
            }
        }

        public void Pause(CompressionJob job)
        {
            if (job.Status == Domain.Entities.CompressionStatus.Running)
            {
                _logger.LogInformation("Pausing compression job {JobId}", job.Id);
                job.Pause();
            }
        }

        public void Resume(CompressionJob job)
        {
            if (job.Status == Domain.Entities.CompressionStatus.Paused)
            {
                _logger.LogInformation("Resuming compression job {JobId}", job.Id);
                job.Resume();
            }
        }

        public void Cancel(CompressionJob job)
        {
            _logger.LogInformation("Cancelling compression job {JobId}", job.Id);
            job.Cancel();
        }
    }
}
