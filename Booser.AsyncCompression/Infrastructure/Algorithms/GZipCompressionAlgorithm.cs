using System.IO;
using System.IO.Compression;
using Buser.AsyncCompression.Domain.Interfaces;

namespace Buser.AsyncCompression.Infrastructure.Algorithms
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
