using System.Diagnostics;
using Reform.Interfaces;

namespace Reform.Objects;

public class OperationTimer(IDebugLogger debugLogger) : IDisposable
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public void Dispose()
    {
        _stopwatch.Stop();
        debugLogger.WriteLine($"ELAPSED TIME: {_stopwatch.Elapsed}{Environment.NewLine}");
    }
}
