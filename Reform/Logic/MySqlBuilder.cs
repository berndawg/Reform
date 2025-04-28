using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Reform.Interfaces;
using Reform.Objects;

namespace Reform.Logic
{
    public sealed class MySqlBuilder<T> : BaseSqlBuilder<T> where T : class
    {
        public MySqlBuilder(IMetadataProvider<T> metadataProvider, IColumnNameFormatter columnNameFormatter, IParameterBuilder parameterBuilder) 
            : base(metadataProvider, columnNameFormatter, parameterBuilder)
        {
        }

        public override string GetCountSql(Query<T> query, out Dictionary<string, object> parameterDictionary)
        {
            parameterDictionary = new Dictionary<string, object>();
            return $"SELECT COUNT(1){GetFromClause()}{GetWhereClause(query, parameterDictionary)}";
        }

        public override string GetExistsSql(Query<T> query, out Dictionary<string, object> parameterDictionary)
        {
            parameterDictionary = new Dictionary<string, object>();
            return $"SELECT EXISTS(SELECT 1{GetFromClause()}{GetWhereClause(query, parameterDictionary)})";
        }

        public override string GetSelectSql(Query<T> query, out Dictionary<string, object> parameterDictionary)
        {
            parameterDictionary = new Dictionary<string, object>();
            string columnNames = GetColumnNames(_metadataProvider.AllProperties);
            string fromClause = GetFromClause();
            string whereClause = GetWhereClause(query, parameterDictionary);
            string orderByClause = GetOrderByClause(query);

            var sql = new StringBuilder($"SELECT {columnNames}{fromClause}{whereClause}{orderByClause}");
            
            if (query.TakeCount.HasValue)
            {
                AppendPagination(sql, query.SkipCount ?? 0, query.TakeCount.Value);
            }

            return sql.ToString();
        }

        public override string GetInsertSql(T instance, ref Dictionary<string, object> parameterDictionary)
        {
            if (parameterDictionary == null)
                parameterDictionary = new Dictionary<string, object>();
                
            var insertableProperties = _metadataProvider.AllProperties.Where(x => !x.IsPrimaryKey).ToList();
            var columnNames = GetColumnNames(insertableProperties);
            var parameters = new List<string>();
            foreach (var property in insertableProperties)
            {
                parameters.Add(AddParameter(parameterDictionary, property, property.GetPropertyValue(instance)));
            }
            var parameterNames = string.Join(",", parameters);

            return $"INSERT INTO {GetTableName()} ({columnNames}) VALUES ({parameterNames})";
        }

        public override string GetUpdateSql(T instance, T original, Expression<Func<T, bool>> predicate, ref Dictionary<string, object> parameterDictionary)
        {
            if (parameterDictionary == null)
                parameterDictionary = new Dictionary<string, object>();
                
            var differences = FindDifferences(instance, original).ToList();
            if (!differences.Any())
                return string.Empty;

            var setClauses = new List<string>();
            foreach (var property in differences)
            {
                var paramName = AddParameter(parameterDictionary, property, property.GetPropertyValue(instance));
                setClauses.Add($"{GetColumnName(property)} = {paramName}");
            }
            var setClause = string.Join(",", setClauses);
            
            string whereClause = predicate != null ? GetWhereClause(new Query<T>().Where(predicate), parameterDictionary) : string.Empty;

            return $"UPDATE {GetTableName()} SET {setClause}{whereClause}";
        }

        public override string GetUpdateSql(T instance, Query<T> query, out Dictionary<string, object> parameterDictionary)
        {
            parameterDictionary = new Dictionary<string, object>();
            var updateableProperties = _metadataProvider.UpdateableProperties.ToList();
            var setClauses = new List<string>();
            foreach (var property in updateableProperties)
            {
                var paramName = AddParameter(parameterDictionary, property, property.GetPropertyValue(instance));
                setClauses.Add($"{GetColumnName(property)} = {paramName}");
            }
            var setClause = string.Join(",", setClauses);
            string whereClause = GetWhereClause(query, parameterDictionary);

            return $"UPDATE {GetTableName()} SET {setClause}{whereClause}";
        }

        public override string GetDeleteSql(Query<T> query, out Dictionary<string, object> parameterDictionary)
        {
            parameterDictionary = new Dictionary<string, object>();
            string whereClause = GetWhereClause(query, parameterDictionary);
            return $"DELETE FROM {GetTableName()}{whereClause}";
        }

        public override string GetMergeSql(T instance, Expression<Func<T, bool>> predicate, ref Dictionary<string, object> parameterDictionary)
        {
            if (parameterDictionary == null)
                parameterDictionary = new Dictionary<string, object>();
                
            var insertableProperties = _metadataProvider.AllProperties.Where(x => !x.IsPrimaryKey).ToList();
            var columnNames = GetColumnNames(insertableProperties);
            
            var parameters = new List<string>();
            foreach (var property in insertableProperties)
            {
                parameters.Add(AddParameter(parameterDictionary, property, property.GetPropertyValue(instance)));
            }
            var parameterNames = string.Join(",", parameters);

            var updateProperties = _metadataProvider.UpdateableProperties.ToList();
            var setClauses = new List<string>();
            foreach (var property in updateProperties)
            {
                setClauses.Add($"{GetColumnName(property)} = VALUES({GetColumnName(property)})");
            }
            var setClause = string.Join(",", setClauses);

            return $"INSERT INTO {GetTableName()} ({columnNames}) VALUES ({parameterNames}) ON DUPLICATE KEY UPDATE {setClause}";
        }

        public override string GetMergeSql(List<T> instances, Query<T> query, out Dictionary<string, object> parameterDictionary)
        {
            parameterDictionary = new Dictionary<string, object>();
            var insertableProperties = _metadataProvider.AllProperties.Where(x => !x.IsPrimaryKey).ToList();
            var columnNames = GetColumnNames(insertableProperties);
            
            var valuesClauses = new List<string>();
            foreach (var instance in instances)
            {
                var parameters = new List<string>();
                foreach (var property in insertableProperties)
                {
                    parameters.Add(AddParameter(parameterDictionary, property, property.GetPropertyValue(instance)));
                }
                valuesClauses.Add($"({string.Join(",", parameters)})");
            }

            var updateProperties = _metadataProvider.UpdateableProperties.ToList();
            var setClauses = new List<string>();
            foreach (var property in updateProperties)
            {
                setClauses.Add($"{GetColumnName(property)} = VALUES({GetColumnName(property)})");
            }
            var setClause = string.Join(",", setClauses);

            return $"INSERT INTO {GetTableName()} ({columnNames}) VALUES {string.Join(",", valuesClauses)} ON DUPLICATE KEY UPDATE {setClause}";
        }

        public override string GetLastInsertIdSql()
        {
            return "SELECT LAST_INSERT_ID()";
        }

        public override string GetTruncateTableSql()
        {
            return $"TRUNCATE TABLE {GetTableName()}";
        }

        protected override string GetColumnName(PropertyMap propertyMap)
        {
            return _columnNameFormatter.Format(propertyMap.ColumnName);
        }

        protected override string FormatColumnName(string columnName)
        {
            return $"`{columnName}`";
        }

        protected override void AppendPagination(StringBuilder sql, int skip, int take)
        {
            sql.Append($" LIMIT {take} OFFSET {skip}");
        }

        private IEnumerable<PropertyMap> FindDifferences(object o1, object o2)
        {
            if (o1.GetType() != o2.GetType())
                throw new ApplicationException("Objects are of different types");

            return _metadataProvider.UpdateableProperties.Where(
                propertyMap => Differ(propertyMap.GetPropertyValue(o1), propertyMap.GetPropertyValue(o2))).ToList();
        }

        private bool Differ(object value1, object value2)
        {
            if (value1 == null && value2 == null)
                return false;

            if (value1 == null || value2 == null)
                return true;

            return !value1.Equals(value2);
        }

        private string GetOrderByClause(Query<T> query)
        {
            if (!query.OrderByExpressions.Any())
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

        private string GetWhereClause(Query<T> query, Dictionary<string, object> parameterDictionary)
        {
            if (!query.WhereExpressions.Any())
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

        private string AddParameter(Dictionary<string, object> parameterDictionary, PropertyMap property, object value)
        {
            var paramName = _parameterBuilder.GetParameterName(property.ColumnName, parameterDictionary.Count);
            parameterDictionary[paramName] = value ?? DBNull.Value;
            return paramName;
        }
    }
} 