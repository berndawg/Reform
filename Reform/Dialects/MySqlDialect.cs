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
            return $"`{name.Replace("`", "``")}`";
        }

        public string ParameterPrefix => "@";

        public string? EscapeLikeValue(string? value)
        {
            if (value == null) return null;
            return value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
        }

        public string LikeEscapeClause => @" ESCAPE '\'";

        public string BooleanTrueLiteral => "1";

        public string GetTruncateSql(string tableName) => $"TRUNCATE TABLE {tableName}";

        public string GetExistsSql(string subquery) => $"SELECT EXISTS({subquery})";

        public string GetColumnMetadataSql(string tableName) =>
            $"""
             SELECT
                 c.COLUMN_NAME AS ColumnName,
                 c.DATA_TYPE AS DataType,
                 CASE WHEN c.COLUMN_KEY = 'PRI' THEN 1 ELSE 0 END AS IsPrimaryKey,
                 CASE WHEN c.EXTRA LIKE '%auto_increment%' THEN 1 ELSE 0 END AS IsIdentity,
                 CASE WHEN c.IS_NULLABLE = 'YES' THEN 1 ELSE 0 END AS IsNullable
             FROM INFORMATION_SCHEMA.COLUMNS c
             WHERE c.TABLE_NAME = '{tableName}'
                 AND c.TABLE_SCHEMA = DATABASE()
             ORDER BY c.ORDINAL_POSITION
             """;
    }
}
