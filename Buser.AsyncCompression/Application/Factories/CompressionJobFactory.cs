using Buser.AsyncCompression.Domain.Entities;
using Buser.AsyncCompression.Domain.Interfaces;
using Buser.AsyncCompression.Domain.ValueObjects;

namespace Buser.AsyncCompression.Application.Factories
{
    public class CompressionJobFactory
    {
        private readonly ICompressionAlgorithm _compressionAlgorithm;

        public CompressionJobFactory(ICompressionAlgorithm compressionAlgorithm)
        {
            _compressionAlgorithm = compressionAlgorithm ?? throw new System.ArgumentNullException(nameof(compressionAlgorithm));
        }

        public CompressionJob CreateJob(string inputFilePath, CompressionSettings? settings = null)
        {
            var inputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(inputFilePath);
            var outputFilePath = inputFilePath + _compressionAlgorithm.FileExtension;
            var outputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(outputFilePath);
            var compressionSettings = settings ?? CompressionSettings.Default;

            return new CompressionJob(inputFile, outputFile, compressionSettings);
        }
    }
}
