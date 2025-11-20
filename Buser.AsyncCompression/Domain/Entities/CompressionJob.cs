using System;
using System.Threading;
using Buser.AsyncCompression.Domain.ValueObjects;

namespace Buser.AsyncCompression.Domain.Entities
{
    /// <summary>
    /// Represents a compression job that tracks the state and progress of a file compression operation.
    /// This is a domain entity that encapsulates all information needed for a compression task.
    /// </summary>
    public class CompressionJob : IDisposable
    {
        /// <summary>
        /// Gets the unique identifier of the compression job.
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Gets the input file information.
        /// </summary>
        public Buser.AsyncCompression.Domain.ValueObjects.FileInfo InputFile { get; }
        
        /// <summary>
        /// Gets the output file information.
        /// </summary>
        public Buser.AsyncCompression.Domain.ValueObjects.FileInfo OutputFile { get; }
        
        /// <summary>
        /// Gets the compression settings for this job.
        /// </summary>
        public CompressionSettings Settings { get; }
        
        /// <summary>
        /// Gets the current status of the compression job.
        /// </summary>
        public CompressionStatus Status { get; private set; }
        
        /// <summary>
        /// Gets the date and time when the job was created.
        /// </summary>
        public DateTime CreatedAt { get; }
        
        /// <summary>
        /// Gets the date and time when the job was started, or null if not started yet.
        /// </summary>
        public DateTime? StartedAt { get; private set; }
        
        /// <summary>
        /// Gets the date and time when the job was completed, cancelled, or failed, or null if still in progress.
        /// </summary>
        public DateTime? CompletedAt { get; private set; }
        
        /// <summary>
        /// Gets the number of bytes processed so far.
        /// </summary>
        public long ProcessedBytes { get; private set; }
        
        /// <summary>
        /// Gets the progress percentage (0.0 to 1.0) of the compression operation.
        /// </summary>
        public double ProgressPercentage => InputFile.Size > 0 ? (double)ProcessedBytes / InputFile.Size : 0;

        // Thread-safe state management for pause/resume/cancel
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ManualResetEventSlim _pauseEvent;
        private bool _disposed = false;

        /// <summary>
        /// Gets the cancellation token for this job. Used to cancel the compression operation.
        /// </summary>
        public CancellationToken CancellationToken => _cancellationTokenSource.Token;
        
        /// <summary>
        /// Gets the pause event for this job. Used to pause and resume the compression operation.
        /// </summary>
        public ManualResetEventSlim PauseEvent => _pauseEvent;

        /// <summary>
        /// Initializes a new instance of the CompressionJob class.
        /// </summary>
        /// <param name="inputFile">The input file to compress.</param>
        /// <param name="outputFile">The output file where the compressed data will be written.</param>
        /// <param name="settings">The compression settings to use.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public CompressionJob(Buser.AsyncCompression.Domain.ValueObjects.FileInfo inputFile, Buser.AsyncCompression.Domain.ValueObjects.FileInfo outputFile, CompressionSettings settings)
        {
            Id = Guid.NewGuid();
            InputFile = inputFile ?? throw new ArgumentNullException(nameof(inputFile));
            OutputFile = outputFile ?? throw new ArgumentNullException(nameof(outputFile));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Status = CompressionStatus.Created;
            CreatedAt = DateTime.UtcNow;
            _cancellationTokenSource = new CancellationTokenSource();
            _pauseEvent = new ManualResetEventSlim(true); // Initially not paused
        }

        /// <summary>
        /// Starts the compression job. The job must be in Created status.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the job is not in Created status.</exception>
        public void Start()
        {
            if (Status != CompressionStatus.Created)
                throw new InvalidOperationException($"Cannot start job in {Status} status");

            Status = CompressionStatus.Running;
            StartedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Pauses the compression job. The job must be in Running status.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the job is not in Running status.</exception>
        public void Pause()
        {
            if (Status != CompressionStatus.Running)
                throw new InvalidOperationException($"Cannot pause job in {Status} status");

            Status = CompressionStatus.Paused;
            _pauseEvent.Reset();
        }

        /// <summary>
        /// Resumes a paused compression job. The job must be in Paused status.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the job is not in Paused status.</exception>
        public void Resume()
        {
            if (Status != CompressionStatus.Paused)
                throw new InvalidOperationException($"Cannot resume job in {Status} status");

            Status = CompressionStatus.Running;
            _pauseEvent.Set();
        }

        /// <summary>
        /// Marks the compression job as completed. The job must be in Running status.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the job is not in Running status.</exception>
        public void Complete()
        {
            if (Status != CompressionStatus.Running)
                throw new InvalidOperationException($"Cannot complete job in {Status} status");

            Status = CompressionStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Cancels the compression job. Cannot be called on a completed job.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the job is already completed.</exception>
        public void Cancel()
        {
            if (Status == CompressionStatus.Completed)
                throw new InvalidOperationException("Cannot cancel completed job");

            Status = CompressionStatus.Cancelled;
            CompletedAt = DateTime.UtcNow;
            _cancellationTokenSource.Cancel();
            _pauseEvent.Set(); // Release any waiting threads
        }

        /// <summary>
        /// Marks the compression job as failed. Cannot be called on a completed job.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the job is already completed.</exception>
        public void Fail()
        {
            if (Status == CompressionStatus.Completed)
                throw new InvalidOperationException("Cannot fail completed job");

            Status = CompressionStatus.Failed;
            CompletedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the progress of the compression job with the number of bytes processed.
        /// </summary>
        /// <param name="processedBytes">The number of bytes processed so far.</param>
        public void UpdateProgress(long processedBytes)
        {
            if (Status != CompressionStatus.Running)
                return;

            ProcessedBytes = Math.Min(processedBytes, InputFile.Size);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cancellationTokenSource?.Dispose();
                _pauseEvent?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Represents the status of a compression job.
    /// </summary>
    public enum CompressionStatus
    {
        /// <summary>
        /// The job has been created but not started yet.
        /// </summary>
        Created,
        
        /// <summary>
        /// The job is currently running.
        /// </summary>
        Running,
        
        /// <summary>
        /// The job is paused and can be resumed.
        /// </summary>
        Paused,
        
        /// <summary>
        /// The job has completed successfully.
        /// </summary>
        Completed,
        
        /// <summary>
        /// The job was cancelled.
        /// </summary>
        Cancelled,
        
        /// <summary>
        /// The job failed due to an error.
        /// </summary>
        Failed
    }
}
