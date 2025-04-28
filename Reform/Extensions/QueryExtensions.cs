using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Reform.Objects;

namespace Reform.Extensions
{
    public static class QueryExtensions
    {
        public static AggregateQuery<T> Aggregate<T>(this Query<T> query) where T : class
        {
            return new AggregateQuery<T>(query);
        }

        public static AggregateQuery<T> Count<T>(this Query<T> query, Expression<Func<T, object>> property = null, string alias = null) where T : class
        {
            return query.Aggregate().Count(property, alias);
        }

        public static AggregateQuery<T> Sum<T>(this Query<T> query, Expression<Func<T, object>> property, string alias = null) where T : class
        {
            return query.Aggregate().Sum(property, alias);
        }

        public static AggregateQuery<T> Avg<T>(this Query<T> query, Expression<Func<T, object>> property, string alias = null) where T : class
        {
            return query.Aggregate().Avg(property, alias);
        }

        public static AggregateQuery<T> Min<T>(this Query<T> query, Expression<Func<T, object>> property, string alias = null) where T : class
        {
            return query.Aggregate().Min(property, alias);
        }

        public static AggregateQuery<T> Max<T>(this Query<T> query, Expression<Func<T, object>> property, string alias = null) where T : class
        {
            return query.Aggregate().Max(property, alias);
        }

        public static AggregateQuery<T> GroupBy<T>(this AggregateQuery<T> query, Expression<Func<T, object>> property) where T : class
        {
            return query.GroupBy(property);
        }

        public static AggregateQuery<T> Having<T>(this AggregateQuery<T> query, Expression<Func<T, bool>> predicate) where T : class
        {
            return query.Having(predicate);
        }

        public static Query<T> WhereBetween<T, TProperty>(this Query<T> query, Expression<Func<T, TProperty>> property, TProperty start, TProperty end) where T : class
        {
            var parameter = Expression.Parameter(typeof(T));
            var propertyAccess = Expression.Invoke(property, parameter);
            
            var greaterThanOrEqual = Expression.GreaterThanOrEqual(propertyAccess, Expression.Constant(start));
            var lessThanOrEqual = Expression.LessThanOrEqual(propertyAccess, Expression.Constant(end));
            
            var combinedExpression = Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);
            var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
            
            return query.Where(lambda);
        }

        public static Query<T> WhereIn<T, TProperty>(this Query<T> query, Expression<Func<T, TProperty>> property, IEnumerable<TProperty> values) where T : class
        {
            if (!values.Any())
                return query;

            var parameter = Expression.Parameter(typeof(T));
            var propertyAccess = Expression.Invoke(property, parameter);
            
            Expression combinedExpression = null;
            foreach (var value in values)
            {
                var equalExpression = Expression.Equal(propertyAccess, Expression.Constant(value));
                combinedExpression = combinedExpression == null 
                    ? equalExpression 
                    : Expression.OrElse(combinedExpression, equalExpression);
            }
            
            var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
            return query.Where(lambda);
        }

        public static Query<T> WhereIsNull<T, TProperty>(this Query<T> query, Expression<Func<T, TProperty>> property) where T : class
        {
            var parameter = Expression.Parameter(typeof(T));
            var propertyAccess = Expression.Invoke(property, parameter);
            var nullExpression = Expression.Equal(propertyAccess, Expression.Constant(null));
            var lambda = Expression.Lambda<Func<T, bool>>(nullExpression, parameter);
            
            return query.Where(lambda);
        }

        public static Query<T> WhereIsNotNull<T, TProperty>(this Query<T> query, Expression<Func<T, TProperty>> property) where T : class
        {
            var parameter = Expression.Parameter(typeof(T));
            var propertyAccess = Expression.Invoke(property, parameter);
            var notNullExpression = Expression.NotEqual(propertyAccess, Expression.Constant(null));
            var lambda = Expression.Lambda<Func<T, bool>>(notNullExpression, parameter);
            
            return query.Where(lambda);
        }

        public static AggregateQuery<T> Having<T>(this AggregateQuery<T> query, string condition, Dictionary<string, object> parameters) where T : class
        {
            return query.Having(condition, parameters);
        }
    }
} 