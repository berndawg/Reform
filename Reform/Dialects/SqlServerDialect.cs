using System.Data;
using Microsoft.Data.SqlClient;
using Reform.Interfaces;

namespace Reform.Dialects
{
    public class SqlServerDialect : IDialect
    {
        public IDbConnection CreateConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }

        public IDbCommand CreateCommand(string commandText, IDbConnection connection)
        {
            return new SqlCommand(commandText, (SqlConnection)connection);
        }

        public string IdentitySql => "SELECT SCOPE_IDENTITY()";

        public string GetPagingSql(int limit, int offset)
        {
            return $"OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY";
        }

        public string QuoteIdentifier(string name)
        {
            return $"[{name.Replace("]", "]]")}]";
        }

        public string ParameterPrefix => "@";

        public string? EscapeLikeValue(string? value)
        {
            if (value == null) return null;
            return value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
        }

        public string LikeEscapeClause => "";

        public string BooleanTrueLiteral => "1";

        public string GetTruncateSql(string tableName) => $"TRUNCATE TABLE {tableName}";

        public string GetExistsSql(string subquery) => $"SELECT CASE WHEN EXISTS({subquery}) THEN 1 ELSE 0 END";
    }
}
