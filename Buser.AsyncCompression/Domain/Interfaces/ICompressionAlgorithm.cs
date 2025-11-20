namespace Buser.AsyncCompression.Domain.Interfaces
{
    /// <summary>
    /// Represents a compression algorithm that can compress byte arrays.
    /// </summary>
    public interface ICompressionAlgorithm
    {
        /// <summary>
        /// Gets the name of the compression algorithm (e.g., "GZip", "Brotli").
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Gets the file extension associated with this compression algorithm (e.g., ".gz", ".br").
        /// </summary>
        string FileExtension { get; }
        
        /// <summary>
        /// Compresses the specified byte array.
        /// </summary>
        /// <param name="bytes">The byte array to compress.</param>
        /// <returns>The compressed byte array.</returns>
        byte[] Compress(byte[] bytes);
    }
}
