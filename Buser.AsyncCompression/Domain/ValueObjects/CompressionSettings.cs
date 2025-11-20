using System;

namespace Buser.AsyncCompression.Domain.ValueObjects
{
    /// <summary>
    /// Represents the settings for a compression operation.
    /// This is a value object that encapsulates compression configuration.
    /// </summary>
    public class CompressionSettings
    {
        /// <summary>
        /// Gets the buffer size in bytes for reading and writing data. Default is 8192 (8 KB).
        /// </summary>
        public int BufferSize { get; }
        
        /// <summary>
        /// Gets the maximum buffer size in bytes. Default is 4194304 (4 MB).
        /// </summary>
        public int MaxBufferSize { get; }
        
        /// <summary>
        /// Gets the bounded capacity for the dataflow pipeline. Default is 100.
        /// </summary>
        public int BoundedCapacity { get; }
        
        /// <summary>
        /// Gets the maximum degree of parallelism for compression operations. Default is the number of processor cores.
        /// </summary>
        public int MaxDegreeOfParallelism { get; }

        /// <summary>
        /// Initializes a new instance of the CompressionSettings class.
        /// </summary>
        /// <param name="bufferSize">The buffer size in bytes. Default is 8192 (8 KB).</param>
        /// <param name="maxBufferSize">The maximum buffer size in bytes. Default is 4194304 (4 MB).</param>
        /// <param name="boundedCapacity">The bounded capacity for the dataflow pipeline. Default is 100.</param>
        /// <param name="maxDegreeOfParallelism">The maximum degree of parallelism. If null, uses the number of processor cores.</param>
        /// <exception cref="ArgumentException">Thrown when bufferSize or maxBufferSize is not positive.</exception>
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

        /// <summary>
        /// Gets the default compression settings with recommended values.
        /// </summary>
        public static CompressionSettings Default => new CompressionSettings();
    }
}
