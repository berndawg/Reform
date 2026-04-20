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

        public string? EscapeLikeValue(string? value)
        {
            if (value == null) return null;
            return value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
        }

        public string LikeEscapeClause => @" ESCAPE '\'";

        public string BooleanTrueLiteral => "TRUE";

        public string GetTruncateSql(string tableName) => $"TRUNCATE TABLE {tableName}";

        public string GetExistsSql(string subquery) => $"SELECT EXISTS({subquery})";

        public IDbCommand CreateColumnMetadataCommand(IDbConnection connection, string tableName)
        {
            const string sql =
                """
                SELECT
                    c.column_name AS "ColumnName",
                    c.data_type AS "DataType",
                    CASE WHEN pk.column_name IS NOT NULL THEN 1 ELSE 0 END AS "IsPrimaryKey",
                    CASE WHEN LOWER(c.column_default) LIKE 'nextval(%' OR c.is_identity = 'YES' THEN 1 ELSE 0 END AS "IsIdentity",
                    CASE WHEN c.is_nullable = 'YES' THEN 1 ELSE 0 END AS "IsNullable"
                FROM information_schema.columns c
                LEFT JOIN (
                    SELECT ku.column_name, ku.table_name, ku.table_schema
                    FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage ku
                        ON tc.constraint_name = ku.constraint_name
                        AND tc.table_schema = ku.table_schema
                    WHERE tc.constraint_type = 'PRIMARY KEY'
                ) pk ON pk.table_name = c.table_name
                    AND pk.table_schema = c.table_schema
                    AND pk.column_name = c.column_name
                WHERE c.table_name = @tableName
                ORDER BY c.ordinal_position
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
