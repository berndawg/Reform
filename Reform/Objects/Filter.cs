using System;
using System.Linq.Expressions;

namespace Reform.Objects
{
    public static class Filter
    {
        public static Expression<Func<T, bool>> EqualTo<T>(string propertyName, object value)
        {
            var parameter = Expression.Parameter(typeof(T));
            var property = Expression.Property(parameter, propertyName);
            var constant = Expression.Constant(value);
            var equal = Expression.Equal(property, constant);
            return Expression.Lambda<Func<T, bool>>(equal, parameter);
        }

        public static Expression<Func<T, bool>> NotEqualTo<T>(string propertyName, object value)
        {
            var parameter = Expression.Parameter(typeof(T));
            var property = Expression.Property(parameter, propertyName);
            var constant = Expression.Constant(value);
            var notEqual = Expression.NotEqual(property, constant);
            return Expression.Lambda<Func<T, bool>>(notEqual, parameter);
        }

        public static Expression<Func<T, bool>> GreaterThan<T>(string propertyName, object value)
        {
            var parameter = Expression.Parameter(typeof(T));
            var property = Expression.Property(parameter, propertyName);
            var constant = Expression.Constant(value);
            var greaterThan = Expression.GreaterThan(property, constant);
            return Expression.Lambda<Func<T, bool>>(greaterThan, parameter);
        }

        public static Expression<Func<T, bool>> GreaterThanOrEqual<T>(string propertyName, object value)
        {
            var parameter = Expression.Parameter(typeof(T));
            var property = Expression.Property(parameter, propertyName);
            var constant = Expression.Constant(value);
            var greaterThanOrEqual = Expression.GreaterThanOrEqual(property, constant);
            return Expression.Lambda<Func<T, bool>>(greaterThanOrEqual, parameter);
        }

        public static Expression<Func<T, bool>> LessThan<T>(string propertyName, object value)
        {
            var parameter = Expression.Parameter(typeof(T));
            var property = Expression.Property(parameter, propertyName);
            var constant = Expression.Constant(value);
            var lessThan = Expression.LessThan(property, constant);
            return Expression.Lambda<Func<T, bool>>(lessThan, parameter);
        }

        public static Expression<Func<T, bool>> LessThanOrEqual<T>(string propertyName, object value)
        {
            var parameter = Expression.Parameter(typeof(T));
            var property = Expression.Property(parameter, propertyName);
            var constant = Expression.Constant(value);
            var lessThanOrEqual = Expression.LessThanOrEqual(property, constant);
            return Expression.Lambda<Func<T, bool>>(lessThanOrEqual, parameter);
        }

        public static Expression<Func<T, bool>> Contains<T>(string propertyName, string value)
        {
            var parameter = Expression.Parameter(typeof(T));
            var property = Expression.Property(parameter, propertyName);
            var constant = Expression.Constant(value);
            var contains = Expression.Call(property, typeof(string).GetMethod("Contains", new[] { typeof(string) }), constant);
            return Expression.Lambda<Func<T, bool>>(contains, parameter);
        }

        public static Expression<Func<T, bool>> StartsWith<T>(string propertyName, string value)
        {
            var parameter = Expression.Parameter(typeof(T));
            var property = Expression.Property(parameter, propertyName);
            var constant = Expression.Constant(value);
            var startsWith = Expression.Call(property, typeof(string).GetMethod("StartsWith", new[] { typeof(string) }), constant);
            return Expression.Lambda<Func<T, bool>>(startsWith, parameter);
        }

        public static Expression<Func<T, bool>> EndsWith<T>(string propertyName, string value)
        {
            var parameter = Expression.Parameter(typeof(T));
            var property = Expression.Property(parameter, propertyName);
            var constant = Expression.Constant(value);
            var endsWith = Expression.Call(property, typeof(string).GetMethod("EndsWith", new[] { typeof(string) }), constant);
            return Expression.Lambda<Func<T, bool>>(endsWith, parameter);
        }

        public static Expression<Func<T, bool>> IsNull<T>(string propertyName)
        {
            var parameter = Expression.Parameter(typeof(T));
            var property = Expression.Property(parameter, propertyName);
            var isNull = Expression.Equal(property, Expression.Constant(null));
            return Expression.Lambda<Func<T, bool>>(isNull, parameter);
        }

        public static Expression<Func<T, bool>> IsNotNull<T>(string propertyName)
        {
            var parameter = Expression.Parameter(typeof(T));
            var property = Expression.Property(parameter, propertyName);
            var isNotNull = Expression.NotEqual(property, Expression.Constant(null));
            return Expression.Lambda<Func<T, bool>>(isNotNull, parameter);
        }
    }
} 