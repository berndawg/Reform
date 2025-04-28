using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Reform.Objects
{
    public class AggregateQuery<T> where T : class
    {
        private readonly Query<T> _baseQuery;
        private readonly List<(string Function, Expression<Func<T, object>> Property, string Alias)> _aggregates;
        private readonly List<Expression<Func<T, object>>> _groupBy;
        private Expression<Func<T, bool>> _having;

        public AggregateQuery(Query<T> baseQuery)
        {
            _baseQuery = baseQuery;
            _aggregates = new List<(string, Expression<Func<T, object>>, string)>();
            _groupBy = new List<Expression<Func<T, object>>>();
        }

        public AggregateQuery<T> Count(Expression<Func<T, object>> property, string alias = null)
        {
            _aggregates.Add(("COUNT", property, alias ?? $"Count_{_aggregates.Count}"));
            return this;
        }

        public AggregateQuery<T> Sum(Expression<Func<T, object>> property, string alias = null)
        {
            _aggregates.Add(("SUM", property, alias ?? $"Sum_{_aggregates.Count}"));
            return this;
        }

        public AggregateQuery<T> Avg(Expression<Func<T, object>> property, string alias = null)
        {
            _aggregates.Add(("AVG", property, alias ?? $"Avg_{_aggregates.Count}"));
            return this;
        }

        public AggregateQuery<T> Min(Expression<Func<T, object>> property, string alias = null)
        {
            _aggregates.Add(("MIN", property, alias ?? $"Min_{_aggregates.Count}"));
            return this;
        }

        public AggregateQuery<T> Max(Expression<Func<T, object>> property, string alias = null)
        {
            _aggregates.Add(("MAX", property, alias ?? $"Max_{_aggregates.Count}"));
            return this;
        }

        public AggregateQuery<T> GroupBy(Expression<Func<T, object>> property)
        {
            _groupBy.Add(property);
            return this;
        }

        public AggregateQuery<T> Having(Expression<Func<T, bool>> predicate)
        {
            _having = predicate;
            return this;
        }

        public IReadOnlyList<(string Function, Expression<Func<T, object>> Property, string Alias)> Aggregates => _aggregates.AsReadOnly();
        public IReadOnlyList<Expression<Func<T, object>>> GroupByExpressions => _groupBy.AsReadOnly();
        public Expression<Func<T, bool>> HavingExpression => _having;
        public Query<T> BaseQuery => _baseQuery;
    }
} 