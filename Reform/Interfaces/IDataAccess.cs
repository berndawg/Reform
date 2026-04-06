using System.Data;
using System.Linq.Expressions;
using Reform.Objects;

namespace Reform.Interfaces
{
    public interface IDataAccess<T> where T : class
    {
        int Count(IDbConnection connection, IDbTransaction? transaction, Expression<Func<T, bool>>? predicate);
        bool Exists(IDbConnection connection, IDbTransaction? transaction, Expression<Func<T, bool>>? predicate);
        void Insert(IDbConnection connection, IDbTransaction? transaction, T instance);
        void Update(IDbConnection connection, IDbTransaction? transaction, T instance);
        void Update(IDbConnection connection, IDbTransaction? transaction, T instance, Expression<Func<T, bool>> predicate);
        void Delete(IDbConnection connection, IDbTransaction? transaction, T instance);
        void Delete(IDbConnection connection, IDbTransaction? transaction, Expression<Func<T, bool>> predicate);
        IEnumerable<T> Select(IDbConnection connection, IDbTransaction? transaction, QueryCriteria<T> queryCriteria);
        void Truncate(IDbConnection connection, IDbTransaction? transaction);

        Task<int> CountAsync(IDbConnection connection, IDbTransaction? transaction, Expression<Func<T, bool>>? predicate);
        Task<bool> ExistsAsync(IDbConnection connection, IDbTransaction? transaction, Expression<Func<T, bool>>? predicate);
        Task InsertAsync(IDbConnection connection, IDbTransaction? transaction, T instance);
        Task UpdateAsync(IDbConnection connection, IDbTransaction? transaction, T instance);
        Task UpdateAsync(IDbConnection connection, IDbTransaction? transaction, T instance, Expression<Func<T, bool>> predicate);
        Task DeleteAsync(IDbConnection connection, IDbTransaction? transaction, T instance);
        Task DeleteAsync(IDbConnection connection, IDbTransaction? transaction, Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> SelectAsync(IDbConnection connection, IDbTransaction? transaction, QueryCriteria<T> queryCriteria);
        Task TruncateAsync(IDbConnection connection, IDbTransaction? transaction);
    }
}
