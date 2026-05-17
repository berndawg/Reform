using System.Diagnostics;

namespace ReformIntegrationTests
{
    internal class TestResult
    {
        public string Name { get; set; }
        public bool Passed { get; set; }
        public string Error { get; set; }
        public TimeSpan Elapsed { get; set; }
    }

    internal class TestRunner
    {
        private readonly List<TestResult> _results = new();

        public void Run(string name, Action action)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                action();
                sw.Stop();
                _results.Add(new TestResult { Name = name, Passed = true, Elapsed = sw.Elapsed });
                WriteResult(name, true, sw.Elapsed, null);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _results.Add(new TestResult { Name = name, Passed = false, Error = ex.Message, Elapsed = sw.Elapsed });
                WriteResult(name, false, sw.Elapsed, ex.Message);
            }
        }

        public async Task RunAsync(string name, Func<Task> action)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await action();
                sw.Stop();
                _results.Add(new TestResult { Name = name, Passed = true, Elapsed = sw.Elapsed });
                WriteResult(name, true, sw.Elapsed, null);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _results.Add(new TestResult { Name = name, Passed = false, Error = ex.Message, Elapsed = sw.Elapsed });
                WriteResult(name, false, sw.Elapsed, ex.Message);
            }
        }

        public int PrintSummary()
        {
            int passed = 0, failed = 0;
            foreach (var r in _results)
            {
                if (r.Passed) passed++;
                else failed++;
            }

            Console.WriteLine();
            Console.ForegroundColor = failed == 0 ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"{passed} passed, {failed} failed out of {_results.Count} total");
            Console.ResetColor();

            return failed == 0 ? 0 : 1;
        }

        private static void WriteResult(string name, bool passed, TimeSpan elapsed, string? error)
        {
            Console.ForegroundColor = passed ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write(passed ? "  [PASS] " : "  [FAIL] ");
            Console.ResetColor();
            Console.Write($"{name} ({elapsed.TotalMilliseconds:F0}ms)");
            if (!passed)
                Console.Write($" - {error}");
            Console.WriteLine();
        }
    }
}
