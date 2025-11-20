using System;
using System.Threading;
using Buser.AsyncCompression.Domain.ValueObjects;

namespace Buser.AsyncCompression.Domain.Entities
{
    public class CompressionJob : IDisposable
    {
        public Guid Id { get; }
        public Buser.AsyncCompression.Domain.ValueObjects.FileInfo InputFile { get; }
        public Buser.AsyncCompression.Domain.ValueObjects.FileInfo OutputFile { get; }
        public CompressionSettings Settings { get; }
        public CompressionStatus Status { get; private set; }
        public DateTime CreatedAt { get; }
        public DateTime? StartedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public long ProcessedBytes { get; private set; }
        public double ProgressPercentage => InputFile.Size > 0 ? (double)ProcessedBytes / InputFile.Size : 0;

        // Thread-safe state management for pause/resume/cancel
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ManualResetEventSlim _pauseEvent;
        private bool _disposed = false;

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;
        public ManualResetEventSlim PauseEvent => _pauseEvent;

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

        public void Start()
        {
            if (Status != CompressionStatus.Created)
                throw new InvalidOperationException($"Cannot start job in {Status} status");

            Status = CompressionStatus.Running;
            StartedAt = DateTime.UtcNow;
        }

        public void Pause()
        {
            if (Status != CompressionStatus.Running)
                throw new InvalidOperationException($"Cannot pause job in {Status} status");

            Status = CompressionStatus.Paused;
            _pauseEvent.Reset();
        }

        public void Resume()
        {
            if (Status != CompressionStatus.Paused)
                throw new InvalidOperationException($"Cannot resume job in {Status} status");

            Status = CompressionStatus.Running;
            _pauseEvent.Set();
        }

        public void Complete()
        {
            if (Status != CompressionStatus.Running)
                throw new InvalidOperationException($"Cannot complete job in {Status} status");

            Status = CompressionStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            if (Status == CompressionStatus.Completed)
                throw new InvalidOperationException("Cannot cancel completed job");

            Status = CompressionStatus.Cancelled;
            CompletedAt = DateTime.UtcNow;
            _cancellationTokenSource.Cancel();
            _pauseEvent.Set(); // Release any waiting threads
        }

        public void Fail()
        {
            if (Status == CompressionStatus.Completed)
                throw new InvalidOperationException("Cannot fail completed job");

            Status = CompressionStatus.Failed;
            CompletedAt = DateTime.UtcNow;
        }

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

    public enum CompressionStatus
    {
        Created,
        Running,
        Paused,
        Completed,
        Cancelled,
        Failed
    }
}
