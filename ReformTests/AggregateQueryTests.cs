using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reform.Interfaces;
using Reform.Logic;
using Reform.Objects;
using Reform.Extensions;

namespace Reform.Tests
{
    [TestClass]
    public class AggregateQueryTests
    {
        private class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal Amount { get; set; }
            public int Quantity { get; set; }
            public DateTime Date { get; set; }
            public decimal? NullableAmount { get; set; }
        }

        private IColumnNameFormatter _mysqlFormatter;
        private SqlExpressionVisitor _mysqlVisitor;
        private IMetadataProvider<TestEntity> _metadataProvider;
        private BaseSqlBuilder<TestEntity> _sqlBuilder;

        // Common test expressions
        private static readonly Expression<Func<TestEntity, bool>> AmountGreaterThan100 = e => e.Amount > 100;
        private static readonly Expression<Func<TestEntity, bool>> QuantityLessThan10 = e => e.Quantity < 10;
        private static readonly Expression<Func<TestEntity, decimal>> AmountSelector = e => e.Amount;
        private static readonly Expression<Func<TestEntity, int>> QuantitySelector = e => e.Quantity;
        private static readonly Expression<Func<TestEntity, decimal?>> NullableAmountSelector = e => e.NullableAmount;

        [TestInitialize]
        public void Setup()
        {
            _mysqlFormatter = new MySqlColumnNameFormatter();
            _mysqlVisitor = new SqlExpressionVisitor(_mysqlFormatter);
            _metadataProvider = new TestMetadataProvider();
            _sqlBuilder = new MySqlBuilder<TestEntity>(_metadataProvider, _mysqlFormatter, new ParameterBuilder());
        }

        [TestMethod]
        public void Count_GeneratesCorrectSql()
        {
            var query = new Query<TestEntity>()
                .Where(AmountGreaterThan100)
                .Count(e => e.Id, "TotalCount");

            var sql = _sqlBuilder.GetAggregateSql(query, out var parameters);
            Assert.AreEqual("SELECT COUNT(`Id`) AS `TotalCount` FROM `TestSchema`.`TestEntity` WHERE (`Amount` > @p1)", sql.Replace("  ", " ").Trim());
            Assert.AreEqual(100m, parameters["@p1"]);
        }

        [TestMethod]
        public void Sum_GeneratesCorrectSql()
        {
            var query = new Query<TestEntity>()
                .Where(QuantityLessThan10)
                .Sum(e => e.Amount, "TotalAmount");

            var sql = _sqlBuilder.GetAggregateSql(query, out var parameters);
            Assert.AreEqual("SELECT SUM(`Amount`) AS `TotalAmount` FROM `TestSchema`.`TestEntity` WHERE (`Quantity` < @p1)", sql.Replace("  ", " ").Trim());
            Assert.AreEqual(10, parameters["@p1"]);
        }

        [TestMethod]
        public void Average_GeneratesCorrectSql()
        {
            var query = new Query<TestEntity>()
                .Where(AmountGreaterThan100)
                .Where(QuantityLessThan10)
                .Avg(e => e.Amount, "AverageAmount");

            var sql = _sqlBuilder.GetAggregateSql(query, out var parameters);
            Assert.AreEqual("SELECT AVG(`Amount`) AS `AverageAmount` FROM `TestSchema`.`TestEntity` WHERE (`Amount` > @p1) AND (`Quantity` < @p2)", sql.Replace("  ", " ").Trim());
            Assert.AreEqual(100m, parameters["@p1"]);
            Assert.AreEqual(10, parameters["@p2"]);
        }

        [TestMethod]
        public void Min_GeneratesCorrectSql()
        {
            var query = new Query<TestEntity>()
                .Where(AmountGreaterThan100)
                .Min(e => e.Amount, "MinAmount");

            var sql = _sqlBuilder.GetAggregateSql(query, out var parameters);
            Assert.AreEqual("SELECT MIN(`Amount`) AS `MinAmount` FROM `TestSchema`.`TestEntity` WHERE (`Amount` > @p1)", sql.Replace("  ", " ").Trim());
            Assert.AreEqual(100m, parameters["@p1"]);
        }

        [TestMethod]
        public void Max_GeneratesCorrectSql()
        {
            var query = new Query<TestEntity>()
                .Where(QuantityLessThan10)
                .Max(e => e.Amount, "MaxAmount");

            var sql = _sqlBuilder.GetAggregateSql(query, out var parameters);
            Assert.AreEqual("SELECT MAX(`Amount`) AS `MaxAmount` FROM `TestSchema`.`TestEntity` WHERE (`Quantity` < @p1)", sql.Replace("  ", " ").Trim());
            Assert.AreEqual(10, parameters["@p1"]);
        }

        [TestMethod]
        public void NullableAggregate_GeneratesCorrectSql()
        {
            var query = new Query<TestEntity>()
                .Where(AmountGreaterThan100)
                .Sum(e => e.NullableAmount, "TotalNullableAmount");

            var sql = _sqlBuilder.GetAggregateSql(query, out var parameters);
            Assert.AreEqual("SELECT SUM(`NullableAmount`) AS `TotalNullableAmount` FROM `TestSchema`.`TestEntity` WHERE (`Amount` > @p1)", sql.Replace("  ", " ").Trim());
            Assert.AreEqual(100m, parameters["@p1"]);
        }

        [TestMethod]
        public void GroupBy_GeneratesCorrectSql()
        {
            var query = new Query<TestEntity>()
                .Where(AmountGreaterThan100)
                .Count(e => e.Id, "Count")
                .GroupBy(e => e.Name);

            var sql = _sqlBuilder.GetAggregateSql(query, out var parameters);
            Assert.AreEqual("SELECT COUNT(`Id`) AS `Count`, `Name` FROM `TestSchema`.`TestEntity` WHERE (`Amount` > @p1) GROUP BY `Name`", sql.Replace("  ", " ").Trim());
            Assert.AreEqual(100m, parameters["@p1"]);
        }

        [TestMethod]
        public void Having_GeneratesCorrectSql()
        {
            var query = new Query<TestEntity>()
                .Where(AmountGreaterThan100)
                .Count(e => e.Id, "Count")
                .GroupBy(e => e.Name)
                .Having("Count > @p2", new Dictionary<string, object> { { "@p2", 5 } });

            var sql = _sqlBuilder.GetAggregateSql(query, out var parameters);
            Assert.AreEqual("SELECT COUNT(`Id`) AS `Count`, `Name` FROM `TestSchema`.`TestEntity` WHERE (`Amount` > @p1) GROUP BY `Name` HAVING (Count > @p2)", sql.Replace("  ", " ").Trim());
            Assert.AreEqual(100m, parameters["@p1"]);
            Assert.AreEqual(5, parameters["@p2"]);
        }

        private class TestMetadataProvider : IMetadataProvider<TestEntity>
        {
            public string SchemaName => "TestSchema";
            public string TableName => "TestEntity";
            public Type Type => typeof(TestEntity);
            public IEnumerable<PropertyMap> AllProperties => Array.Empty<PropertyMap>();
            public IEnumerable<PropertyMap> RequiredProperties => Array.Empty<PropertyMap>();
            public IEnumerable<PropertyMap> UpdateableProperties => Array.Empty<PropertyMap>();
            public string SymmetricKeyName => string.Empty;
            public string SymmetricKeyCertificate => string.Empty;
            public string DatabaseName => string.Empty;
            public bool HasEncryptedFields => false;
            public string PrimaryKeyPropertyName => "Id";
            public string PrimaryKeyColumnName => "Id";

            PropertyMap IMetadataProvider<TestEntity>.GetPropertyMapByPropertyName(string propertyName) => null;
            PropertyMap IMetadataProvider<TestEntity>.GetPropertyMapByColumnName(string columnName) => null;
            object IMetadataProvider<TestEntity>.GetPrimaryKeyValue(TestEntity instance) => instance.Id;
            void IMetadataProvider<TestEntity>.SetPrimaryKeyValue(TestEntity instance, object id) => instance.Id = (int)id;
        }

        private class ParameterBuilder : IParameterBuilder
        {
            public string GetParameterName(string columnName, int index) => $"@p{index + 1}";
        }
    }
} 