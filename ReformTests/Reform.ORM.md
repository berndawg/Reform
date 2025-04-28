# Reform

Reform is an ORM that's extremely easy to use and extend. Stop writing SQL! Use the IReform interface instead. It includes methods like Count, Exists, Select, Insert, Update, Delete, Merge, and even BulkInsert. It's fully customizable and database provider agnostic, supporting various database systems like MySQL, PostgreSQL, and SQL Server.

Reform puts the C# developer back in control of how data is shaped and manipulated. Define POCOs, add Reform code attributes, and you're done. It includes support for validation and transactions and it's the perfect tool for ETL applications. Forget all about Martin Fowler's silly IRepository pattern. Use IReform instead. You'll be glad you did!

The IReform<T> interface includes the following methods:

- IDbConnection GetConnection()
- TransactionScope GetScope();
- int Count();
- int Count(IDbConnection connection);
- int Count(List<Filter> filters);
- int Count(IDbConnection connection, List<Filter> filters);
- bool Exists(Filter filter);
- bool Exists(List<Filter> filters);
- bool Exists(IDbConnection connection, List<Filter> filters);
- void Insert(T item);
- void Insert(List<T> items);
- void Insert(IDbConnection connection, T item);
- void Insert(IDbConnection connection, List<T> items);
- void Update(T item);
- void Update(List<T> list);
- void Update(IDbConnection connection, T item);
- void Update(IDbConnection connection, List<T> list);
- void Delete(T item);
- void Delete(List<T> list);
- void Delete(IDbConnection connection, T item);
- void Delete(IDbConnection connection, List<T> list);
- T SelectSingle(List<Filter> filters);
- T SelectSingle(List<Filter> filters, T defaultObject);
- T SelectSingle(IDbConnection connection, List<Filter> filters);
- T SelectSingle(IDbConnection connection, List<Filter> filters, T defaultObject);
- IEnumerable<T> Select();
- IEnumerable<T> Select(Filter filter);
- IEnumerable<T> Select(List<Filter> filters);
- IEnumerable<T> Select(QueryCriteria queryCriteria);
- IEnumerable<T> Select(QueryCriteria queryCriteria, out int totalCount);
- IEnumerable<T> Select(IDbConnection connection, List<Filter> filters);
- IEnumerable<T> Select(IDbConnection connection, QueryCriteria queryCriteria);
- void Truncate();
- void Truncate(IDbConnection connection);
- void BulkInsert(List<T> list);
- void BulkInsert(IDbConnection connection, List<T> list);
- void Merge(List<T> list);
- void Merge(List<T> list, Filter filter);
- void Merge(List<T> list, List<Filter> filters);
- void Merge(IDbConnection connection, List<T> list, List<Filter> filters);

In addition, the Reform<T> implementation supports the following overrides:
- IDbConnection OnGetConnection()
- TransactionScope OnGetScope()
- void OnValidate(IDbConnection connection, T item)
- int OnCount(IDbConnection connection, List<Filter> filters)
- bool OnExists(IDbConnection connection, List<Filter> filters)
- IEnumerable<T> OnSelect(IDbConnection connection, QueryCriteria queryCriteria)
- void OnInsert(IDbConnection connection, T item)
- void OnUpdate(IDbConnection connection, T item)
- void OnDelete(IDbConnection connection, T item)
- void OnBeforeInsert(IDbConnection connection, T item)
- void OnBeforeUpdate(IDbConnection connection, T item)
- void OnAfterInsert(IDbConnection connection, T item)
- void OnAfterUpdate(IDbConnection connection, T item)
- void OnBeforeDelete(IDbConnection connection, T item)
- void OnAfterDelete(IDbConnection connection, T item)
- void OnTruncate(IDbConnection connection)
- void OnBulkInsert(IDbConnection connection, List<T> list)
- void OnMerge(IDbConnection connection, List<T> list, List<Filter> filters)

The Reform implementation is also customizable. You can register your own types to replace portions of Reform functionality. Just use the Reformer.RegisterType method to register your own custom implementation of any of the Reform interfaces.

Sample usage:

    using System;
    using System.Collections.Generic;
    using Reform.Attributes;
    using Reform.Enum;
    using Reform.Interfaces;
    using Reform.Logic;
    using Reform.Objects;

    namespace ReformSample
    {
        class Program
        {
            static void Main(string[] args)
            {
                Reformer.RegisterType(typeof(IConnectionStringProvider), typeof(ConnectionStringProvider));

                IReform<User> logic = Reformer.Reform<User>();

                IEnumerable<User> list = logic.Select(new Filter(nameof(User.Username), Operator.Like, "admin%"));

                foreach (var item in list)
                    Console.WriteLine(item.Username);

                Console.ReadKey();
            }
        }

        [EntityMetadata(DatabaseName="myapp", TableName="users")]
        class User
        {
            [PropertyMetadata(ColumnName = "id")]
            public int Id { get; set; }

            [PropertyMetadata(ColumnName = "username")]
            public string Username { get; set; }
        }

        class ConnectionStringProvider : IConnectionStringProvider
        {
            public string GetConnectionString(string databaseName)
            {
                return $"Server=localhost;Database={databaseName};Uid=root;Pwd=your_password;";
            }
        }
    }

# Reform ORM Tests

This document outlines the test cases for the Reform ORM library.

## Core Functionality Tests

### Query Operations
- Test query building with Where conditions
- Test query building with OrderBy clauses
- Test query building with Skip/Take for pagination
- Test query execution and result mapping

### CRUD Operations
- Test Select with various query conditions
- Test Insert with single and multiple records
- Test Update with different field combinations
- Test Delete with query conditions
- Test Merge operations with query conditions

### Transaction Support
- Test transaction commit
- Test transaction rollback
- Test nested transactions

### Validation
- Test entity validation before save
- Test custom validation rules
- Test validation error handling

### Provider Support
- Test MySQL provider implementation
- Test SQL Server provider implementation
- Test provider-specific SQL generation

## Integration Tests

### Database Connection
- Test connection string configuration
- Test connection pooling
- Test connection disposal

### Mapping
- Test property mapping
- Test custom type conversions
- Test null handling

### Performance
- Test bulk operations
- Test query performance
- Test connection management

## Unit Tests

### Builder Classes
- Test SQL builder implementations
- Test parameter builder
- Test query builder

### Utility Classes
- Test metadata provider
- Test column name formatter
- Test parameter naming 