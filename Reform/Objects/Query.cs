using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Reform.Objects
{
    public class Query<T> where T : class
    {
        private List<Expression<Func<T, bool>>> _whereExpressions;
        private List<(Expression<Func<T, object>> KeySelector, bool Ascending)> _orderByExpressions;
        private int? _skip;
        private int? _take;

        public Query()
        {
            _whereExpressions = new List<Expression<Func<T, bool>>>();
            _orderByExpressions = new List<(Expression<Func<T, object>>, bool)>();
        }

        public Query<T> Where(Expression<Func<T, bool>> predicate)
        {
            _whereExpressions.Add(predicate);
            return this;
        }

        public Query<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            _orderByExpressions.Add((Expression.Lambda<Func<T, object>>(
                Expression.Convert(keySelector.Body, typeof(object)),
                keySelector.Parameters), true));
            return this;
        }

        public Query<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            _orderByExpressions.Add((Expression.Lambda<Func<T, object>>(
                Expression.Convert(keySelector.Body, typeof(object)),
                keySelector.Parameters), false));
            return this;
        }

        public Query<T> Skip(int count)
        {
            _skip = count;
            return this;
        }

        public Query<T> Take(int count)
        {
            _take = count;
            return this;
        }

        public IReadOnlyList<Expression<Func<T, bool>>> WhereExpressions => _whereExpressions.AsReadOnly();
        public IReadOnlyList<(Expression<Func<T, object>> KeySelector, bool Ascending)> OrderByExpressions => _orderByExpressions.AsReadOnly();
        public int? SkipCount => _skip;
        public int? TakeCount => _take;
        public bool HasFilters => _whereExpressions.Any();
    }
} 