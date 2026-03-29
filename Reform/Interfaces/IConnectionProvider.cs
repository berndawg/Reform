using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Reform.Interfaces
{
    public interface IConnectionProvider<T> where T : class
    {
        IDbConnection GetConnection();
        Task<DbConnection> GetConnectionAsync();
    }
}
