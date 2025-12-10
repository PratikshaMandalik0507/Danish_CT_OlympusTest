
using System;
using System.Threading;

namespace ThreadedFileWriter
{
    /// <summary>
    /// Represents a background worker that appends N lines to the shared file.
    /// </summary>
    public sealed class Worker
    {
        private readonly SynchronizedFileWriter _writer;
        private readonly int _writesPerThread;
        private readonly Action<Exception>? _onError;

        public Worker(SynchronizedFileWriter writer, int writesPerThread, Action<Exception>? onError = null)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _writesPerThread = writesPerThread > 0 ? writesPerThread : throw new ArgumentOutOfRangeException(nameof(writesPerThread));
            _onError = onError;
        }

        public void Run()
        {
            try
            {
                for (int i = 0; i < _writesPerThread; i++)
                {
                    // No sleeps/spin waits; write as fast as possible with synchronization inside the writer
                    int threadId = Environment.CurrentManagedThreadId;
                    _writer.AppendNext(threadId);
                }
            }
            catch (Exception ex)
            {
                _onError?.Invoke(ex);
            }
        }
    }
}
