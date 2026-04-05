using System;
using System.Threading.Tasks;
using Reform;
using Reform.Interfaces;

namespace ReformIntegrationTests
{
    internal class Program
    {
        private const string DefaultConnectionString =
            "Server=.\\SQLEXPRESS;Database=ReformIntegrationTest;Trusted_Connection=True;TrustServerCertificate=True;";

        static async Task<int> Main(string[] args)
        {
            string connectionString = ResolveConnectionString(args);

            Console.WriteLine($"Connection: {connectionString}");
            Console.WriteLine();

            try
            {
                Console.WriteLine("Setting up database...");
                DatabaseSetup.EnsureDatabase(connectionString);
                DatabaseSetup.EnsureTables(connectionString);
                DatabaseSetup.CleanTables(connectionString);
                Console.WriteLine("Database ready.");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Database setup failed: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Ensure SQL Server Express is installed and running.");
                Console.WriteLine("You can specify a connection string with:");
                Console.WriteLine("  --connection-string \"Server=.\\SQLEXPRESS;Database=ReformIntegrationTest;Trusted_Connection=True;TrustServerCertificate=True;\"");
                Console.ResetColor();
                return 1;
            }

            using var reformer = new Reformer()
                .UseSqlServer(connectionString)
                .Register(typeof(IDebugLogger), typeof(ConsoleDebugLogger))
                .Build();

            var runner = new TestRunner();

            Console.WriteLine("Sync Tests");
            Console.WriteLine(new string('-', 40));
            var syncTests = new SqlServerTests(reformer, connectionString);
            syncTests.RunAll(runner);

            Console.WriteLine();
            Console.WriteLine("Async Tests");
            Console.WriteLine(new string('-', 40));
            var asyncTests = new SqlServerAsyncTests(reformer, connectionString);
            await asyncTests.RunAll(runner);

            return runner.PrintSummary();
        }

        private static string ResolveConnectionString(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--connection-string")
                    return args[i + 1];
            }

            string envValue = Environment.GetEnvironmentVariable("REFORM_TEST_CONNECTION_STRING");
            if (!string.IsNullOrEmpty(envValue))
                return envValue;

            return DefaultConnectionString;
        }
    }

    internal class ConsoleDebugLogger : IDebugLogger
    {
        public void WriteLine(string stringValue)
        {
            Console.WriteLine(stringValue);
        }
    }
}
