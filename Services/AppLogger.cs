using Serilog;
using Serilog.Events;

namespace BocceManager.Services;

/// <summary>
/// Thin wrapper so app code doesn't take a direct Serilog dependency everywhere.
/// </summary>
public static class AppLogger
{
    private static ILogger _log = Log.Logger;

    public static void Init(ILogger logger) => _log = logger;

    public static void Info(string message, params object[] args) =>
        _log.Information(message, args);

    public static void Warn(string message, params object[] args) =>
        _log.Warning(message, args);

    public static void Error(Exception ex, string message, params object[] args) =>
        _log.Error(ex, message, args);

    public static void Error(string message, params object[] args) =>
        _log.Error(message, args);

    public static void Debug(string message, params object[] args) =>
        _log.Debug(message, args);
}
