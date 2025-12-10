Developer Technical Notes:

Synchronization Strategy

In‑process mutual exclusion via SemaphoreSlim(1,1) ensures only one thread writes at a time.
The line counter is updated only after a successful write inside the critical section to prevent missing counts due to exceptions.
The file is opened with FileStream in Append mode and FileShare.Read to avoid concurrent writers. This pattern avoids OS-level contention while allowing readers when needed.

Exception & Error Handling

Initialization:

If /log cannot be prepared, we log the error and fallback to /tmp/out.txt. This demonstrates resilience; .


Background threads:

Each worker wraps its loop in try/catch and reports via a callback, so a single thread failure doesn’t crash the app.


UI / Unhandled exceptions:

AppDomain.CurrentDomain.UnhandledException is hooked to log fatal errors.
Console.CancelKeyPress allows graceful termination on Ctrl+C.
Keypress is robustly handled with fallbacks when STDIN isn’t interactive (e.g., CI runs).

Object‑Oriented Design

Clear responsibilities:

SynchronizedFileWriter (resource owner, synchronization, formatting & writing).
Worker (thread behavior, iteration count, error signaling).
Program (orchestration, global handlers, environment-specific behavior).

Performance Considerations

No sleeps or spin waits; threads enter the critical section as soon as they can.
Buffer size of StreamWriter is set to reasonable defaults; per‑write open/close keeps the code simple and robust for containerized environments without holding the file handle open across threads.

