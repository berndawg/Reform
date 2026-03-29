using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Reform.Interfaces;

namespace Reform.Logic
{
    public sealed class ConnectionProvider<T> : IConnectionProvider<T> where T : class
    {
        private readonly IConnectionStringProvider _connectionStringProvider;
        private readonly IMetadataProvider<T> _metadataProvider;
        private readonly IDialect _dialect;

        public ConnectionProvider(IConnectionStringProvider connectionStringProvider, IMetadataProvider<T> metadataProvider, IDialect dialect)
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
                throw new ApplicationException($"Failed to open database connection for '{_metadataProvider.DatabaseName}'", ex);
            }
        }

        public async Task<DbConnection> GetConnectionAsync()
        {
            string connectionString = _connectionStringProvider.GetConnectionString(_metadataProvider.DatabaseName);

            try
            {
                var connection = (DbConnection)_dialect.CreateConnection(connectionString);
                await connection.OpenAsync();
                return connection;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to open database connection for '{_metadataProvider.DatabaseName}'", ex);
            }
        }
    }
}
