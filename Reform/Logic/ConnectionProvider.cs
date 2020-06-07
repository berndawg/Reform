// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System;
using System.Data.SqlClient;
using Reform.Interfaces;

namespace Reform.Logic
{
    internal sealed class ConnectionProvider<T> : IConnectionProvider<T> where T : class
    {
        private readonly IConnectionStringProvider _connectionStringProvider;
        private readonly IMetadataProvider<T> _metadataProvider;

        #region Constructors

        internal ConnectionProvider(IConnectionStringProvider connectionStringProvider, IMetadataProvider<T> metadataProvider)
        {
            _connectionStringProvider = connectionStringProvider;
            _metadataProvider = metadataProvider;
        }

        #endregion

        #region Interface Implementation

        public SqlConnection GetConnection()
        {
            string connectionString = _connectionStringProvider.GetConnectionString(_metadataProvider.DatabaseName);

            try
            {
                var connection = new SqlConnection(connectionString);

                connection.Open();

                return connection;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to open SqlConnection using connection string: {connectionString}", ex);
            }
        }

        #endregion
    }
}