using MySqlConnector;

namespace ReformIntegrationTests
{
    internal static class DatabaseSetup
    {
        public static void EnsureDatabase(string connectionString)
        {
            var builder = new MySqlConnectionStringBuilder(connectionString);
            var databaseName = builder.Database;
            builder.Database = "";

            using var connection = new MySqlConnection(builder.ConnectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{databaseName}`;";
            cmd.ExecuteNonQuery();
        }

        public static void EnsureTables(string connectionString)
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS `Country` (
                    `CountryId` INT NOT NULL PRIMARY KEY AUTO_INCREMENT,
                    `CountryName` VARCHAR(200) NOT NULL
                );

                CREATE TABLE IF NOT EXISTS `Airport` (
                    `AirportId` INT NOT NULL AUTO_INCREMENT,
                    `AirportCode` VARCHAR(10) NOT NULL,
                    `AirportName` VARCHAR(50) NOT NULL,
                    `CountryId` INT NOT NULL,
                    PRIMARY KEY (`AirportId`)
                );";
            cmd.ExecuteNonQuery();
        }

        public static void CleanTables(string connectionString)
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM `Airport`; DELETE FROM `Country`;";
            cmd.ExecuteNonQuery();
        }
    }
}
