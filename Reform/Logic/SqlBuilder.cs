// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using Reform.Enum;
using Reform.Extensions;
using Reform.Interfaces;
using Reform.Objects;

[assembly: InternalsVisibleTo("UnitTests")]
namespace Reform.Logic
{
    public class SqlBuilder<T> : BaseSqlBuilder<T> where T : class
    {
        public SqlBuilder(IMetadataProvider<T> metadataProvider, IColumnNameFormatter columnNameFormatter, IParameterBuilder parameterBuilder)
            : base(metadataProvider, columnNameFormatter, parameterBuilder)
        {
        }

        public override string GetCountSql(Query<T> query, out Dictionary<string, object> parameterDictionary)
        {
            parameterDictionary = new Dictionary<string, object>();
            var sql = new StringBuilder();

            sql.Append("SELECT COUNT(*)");
            sql.Append(GetFromClause());
            sql.Append(BuildWhereClause(query, parameterDictionary));

            return sql.ToString();
        }

        public override string GetExistsSql(Query<T> query, out Dictionary<string, object> parameterDictionary)
        {
            parameterDictionary = new Dictionary<string, object>();
            var sql = new StringBuilder();

            sql.Append("SELECT EXISTS(SELECT 1");
            sql.Append(GetFromClause());
            sql.Append(BuildWhereClause(query, parameterDictionary));
            sql.Append(")");

            return sql.ToString();
        }

        public override string GetSelectSql(Query<T> query, out Dictionary<string, object> parameterDictionary)
        {
            parameterDictionary = new Dictionary<string, object>();
            var sql = new StringBuilder();

            sql.Append("SELECT ");
            sql.Append(GetColumnNames(_metadataProvider.AllProperties));
            sql.Append(GetFromClause());
            sql.Append(BuildWhereClause(query, parameterDictionary));
            sql.Append(BuildOrderByClause(query));
            sql.Append(BuildPaginationClause(query));

            return sql.ToString();
        }

        public override string GetInsertSql(T instance, ref Dictionary<string, object> parameterDictionary)
        {
            var properties = _metadataProvider.UpdateableProperties;
            var columnNames = GetColumnNames(properties);
            var values = GetParameterList(instance, properties, ref parameterDictionary);

            return $"INSERT INTO {GetTableName()} ({columnNames}) VALUES ({values})";
        }

        public override string GetUpdateSql(T instance, T original, Expression<Func<T, bool>> predicate, ref Dictionary<string, object> parameterDictionary)
        {
            var properties = GetChangedProperties(instance, original);
            if (!properties.Any())
                return string.Empty;

            var setClause = BuildSetClause(instance, properties, ref parameterDictionary);
            var whereClause = BuildWhereClause(predicate, ref parameterDictionary);

            return $"UPDATE {GetTableName()} SET {setClause}{whereClause}";
        }

        public override string GetUpdateSql(T instance, Query<T> query, out Dictionary<string, object> parameterDictionary)
        {
            parameterDictionary = new Dictionary<string, object>();
            var properties = _metadataProvider.UpdateableProperties;
            var setClause = BuildSetClause(instance, properties, ref parameterDictionary);
            var whereClause = BuildWhereClause(query, parameterDictionary);

            return $"UPDATE {GetTableName()} SET {setClause}{whereClause}";
        }

        public override string GetDeleteSql(Query<T> query, out Dictionary<string, object> parameterDictionary)
        {
            parameterDictionary = new Dictionary<string, object>();
            var sql = new StringBuilder();

            sql.Append("DELETE");
            sql.Append(GetFromClause());
            sql.Append(BuildWhereClause(query, parameterDictionary));

            return sql.ToString();
        }

        public override string GetMergeSql(T instance, Expression<Func<T, bool>> predicate, ref Dictionary<string, object> parameterDictionary)
        {
            var properties = _metadataProvider.UpdateableProperties;
            var columnNames = GetColumnNames(properties);
            var values = GetParameterList(instance, properties, ref parameterDictionary);
            var updateValues = BuildSetClause(instance, properties, ref parameterDictionary);

            return $"INSERT INTO {GetTableName()} ({columnNames}) VALUES ({values}) ON DUPLICATE KEY UPDATE {updateValues}";
        }

        public override string GetMergeSql(List<T> instances, Query<T> query, out Dictionary<string, object> parameterDictionary)
        {
            parameterDictionary = new Dictionary<string, object>();
            var properties = _metadataProvider.UpdateableProperties;
            var columnNames = GetColumnNames(properties);
            var valuesList = new List<string>();

            foreach (var instance in instances)
            {
                valuesList.Add($"({GetParameterList(instance, properties, ref parameterDictionary)})");
            }

            var values = string.Join(",", valuesList);
            var updateValues = BuildSetClause(instances.First(), properties, ref parameterDictionary);

            return $"INSERT INTO {GetTableName()} ({columnNames}) VALUES {values} ON DUPLICATE KEY UPDATE {updateValues}";
        }

        public override string GetLastInsertIdSql()
        {
            return "SELECT LAST_INSERT_ID()";
        }

        public override string GetTruncateTableSql()
        {
            return $"TRUNCATE TABLE {GetTableName()}";
        }

        protected override void AppendPagination(StringBuilder sql, int skip, int take)
        {
            sql.Append($" LIMIT {take} OFFSET {skip}");
        }

        protected override string FormatColumnName(string columnName)
        {
            return $"`{columnName}`";
        }

        protected override string GetColumnName(PropertyMap propertyMap)
        {
            return _columnNameFormatter.Format(propertyMap.ColumnName);
        }

        private string GetParameterList(T instance, IEnumerable<PropertyMap> properties, ref Dictionary<string, object> parameterDictionary)
        {
            var parameters = new List<string>();
            foreach (var property in properties)
            {
                parameters.Add(AddParameter(property, instance, ref parameterDictionary));
            }
            return string.Join(",", parameters);
        }

        private string AddParameter(PropertyMap property, T instance, ref Dictionary<string, object> parameterDictionary)
        {
            var value = property.GetPropertyValue(instance);
            var paramName = $"@p{parameterDictionary.Count}";
            parameterDictionary.Add(paramName, value ?? DBNull.Value);
            return paramName;
        }

        private IEnumerable<PropertyMap> GetChangedProperties(T current, T original)
        {
            var changedProperties = new List<PropertyMap>();
            foreach (var property in _metadataProvider.UpdateableProperties)
            {
                var currentValue = property.GetPropertyValue(current);
                var originalValue = property.GetPropertyValue(original);
                if (!Equals(currentValue, originalValue))
                {
                    changedProperties.Add(property);
                }
            }
            return changedProperties;
        }

        private string BuildWhereClause(Expression<Func<T, bool>> predicate, ref Dictionary<string, object> parameterDictionary)
        {
            if (predicate == null)
                return string.Empty;

            var visitor = new SqlExpressionVisitor(_columnNameFormatter);
            visitor.Visit(predicate);
            var (sql, parameters) = visitor.GetResult();

            foreach (var param in parameters)
            {
                parameterDictionary[param.Key] = param.Value;
            }

            return " WHERE " + sql;
        }

        private string BuildWhereClause(Query<T> query, Dictionary<string, object> parameterDictionary)
        {
            if (query == null || !query.WhereExpressions.Any())
                return string.Empty;

            var visitor = new SqlExpressionVisitor(_columnNameFormatter);
            var whereParts = new List<string>();

            foreach (var expr in query.WhereExpressions)
            {
                visitor.Visit(expr);
                var (sql, parameters) = visitor.GetResult();
                whereParts.Add(sql);

                foreach (var param in parameters)
                {
                    parameterDictionary[param.Key] = param.Value;
                }
            }

            return " WHERE " + string.Join(" AND ", whereParts);
        }

        private string BuildOrderByClause(Query<T> query)
        {
            if (query == null || !query.OrderByExpressions.Any())
                return string.Empty;

            var orderByParts = new List<string>();
            foreach (var expr in query.OrderByExpressions)
            {
                var visitor = new SqlExpressionVisitor(_columnNameFormatter);
                visitor.Visit(expr.KeySelector);
                var (columnName, _) = visitor.GetResult();
                orderByParts.Add($"{columnName}{(expr.Ascending ? "" : " DESC")}");
            }

            return " ORDER BY " + string.Join(", ", orderByParts);
        }

        private string BuildPaginationClause(Query<T> query)
        {
            if (query == null || !query.TakeCount.HasValue)
                return string.Empty;

            return $" LIMIT {query.TakeCount.Value} OFFSET {query.SkipCount ?? 0}";
        }

        private string BuildSetClause(T instance, IEnumerable<PropertyMap> properties, ref Dictionary<string, object> parameterDictionary)
        {
            var setClauses = new List<string>();
            foreach (var property in properties)
            {
                var paramName = AddParameter(property, instance, ref parameterDictionary);
                setClauses.Add($"{FormatColumnName(property.ColumnName)} = {paramName}");
            }
            return string.Join(",", setClauses);
        }
    }
}