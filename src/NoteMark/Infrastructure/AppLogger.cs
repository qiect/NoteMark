namespace NoteMark
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Threading;

    public sealed class AppLogger : IDisposable
    {
        private static readonly object InstanceLock = new object();
        private static AppLogger instance;

        private readonly ConcurrentQueue<string> logQueue;
        private readonly Timer flushTimer;
        private readonly object fileLock;
        private readonly string logDirectory;
        private bool disposed;

        private AppLogger()
        {
            logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NoteMark", "logs");

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            logQueue = new ConcurrentQueue<string>();
            fileLock = new object();
            flushTimer = new Timer(FlushCallback, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));

            CleanOldLogs();
        }

        public static AppLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (instance == null)
                        {
                            instance = new AppLogger();
                        }
                    }
                }

                return instance;
            }
        }

        public void Debug(string msg)
        {
            EnqueueLog("DEBUG", msg);
        }

        public void Info(string msg)
        {
            EnqueueLog("INFO", msg);
        }

        public void Warning(string msg)
        {
            EnqueueLog("WARN", msg);
        }

        public void Error(string msg)
        {
            EnqueueLog("ERROR", msg);
        }

        public void Error(string msg, Exception ex)
        {
            var fullMsg = string.Format("{0}\nException: {1}\nStackTrace: {2}",
                msg,
                ex != null ? ex.Message : "null",
                ex != null ? ex.StackTrace : "null");

            if (ex != null && ex.InnerException != null)
            {
                fullMsg += string.Format("\nInnerException: {0}\nInnerStackTrace: {1}",
                    ex.InnerException.Message,
                    ex.InnerException.StackTrace);
            }

            EnqueueLog("ERROR", fullMsg);
        }

        public void Flush()
        {
            FlushToFile();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            try
            {
                flushTimer.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Timer already disposed
            }

            FlushToFile();
        }

        private void EnqueueLog(string level, string msg)
        {
            if (disposed)
            {
                return;
            }

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var entry = string.Format("[{0}] [{1}] {2}", timestamp, level, msg);
            logQueue.Enqueue(entry);
        }

        private void FlushCallback(object state)
        {
            FlushToFile();
        }

        private void FlushToFile()
        {
            if (logQueue.IsEmpty)
            {
                return;
            }

            var logFileName = string.Format("onemark_{0}.log", DateTime.Now.ToString("yyyyMMdd"));
            var logFilePath = Path.Combine(logDirectory, logFileName);

            var entries = new System.Collections.Generic.List<string>();
            while (logQueue.TryDequeue(out string entry))
            {
                entries.Add(entry);
            }

            if (entries.Count == 0)
            {
                return;
            }

            lock (fileLock)
            {
                try
                {
                    File.AppendAllLines(logFilePath, entries);
                }
                catch (IOException)
                {
                    // File may be locked; re-enqueue entries
                    foreach (var e in entries)
                    {
                        logQueue.Enqueue(e);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // No permission; drop entries
                }
            }
        }

        private void CleanOldLogs()
        {
            try
            {
                if (!Directory.Exists(logDirectory))
                {
                    return;
                }

                var cutoff = DateTime.Now.AddDays(-30);
                var logFiles = Directory.GetFiles(logDirectory, "onemark_*.log");

                foreach (var file in logFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.LastWriteTime < cutoff)
                        {
                            File.Delete(file);
                        }
                    }
                    catch (IOException)
                    {
                        // Skip files that cannot be accessed
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Skip files that cannot be accessed
                    }
                }
            }
            catch (IOException)
            {
                // Directory may not be accessible
            }
            catch (UnauthorizedAccessException)
            {
                // No permission
            }
        }
    }
}
