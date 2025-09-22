using System;

namespace Booser.AsyncCompression.Domain.ValueObjects
{
    public class CompressionSettings
    {
        public int BufferSize { get; }
        public int MaxBufferSize { get; }
        public int BoundedCapacity { get; }
        public int MaxDegreeOfParallelism { get; }

        public CompressionSettings(int bufferSize = 8192, int maxBufferSize = 4194304, 
            int boundedCapacity = 100, int? maxDegreeOfParallelism = null)
        {
            if (bufferSize <= 0)
                throw new ArgumentException("Buffer size must be positive", nameof(bufferSize));
            
            if (maxBufferSize <= 0)
                throw new ArgumentException("Max buffer size must be positive", nameof(maxBufferSize));

            BufferSize = Math.Min(bufferSize, maxBufferSize);
            MaxBufferSize = maxBufferSize;
            BoundedCapacity = boundedCapacity;
            MaxDegreeOfParallelism = maxDegreeOfParallelism ?? Environment.ProcessorCount;
        }

        public static CompressionSettings Default => new CompressionSettings();
    }
}
