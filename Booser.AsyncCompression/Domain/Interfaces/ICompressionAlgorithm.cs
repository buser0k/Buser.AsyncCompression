namespace Buser.AsyncCompression.Domain.Interfaces
{
    public interface ICompressionAlgorithm
    {
        string Name { get; }
        string FileExtension { get; }
        byte[] Compress(byte[] bytes);
    }
}
