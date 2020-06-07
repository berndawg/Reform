using Reform.Interfaces;
using System;
using System.Diagnostics;

namespace Reform.Objects
{
    public class OperationTimer : IDisposable
    {
        private readonly DateTime _startDateTime;
        private readonly IDebugLogger _debugLogger;

        public OperationTimer(IDebugLogger debugLogger)
        {
            _debugLogger = debugLogger;
            _startDateTime = DateTime.Now;
        }

        public void Dispose()
        {
            DateTime endDateTime = DateTime.Now;

            TimeSpan timespan = endDateTime - _startDateTime;

            _debugLogger.WriteLine($"ELLAPASED TIME: {timespan}{Environment.NewLine}");
        }
    }
}
