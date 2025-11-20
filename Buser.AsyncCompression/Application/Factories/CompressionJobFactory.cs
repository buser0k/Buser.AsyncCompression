using Buser.AsyncCompression.Domain.Entities;
using Buser.AsyncCompression.Domain.Interfaces;
using Buser.AsyncCompression.Domain.ValueObjects;

namespace Buser.AsyncCompression.Application.Factories
{
    public class CompressionJobFactory
    {
        private readonly ICompressionAlgorithm _defaultCompressionAlgorithm;
        private readonly CompressionAlgorithmFactory _algorithmFactory;

        public CompressionJobFactory(
            ICompressionAlgorithm defaultCompressionAlgorithm,
            CompressionAlgorithmFactory algorithmFactory)
        {
            _defaultCompressionAlgorithm = defaultCompressionAlgorithm ?? throw new System.ArgumentNullException(nameof(defaultCompressionAlgorithm));
            _algorithmFactory = algorithmFactory ?? throw new System.ArgumentNullException(nameof(algorithmFactory));
        }

        public CompressionJob CreateJob(string inputFilePath, CompressionSettings? settings = null, string? algorithmName = null)
        {
            var inputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(inputFilePath);
            
            // Use specified algorithm or default
            ICompressionAlgorithm algorithm = string.IsNullOrEmpty(algorithmName)
                ? _defaultCompressionAlgorithm
                : _algorithmFactory.CreateAlgorithm(algorithmName);
            
            var outputFilePath = inputFilePath + algorithm.FileExtension;
            var outputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(outputFilePath);
            var compressionSettings = settings ?? CompressionSettings.Default;

            return new CompressionJob(inputFile, outputFile, compressionSettings);
        }
    }
}
