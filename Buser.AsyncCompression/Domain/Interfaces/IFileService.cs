using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Buser.AsyncCompression.Domain.ValueObjects;

namespace Buser.AsyncCompression.Domain.Interfaces
{
    /// <summary>
    /// Represents a service for file system operations with asynchronous support.
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Opens a file for reading asynchronously.
        /// </summary>
        /// <param name="file">The file information.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a readable stream.</returns>
        Task<Stream> OpenReadAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Creates a new file for writing asynchronously.
        /// </summary>
        /// <param name="file">The file information.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a writable stream.</returns>
        Task<Stream> CreateAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Checks if a file exists asynchronously.
        /// </summary>
        /// <param name="file">The file information.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is true if the file exists; otherwise, false.</returns>
        Task<bool> ExistsAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes a file asynchronously if it exists.
        /// </summary>
        /// <param name="file">The file information.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync(Buser.AsyncCompression.Domain.ValueObjects.FileInfo file, CancellationToken cancellationToken = default);
    }
}
