
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace ThreadedFileWriter
{
    /// <summary>
    /// Thread-safe writer for a single file. Ensures mutually exclusive access
    /// using SemaphoreSlim and writes lines in append mode.
    /// </summary>
    public sealed class SynchronizedFileWriter : IDisposable
    {
        private readonly string _filePath;
        private readonly SemaphoreSlim _gate = new(initialCount: 1, maxCount: 1);
        private int _lineCount = 0;
        private bool _initialized = false;
        private bool _disposed = false;

        public SynchronizedFileWriter(string filePath)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        }

        /// <summary>
        /// Initializes the file and writes the first line: "0, 0, <timestamp>".
        /// </summary>
        public void Initialize()
        {
            ThrowIfDisposed();

            // Exclusively initialize once
            _gate.Wait();
            try
            {
                if (_initialized) return;

                using var fs = new FileStream(
                    path: _filePath,
                    mode: FileMode.Create,          // start fresh per the requirement
                    access: FileAccess.Write,
                    share: FileShare.Read);          // readers allowed; writers serialized by semaphore

                using var sw = new StreamWriter(fs, Encoding.UTF8, bufferSize: 4096, leaveOpen: false);
                var ts = TimestampNow();
                sw.WriteLine($"0, 0, {ts}");
                sw.Flush();

                _lineCount = 0;
                _initialized = true;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// Appends one line with the next count: "&lt;count&gt;, &lt;thread_id&gt;, &lt;timestamp&gt;".
        /// The counter only advances on successful write to avoid gaps.
        /// </summary>
        public void AppendNext(int threadId)
        {
            ThrowIfDisposed();
            if (!_initialized)
                throw new InvalidOperationException("Writer not initialized. Call Initialize() first.");

            _gate.Wait();
            try
            {
                // Prepare line inside the critical section to avoid race on line count
                int nextCount = _lineCount + 1;

                using var fs = new FileStream(
                    path: _filePath,
                    mode: FileMode.Append,
                    access: FileAccess.Write,
                    share: FileShare.Read);

                using var sw = new StreamWriter(fs, Encoding.UTF8, bufferSize: 4096, leaveOpen: false);
                var ts = TimestampNow();
                sw.WriteLine($"{nextCount}, {threadId}, {ts}");
                sw.Flush();

                // Commit the count only after successful write
                _lineCount = nextCount;
            }
            finally
            {
                _gate.Release();
            }
        }

        private static string TimestampNow()
            => DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SynchronizedFileWriter));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _gate.Dispose();
            _disposed = true;
        }
    }
}
