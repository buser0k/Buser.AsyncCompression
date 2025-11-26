using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buser.AsyncCompression.Domain.Interfaces;

namespace Buser.AsyncCompression
{
    /// <summary>
    /// A console progress bar that displays compression progress with animation.
    /// </summary>
    public class ProgressBar : IDisposable, IProgress<double>, IProgressReporter
    {
        private const int BlockCount = 10;
        private readonly TimeSpan _animationInterval = TimeSpan.FromSeconds(1.0 / 8);
        private const string Animation = @"|/-\";

        private PeriodicTimer? _timer;
        private Task? _timerTask;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private double _currentProgress;
        private string _currentText = string.Empty;
        private bool _disposed;
        private int _animationIndex;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Initializes a new instance of the ProgressBar class.
        /// </summary>
        public ProgressBar()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            // A progress bar is only for temporary display in a console window.
            // If the console output is redirected to a file, draw nothing.
            // Otherwise, we'll end up with a lot of garbage in the target file.
            if (!Console.IsOutputRedirected)
            {
                StartTimer();
            }
        }

        /// <summary>
        /// Reports the progress value (0.0 to 1.0).
        /// </summary>
        /// <param name="value">The progress value between 0.0 and 1.0.</param>
        public void Report(double value)
        {
            // Make sure value is in [0..1] range
            value = Math.Max(0, Math.Min(1, value));
            Interlocked.Exchange(ref _currentProgress, value);

            // Для очень быстрых операций (например, небольшие директории,
            // упаковываемые в единый архив) таймер может не успеть отрисовать
            // прогресс ни разу. Поэтому дополнительно обновляем отображение
            // синхронно при каждом Report, если вывод не перенаправлен.
            if (!Console.IsOutputRedirected)
            {
                lock (_lockObject)
                {
                    if (_disposed) return;

                    Render();
                }
            }
        }

        private void Render()
        {
            int progressBlockCount = (int)(_currentProgress * BlockCount);
            int percent = (int)(_currentProgress * 100);
            string text = string.Format("[{0}{1}] {2,3}% {3}",
                new string('#', progressBlockCount), new string('-', BlockCount - progressBlockCount),
                percent,
                Animation[_animationIndex++ % Animation.Length]);
            UpdateText(text);
        }

        private void StartTimer()
        {
            _timer = new PeriodicTimer(_animationInterval);
            _timerTask = Task.Run(async () =>
            {
                try
                {
                    while (await _timer.WaitForNextTickAsync(_cancellationTokenSource.Token))
                    {
                        lock (_lockObject)
                        {
                            if (_disposed) return;

                            Render();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when disposed
                }
            });
        }

        private void UpdateText(string text)
        {
            // Get length of common portion
            int commonPrefixLength = 0;
            int commonLength = Math.Min(_currentText.Length, text.Length);
            while (commonPrefixLength < commonLength && text[commonPrefixLength] == _currentText[commonPrefixLength])
            {
                commonPrefixLength++;
            }

            // Backtrack to the first differing character
            var outputBuilder = new StringBuilder();
            outputBuilder.Append('\b', _currentText.Length - commonPrefixLength);

            // Output new suffix
            outputBuilder.Append(text.Substring(commonPrefixLength));

            // If the new text is shorter than the old one: delete overlapping characters
            int overlapCount = _currentText.Length - text.Length;
            if (overlapCount > 0)
            {
                outputBuilder.Append(' ', overlapCount);
                outputBuilder.Append('\b', overlapCount);
            }

            Console.Write(outputBuilder);
            _currentText = text;
        }

        /// <summary>
        /// Releases all resources used by the ProgressBar.
        /// </summary>
        public void Dispose()
        {
            lock (_lockObject)
            {
                if (_disposed) return;
                _disposed = true;
                UpdateText(string.Empty);
            }

            _cancellationTokenSource.Cancel();
            _timer?.Dispose();
            
            try
            {
                _timerTask?.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            
            _cancellationTokenSource.Dispose();
        }
    }
}