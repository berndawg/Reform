using System.Data;
using Microsoft.Data.Sqlite;
using Reform.Interfaces;

namespace Reform.Dialects
{
    public class SqliteDialect : IDialect
    {
        public IDbConnection CreateConnection(string connectionString)
        {
            return new SqliteConnection(connectionString);
        }

        public IDbCommand CreateCommand(string commandText, IDbConnection connection)
        {
            return new SqliteCommand(commandText, (SqliteConnection)connection);
        }

        public string IdentitySql => "SELECT last_insert_rowid()";

        public string GetPagingSql(int limit, int offset)
        {
            return $"LIMIT {limit} OFFSET {offset}";
        }

        public string QuoteIdentifier(string name)
        {
            return $"[{name.Replace("]", "]]")}]";
        }

        public string ParameterPrefix => "@";

        public string? EscapeLikeValue(string? value)
        {
            if (value == null) return null;
            return value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
        }

        public string LikeEscapeClause => @" ESCAPE '\'";

        public string BooleanTrueLiteral => "1";

        public string GetTruncateSql(string tableName) => $"DELETE FROM {tableName}";

        public string GetExistsSql(string subquery) => $"SELECT EXISTS({subquery})";

        public IDbCommand CreateColumnMetadataCommand(IDbConnection connection, string tableName)
        {
            const string sql =
                """
                SELECT
                    name AS ColumnName,
                    type AS DataType,
                    pk AS IsPrimaryKey,
                    CASE WHEN pk = 1 AND UPPER(type) = 'INTEGER' THEN 1 ELSE 0 END AS IsIdentity,
                    CASE WHEN "notnull" = 0 AND pk = 0 THEN 1 ELSE 0 END AS IsNullable
                FROM pragma_table_info(@tableName)
                """;

            var command = CreateCommand(sql, connection);
            var param = command.CreateParameter();
            param.ParameterName = $"{ParameterPrefix}tableName";
            param.Value = tableName;
            command.Parameters.Add(param);
            return command;
        }
    }
}
