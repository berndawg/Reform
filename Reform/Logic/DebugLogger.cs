using System;
using System.Diagnostics;
using Reform.Interfaces;

namespace Reform.Logic
{
    public class DebugLogger : IDebugLogger
    {
        public void WriteLine(string stringValue)
        {
            Debug.WriteLine(stringValue);
        }
    }
}
