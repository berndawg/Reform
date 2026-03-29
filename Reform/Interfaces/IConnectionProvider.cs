using System.Data;

namespace Reform.Interfaces
{
    public interface IConnectionProvider<T> where T : class
    {
        IDbConnection GetConnection();
    }
}
