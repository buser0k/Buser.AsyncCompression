using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Buser.AsyncCompression.Domain.Interfaces;

namespace Buser.AsyncCompression
{
    class TaskCompression
    {
        private const int MaxBufferSize = 4194304;
        private readonly ICompressionAlgorithm _compressionAlgorithm;
        private int _processedBlocks;
        private bool _interrupted = false;

        private readonly ManualResetEvent _pauseEvent = new ManualResetEvent(true);

        internal delegate void ChunkProcessedEventHandler(object sender, ChunkProcessedEventHandlerArgs args);

        internal class ChunkProcessedEventHandlerArgs
        {
            public int BlockNumber { get; private set; }

            public ChunkProcessedEventHandlerArgs(int blockSize, int blockNumber)
            {
                BlockNumber = blockNumber;
            }
        }

        public event EventHandler<ChunkProcessedEventHandlerArgs>? ChunkProcessed;

        public int BufferSize { get; private set; }

        public TaskCompression(ICompressionAlgorithm algorithm)
        {
            _compressionAlgorithm = algorithm;
            BufferSize = MaxBufferSize;
        }

        public TaskCompression(ICompressionAlgorithm algorithm, int bufferSize) : this(algorithm)
        {
            BufferSize = (bufferSize > MaxBufferSize) ? MaxBufferSize : bufferSize;
        }

        public Task Compress(Stream inputStream, Stream outputStream)
        {
            return new Task(() =>
            {
                var buffer = new BufferBlock<byte[]>(new DataflowBlockOptions { BoundedCapacity = 100 });

                var compressorOptions = new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    BoundedCapacity = 100
                };
                var compressor = new TransformBlock<byte[], byte[]>(bytes => _compressionAlgorithm.Compress(bytes),
                    compressorOptions);

                var writerOptions = new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 100,
                    SingleProducerConstrained = true
                };
                var writer = new ActionBlock<byte[]>(bytes =>
                {
                    outputStream.Write(bytes, 0, bytes.Length);

                    ChunkProcessed?.Invoke(this, new ChunkProcessedEventHandlerArgs(bytes.Length, Interlocked.Increment(ref _processedBlocks)));
                }, writerOptions);

                buffer.LinkTo(compressor);
                buffer.Completion.ContinueWith(task => compressor.Complete());

                compressor.LinkTo(writer);
                compressor.Completion.ContinueWith(task => writer.Complete());

                var readBuffer = new byte[BufferSize];

                while (true)
                {
                    if (_interrupted)
                    {
                        // Stop accepting new data chunks
                        buffer.Complete();
                        // Wait for writer to finish
                        writer.Completion.Wait();
                        return;
                    }

                    _pauseEvent.WaitOne(Timeout.Infinite);

                    int readCount = inputStream.Read(readBuffer, 0, BufferSize);

                    if (readCount > 0)
                    {
                        var postData = new byte[readCount];
                        Buffer.BlockCopy(readBuffer, 0, postData, 0, readCount);

                        while (!buffer.Post(postData))
                        {
                            // Wait until buffer can accept data
                        }
                    }

                    if (readCount != BufferSize)
                    {
                        buffer.Complete();
                        break;
                    }
                }

                writer.Completion.Wait();
            });
        }

        public void Pause()
        {
            _pauseEvent.Reset();
        }

        public void Resume()
        {
            _pauseEvent.Set();
        }

        internal void Interrupt()
        {
            _interrupted = true;
            Resume();
        }
    }
}
