using System.Diagnostics;
using Reform.Interfaces;

namespace Reform.Objects;

public sealed class OperationTimer(IDebugLogger debugLogger) : IDisposable
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _stopwatch.Stop();
        debugLogger.WriteLine($"ELAPSED TIME: {_stopwatch.Elapsed}{Environment.NewLine}");
    }
}
