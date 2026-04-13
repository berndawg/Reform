using System.Data;
using System.Linq.Expressions;
using Reform.Objects;

namespace Reform.Interfaces
{
    public interface IReform<T> where T : class
    {
        IDbConnection GetConnection();

        int Count();
        int Count(Expression<Func<T, bool>> predicate);

        bool Exists(Expression<Func<T, bool>> predicate);

        void Insert(T item);
        void Insert(List<T> items);
        void Insert(IDbConnection connection, T item);
        void Insert(IDbConnection connection, IDbTransaction transaction, T item);

        void Update(T item);
        void Update(List<T> list);
        void Update(IDbConnection connection, T item);
        void Update(IDbConnection connection, IDbTransaction transaction, T item);

        void Delete(T item);
        void Delete(List<T> list);
        void Delete(IDbConnection connection, T item);
        void Delete(IDbConnection connection, IDbTransaction transaction, T item);

        void Merge(List<T> list);

        void Truncate();

        T SelectSingle(Expression<Func<T, bool>> predicate);
        T SelectSingleOrDefault(Expression<Func<T, bool>> predicate);

        IEnumerable<T> Select();
        IEnumerable<T> Select(Expression<Func<T, bool>> predicate);
        IEnumerable<T> Select(QueryCriteria<T> queryCriteria);

        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);

        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

        Task InsertAsync(T item);
        Task InsertAsync(List<T> items);
        Task InsertAsync(IDbConnection connection, IDbTransaction transaction, T item);

        Task UpdateAsync(T item);
        Task UpdateAsync(List<T> list);
        Task UpdateAsync(IDbConnection connection, IDbTransaction transaction, T item);

        Task DeleteAsync(T item);
        Task DeleteAsync(List<T> list);
        Task DeleteAsync(IDbConnection connection, IDbTransaction transaction, T item);

        Task MergeAsync(List<T> list);

        Task TruncateAsync();

        Task<T> SelectSingleAsync(Expression<Func<T, bool>> predicate);
        Task<T> SelectSingleOrDefaultAsync(Expression<Func<T, bool>> predicate);

        Task<IEnumerable<T>> SelectAsync();
        Task<IEnumerable<T>> SelectAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> SelectAsync(QueryCriteria<T> queryCriteria);
    }
}
