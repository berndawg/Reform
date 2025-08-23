using System.Collections.Generic;
using System.Text;
using Reform.Logic;
using Reform.Objects;
using ReformTests.Objects;

namespace ReformTests.Logic
{
    public class CodeGenerator : Reform<Metadata>, ICodeGenerator
    {
        public string GenerateCode(string tableName)
        {
            IEnumerable<Metadata> metadataList = Select(Filter.EqualTo(nameof(Metadata.ObjectName), tableName));

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"[EntityMetadata(DatabaseName=\"ReformDB\", TableName=\"{tableName}\")]");
            stringBuilder.AppendLine($"public class {tableName}");
            stringBuilder.AppendLine("{");

            foreach (Metadata metadata in metadataList)
            {
                stringBuilder.Append($"\t[PropertyMetadata(ColumnName=\"{metadata.ColumnName}\", DisplayName=\"{metadata.ColumnName}\"");
                if (metadata.IsPrimary) stringBuilder.Append(", IsPrimaryKey=true");
                if (metadata.IsIdentity) stringBuilder.Append(", IsIdentity=true");
                if (metadata.IsEncrypted) stringBuilder.Append(", IsEncrypted=true");
                if (metadata.IsRequired) stringBuilder.Append(", IsRequired=true");
                stringBuilder.AppendLine(")]");

                stringBuilder.Append($"\tpublic {GetType(metadata)} {metadata.ColumnName} ");
                stringBuilder.AppendLine("{ get; set; }");
                stringBuilder.AppendLine();
            }
            stringBuilder.AppendLine("}");

            return stringBuilder.ToString();
        }


        private string GetType(Metadata metadata)
        {
            string baseType = GetBaseType(metadata);

            if (metadata.IsNullable)
                baseType = $"{baseType}?";

            return baseType;
        }


        private string GetBaseType(Metadata metadata)
        {
            switch (metadata.TypeName)
            {
                case "varbinary": return "string";
                case "varchar": return "string";
                case "nvarchar": return "string";
                case "char": return "string";
                case "nchar": return "string";
                case "text": return "string";
                case "ntext": return "string";
                case "uniqueidentifier": return "Guid";
                case "date": return "DateTime";
                case "datetime": return "DateTime";
                case "datetime2": return "DateTime";
                case "smalldatetime": return "DateTime";
                case "bit": return "bool";
                case "float": return "double";
                case "tinyint": return "byte";
                case "smallint": return "Int16";
                case "int": return "int";
                case "bigint": return "Int64";
                case "real": return "Single";
                case "decimal": return "decimal";
                case "numeric": return "decimal";
                case "money": return "decimal";
                case "rowversion": return "Byte[]";
                case "xml": return "string";
                default:
                    return "object";
            }
        }
    }
}
