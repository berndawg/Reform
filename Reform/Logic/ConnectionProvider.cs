using System.Data;
using System.Data.Common;
using Reform.Interfaces;

namespace Reform.Logic
{
    public sealed class ConnectionProvider<T>(
        IMetadataProvider<T> metadataProvider,
        IDialect dialect,
        IConnectionStringProvider? connectionStringProvider = null)
        : IConnectionProvider<T>
        where T : class
    {
        public IDbConnection GetConnection()
        {
            if (connectionStringProvider == null)
                throw new InvalidOperationException(
                    "No connection string configured. Provide a connection string (e.g., UseSqlite(\"...\")) " +
                    "or register a custom IConnectionStringProvider.");

            var connectionString = connectionStringProvider.GetConnectionString(metadataProvider.DatabaseName);

            try
            {
                var connection = dialect.CreateConnection(connectionString);
                connection.Open();
                return connection;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to open database connection for '{metadataProvider.DatabaseName}'.", ex);
            }
        }

        public async Task<DbConnection> GetConnectionAsync()
        {
            if (connectionStringProvider == null)
                throw new InvalidOperationException(
                    "No connection string configured. Provide a connection string (e.g., UseSqlite(\"...\")) " +
                    "or register a custom IConnectionStringProvider.");

            var connectionString = connectionStringProvider.GetConnectionString(metadataProvider.DatabaseName);

            try
            {
                var connection = (DbConnection)dialect.CreateConnection(connectionString);
                await connection.OpenAsync();
                return connection;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to open database connection for '{metadataProvider.DatabaseName}'.", ex);
            }
        }
    }
}
