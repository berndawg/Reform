using System.Linq.Expressions;
using Reform.Objects;

namespace Reform.Interfaces
{
    public interface ISqlBuilder<T> where T : class
    {
        string GetCountSql(Expression<Func<T, bool>>? predicate, out Dictionary<string, object> parameters);
        string GetExistsSql(Expression<Func<T, bool>>? predicate, out Dictionary<string, object> parameters);
        string GetSelectSql(QueryCriteria<T> queryCriteria, ref Dictionary<string, object> parameters);
        string GetInsertSql(T instance, ref Dictionary<string, object> parameters);
        string GetUpdateSql(T instance, T original, ref Dictionary<string, object> parameters,
                            Expression<Func<T, bool>> predicate);
        string GetDeleteSql(Expression<Func<T, bool>> predicate, ref Dictionary<string, object> parameters);
        string GetTruncateSql();
    }
}
