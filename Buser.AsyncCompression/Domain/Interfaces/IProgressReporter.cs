namespace Buser.AsyncCompression.Domain.Interfaces
{
    /// <summary>
    /// Represents a service for reporting progress of long-running operations.
    /// </summary>
    public interface IProgressReporter
    {
        /// <summary>
        /// Reports the progress of an operation.
        /// </summary>
        /// <param name="progress">The progress value, typically between 0.0 and 1.0, where 1.0 represents completion.</param>
        void Report(double progress);
    }
}
