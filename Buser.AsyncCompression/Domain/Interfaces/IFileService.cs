using System.IO;
using System.Threading.Tasks;
using Buser.AsyncCompression.Domain.ValueObjects;

namespace Buser.AsyncCompression.Domain.Interfaces
{
    public interface IFileService
    {
        Task<Stream> OpenReadAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file);
        Task<Stream> CreateAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file);
        Task<bool> ExistsAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file);
        Task DeleteAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file);
    }
}
