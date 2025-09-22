using System.IO;
using System.IO.Compression;
using Booser.AsyncCompression.Domain.Interfaces;

namespace Booser.AsyncCompression.Infrastructure.Algorithms
{
    public class GZipCompressionAlgorithm : ICompressionAlgorithm
    {
        public string Name => "GZip";
        public string FileExtension => ".gz";

        public byte[] Compress(byte[] bytes)
        {
            using (var outStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(outStream, CompressionMode.Compress))
                using (var srcStream = new MemoryStream(bytes))
                {
                    srcStream.CopyTo(gzipStream);
                }

                return outStream.ToArray();
            }
        }
    }
}
