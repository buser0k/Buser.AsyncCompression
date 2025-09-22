using System.IO;
using System.Threading.Tasks;
using Booser.AsyncCompression.Domain.Interfaces;
using Booser.AsyncCompression.Domain.ValueObjects;

namespace Booser.AsyncCompression.Infrastructure.Services
{
    public class FileService : IFileService
    {
        public async Task<Stream> OpenReadAsync(Booser.AsyncCompression.Domain.ValueObjects.FileInfo file)
        {
            if (!file.Exists)
                throw new FileNotFoundException($"File not found: {file.FullPath}");

            return await Task.FromResult(System.IO.File.OpenRead(file.FullPath));
        }

        public async Task<Stream> CreateAsync(Booser.AsyncCompression.Domain.ValueObjects.FileInfo file)
        {
            return await Task.FromResult(System.IO.File.Create(file.FullPath));
        }

        public async Task<bool> ExistsAsync(Booser.AsyncCompression.Domain.ValueObjects.FileInfo file)
        {
            return await Task.FromResult(System.IO.File.Exists(file.FullPath));
        }

        public async Task DeleteAsync(Booser.AsyncCompression.Domain.ValueObjects.FileInfo file)
        {
            if (file.Exists)
            {
                System.IO.File.Delete(file.FullPath);
            }
            await Task.CompletedTask;
        }
    }
}
