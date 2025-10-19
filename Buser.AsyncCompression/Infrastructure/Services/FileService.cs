using System.IO;
using System.Threading.Tasks;
using Buser.AsyncCompression.Domain.Interfaces;
using Buser.AsyncCompression.Domain.ValueObjects;

namespace Buser.AsyncCompression.Infrastructure.Services
{
    public class FileService : IFileService
    {
        public async Task<Stream> OpenReadAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file)
        {
            if (!file.Exists)
                throw new FileNotFoundException($"File not found: {file.FullPath}");

            return await Task.FromResult(System.IO.File.OpenRead(file.FullPath));
        }

        public async Task<Stream> CreateAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file)
        {
            return await Task.FromResult(System.IO.File.Create(file.FullPath));
        }

        public async Task<bool> ExistsAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file)
        {
            return await Task.FromResult(System.IO.File.Exists(file.FullPath));
        }

        public async Task DeleteAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file)
        {
            if (file.Exists)
            {
                System.IO.File.Delete(file.FullPath);
            }
            await Task.CompletedTask;
        }
    }
}
