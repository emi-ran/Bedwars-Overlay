using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BedwarsOverlay.Core.LogWatching
{
    public class LogTailer
    {
        private readonly string _filePath;
        private CancellationTokenSource? _cts;
        private Task? _tailTask;

        /// <summary>
        /// Triggered when a new line is successfully read from the log.
        /// </summary>
        public event Action<string>? LineAppended;

        /// <summary>
        /// Triggered when an error occurs during log reading.
        /// </summary>
        public event Action<string>? ErrorEncountered;

        /// <summary>
        /// True if the log tailer background task is currently active.
        /// </summary>
        public bool IsRunning => _tailTask != null && !_tailTask.IsCompleted;

        public LogTailer(string filePath)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        }

        /// <summary>
        /// Starts watching the log file asynchronously.
        /// </summary>
        public void Start()
        {
            if (IsRunning) return;

            _cts = new CancellationTokenSource();
            _tailTask = Task.Run(() => TailFileAsync(_filePath, _cts.Token));
        }

        /// <summary>
        /// Stops the background watch task.
        /// </summary>
        public void Stop()
        {
            if (!IsRunning) return;

            _cts?.Cancel();
            try
            {
                // Wait for the background task to wind down
                _tailTask?.Wait(1000);
            }
            catch (AggregateException)
            {
                // Task cancellation exceptions are expected
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
                _tailTask = null;
            }
        }

        private async Task TailFileAsync(string path, CancellationToken token)
        {
            long lastMaxOffset = 0;

            // Wait for file to be created if it doesn't exist yet
            while (!File.Exists(path))
            {
                if (token.IsCancellationRequested) return;
                await Task.Delay(1000, token);
            }

            // Seek to the end of the file on startup so we only capture new entries
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    lastMaxOffset = fs.Length;
                }
            }
            catch (Exception ex)
            {
                ErrorEncountered?.Invoke($"Failed to initialize log file offset: {ex.Message}");
            }

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!File.Exists(path))
                    {
                        await Task.Delay(1000, token);
                        continue;
                    }

                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        // Handle log rotation or clearing (if file size is shrunk, start from beginning)
                        if (fs.Length < lastMaxOffset)
                        {
                            lastMaxOffset = 0;
                        }

                        if (fs.Length > lastMaxOffset)
                        {
                            fs.Seek(lastMaxOffset, SeekOrigin.Begin);
                            using (var reader = new StreamReader(fs, Encoding.Latin1))
                            {
                                string? line;
                                while ((line = await reader.ReadLineAsync(token)) != null)
                                {
                                    LineAppended?.Invoke(line);
                                }
                                lastMaxOffset = fs.Position;
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ErrorEncountered?.Invoke($"Error reading log file: {ex.Message}");
                }

                // Wait 100ms before checking for new lines again
                await Task.Delay(100, token);
            }
        }
    }
}
