// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Reform.Enum;
using Reform.Extensions;
using Reform.Interfaces;
using Reform.Objects;

[assembly: InternalsVisibleTo("UnitTests")]
namespace Reform.Logic
{
    internal sealed class SqlBuilder<T> : ISqlBuilder<T> where T : class
    {
        #region Fields

        private readonly IMetadataProvider<T> _metadataProvider;

        #endregion

        #region Constructor

        internal SqlBuilder(IMetadataProvider<T> metadataProvider)
        {
            _metadataProvider = metadataProvider;
        }

        #endregion

        #region ISqlBuilder Members

        public string GetCountSql(List<Filter> filters, out Dictionary<string, object> parameterDictionary)
        {
            parameterDictionary = new Dictionary<string, object>();

            string fromClause = GetFromClause();
            string whereClause = GetWhereClause(filters, ref parameterDictionary);

            string sql = $"SELECT COUNT(1){fromClause}{whereClause}";

            if (_metadataProvider.HasEncryptedFields)
                sql = WrapCommandTextInOpenCloseKey(sql);

            return sql;
        }

        public string GetExistsSql(List<Filter> filters, out Dictionary<string, object> parameterDictionary)
        {
            parameterDictionary = new Dictionary<string, object>();

            string fromClause = GetFromClause();
            string whereClause = GetWhereClause(filters, ref parameterDictionary);

            string sql = $"IF EXISTS(SELECT 1{fromClause}{whereClause}) SELECT 1 ELSE SELECT 0";

            if (_metadataProvider.HasEncryptedFields)
                sql = WrapCommandTextInOpenCloseKey(sql);

            return sql;
        }

        public string GetSelectSql(QueryCriteria queryCriteria, ref Dictionary<string, object> parameterDictionary)
        {
            PageCriteria pageCriteria = queryCriteria.PageCriteria;

            bool doPaging = pageCriteria != null && pageCriteria.PageSize != 0 && pageCriteria.Page != 0;

            string sql = doPaging
                ? GetSelectSqlPaged(pageCriteria, ref parameterDictionary, queryCriteria)
                : GetSelectSqlNonPaged(ref parameterDictionary, queryCriteria);

            if (_metadataProvider.HasEncryptedFields)
                sql = WrapCommandTextInOpenCloseKey(sql);

            return sql;
        }

        public string GetInsertSql(T instance, ref Dictionary<string, object> parameterDictionary)
        {
            string tableName = GetTableName();
            string columnNames = GetColumnNames(_metadataProvider.UpdateableProperties);
            string values = GetValuesForInsert(instance, ref parameterDictionary);

            string sql = $"INSERT INTO {tableName} ({columnNames}) VALUES ({values})";

            if (_metadataProvider.HasEncryptedFields)
                sql = WrapCommandTextInOpenCloseKey(sql);

            return sql;
        }

        public string GetUpdateSql(T instance, object original, ref Dictionary<string, object> parameterDictionary, List<Filter> filters)
        {
            string nameValuePairs = GetValuesForUpdate(instance, original, ref parameterDictionary);

            if (string.IsNullOrEmpty(nameValuePairs))
                return string.Empty;

            string tableName = GetTableName();
            string whereClause = GetWhereClause(filters, ref parameterDictionary);

            string sql = $"UPDATE {tableName} SET {nameValuePairs} {whereClause}";

            if (_metadataProvider.HasEncryptedFields)
                sql = WrapCommandTextInOpenCloseKey(sql);

            return sql;
        }

        public string GetDeleteSql(List<Filter> filters, ref Dictionary<string, object> parameterDictionary)
        {
            string fromClause = GetFromClause();
            string whereClause = GetWhereClause(filters, ref parameterDictionary);

            return $"DELETE{fromClause}{whereClause}";
        }

        public string GetMergeSql(string tempTableName, List<Filter> filters, ref Dictionary<string, object> parameterDictionary)
        {
            var stringBuilder = new StringBuilder();

            string primaryKey = _metadataProvider.PrimaryKeyColumnName;
            string columnNames = GetColumnNames(_metadataProvider.UpdateableProperties);
            string columnValues = string.Join(",", _metadataProvider.UpdateableProperties.Select(x => $"s.[{x.ColumnName}]"));
            string nameValuePairs = string.Join(",", _metadataProvider.UpdateableProperties.Select(x => $"t.[{x.ColumnName}]=s.[{x.ColumnName}]"));

            string cteQuery = GetSelectSqlNonPaged(ref parameterDictionary, new QueryCriteria { Filters = filters });

            stringBuilder.AppendLine($"WITH t AS ({cteQuery})");
            stringBuilder.AppendLine($"MERGE INTO t");
            stringBuilder.AppendLine($"USING {tempTableName} s");
            stringBuilder.AppendLine($"ON t.[{primaryKey}] = s.[{primaryKey}]");
            stringBuilder.AppendLine($"WHEN MATCHED THEN UPDATE SET {nameValuePairs}");
            stringBuilder.AppendLine($"WHEN NOT MATCHED BY TARGET THEN INSERT ({columnNames}) VALUES ({columnValues})");
            stringBuilder.AppendLine($"WHEN NOT MATCHED BY SOURCE THEN DELETE;");

            return stringBuilder.ToString();
        }

        #endregion

        #region Helpers

        private string WrapCommandTextInOpenCloseKey(string sql)
        {
            return $"{GetOpenSymmetricKeySql()}; {sql}; {GetCloseSymmetricKeySql()};";
        }

        private string GetOpenSymmetricKeySql()
        {
            return $"OPEN SYMMETRIC KEY {_metadataProvider.SymmetricKeyName} DECRYPTION BY CERTIFICATE {_metadataProvider.SymmetricKeyCertificate}";
        }

        private string GetCloseSymmetricKeySql()
        {
            return $"CLOSE SYMMETRIC KEY {_metadataProvider.SymmetricKeyName}";
        }

        private string GetSelectSqlNonPaged(ref Dictionary<string, object> parameterDictionary, QueryCriteria queryCriteria)
        {
            string columnNames = GetColumnNames(_metadataProvider.AllProperties);
            string fromClause = GetFromClause();
            string whereClause = GetWhereClause(queryCriteria.Filters, ref parameterDictionary);
            string orderByClause = GetOrderByClause(queryCriteria.SortCriteria);

            return $"SELECT {columnNames}{fromClause}{whereClause}{orderByClause}";
        }

        private string GetSelectSqlPaged(PageCriteria pageCriteria, ref Dictionary<string, object> parameterDictionary, QueryCriteria queryCriteria)
        {
            if (queryCriteria.SortCriteria.Count < 1)
                throw new ArgumentException("Paging requires at least one SortCriterion");

            string columnNames = GetColumnNames(_metadataProvider.AllProperties);
            string fromClause = GetFromClause();
            string whereClause = GetWhereClause(queryCriteria.Filters, ref parameterDictionary);
            string orderByClause = GetOrderByClause(queryCriteria.SortCriteria);

            string pagedSql = $"SELECT {columnNames}, ROW_NUMBER() OVER({orderByClause}) AS [RowNumber]{fromClause}{whereClause}";

            int lastRow = pageCriteria.Page * pageCriteria.PageSize;
            int firstRow = lastRow - pageCriteria.PageSize + 1;

            return $"WITH CTE AS ({pagedSql}) SELECT {columnNames} FROM CTE WHERE [RowNumber] BETWEEN {firstRow} AND {lastRow};";
        }

        private string GetFromClause()
        {
            return $" FROM {GetTableName()}";
        }

        private string GetTableName()
        {
            return $"[{_metadataProvider.SchemaName}].[{_metadataProvider.TableName}]";
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
                    {
                        throw new ApplicationException($"The type '{_metadataProvider.Type}' does not contain property metadata for the property '{sortCriteria.PropertyName}'");
                    }

                    stringBuilder.Append($"[{propertyMap.ColumnName}]");

                    if (sortCriteria.Direction == SortDirection.Descending)
                        stringBuilder.Append(" DESC");
                }

            if (stringBuilder.Length > 0)
                stringBuilder.Insert(0, " ORDER BY ");

            return stringBuilder.ToString();
        }

        private string GetWhereClause(List<Filter> filters, ref Dictionary<string, object> parameterDictionary)
        {
            var stringBuilder = new StringBuilder();

            BuildSql(stringBuilder, filters, ref parameterDictionary);

            if (stringBuilder.Length > 0)
                stringBuilder.Insert(0, " WHERE ");

            return stringBuilder.ToString();
        }

        private string GetValuesForInsert(object instance, ref Dictionary<string, object> parameterDictionary)
        {
            var stringBuilder = new StringBuilder();

            foreach (PropertyMap propertyMap in _metadataProvider.UpdateableProperties)
                stringBuilder.AppendFormat("{0},",
                    WrapIfEncrypted(AddParameter(parameterDictionary, propertyMap, instance), propertyMap));

            return stringBuilder.ToString().RemoveFromEnd(",");
        }

        private string GetValuesForUpdate(object instance, object original, ref Dictionary<string, object> parameterDictionary)
        {
            var stringBuilder = new StringBuilder();

            IEnumerable<PropertyMap> propertyMapList = FindDifferences(instance, original);

            foreach (PropertyMap propertyMap in propertyMapList)
                stringBuilder.AppendFormat("[{0}]={1},", propertyMap.ColumnName,
                    WrapIfEncrypted(AddParameter(parameterDictionary, propertyMap, instance), propertyMap));

            return stringBuilder.ToString().RemoveFromEnd(",");
        }

        private string WrapIfEncrypted(string parameterName, PropertyMap propertyMap)
        {
            return propertyMap.IsEncrypted ? EncryptByKey(parameterName) : "@" + parameterName;
        }

        private string EncryptByKey(string parameterName)
        {
            return $"EncryptByKey(Key_GUID('{_metadataProvider.SymmetricKeyName}'),CONVERT(VARCHAR(MAX),@{parameterName}))";
        }

        private string WrapWithDecrypt(string columnName)
        {
            return $"CONVERT(VARCHAR(MAX), DECRYPTBYKEY({columnName}))";
        }

        private IEnumerable<PropertyMap> FindDifferences(object o1, object o2)
        {
            if (o1.GetType() != o2.GetType())
                throw new ApplicationException("Objects are of different types");

            return
                _metadataProvider.UpdateableProperties.Where(
                    propertyMap => Differ(propertyMap.GetPropertyValue(o1), propertyMap.GetPropertyValue(o2))).ToList();
        }

        private bool Differ(object v1, object v2)
        {
            if (v1 == null && v2 == null)
                return false;

            if (v1 == null || v2 == null)
                return true;

            return v1.ToString() != v2.ToString();
        }

        private string AddParameter(Dictionary<string, object> parameterDictionary, PropertyMap propertyMap, object instance)
        {
            object paramValue = propertyMap.GetPropertyValue(instance);

            if (paramValue is DateTime)
            {
                if ((DateTime)paramValue == DateTime.MinValue)
                {
                    paramValue = DBNull.Value;
                }
            }

            string paramName = GetParameterName(parameterDictionary);

            parameterDictionary.Add(paramName, paramValue);

            return paramName;
        }

        private string GetParameterName(Dictionary<string, object> parameterDictionary)
        {
            return $"P{parameterDictionary.Count + 1}";
        }

        private string GetColumnNames(IEnumerable<PropertyMap> propertyMaps)
        {
            return string.Join(",", propertyMaps.Select(x => $"[{x.ColumnName}]"));
        }

        private string GetColumnNameAndWrapIfEncrypted(string propertyName, string tableAlias = null)
        {
            return GetColumnName(_metadataProvider.GetPropertyMapByPropertyName(propertyName), tableAlias);
        }

        private string GetColumnName(PropertyMap item, string tableAlias = null)
        {
            string columnName = $"[{item.ColumnName}]";

            if (!string.IsNullOrEmpty(tableAlias))
                columnName = $"{tableAlias}.{columnName}";

            if (item.IsEncrypted)
                columnName = WrapWithDecrypt(columnName);

            return columnName;
        }

        public void BuildSql(StringBuilder stringBuilder, List<Filter> filters, ref Dictionary<string, object> parameterDictonary, string tableAlias = null)
        {
            if (filters == null)
                return;

            foreach (Filter filter in filters)
            {
                if (stringBuilder.Length > 0)
                    stringBuilder.Append(" AND ");

                BuildSql(stringBuilder, filter, ref parameterDictonary, tableAlias);
            }
        }

        private void BuildSql(StringBuilder stringBuilder, Filter filter, ref Dictionary<string, object> parameterDictonary, string tableAlias = null)
        {
            switch (filter.Relationship)
            {
                case Relationship.Not:
                    BuildSqlRelationshipNot(stringBuilder, filter, ref parameterDictonary, tableAlias);
                    break;

                case Relationship.And:
                    BuildSqlRelationshipAnd(filter, stringBuilder, ref parameterDictonary, tableAlias);
                    break;

                case Relationship.Or:
                    BuildSqlRelationshipOr(stringBuilder, filter, ref parameterDictonary, tableAlias);
                    break;

                default:
                    BuildSimpleExpression(stringBuilder, filter, ref parameterDictonary, tableAlias);
                    break;
            }
        }

        private void BuildSqlRelationshipNot(StringBuilder stringBuilder, Filter criterion,
            ref Dictionary<string, object> parameterDictonary, string tableAlias = null)
        {
            stringBuilder.Append("NOT (");
            BuildSql(stringBuilder, criterion.LeftChild, ref parameterDictonary, tableAlias);
            stringBuilder.Append(")");
        }

        private void BuildSqlRelationshipOr(StringBuilder stringBuilder, Filter criterion,
            ref Dictionary<string, object> parameterDictonary, string tableAlias = null)
        {
            stringBuilder.Append('(');
            BuildSql(stringBuilder, criterion.LeftChild, ref parameterDictonary, tableAlias);
            stringBuilder.Append(" OR ");
            BuildSql(stringBuilder, criterion.RightChild, ref parameterDictonary, tableAlias);
            stringBuilder.Append(')');
        }

        private void BuildSqlRelationshipAnd(Filter criterion, StringBuilder stringBuilder,
            ref Dictionary<string, object> parameterDictonary, string tableAlias = null)
        {
            stringBuilder.Append('(');
            BuildSql(stringBuilder, criterion.LeftChild, ref parameterDictonary, tableAlias);
            stringBuilder.Append(" AND ");
            BuildSql(stringBuilder, criterion.RightChild, ref parameterDictonary, tableAlias);
            stringBuilder.Append(')');
        }

        private void BuildSimpleExpression(StringBuilder stringBuilder, Filter criterion, ref Dictionary<string, object> parameterDictonary, string tableAlias = null)
        {
            switch (criterion.Operator)
            {
                case Operator.In:
                    stringBuilder.Append(BuildInClause(criterion, parameterDictonary, true));
                    break;

                case Operator.NotIn:
                    stringBuilder.Append(BuildInClause(criterion, parameterDictonary, false));
                    break;

                case Operator.IsNull:
                    stringBuilder.AppendFormat("{0} IS NULL", GetColumnNameAndWrapIfEncrypted(criterion.PropertyName, tableAlias));
                    break;

                case Operator.IsNotNull:
                    stringBuilder.AppendFormat("{0} IS NOT NULL", GetColumnNameAndWrapIfEncrypted(criterion.PropertyName, tableAlias));
                    break;

                default:
                    string parameterName = GetParameterName(parameterDictonary);

                    parameterDictonary.Add(parameterName, criterion.PropertyValue);

                    stringBuilder.AppendFormat("{0}{1}@{2}",
                        GetColumnNameAndWrapIfEncrypted(criterion.PropertyName, tableAlias),
                        GetOperator(criterion.Operator),
                        parameterName);
                    break;
            }
        }

        private string BuildInClause(Filter criterion, Dictionary<string, object> parameterDictonary, bool isInClause)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendFormat("{0} {1} (",
                GetColumnNameAndWrapIfEncrypted(criterion.PropertyName),
                isInClause ? "IN" : "NOT IN");

            const string singleQuote = ",";

            if (criterion.PropertyValue is object[] list)
            {
                foreach (object item in list)
                {
                    string parameterName = GetParameterName(parameterDictonary);

                    parameterDictonary.Add(parameterName, item);

                    stringBuilder.Append($"@{parameterName}{singleQuote}");
                }
            }

            return stringBuilder.ToString().RemoveFromEnd(singleQuote) + ")";
        }

        private object GetOperator(Operator? op)
        {
            switch (op)
            {
                case Operator.EqualTo:
                    return "=";

                case Operator.NotEqualTo:
                    return "<>";

                case Operator.GreaterThan:
                    return ">";

                case Operator.GreaterThanOrEqualTo:
                    return ">=";

                case Operator.LessThan:
                    return "<";

                case Operator.LessThanOrEqualTo:
                    return "<=";

                case Operator.Like:
                    return " LIKE ";

                case Operator.NotLike:
                    return " NOT LIKE ";
                default:
                    throw new ApplicationException("Inconsistency");
            }
        }

        #endregion
    }
}