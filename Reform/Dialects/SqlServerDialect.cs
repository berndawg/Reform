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

        public string GetColumnMetadataSql(string tableName) =>
            $"""
             SELECT
                 c.COLUMN_NAME AS ColumnName,
                 c.DATA_TYPE AS DataType,
                 CAST(CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS IsPrimaryKey,
                 CAST(COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') AS BIT) AS IsIdentity,
                 CAST(CASE WHEN c.IS_NULLABLE = 'YES' THEN 1 ELSE 0 END AS BIT) AS IsNullable
             FROM INFORMATION_SCHEMA.COLUMNS c
             LEFT JOIN (
                 SELECT ku.COLUMN_NAME, ku.TABLE_NAME, ku.TABLE_SCHEMA
                 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                 JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                     ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                     AND tc.TABLE_SCHEMA = ku.TABLE_SCHEMA
                 WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
             ) pk ON pk.TABLE_NAME = c.TABLE_NAME
                 AND pk.TABLE_SCHEMA = c.TABLE_SCHEMA
                 AND pk.COLUMN_NAME = c.COLUMN_NAME
             WHERE c.TABLE_NAME = '{tableName}'
             ORDER BY c.ORDINAL_POSITION
             """;
    }
}
