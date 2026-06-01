using System.Collections.Concurrent;

namespace NoteMark.AddIn;

public sealed class AppLogger
{
    private static readonly Lazy<AppLogger> _instance = new(() => new AppLogger());
    public static AppLogger Instance => _instance.Value;

    private readonly string _logDirectory;
    private readonly string _logFilePath;
    private readonly ConcurrentQueue<string> _queue = new();
    private readonly System.Threading.Timer _flushTimer;
    private readonly int _maxLogAgeDays = 30;
    private bool _disposed;

    private AppLogger()
    {
        _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NoteMark", "logs");
        Directory.CreateDirectory(_logDirectory);

        var date = DateTime.Now.ToString("yyyyMMdd");
        _logFilePath = Path.Combine(_logDirectory, $"onemark_{date}.log");

        _flushTimer = new System.Threading.Timer(_ => Flush(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));

        CleanOldLogs();
    }

    public void LogInfo(string message)
    {
        Enqueue("INFO", message);
    }

    public void LogWarning(string message)
    {
        Enqueue("WARN", message);
    }

    public void LogError(string message, Exception? ex = null)
    {
        var entry = ex is not null
            ? $"{message} | Exception: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}"
            : message;
        Enqueue("ERROR", entry);
    }

    private void Enqueue(string level, string message)
    {
        if (_disposed) return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        var line = $"[{timestamp}] [{level}] [T{threadId}] {message}";
        _queue.Enqueue(line);
    }

    private void Flush()
    {
        if (_disposed) return;

        var sb = new System.Text.StringBuilder();
        while (_queue.TryDequeue(out var line))
        {
            sb.AppendLine(line);
        }

        if (sb.Length > 0)
        {
            try
            {
                File.AppendAllText(_logFilePath, sb.ToString());
            }
            catch
            {
            }
        }
    }

    private void CleanOldLogs()
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-_maxLogAgeDays);
            foreach (var file in Directory.EnumerateFiles(_logDirectory, "onemark_*.log"))
            {
                if (File.GetLastWriteTimeUtc(file) < cutoff)
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _flushTimer.Dispose();
        Flush();
    }
}
