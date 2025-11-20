using System.Threading.Tasks;
using Buser.AsyncCompression.Domain.Entities;

namespace Buser.AsyncCompression.Domain.Interfaces
{
    /// <summary>
    /// Represents a service for compressing files asynchronously with support for pause, resume, and cancellation.
    /// </summary>
    public interface ICompressionService
    {
        /// <summary>
        /// Compresses a file asynchronously based on the provided compression job.
        /// </summary>
        /// <param name="job">The compression job containing file information and settings.</param>
        /// <returns>A task that represents the asynchronous compression operation. The task result contains the completed compression job.</returns>
        Task<CompressionJob> CompressAsync(CompressionJob job);
        
        /// <summary>
        /// Pauses the compression operation for the specified job.
        /// </summary>
        /// <param name="job">The compression job to pause.</param>
        void Pause(CompressionJob job);
        
        /// <summary>
        /// Resumes a paused compression operation for the specified job.
        /// </summary>
        /// <param name="job">The compression job to resume.</param>
        void Resume(CompressionJob job);
        
        /// <summary>
        /// Cancels the compression operation for the specified job.
        /// </summary>
        /// <param name="job">The compression job to cancel.</param>
        void Cancel(CompressionJob job);
    }
}
