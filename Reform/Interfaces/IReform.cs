using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Transactions;
using Reform.Objects;

namespace Reform.Interfaces
{
    public interface IReform<T> where T : class
    {
        IDbConnection GetConnection();
        TransactionScope GetScope();

        int Count();
        int Count(Expression<Func<T, bool>> predicate);

        bool Exists(Expression<Func<T, bool>> predicate);

        void Insert(T item);
        void Insert(List<T> items);
        void Insert(IDbConnection connection, T item);

        void Update(T item);
        void Update(List<T> list);
        void Update(IDbConnection connection, T item);

        void Delete(T item);
        void Delete(List<T> list);
        void Delete(IDbConnection connection, T item);

        T SelectSingle(Expression<Func<T, bool>> predicate);
        T SelectSingleOrDefault(Expression<Func<T, bool>> predicate);

        IEnumerable<T> Select();
        IEnumerable<T> Select(Expression<Func<T, bool>> predicate);
        IEnumerable<T> Select(QueryCriteria<T> queryCriteria);
    }
}
