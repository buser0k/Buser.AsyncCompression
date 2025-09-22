using Booser.AsyncCompression.Domain.Entities;
using Booser.AsyncCompression.Domain.ValueObjects;

namespace Booser.AsyncCompression.Application.Factories
{
    public class CompressionJobFactory
    {
        public CompressionJob CreateJob(string inputFilePath, CompressionSettings? settings = null)
        {
            var inputFile = new Booser.AsyncCompression.Domain.ValueObjects.FileInfo(inputFilePath);
            var outputFile = new Booser.AsyncCompression.Domain.ValueObjects.FileInfo(inputFilePath + ".gz");
            var compressionSettings = settings ?? CompressionSettings.Default;

            return new CompressionJob(inputFile, outputFile, compressionSettings);
        }
    }
}
