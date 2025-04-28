using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reform.Interfaces;
using Reform.Logic;
using Reform.Objects;
using Reform.Extensions;

namespace Reform.Tests
{
    [TestClass]
    public class QueryTests
    {
        private class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal Amount { get; set; }
            public DateTime Date { get; set; }
            public string Status { get; set; }
            public int? NullableValue { get; set; }
        }

        private IColumnNameFormatter _mysqlFormatter;
        private SqlExpressionVisitor _mysqlVisitor;

        // Common test expressions
        private static readonly Expression<Func<TestEntity, bool>> IdEquals1 = e => e.Id == 1;
        private static readonly Expression<Func<TestEntity, bool>> NameEqualsTest = e => e.Name == "Test";
        private static readonly Expression<Func<TestEntity, bool>> AmountGreaterThan100 = e => e.Amount > 100;
        private static readonly Expression<Func<TestEntity, bool>> StatusNotPending = e => e.Status != "Pending";
        private static readonly Expression<Func<TestEntity, bool>> DateAfterToday = e => e.Date >= DateTime.Today;
        private static readonly Expression<Func<TestEntity, bool>> NullableValueIsNull = e => e.NullableValue == null;
        private static readonly Expression<Func<TestEntity, bool>> NameContainsTest = e => e.Name.Contains("test");
        private static readonly Expression<Func<TestEntity, bool>> NameStartsWithA = e => e.Name.StartsWith("A");
        private static readonly Expression<Func<TestEntity, bool>> NameEndsWithZ = e => e.Name.EndsWith("Z");

        // Order by expressions
        private static readonly Expression<Func<TestEntity, object>> OrderByName = e => e.Name;
        private static readonly Expression<Func<TestEntity, object>> OrderByDate = e => e.Date;

        [TestInitialize]
        public void Setup()
        {
            _mysqlFormatter = new MySqlColumnNameFormatter();
            _mysqlVisitor = new SqlExpressionVisitor(_mysqlFormatter);
        }

        [TestMethod]
        public void BasicWhereClause_GeneratesCorrectSql()
        {
            var query = new Query<TestEntity>()
                .Where(IdEquals1)
                .Where(NameEqualsTest);

            AssertSqlGeneration(
                query.WhereExpressions[0],
                "(`Id` = @p1)",
                new { p1 = 1 });

            AssertSqlGeneration(
                query.WhereExpressions[1],
                "(`Name` = @p1)",
                new { p1 = "Test" });
        }

        [TestMethod]
        public void ComplexWhereClause_GeneratesCorrectSql()
        {
            var complexCondition1 = CombineExpressions(AmountGreaterThan100, StatusNotPending, ExpressionType.AndAlso);
            var complexCondition2 = CombineExpressions(DateAfterToday, NullableValueIsNull, ExpressionType.OrElse);

            var query = new Query<TestEntity>()
                .Where(complexCondition1)
                .Where(complexCondition2);

            AssertSqlGeneration(
                query.WhereExpressions[0],
                "((`Amount` > @p1) AND (`Status` <> @p2))",
                new { p1 = 100m, p2 = "Pending" });
        }

        [TestMethod]
        public void StringOperations_GeneratesCorrectSql()
        {
            var query = new Query<TestEntity>()
                .Where(NameContainsTest)
                .Where(NameStartsWithA)
                .Where(NameEndsWithZ);

            AssertSqlGeneration(
                query.WhereExpressions[0],
                "(`Name` LIKE CONCAT('%', @p1, '%'))",
                new { p1 = "test" });

            AssertSqlGeneration(
                query.WhereExpressions[1],
                "(`Name` LIKE CONCAT(@p1, '%'))",
                new { p1 = "A" });

            AssertSqlGeneration(
                query.WhereExpressions[2],
                "(`Name` LIKE CONCAT('%', @p1))",
                new { p1 = "Z" });
        }

        [TestMethod]
        public void OrderByClause_GeneratesCorrectSql()
        {
            var query = new Query<TestEntity>()
                .OrderBy(OrderByName)
                .OrderByDescending(OrderByDate);

            var orderBy = query.OrderByExpressions;
            Assert.AreEqual(2, orderBy.Count);
            Assert.IsTrue(orderBy[0].Ascending);
            Assert.IsFalse(orderBy[1].Ascending);

            AssertColumnName(orderBy[0].KeySelector, "`Name`");
            AssertColumnName(orderBy[1].KeySelector, "`Date`");
        }

        [TestMethod]
        public void Pagination_GeneratesCorrectSql()
        {
            var query = new Query<TestEntity>()
                .Skip(10)
                .Take(5);

            Assert.AreEqual(10, query.SkipCount);
            Assert.AreEqual(5, query.TakeCount);
        }

        [TestMethod]
        public void WhereIn_GeneratesCorrectSql()
        {
            var ids = new[] { 1, 2, 3 };
            var idProperty = CreatePropertySelector<TestEntity, int>(e => e.Id);
            var query = new Query<TestEntity>().WhereIn(idProperty, ids);

            AssertSqlGeneration(
                query.WhereExpressions[0],
                "((`Id` = @p1) OR (`Id` = @p2) OR (`Id` = @p3))",
                new { p1 = 1, p2 = 2, p3 = 3 });
        }

        [TestMethod]
        public void WhereBetween_GeneratesCorrectSql()
        {
            var amountProperty = CreatePropertySelector<TestEntity, decimal>(e => e.Amount);
            var query = new Query<TestEntity>().WhereBetween(amountProperty, 100m, 200m);

            AssertSqlGeneration(
                query.WhereExpressions[0],
                "((`Amount` >= @p1) AND (`Amount` <= @p2))",
                new { p1 = 100m, p2 = 200m });
        }

        [TestMethod]
        public void NullChecks_GeneratesCorrectSql()
        {
            var nullableProperty = CreatePropertySelector<TestEntity, int?>(e => e.NullableValue);
            var nameProperty = CreatePropertySelector<TestEntity, string>(e => e.Name);

            var query = new Query<TestEntity>()
                .WhereIsNull(nullableProperty)
                .WhereIsNotNull(nameProperty);

            AssertSqlGeneration(
                query.WhereExpressions[0],
                "(`NullableValue` IS NULL)",
                null);

            AssertSqlGeneration(
                query.WhereExpressions[1],
                "(`Name` IS NOT NULL)",
                null);
        }

        private void AssertSqlGeneration(Expression expression, string expectedSql, object expectedParameters)
        {
            _mysqlVisitor.Visit(expression);
            var (sql, parameters) = _mysqlVisitor.GetResult();
            Assert.AreEqual(expectedSql, sql);

            if (expectedParameters != null)
            {
                foreach (var prop in expectedParameters.GetType().GetProperties())
                {
                    Assert.AreEqual(prop.GetValue(expectedParameters), parameters[$"@{prop.Name}"]);
                }
            }
        }

        private void AssertColumnName(Expression expression, string expectedColumnName)
        {
            _mysqlVisitor.Visit(expression);
            var (sql, _) = _mysqlVisitor.GetResult();
            Assert.AreEqual(expectedColumnName, sql);
        }

        private static Expression<Func<T, bool>> CombineExpressions<T>(
            Expression<Func<T, bool>> expr1,
            Expression<Func<T, bool>> expr2,
            ExpressionType combinationType)
        {
            var parameter = Expression.Parameter(typeof(T));
            var visitor1 = new ReplaceParameterVisitor(expr1.Parameters[0], parameter);
            var visitor2 = new ReplaceParameterVisitor(expr2.Parameters[0], parameter);

            var combined = Expression.MakeBinary(
                combinationType,
                visitor1.Visit(expr1.Body),
                visitor2.Visit(expr2.Body));

            return Expression.Lambda<Func<T, bool>>(combined, parameter);
        }

        private static Expression<Func<T, TProperty>> CreatePropertySelector<T, TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            return propertyExpression;
        }

        private class ReplaceParameterVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParameter;
            private readonly ParameterExpression _newParameter;

            public ReplaceParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
            {
                _oldParameter = oldParameter;
                _newParameter = newParameter;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _oldParameter ? _newParameter : base.VisitParameter(node);
            }
        }
    }
} 