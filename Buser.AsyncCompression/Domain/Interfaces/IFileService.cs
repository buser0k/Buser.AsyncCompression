using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Buser.AsyncCompression.Domain.ValueObjects;

namespace Buser.AsyncCompression.Domain.Interfaces
{
    public interface IFileService
    {
        Task<Stream> OpenReadAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file, CancellationToken cancellationToken = default);
        Task<Stream> CreateAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file, CancellationToken cancellationToken = default);
        Task DeleteAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file, CancellationToken cancellationToken = default);
    }
}
