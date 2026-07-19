using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace ClinicApp.Api.Logging;

[ProviderAlias("File")]
public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    private readonly LogLevel _minLevel;
    private readonly object _lock = new();

    public FileLoggerProvider(string filePath, LogLevel minLevel = LogLevel.Trace)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _minLevel = minLevel;

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(_filePath, _minLevel, categoryName, _lock);

    public void Dispose()
    {
    }
}

public class FileLogger : ILogger
{
    private readonly string _filePath;
    private readonly LogLevel _minLevel;
    private readonly string _categoryName;
    private readonly object _lock;

    public FileLogger(string filePath, LogLevel minLevel, string categoryName, object lockObject)
    {
        _filePath = filePath;
        _minLevel = minLevel;
        _categoryName = categoryName;
        _lock = lockObject;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel && logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var line = $"[{timestamp}] [{logLevel}] [{_categoryName}] {message}";

        if (exception is not null)
        {
            line += Environment.NewLine + exception;
        }

        lock (_lock)
        {
            File.AppendAllText(_filePath, line + Environment.NewLine);
        }
    }
}
