using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using Reform.Enum;
using Reform.Extensions;
using Reform.Interfaces;
using Reform.Objects;

[assembly: InternalsVisibleTo("ReformTests")]
namespace Reform.Logic;

public sealed class SqlBuilder<T>(IMetadataProvider<T> metadataProvider, IDialect dialect) : ISqlBuilder<T>
    where T : class
{
    private readonly WhereClauseBuilder<T> _whereClauseBuilder = new(metadataProvider, dialect);

    public string GetCountSql(Expression<Func<T, bool>>? predicate, out Dictionary<string, object> parameters)
    {
        var (whereClause, whereParams) = BuildWhereClause(predicate);
        parameters = whereParams;

        var fromClause = GetFromClause();
        var where = string.IsNullOrEmpty(whereClause) ? "" : $" WHERE {whereClause}";

        return $"SELECT COUNT(1){fromClause}{where}";
    }

    public string GetExistsSql(Expression<Func<T, bool>> predicate, out Dictionary<string, object> parameters)
    {
        var (whereClause, whereParams) = BuildWhereClause(predicate);
        parameters = whereParams;

        var fromClause = GetFromClause();
        var where = string.IsNullOrEmpty(whereClause) ? "" : $" WHERE {whereClause}";

        return $"SELECT EXISTS(SELECT 1{fromClause}{where})";
    }

    public string GetSelectSql(QueryCriteria<T> queryCriteria, ref Dictionary<string, object> parameters)
    {
        var pageCriteria = queryCriteria.PageCriteria;
        var doPaging = pageCriteria != null && pageCriteria.PageSize != 0 && pageCriteria.Page != 0;

        return doPaging
            ? GetSelectSqlPaged(pageCriteria!, ref parameters, queryCriteria)
            : GetSelectSqlNonPaged(ref parameters, queryCriteria);
    }

    public string GetInsertSql(T instance, ref Dictionary<string, object> parameters)
    {
        var tableName = GetTableName();
        var columnNames = GetColumnNames(metadataProvider.UpdateableProperties);
        var values = GetValuesForInsert(instance, ref parameters);

        return $"INSERT INTO {tableName} ({columnNames}) VALUES ({values})";
    }

    public string GetUpdateSql(T instance, object original, ref Dictionary<string, object> parameters,
                                Expression<Func<T, bool>>? predicate)
    {
        var nameValuePairs = GetValuesForUpdate(instance, original, ref parameters);

        if (string.IsNullOrEmpty(nameValuePairs))
            return string.Empty;

        var tableName = GetTableName();

        var (whereClause, whereParams) = BuildWhereClause(predicate, parameters.Count);
        foreach (var kvp in whereParams)
            parameters.Add(kvp.Key, kvp.Value);

        var where = string.IsNullOrEmpty(whereClause) ? "" : $" WHERE {whereClause}";

        return $"UPDATE {tableName} SET {nameValuePairs}{where}";
    }

    public string GetDeleteSql(Expression<Func<T, bool>>? predicate, ref Dictionary<string, object> parameters)
    {
        var fromClause = GetFromClause();

        var (whereClause, whereParams) = BuildWhereClause(predicate, parameters.Count);
        foreach (var kvp in whereParams)
            parameters.Add(kvp.Key, kvp.Value);

        var where = string.IsNullOrEmpty(whereClause) ? "" : $" WHERE {whereClause}";

        return $"DELETE{fromClause}{where}";
    }

    private (string sql, Dictionary<string, object> parameters) BuildWhereClause(Expression<Func<T, bool>>? predicate, int startingIndex = 0)
    {
        return _whereClauseBuilder.Build(predicate, startingIndex);
    }

    private string GetSelectSqlNonPaged(ref Dictionary<string, object> parameters, QueryCriteria<T> queryCriteria)
    {
        var columnNames = GetColumnNames(metadataProvider.AllProperties);
        var fromClause = GetFromClause();

        var (whereClause, whereParams) = BuildWhereClause(queryCriteria.Predicate);
        foreach (var kvp in whereParams)
            parameters.Add(kvp.Key, kvp.Value);

        var where = string.IsNullOrEmpty(whereClause) ? "" : $" WHERE {whereClause}";
        var orderByClause = GetOrderByClause(queryCriteria.SortCriteria);

        return $"SELECT {columnNames}{fromClause}{where}{orderByClause}";
    }

    private string GetSelectSqlPaged(PageCriteria pageCriteria, ref Dictionary<string, object> parameters, QueryCriteria<T> queryCriteria)
    {
        if (queryCriteria.SortCriteria.Count < 1)
            throw new ArgumentException("Paging requires at least one SortCriterion");

        var columnNames = GetColumnNames(metadataProvider.AllProperties);
        var fromClause = GetFromClause();

        var (whereClause, whereParams) = BuildWhereClause(queryCriteria.Predicate);
        foreach (var kvp in whereParams)
            parameters.Add(kvp.Key, kvp.Value);

        var where = string.IsNullOrEmpty(whereClause) ? "" : $" WHERE {whereClause}";
        var orderByClause = GetOrderByClause(queryCriteria.SortCriteria);

        var offset = (pageCriteria.Page - 1) * pageCriteria.PageSize;
        var limit = pageCriteria.PageSize;

        return $"SELECT {columnNames}{fromClause}{where}{orderByClause} {dialect.GetPagingSql(limit, offset)}";
    }

    private string GetFromClause()
    {
        return $" FROM {GetTableName()}";
    }

    private string GetTableName()
    {
        return dialect.QuoteIdentifier(metadataProvider.TableName);
    }

    private string GetOrderByClause(IEnumerable<SortCriterion>? sortCriteriaList)
    {
        var stringBuilder = new StringBuilder();

        if (sortCriteriaList != null)
            foreach (var sortCriteria in sortCriteriaList)
            {
                if (stringBuilder.Length > 0)
                    stringBuilder.Append(", ");

                var propertyMap = metadataProvider.GetPropertyMapByPropertyName(sortCriteria.PropertyName);

                if (propertyMap == null)
                    throw new ApplicationException($"The type '{metadataProvider.Type}' does not contain property metadata for the property '{sortCriteria.PropertyName}'");

                stringBuilder.Append(dialect.QuoteIdentifier(propertyMap.ColumnName));

                if (sortCriteria.Direction == SortDirection.Descending)
                    stringBuilder.Append(" DESC");
            }

        if (stringBuilder.Length > 0)
            stringBuilder.Insert(0, " ORDER BY ");

        return stringBuilder.ToString();
    }

    private string GetValuesForInsert(object instance, ref Dictionary<string, object> parameters)
    {
        var stringBuilder = new StringBuilder();

        foreach (var propertyMap in metadataProvider.UpdateableProperties)
            stringBuilder.AppendFormat("@{0},", AddParameter(parameters, propertyMap, instance));

        return stringBuilder.ToString().RemoveFromEnd(",");
    }

    private string GetValuesForUpdate(object instance, object original, ref Dictionary<string, object> parameters)
    {
        var stringBuilder = new StringBuilder();

        var propertyMapList = FindDifferences(instance, original);

        foreach (var propertyMap in propertyMapList)
            stringBuilder.AppendFormat("{0}=@{1},", dialect.QuoteIdentifier(propertyMap.ColumnName),
                AddParameter(parameters, propertyMap, instance));

        return stringBuilder.ToString().RemoveFromEnd(",");
    }

    private IEnumerable<PropertyMap> FindDifferences(object o1, object o2)
    {
        if (o1.GetType() != o2.GetType())
            throw new ApplicationException("Objects are of different types");

        return metadataProvider.UpdateableProperties.Where(
            propertyMap => Differ(propertyMap.GetPropertyValue(o1), propertyMap.GetPropertyValue(o2))).ToList();
    }

    private bool Differ(object? v1, object? v2)
    {
        if (v1 == null && v2 == null) return false;
        if (v1 == null || v2 == null) return true;
        return !v1.Equals(v2);
    }

    private string AddParameter(Dictionary<string, object> parameters, PropertyMap propertyMap, object instance)
    {
        var paramValue = propertyMap.GetPropertyValue(instance);

        if (paramValue is DateTime dt && dt == DateTime.MinValue)
            paramValue = DBNull.Value;

        var paramName = $"P{parameters.Count + 1}";
        parameters.Add(paramName, paramValue);
        return paramName;
    }

    private string GetColumnNames(IEnumerable<PropertyMap> propertyMaps)
    {
        return string.Join(",", propertyMaps.Select(x => dialect.QuoteIdentifier(x.ColumnName)));
    }
}
