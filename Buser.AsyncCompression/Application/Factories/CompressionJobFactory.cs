using Buser.AsyncCompression.Domain.Entities;
using Buser.AsyncCompression.Domain.ValueObjects;

namespace Buser.AsyncCompression.Application.Factories
{
    public class CompressionJobFactory
    {
        public CompressionJob CreateJob(string inputFilePath, CompressionSettings? settings = null)
        {
            var inputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(inputFilePath);
            var outputFile = new Buser.AsyncCompression.Domain.ValueObjects.FileInfo(inputFilePath + ".gz");
            var compressionSettings = settings ?? CompressionSettings.Default;

            return new CompressionJob(inputFile, outputFile, compressionSettings);
        }
    }
}
