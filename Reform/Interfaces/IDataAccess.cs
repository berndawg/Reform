using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using Reform.Objects;

namespace Reform.Interfaces
{
    public interface IDataAccess<T> where T : class
    {
        int Count(IDbConnection connection, Expression<Func<T, bool>> predicate);
        bool Exists(IDbConnection connection, Expression<Func<T, bool>> predicate);
        void Insert(IDbConnection connection, T instance);
        void Update(IDbConnection connection, T instance);
        void Update(IDbConnection connection, T instance, Expression<Func<T, bool>> predicate);
        void Delete(IDbConnection connection, T instance);
        void Delete(IDbConnection connection, Expression<Func<T, bool>> predicate);
        IEnumerable<T> Select(IDbConnection connection, QueryCriteria<T> queryCriteria);
    }
}
