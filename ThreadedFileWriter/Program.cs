
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ThreadedFileWriter
{
    internal static class Program
    {

        private static readonly string PreferredDirectory = @"C:\Code_Assignment";
        private static readonly string PreferredFilePath = Path.Combine(PreferredDirectory, "out.txt");
        private static readonly string FallbackFilePath = Path.Combine(Path.GetTempPath(), "out.txt");


        private static void Main(string[] args)
        {
            // Global exception handlers to demonstrate UI/event handler behavior
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Console.Error.WriteLine($"[FATAL] Unhandled exception: {e.ExceptionObject}");
            };
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Ctrl+C pressed. Exiting gracefully...");
                e.Cancel = false; // allow normal termination
            };

            string targetPath = PreferredFilePath;

            try
            {
                EnsureDirectoryExists(PreferredDirectory);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[WARN] Could not ensure directory '{PreferredDirectory}': {ex.Message}");
                Console.Error.WriteLine($"[INFO] Falling back to '{FallbackFilePath}'.");
                targetPath = FallbackFilePath;
                EnsureDirectoryExists(Path.GetDirectoryName(FallbackFilePath)!);
            }

            var writer = new SynchronizedFileWriter(targetPath);

            try
            {
                writer.Initialize(); // writes: 0, 0, <timestamp>
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] Initialization failed for '{targetPath}': {ex.Message}");
                ExitAfterKeypressIfInteractive();
                return;
            }

            const int threadCount = 10;
            const int writesPerThread = 10;
            var threads = new List<Thread>(capacity: threadCount);
            var errors = new List<Exception>();
            var errorLock = new object();

            Console.WriteLine($"Starting {threadCount} threads, each writing {writesPerThread} lines to: {targetPath}");

            for (int i = 0; i < threadCount; i++)
            {
                var worker = new Worker(
                    writer: writer,
                    writesPerThread: writesPerThread,
                    onError: (ex) =>
                    {
                        lock (errorLock)
                        {
                            errors.Add(ex);
                        }
                        Console.Error.WriteLine($"[THREAD ERROR] {ex.GetType().Name}: {ex.Message}");
                    });

                var thread = new Thread(worker.Run)
                {
                    IsBackground = true, // background threads; we explicitly Join below
                    Name = $"Writer-{i + 1}"
                };
                threads.Add(thread);
            }

            // Launch simultaneously
            foreach (var t in threads) t.Start();

            // Wait for all to finish
            foreach (var t in threads) t.Join();

            Console.WriteLine("All threads have terminated.");

            if (errors.Count > 0)
            {
                Console.Error.WriteLine($"[SUMMARY] Encountered {errors.Count} error(s). See above logs.");
            }

            Console.WriteLine("Press any key to exit...");
            ExitAfterKeypressIfInteractive();
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Directory path is null or whitespace.", nameof(path));

            Directory.CreateDirectory(path);
            Console.WriteLine($"Writing to: {path}");
        }

        private static void ExitAfterKeypressIfInteractive()
        {
            // Allow automated runs without interaction via env var AUTO_EXIT=true
            var autoExit = Environment.GetEnvironmentVariable("AUTO_EXIT");
            if (string.Equals(autoExit, "true", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                // Requires -it when running under Docker for TTY
                Console.ReadKey(intercept: true);
            }
            catch (InvalidOperationException)
            {
                // Input may be redirected; just exit gracefully.
            }
            catch (IOException)
            {
                // No TTY; exit gracefully.
            }
        }
    }
}