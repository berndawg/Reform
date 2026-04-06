using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using Reform.Enum;
using Reform.Interfaces;
using Reform.Objects;

[assembly: InternalsVisibleTo("ReformTests")]
namespace Reform.Logic
{
    public sealed class SqlBuilder<T> : ISqlBuilder<T> where T : class
    {
        private readonly IMetadataProvider<T> _metadataProvider;
        private readonly IDialect _dialect;
        private readonly WhereClauseBuilder<T> _whereClauseBuilder;

        public SqlBuilder(IMetadataProvider<T> metadataProvider, IDialect dialect)
        {
            _metadataProvider = metadataProvider;
            _dialect = dialect;
            _whereClauseBuilder = new WhereClauseBuilder<T>(metadataProvider, dialect);
        }

        public string GetCountSql(Expression<Func<T, bool>> predicate, out Dictionary<string, object> parameters)
        {
            var (whereClause, whereParams) = BuildWhereClause(predicate);
            parameters = whereParams;

            string fromClause = GetFromClause();
            string where = string.IsNullOrEmpty(whereClause) ? "" : $" WHERE {whereClause}";

            return $"SELECT COUNT(1){fromClause}{where}";
        }

        public string GetExistsSql(Expression<Func<T, bool>> predicate, out Dictionary<string, object> parameters)
        {
            var (whereClause, whereParams) = BuildWhereClause(predicate);
            parameters = whereParams;

            string fromClause = GetFromClause();
            string where = string.IsNullOrEmpty(whereClause) ? "" : $" WHERE {whereClause}";

            return _dialect.GetExistsSql($"SELECT 1{fromClause}{where}");
        }

        public string GetSelectSql(QueryCriteria<T> queryCriteria, ref Dictionary<string, object> parameters)
        {
            PageCriteria pageCriteria = queryCriteria.PageCriteria;
            bool doPaging = pageCriteria != null && pageCriteria.PageSize != 0 && pageCriteria.Page != 0;

            return doPaging
                ? GetSelectSqlPaged(pageCriteria, ref parameters, queryCriteria)
                : GetSelectSqlNonPaged(ref parameters, queryCriteria);
        }

        public string GetInsertSql(T instance, ref Dictionary<string, object> parameters)
        {
            string tableName = GetTableName();
            string columnNames = GetColumnNames(_metadataProvider.UpdateableProperties);
            string values = GetValuesForInsert(instance, ref parameters);

            return $"INSERT INTO {tableName} ({columnNames}) VALUES ({values})";
        }

        public string GetUpdateSql(T instance, T original, ref Dictionary<string, object> parameters,
                                    Expression<Func<T, bool>> predicate)
        {
            string nameValuePairs = GetValuesForUpdate(instance, original, ref parameters);

            if (string.IsNullOrEmpty(nameValuePairs))
                return string.Empty;

            string tableName = GetTableName();

            var (whereClause, whereParams) = BuildWhereClause(predicate, parameters.Count);
            foreach (var kvp in whereParams)
                parameters.Add(kvp.Key, kvp.Value);

            string where = string.IsNullOrEmpty(whereClause) ? "" : $" WHERE {whereClause}";

            return $"UPDATE {tableName} SET {nameValuePairs}{where}";
        }

        public string GetDeleteSql(Expression<Func<T, bool>> predicate, ref Dictionary<string, object> parameters)
        {
            string fromClause = GetFromClause();

            var (whereClause, whereParams) = BuildWhereClause(predicate, parameters.Count);
            foreach (var kvp in whereParams)
                parameters.Add(kvp.Key, kvp.Value);

            string where = string.IsNullOrEmpty(whereClause) ? "" : $" WHERE {whereClause}";

            return $"DELETE{fromClause}{where}";
        }

        public string GetTruncateSql()
        {
            return _dialect.GetTruncateSql(GetTableName());
        }

        private (string sql, Dictionary<string, object> parameters) BuildWhereClause(Expression<Func<T, bool>> predicate, int startingIndex = 0)
        {
            return _whereClauseBuilder.Build(predicate, startingIndex);
        }

        private string GetSelectSqlNonPaged(ref Dictionary<string, object> parameters, QueryCriteria<T> queryCriteria)
        {
            string columnNames = GetColumnNames(_metadataProvider.AllProperties);
            string fromClause = GetFromClause();

            var (whereClause, whereParams) = BuildWhereClause(queryCriteria.Predicate);
            foreach (var kvp in whereParams)
                parameters.Add(kvp.Key, kvp.Value);

            string where = string.IsNullOrEmpty(whereClause) ? "" : $" WHERE {whereClause}";
            string orderByClause = GetOrderByClause(queryCriteria.SortCriteria);

            return $"SELECT {columnNames}{fromClause}{where}{orderByClause}";
        }

        private string GetSelectSqlPaged(PageCriteria pageCriteria, ref Dictionary<string, object> parameters, QueryCriteria<T> queryCriteria)
        {
            if (queryCriteria.SortCriteria.Count < 1)
                throw new ArgumentException("Paging requires at least one SortCriterion");

            string columnNames = GetColumnNames(_metadataProvider.AllProperties);
            string fromClause = GetFromClause();

            var (whereClause, whereParams) = BuildWhereClause(queryCriteria.Predicate);
            foreach (var kvp in whereParams)
                parameters.Add(kvp.Key, kvp.Value);

            string where = string.IsNullOrEmpty(whereClause) ? "" : $" WHERE {whereClause}";
            string orderByClause = GetOrderByClause(queryCriteria.SortCriteria);

            int offset = (pageCriteria.Page - 1) * pageCriteria.PageSize;
            int limit = pageCriteria.PageSize;

            return $"SELECT {columnNames}{fromClause}{where}{orderByClause} {_dialect.GetPagingSql(limit, offset)}";
        }

        private string GetFromClause()
        {
            return $" FROM {GetTableName()}";
        }

        private string GetTableName()
        {
            return _dialect.QuoteIdentifier(_metadataProvider.TableName);
        }

        private string GetOrderByClause(IEnumerable<SortCriterion> sortCriteriaList)
        {
            var stringBuilder = new StringBuilder();

            if (sortCriteriaList != null)
                foreach (SortCriterion sortCriteria in sortCriteriaList)
                {
                    if (stringBuilder.Length > 0)
                        stringBuilder.Append(", ");

                    PropertyMap propertyMap = _metadataProvider.GetPropertyMapByPropertyName(sortCriteria.PropertyName);

                    if (propertyMap == null)
                        throw new InvalidOperationException($"The type '{_metadataProvider.Type}' does not contain property metadata for the property '{sortCriteria.PropertyName}'.");

                    stringBuilder.Append(_dialect.QuoteIdentifier(propertyMap.ColumnName));

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
            bool first = true;

            foreach (PropertyMap propertyMap in _metadataProvider.UpdateableProperties)
            {
                if (!first) stringBuilder.Append(',');
                first = false;
                stringBuilder.Append('@').Append(AddParameter(parameters, propertyMap, instance));
            }

            return stringBuilder.ToString();
        }

        private string GetValuesForUpdate(T instance, T original, ref Dictionary<string, object> parameters)
        {
            var stringBuilder = new StringBuilder();
            bool first = true;

            IEnumerable<PropertyMap> propertyMapList = FindDifferences(instance, original);

            foreach (PropertyMap propertyMap in propertyMapList)
            {
                if (!first) stringBuilder.Append(',');
                first = false;
                stringBuilder.Append(_dialect.QuoteIdentifier(propertyMap.ColumnName))
                    .Append("=@")
                    .Append(AddParameter(parameters, propertyMap, instance));
            }

            return stringBuilder.ToString();
        }

        private IEnumerable<PropertyMap> FindDifferences(T instance, T original)
        {
            return _metadataProvider.UpdateableProperties.Where(
                propertyMap => Differ(propertyMap.GetPropertyValue(instance), propertyMap.GetPropertyValue(original))).ToList();
        }

        private bool Differ(object v1, object v2)
        {
            if (v1 == null && v2 == null) return false;
            if (v1 == null || v2 == null) return true;
            return !v1.Equals(v2);
        }

        private string AddParameter(Dictionary<string, object> parameters, PropertyMap propertyMap, object instance)
        {
            object paramValue = propertyMap.GetPropertyValue(instance);

            string paramName = $"P{parameters.Count + 1}";
            parameters.Add(paramName, paramValue);
            return paramName;
        }

        private string GetColumnNames(IEnumerable<PropertyMap> propertyMaps)
        {
            return string.Join(",", propertyMaps.Select(x => _dialect.QuoteIdentifier(x.ColumnName)));
        }
    }
}
