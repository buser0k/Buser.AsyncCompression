using System.Threading.Tasks;
using Booser.AsyncCompression.Domain.Entities;

namespace Booser.AsyncCompression.Domain.Interfaces
{
    public interface ICompressionService
    {
        Task<CompressionJob> CompressAsync(CompressionJob job);
        void Pause(CompressionJob job);
        void Resume(CompressionJob job);
        void Cancel(CompressionJob job);
    }
}
