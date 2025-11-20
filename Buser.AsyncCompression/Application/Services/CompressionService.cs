using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Buser.AsyncCompression.Domain.Entities;
using Buser.AsyncCompression.Domain.Interfaces;

namespace Buser.AsyncCompression.Application.Services
{
    public class CompressionService : ICompressionService
    {
        private readonly ICompressionAlgorithm _compressionAlgorithm;
        private readonly IFileService _fileService;
        private readonly IProgressReporter _progressReporter;

        public CompressionService(
            ICompressionAlgorithm compressionAlgorithm,
            IFileService fileService,
            IProgressReporter progressReporter)
        {
            _compressionAlgorithm = compressionAlgorithm ?? throw new ArgumentNullException(nameof(compressionAlgorithm));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
        }

        public async Task<CompressionJob> CompressAsync(CompressionJob job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            job.Start();

            try
            {
                using (var inputStream = await _fileService.OpenReadAsync(job.InputFile, job.CancellationToken))
                using (var outputStream = await _fileService.CreateAsync(job.OutputFile, job.CancellationToken))
                {
                    await ProcessCompressionAsync(job, inputStream, outputStream);
                }

                job.Complete();
                return job;
            }
            catch (Exception)
            {
                job.Fail();
                throw;
            }
        }

        private async Task ProcessCompressionAsync(CompressionJob job, Stream inputStream, Stream outputStream)
        {
            var settings = job.Settings;
            var cancellationToken = job.CancellationToken;
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
                    return _compressionAlgorithm.Compress(bytes);
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
                cancellationToken.ThrowIfCancellationRequested();
                await outputStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                job.UpdateProgress(job.ProcessedBytes + bytes.Length);
                _progressReporter.Report(job.ProgressPercentage);
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
                job.Pause();
            }
        }

        public void Resume(CompressionJob job)
        {
            if (job.Status == Domain.Entities.CompressionStatus.Paused)
            {
                job.Resume();
            }
        }

        public void Cancel(CompressionJob job)
        {
            job.Cancel();
        }
    }
}
