using System.IO;
using System.IO.Compression;
using Buser.AsyncCompression.Domain.Interfaces;

namespace Buser.AsyncCompression.Infrastructure.Algorithms
{
    public class BrotliCompressionAlgorithm : ICompressionAlgorithm
    {
        public string Name => "Brotli";
        public string FileExtension => ".br";

        public byte[] Compress(byte[] bytes)
        {
            using (var outStream = new MemoryStream())
            {
                using (var brotliStream = new BrotliStream(outStream, CompressionMode.Compress))
                using (var srcStream = new MemoryStream(bytes))
                {
                    srcStream.CopyTo(brotliStream);
                }

                return outStream.ToArray();
            }
        }
    }
}

