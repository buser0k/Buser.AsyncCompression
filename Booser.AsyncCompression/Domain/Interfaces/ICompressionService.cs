using System.Threading.Tasks;
using Buser.AsyncCompression.Domain.Entities;

namespace Buser.AsyncCompression.Domain.Interfaces
{
    public interface ICompressionService
    {
        Task<CompressionJob> CompressAsync(CompressionJob job);
        void Pause(CompressionJob job);
        void Resume(CompressionJob job);
        void Cancel(CompressionJob job);
    }
}
