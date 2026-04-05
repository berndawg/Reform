using System.Data;
using Npgsql;
using Reform.Interfaces;

namespace Reform.Dialects
{
    public class PostgreSqlDialect : IDialect
    {
        public IDbConnection CreateConnection(string connectionString)
        {
            return new NpgsqlConnection(connectionString);
        }

        public IDbCommand CreateCommand(string commandText, IDbConnection connection)
        {
            return new NpgsqlCommand(commandText, (NpgsqlConnection)connection);
        }

        public string IdentitySql => "SELECT lastval()";

        public string GetPagingSql(int limit, int offset)
        {
            return $"LIMIT {limit} OFFSET {offset}";
        }

        public string QuoteIdentifier(string name)
        {
            return $"\"{name.Replace("\"", "\"\"")}\"";
        }

        public string ParameterPrefix => "@";

        public string EscapeLikeValue(string value)
        {
            if (value == null) return null;
            return value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
        }

        public string LikeEscapeClause => @" ESCAPE '\'";

        public string BooleanTrueLiteral => "TRUE";

        public string GetTruncateSql(string tableName) => $"TRUNCATE TABLE {tableName}";

        public string GetExistsSql(string subquery) => $"SELECT EXISTS({subquery})";
    }
}
