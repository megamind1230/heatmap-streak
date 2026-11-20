using Microsoft.Extensions.Logging;

namespace HeatMapStreak.Web.Logging;

/// <summary>
/// Minimal file logger so diagnostics survive after the terminal is gone.
/// One file per day: <c>app-yyyy-MM-dd.log</c> in the configured log directory.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    /// <summary>Per-query EF Core logging; too chatty for a persistent log file.</summary>
    private const string NoisyCategory = "Microsoft.EntityFrameworkCore.Database.Command";

    private readonly string _directory;
    private readonly object _sync = new();

    public FileLoggerProvider(string directory)
    {
        _directory = directory;
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    public void Dispose()
    {
        // Nothing is held open: every line is appended and closed immediately.
    }

    private void Write(string line)
    {
        // Never let logging take the app down (missing directory, read-only disk, ...).
        try
        {
            lock (_sync)
            {
                Directory.CreateDirectory(_directory);
                var path = Path.Combine(_directory, $"app-{DateTime.UtcNow:yyyy-MM-dd}.log");
                File.AppendAllText(path, line);
            }
        }
        catch
        {
            // Swallow: a failed log write must not break a request.
        }
    }

    private sealed class FileLogger(FileLoggerProvider owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel) || category.StartsWith(NoisyCategory, StringComparison.Ordinal))
                return;

            owner.Write($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] [{category}] {formatter(state, exception)}{Environment.NewLine}{exception}");
        }
    }
}
