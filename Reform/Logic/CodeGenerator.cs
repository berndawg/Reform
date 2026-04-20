using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;
using Reform.Interfaces;

namespace Reform.Logic;

public sealed partial class CodeGenerator(IDialect dialect, IConnectionStringProvider connectionStringProvider)
    : ICodeGenerator
{
    private record ColumnInfo(string ColumnName, string DataType, bool IsPrimaryKey, bool IsIdentity, bool IsNullable);

    private static readonly Dictionary<string, string> SqlTypeToCSharpType = new(StringComparer.OrdinalIgnoreCase)
    {
        // Integer types
        { "int", "int" },
        { "integer", "int" },
        { "bigint", "long" },
        { "smallint", "short" },
        { "tinyint", "byte" },

        // Boolean
        { "bit", "bool" },
        { "boolean", "bool" },

        // Decimal / money
        { "decimal", "decimal" },
        { "numeric", "decimal" },
        { "money", "decimal" },
        { "smallmoney", "decimal" },

        // Floating point
        { "float", "double" },
        { "double precision", "double" },
        { "real", "float" },

        // Date/time
        { "date", "DateTime" },
        { "datetime", "DateTime" },
        { "datetime2", "DateTime" },
        { "smalldatetime", "DateTime" },
        { "timestamp without time zone", "DateTime" },
        { "datetimeoffset", "DateTimeOffset" },
        { "timestamp with time zone", "DateTimeOffset" },
        { "time", "TimeSpan" },
        { "time without time zone", "TimeSpan" },

        // GUID
        { "uniqueidentifier", "Guid" },
        { "uuid", "Guid" },

        // Binary
        { "varbinary", "byte[]" },
        { "binary", "byte[]" },
        { "image", "byte[]" },
        { "bytea", "byte[]" },
        { "blob", "byte[]" },
    };

    private static readonly HashSet<string> ValueTypes =
    [
        "int", "long", "short", "byte", "bool", "decimal", "double", "float",
        "DateTime", "DateTimeOffset", "TimeSpan", "Guid"
    ];

    public string CodeGen(string tableName)
    {
        var columns = GetColumns(tableName);

        if (columns.Count == 0)
            throw new InvalidOperationException($"Table '{tableName}' not found or has no columns.");

        return BuildClass(tableName, columns);
    }

    private List<ColumnInfo> GetColumns(string tableName)
    {
        var connectionString = connectionStringProvider.GetConnectionString("");
        using var connection = dialect.CreateConnection(connectionString);
        connection.Open();

        var sql = dialect.GetColumnMetadataSql(tableName);
        using var command = dialect.CreateCommand(sql, connection);

        var columns = new List<ColumnInfo>();
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            columns.Add(new ColumnInfo(
                ColumnName: reader["ColumnName"].ToString()!,
                DataType: reader["DataType"].ToString()!,
                IsPrimaryKey: Convert.ToBoolean(reader["IsPrimaryKey"]),
                IsIdentity: Convert.ToBoolean(reader["IsIdentity"]),
                IsNullable: Convert.ToBoolean(reader["IsNullable"])
            ));
        }

        return columns;
    }

    private static string BuildClass(string tableName, List<ColumnInfo> columns)
    {
        var className = ToPascalCase(tableName);
        var sb = new StringBuilder();

        sb.AppendLine("using Reform.Attributes;");
        sb.AppendLine();
        sb.AppendLine($"[EntityMetadata(TableName = \"{tableName}\")]");
        sb.AppendLine($"public class {className}");
        sb.AppendLine("{");

        for (var i = 0; i < columns.Count; i++)
        {
            var col = columns[i];
            var propertyName = ToPascalCase(col.ColumnName);
            var csharpType = MapToCSharpType(col.DataType);
            var isValueType = ValueTypes.Contains(csharpType);
            var isReferenceType = !isValueType;

            if (col.IsNullable)
                csharpType += "?";

            // Build attribute
            var attrParts = new List<string> { $"ColumnName = \"{col.ColumnName}\"" };

            var displayName = InsertSpaces(propertyName);
            if (displayName != col.ColumnName)
                attrParts.Add($"DisplayName = \"{displayName}\"");

            if (col.IsPrimaryKey)
                attrParts.Add("IsPrimaryKey = true");

            if (col.IsIdentity)
                attrParts.Add("IsIdentity = true");

            if (!col.IsNullable && !col.IsPrimaryKey)
                attrParts.Add("IsRequired = true");

            sb.AppendLine($"    [PropertyMetadata({string.Join(", ", attrParts)})]");

            // Build property
            var defaultValue = !col.IsNullable && isReferenceType && csharpType == "string"
                ? " = \"\";"
                : "";

            sb.AppendLine($"    public {csharpType} {propertyName} {{ get; set; }}{defaultValue}");

            if (i < columns.Count - 1)
                sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string MapToCSharpType(string sqlType)
    {
        var normalized = sqlType.Trim().ToLowerInvariant();

        if (SqlTypeToCSharpType.TryGetValue(normalized, out var csharpType))
            return csharpType;

        // Handle types with length specifiers like "varchar(50)", "numeric(10,2)"
        var parenIndex = normalized.IndexOf('(');
        if (parenIndex > 0)
        {
            var baseType = normalized[..parenIndex].Trim();
            if (SqlTypeToCSharpType.TryGetValue(baseType, out csharpType))
                return csharpType;
        }

        return "string";
    }

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        // Split on underscores and capitalize each segment
        var parts = name.Split('_', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();

        foreach (var part in parts)
        {
            if (part.Length > 0)
            {
                sb.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                    sb.Append(part[1..]);
            }
        }

        return sb.ToString();
    }

    private static string InsertSpaces(string pascalCase)
    {
        return InsertSpacesRegex().Replace(pascalCase, " $1").Trim();
    }

    [GeneratedRegex(@"(?<=[a-z])([A-Z])|(?<=[A-Z])([A-Z][a-z])")]
    private static partial Regex InsertSpacesRegex();
}
