using Buser.AsyncCompression.Domain.Interfaces;

namespace Buser.AsyncCompression.Application.Factories
{
    public class CompressionAlgorithmFactory
    {
        public ICompressionAlgorithm CreateGZipAlgorithm()
        {
            return new Infrastructure.Algorithms.GZipCompressionAlgorithm();
        }

        public ICompressionAlgorithm CreateAlgorithm(string algorithmName)
        {
            if (string.IsNullOrEmpty(algorithmName))
                return CreateGZipAlgorithm();

            switch (algorithmName.ToLower())
            {
                case "gzip":
                    return CreateGZipAlgorithm();
                default:
                    return CreateGZipAlgorithm(); // Default to GZip
            }
        }
    }
}
