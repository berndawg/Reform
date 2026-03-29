using System;
using System.Data;
using Reform.Interfaces;

namespace Reform.Logic
{
    internal sealed class ConnectionProvider<T> : IConnectionProvider<T> where T : class
    {
        private readonly IConnectionStringProvider _connectionStringProvider;
        private readonly IMetadataProvider<T> _metadataProvider;
        private readonly IDialect _dialect;

        internal ConnectionProvider(IConnectionStringProvider connectionStringProvider, IMetadataProvider<T> metadataProvider, IDialect dialect)
        {
            _connectionStringProvider = connectionStringProvider;
            _metadataProvider = metadataProvider;
            _dialect = dialect;
        }

        public IDbConnection GetConnection()
        {
            string connectionString = _connectionStringProvider.GetConnectionString(_metadataProvider.DatabaseName);

            try
            {
                var connection = _dialect.CreateConnection(connectionString);
                connection.Open();
                return connection;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to open database connection using connection string: {connectionString}", ex);
            }
        }
    }
}
