using Reform.Interfaces;

namespace Reform.Logic
{
    internal sealed class DefaultConnectionStringProvider : IConnectionStringProvider
    {
        private readonly string _connectionString;

        public DefaultConnectionStringProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        public string GetConnectionString(string databaseName)
        {
            return _connectionString;
        }
    }
}
