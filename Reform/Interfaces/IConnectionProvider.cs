using System.Data;
using System.Data.Common;

namespace Reform.Interfaces;

public interface IConnectionProvider<T> where T : class
{
    IDbConnection GetConnection();
    Task<DbConnection> GetConnectionAsync();
}
