using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Buser.AsyncCompression.Domain.Interfaces;
using Buser.AsyncCompression.Domain.ValueObjects;

namespace Buser.AsyncCompression.Infrastructure.Services
{
    public class FileService : IFileService
    {
        public async Task<Stream> OpenReadAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (!file.Exists)
                throw new FileNotFoundException($"File not found: {file.FullPath}");

            // Use FileStream with async operations for better performance
            var stream = new FileStream(
                file.FullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);
            
            return await Task.FromResult<Stream>(stream);
        }

        public async Task<Stream> CreateAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Use FileStream with async operations for better performance
            var stream = new FileStream(
                file.FullPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true);
            
            return await Task.FromResult<Stream>(stream);
        }

        public Task<bool> ExistsAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // File.Exists is a synchronous operation, but we can make it non-blocking
            return Task.FromResult(System.IO.File.Exists(file.FullPath));
        }

        public Task DeleteAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (file.Exists)
            {
                System.IO.File.Delete(file.FullPath);
            }
            return Task.CompletedTask;
        }
    }
}
