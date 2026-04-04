using System.Data;
using MySqlConnector;
using Reform.Interfaces;

namespace Reform.Dialects
{
    public class MySqlDialect : IDialect
    {
        public IDbConnection CreateConnection(string connectionString)
        {
            return new MySqlConnection(connectionString);
        }

        public IDbCommand CreateCommand(string commandText, IDbConnection connection)
        {
            return new MySqlCommand(commandText, (MySqlConnection)connection);
        }

        public string IdentitySql => "SELECT LAST_INSERT_ID()";

        public string GetPagingSql(int limit, int offset)
        {
            return $"LIMIT {limit} OFFSET {offset}";
        }

        public string QuoteIdentifier(string name)
        {
            return $"`{name}`";
        }

        public string ParameterPrefix => "@";

        public string EscapeLikeValue(string value)
        {
            if (value == null) return null;
            return value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
        }
    }
}
