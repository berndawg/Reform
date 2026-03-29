using System;
using System.Data;
using Microsoft.Data.Sqlite;
using Reform.Interfaces;

namespace Reform.Logic
{
    internal sealed class ConnectionProvider<T> : IConnectionProvider<T> where T : class
    {
        private readonly IConnectionStringProvider _connectionStringProvider;
        private readonly IMetadataProvider<T> _metadataProvider;

        internal ConnectionProvider(IConnectionStringProvider connectionStringProvider, IMetadataProvider<T> metadataProvider)
        {
            _connectionStringProvider = connectionStringProvider;
            _metadataProvider = metadataProvider;
        }

        public IDbConnection GetConnection()
        {
            string connectionString = _connectionStringProvider.GetConnectionString(_metadataProvider.DatabaseName);

            try
            {
                var connection = new SqliteConnection(connectionString);
                connection.Open();
                return connection;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to open SqliteConnection using connection string: {connectionString}", ex);
            }
        }
    }
}
