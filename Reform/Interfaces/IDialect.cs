using System.Data;

namespace Reform.Interfaces
{
    public interface IDialect
    {
        IDbConnection CreateConnection(string connectionString);
        IDbCommand CreateCommand(string commandText, IDbConnection connection);
        string IdentitySql { get; }
        string GetPagingSql(int limit, int offset);
        string QuoteIdentifier(string name);
        string ParameterPrefix { get; }
        string EscapeLikeValue(string value);
    }
}
