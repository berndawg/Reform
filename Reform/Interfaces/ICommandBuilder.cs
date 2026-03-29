using System;
using System.Data;
using System.Linq.Expressions;
using Reform.Objects;

namespace Reform.Interfaces
{
    internal interface ICommandBuilder<T> where T : class
    {
        IDbCommand GetCountCommand(IDbConnection connection, Expression<Func<T, bool>> predicate);
        IDbCommand GetExistsCommand(IDbConnection connection, Expression<Func<T, bool>> predicate);
        IDbCommand GetSelectCommand(IDbConnection connection, QueryCriteria<T> queryCriteria);
        IDbCommand GetInsertCommand(IDbConnection connection, T instance);
        IDbCommand GetUpdateCommand(IDbConnection connection, T instance, T original,
                                    Expression<Func<T, bool>> predicate);
        IDbCommand GetDeleteCommand(IDbConnection connection, Expression<Func<T, bool>> predicate);
    }
}
