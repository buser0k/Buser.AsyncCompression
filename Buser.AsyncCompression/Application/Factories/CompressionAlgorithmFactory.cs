using Buser.AsyncCompression.Domain.Interfaces;

namespace Buser.AsyncCompression.Application.Factories
{
    /// <summary>
    /// Factory for creating compression algorithm instances.
    /// Supports multiple algorithms: GZip, Brotli, and can be extended for LZ4, Zstandard, etc.
    /// Used by CompressionService to determine the appropriate algorithm based on file extension.
    /// </summary>
    public class CompressionAlgorithmFactory
    {
        public ICompressionAlgorithm CreateGZipAlgorithm()
        {
            return new Infrastructure.Algorithms.GZipCompressionAlgorithm();
        }

        public ICompressionAlgorithm CreateBrotliAlgorithm()
        {
            return new Infrastructure.Algorithms.BrotliCompressionAlgorithm();
        }

        public ICompressionAlgorithm CreateAlgorithm(string algorithmName)
        {
            if (string.IsNullOrEmpty(algorithmName))
                return CreateGZipAlgorithm();

            return algorithmName.ToLower() switch
            {
                "gzip" => CreateGZipAlgorithm(),
                "brotli" or "br" => CreateBrotliAlgorithm(),
                _ => CreateGZipAlgorithm() // Default to GZip for unknown algorithms
            };
        }

        public ICompressionAlgorithm CreateDefaultAlgorithm()
        {
            return CreateGZipAlgorithm();
        }
    }
}
