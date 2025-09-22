using System.IO;
using System.Threading.Tasks;
using Booser.AsyncCompression.Domain.ValueObjects;

namespace Booser.AsyncCompression.Domain.Interfaces
{
    public interface IFileService
    {
        Task<Stream> OpenReadAsync(Booser.AsyncCompression.Domain.ValueObjects.FileInfo file);
        Task<Stream> CreateAsync(Booser.AsyncCompression.Domain.ValueObjects.FileInfo file);
        Task<bool> ExistsAsync(Booser.AsyncCompression.Domain.ValueObjects.FileInfo file);
        Task DeleteAsync(Booser.AsyncCompression.Domain.ValueObjects.FileInfo file);
    }
}
