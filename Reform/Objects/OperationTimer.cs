using Reform.Interfaces;
using System;
using System.Diagnostics;

namespace Reform.Objects
{
    public class OperationTimer : IDisposable
    {
        private readonly Stopwatch _stopwatch;
        private readonly IDebugLogger _debugLogger;

        public OperationTimer(IDebugLogger debugLogger)
        {
            _debugLogger = debugLogger;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _debugLogger.WriteLine($"ELAPSED TIME: {_stopwatch.Elapsed}{Environment.NewLine}");
        }
    }
}
