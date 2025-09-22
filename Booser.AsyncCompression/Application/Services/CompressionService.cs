using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Booser.AsyncCompression.Domain.Entities;
using Booser.AsyncCompression.Domain.Interfaces;

namespace Booser.AsyncCompression.Application.Services
{
    public class CompressionService : ICompressionService
    {
        private readonly ICompressionAlgorithm _compressionAlgorithm;
        private readonly IFileService _fileService;
        private readonly IProgressReporter _progressReporter;
        private readonly ManualResetEvent _pauseEvent = new ManualResetEvent(true);
        private bool _interrupted = false;

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
                using (var inputStream = await _fileService.OpenReadAsync(job.InputFile))
                using (var outputStream = await _fileService.CreateAsync(job.OutputFile))
                {
                    await ProcessCompressionAsync(job, inputStream, outputStream);
                }

                job.Complete();
                return job;
            }
            catch (Exception)
            {
                job.Cancel();
                throw;
            }
        }

        private async Task ProcessCompressionAsync(CompressionJob job, Stream inputStream, Stream outputStream)
        {
            var settings = job.Settings;
            var buffer = new BufferBlock<byte[]>(new DataflowBlockOptions { BoundedCapacity = settings.BoundedCapacity });

            var compressorOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism,
                BoundedCapacity = settings.BoundedCapacity
            };

            var compressor = new TransformBlock<byte[], byte[]>(
                bytes => _compressionAlgorithm.Compress(bytes),
                compressorOptions);

            var writerOptions = new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = settings.BoundedCapacity,
                SingleProducerConstrained = true
            };

            var writer = new ActionBlock<byte[]>(bytes =>
            {
                outputStream.Write(bytes, 0, bytes.Length);
                job.UpdateProgress(job.ProcessedBytes + bytes.Length);
                _progressReporter.Report(job.ProgressPercentage);
            }, writerOptions);

            // Link the pipeline
            buffer.LinkTo(compressor);
            await buffer.Completion.ContinueWith(task => compressor.Complete());

            compressor.LinkTo(writer);
            await compressor.Completion.ContinueWith(task => writer.Complete());

            var readBuffer = new byte[settings.BufferSize];

            while (true)
            {
                if (_interrupted)
                {
                    buffer.Complete();
                    await writer.Completion;
                    return;
                }

                _pauseEvent.WaitOne(Timeout.Infinite);

                int readCount = await inputStream.ReadAsync(readBuffer, 0, settings.BufferSize);

                if (readCount > 0)
                {
                    var postData = new byte[readCount];
                    Buffer.BlockCopy(readBuffer, 0, postData, 0, readCount);

                    while (!buffer.Post(postData))
                    {
                        // Wait until buffer can accept data
                    }
                }

                if (readCount != settings.BufferSize)
                {
                    buffer.Complete();
                    break;
                }
            }

            await writer.Completion;
        }

        public void Pause(CompressionJob job)
        {
            if (job.Status == Domain.Entities.CompressionStatus.Running)
            {
                job.Pause();
                _pauseEvent.Reset();
            }
        }

        public void Resume(CompressionJob job)
        {
            if (job.Status == Domain.Entities.CompressionStatus.Paused)
            {
                job.Resume();
                _pauseEvent.Set();
            }
        }

        public void Cancel(CompressionJob job)
        {
            _interrupted = true;
            _pauseEvent.Set();
            job.Cancel();
        }
    }
}
