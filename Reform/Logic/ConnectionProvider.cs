// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System;
using System.Data;
using System.Data.Common;
using MySql.Data.MySqlClient;
using Reform.Interfaces;

namespace Reform.Logic
{
    internal sealed class ConnectionProvider<T> : IConnectionProvider<T> where T : class
    {
        private readonly IConnectionStringProvider _connectionStringProvider;
        private readonly IMetadataProvider<T> _metadataProvider;
        private readonly DbProviderFactory _dbProviderFactory;

        #region Constructors

        internal ConnectionProvider(IConnectionStringProvider connectionStringProvider, IMetadataProvider<T> metadataProvider)
        {
            _connectionStringProvider = connectionStringProvider;
            _metadataProvider = metadataProvider;
            _dbProviderFactory = MySqlClientFactory.Instance;
        }

        #endregion

        #region Interface Implementation

        public IDbConnection GetConnection()
        {
            string connectionString = _connectionStringProvider.GetConnectionString(_metadataProvider.DatabaseName);

            try
            {
                var connection = _dbProviderFactory.CreateConnection();
                if (connection == null)
                    throw new ApplicationException("Failed to create database connection");

                connection.ConnectionString = connectionString;
                connection.Open();
                return connection;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to open database connection using connection string: {connectionString}", ex);
            }
        }

        #endregion
    }
}