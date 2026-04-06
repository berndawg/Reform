using System;
using Microsoft.Data.SqlClient;

namespace ReformIntegrationTests
{
    internal static class DatabaseSetup
    {
        public static void EnsureDatabase(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            string databaseName = builder.InitialCatalog;
            builder.InitialCatalog = "master";

            using var connection = new SqlConnection(builder.ConnectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $@"
                IF DB_ID('{databaseName}') IS NULL
                    CREATE DATABASE [{databaseName}];";
            cmd.ExecuteNonQuery();
        }

        public static void EnsureTables(string connectionString)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                IF OBJECT_ID('dbo.Country', 'U') IS NULL
                CREATE TABLE [dbo].[Country] (
                    [CountryId] INT NOT NULL PRIMARY KEY IDENTITY,
                    [CountryName] VARCHAR(200) NOT NULL
                );

                IF OBJECT_ID('dbo.Airport', 'U') IS NULL
                CREATE TABLE [dbo].[Airport] (
                    [AirportId] INT IDENTITY(1,1) NOT NULL,
                    [AirportCode] VARCHAR(10) NOT NULL,
                    [AirportName] VARCHAR(50) NOT NULL,
                    [CountryId] INT NOT NULL,
                    CONSTRAINT [PK_Airport] PRIMARY KEY CLUSTERED ([AirportId] ASC)
                );";
            cmd.ExecuteNonQuery();
        }

        public static void CleanTables(string connectionString)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM [Airport]; DELETE FROM [Country];";
            cmd.ExecuteNonQuery();
        }
    }
}
