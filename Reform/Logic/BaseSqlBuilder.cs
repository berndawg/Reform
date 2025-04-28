using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Reform.Interfaces;
using Reform.Objects;

namespace Reform.Logic
{
    public abstract class BaseSqlBuilder<T> : ISqlBuilder<T> where T : class
    {
        protected readonly IMetadataProvider<T> _metadataProvider;
        protected readonly IColumnNameFormatter _columnNameFormatter;
        protected readonly IParameterBuilder _parameterBuilder;

        protected BaseSqlBuilder(IMetadataProvider<T> metadataProvider, IColumnNameFormatter columnNameFormatter, IParameterBuilder parameterBuilder)
        {
            _metadataProvider = metadataProvider;
            _columnNameFormatter = columnNameFormatter;
            _parameterBuilder = parameterBuilder;
        }

        public abstract string GetCountSql(Query<T> query, out Dictionary<string, object> parameterDictionary);
        public abstract string GetExistsSql(Query<T> query, out Dictionary<string, object> parameterDictionary);
        public abstract string GetSelectSql(Query<T> query, out Dictionary<string, object> parameterDictionary);
        public abstract string GetInsertSql(T instance, ref Dictionary<string, object> parameterDictionary);
        public abstract string GetUpdateSql(T instance, T original, Expression<Func<T, bool>> predicate, ref Dictionary<string, object> parameterDictionary);
        public abstract string GetUpdateSql(T instance, Query<T> query, out Dictionary<string, object> parameterDictionary);
        public abstract string GetDeleteSql(Query<T> query, out Dictionary<string, object> parameterDictionary);
        public abstract string GetMergeSql(T instance, Expression<Func<T, bool>> predicate, ref Dictionary<string, object> parameterDictionary);
        public abstract string GetMergeSql(List<T> instances, Query<T> query, out Dictionary<string, object> parameterDictionary);
        public abstract string GetLastInsertIdSql();
        public abstract string GetTruncateTableSql();

        public string GetAggregateSql(AggregateQuery<T> query, out Dictionary<string, object> parameterDictionary)
        {
            parameterDictionary = new Dictionary<string, object>();
            var visitor = new SqlExpressionVisitor(_columnNameFormatter);
            var sql = new StringBuilder();

            // Select clause with aggregates
            sql.Append("SELECT ");
            for (int i = 0; i < query.Aggregates.Count; i++)
            {
                if (i > 0) sql.Append(", ");
                var (function, property, alias) = query.Aggregates[i];
                visitor.VisitAggregate(function, property?.Body, alias);
            }

            // Add group by columns to select clause if present
            if (query.GroupByExpressions.Any())
            {
                if (query.Aggregates.Any()) sql.Append(", ");
                for (int i = 0; i < query.GroupByExpressions.Count; i++)
                {
                    if (i > 0) sql.Append(", ");
                    visitor.Visit(query.GroupByExpressions[i].Body);
                }
            }

            sql.Append(GetFromClause());

            // Where clause from base query
            if (query.BaseQuery.WhereExpressions.Any())
            {
                sql.Append(" WHERE ");
                for (int i = 0; i < query.BaseQuery.WhereExpressions.Count; i++)
                {
                    if (i > 0) sql.Append(" AND ");
                    visitor.Visit(query.BaseQuery.WhereExpressions[i]);
                    var (whereSql, parameters) = visitor.GetResult();
                    sql.Append(whereSql);
                    foreach (var param in parameters)
                        parameterDictionary.Add(param.Key, param.Value);
                }
            }

            // Group by clause
            if (query.GroupByExpressions.Any())
            {
                sql.Append(" GROUP BY ");
                for (int i = 0; i < query.GroupByExpressions.Count; i++)
                {
                    if (i > 0) sql.Append(", ");
                    visitor.Visit(query.GroupByExpressions[i].Body);
                }
            }

            // Having clause
            if (query.HavingExpression != null)
            {
                sql.Append(" HAVING ");
                visitor.Visit(query.HavingExpression);
                var (havingSql, parameters) = visitor.GetResult();
                sql.Append(havingSql);
                foreach (var param in parameters)
                    parameterDictionary.Add(param.Key, param.Value);
            }

            return sql.ToString();
        }

        protected string GetFromClause()
        {
            return $" FROM {GetTableName()}";
        }

        protected string GetTableName()
        {
            return $"{_metadataProvider.SchemaName}.{_metadataProvider.TableName}";
        }

        protected string GetColumnNames(IEnumerable<PropertyMap> propertyMaps)
        {
            return string.Join(",", propertyMaps.Select(x => FormatColumnName(x.ColumnName)));
        }

        protected abstract string GetColumnName(PropertyMap propertyMap);

        protected abstract void AppendPagination(StringBuilder sql, int skip, int take);

        protected virtual string BuildWhereClause<T>(Query<T> query, IDbCommand command) where T : class
        {
            if (query == null || !query.HasFilters)
                return string.Empty;

            var whereClause = new System.Text.StringBuilder(" WHERE ");
            var visitor = new SqlExpressionVisitor(_columnNameFormatter);
            var whereParts = new List<string>();

            foreach (var expr in query.WhereExpressions)
            {
                visitor.Visit(expr);
                var (sql, parameters) = visitor.GetResult();
                whereParts.Add(sql);

                foreach (var param in parameters)
                {
                    var dbParam = command.CreateParameter();
                    dbParam.ParameterName = param.Key;
                    dbParam.Value = param.Value ?? DBNull.Value;
                    command.Parameters.Add(dbParam);
                }
            }

            whereClause.Append(string.Join(" AND ", whereParts));
            return whereClause.ToString();
        }

        protected virtual string BuildOrderByClause<T>(Query<T> query) where T : class
        {
            if (query == null || !query.OrderByExpressions.Any())
                return string.Empty;

            var orderByClause = new System.Text.StringBuilder(" ORDER BY ");
            var visitor = new SqlExpressionVisitor(_columnNameFormatter);
            var sortings = new List<string>();

            foreach (var (expr, ascending) in query.OrderByExpressions)
            {
                visitor.Visit(expr);
                var (columnName, _) = visitor.GetResult();
                sortings.Add($"{columnName} {(ascending ? "ASC" : "DESC")}");
            }

            orderByClause.Append(string.Join(", ", sortings));
            return orderByClause.ToString();
        }

        protected abstract string FormatColumnName(string columnName);

        protected virtual string BuildPaginationClause<T>(Query<T> query) where T : class
        {
            if (!query.TakeCount.HasValue)
                return string.Empty;

            var sql = new StringBuilder();
            AppendPagination(sql, query.SkipCount ?? 0, query.TakeCount.Value);
            return sql.ToString();
        }
    }
} 