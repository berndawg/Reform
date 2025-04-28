# Reform ORM

Reform is a lightweight ORM for .NET that uses ADO.NET abstractions for database operations, with a primary focus on MySQL support.

## Features

- Fluent query API for building type-safe queries using LINQ expressions
- Strong MySQL support with proper handling of MySQL-specific syntax
- Automatic mapping between database and .NET types
- Transaction support
- Bulk operations with `ON DUPLICATE KEY UPDATE` support
- Pagination support using `LIMIT` and `OFFSET`
- Provider-agnostic design through ADO.NET abstractions (`IDbConnection`, `IDbCommand`, etc.)

## Basic Usage

```csharp
// Create a query using the fluent API
var query = new Query<User>()
    .Where(u => u.Age > 18)
    .OrderBy(u => u.LastName)
    .ThenBy(u => u.FirstName)
    .Skip(0)
    .Take(10);

// Execute the query
var users = reform.Select(query);

// Count records
var count = reform.Count(query);

// Check if records exist
var exists = reform.Exists(query);

// Merge records (uses MySQL's ON DUPLICATE KEY UPDATE)
var list = new List<User> { /* users to merge */ };
reform.Merge(list, query);
```

## Installation

```powershell
Install-Package Reform
```

## Configuration

```csharp
// Register services
services.AddReform(options => {
    options.UseMySQL(connectionString);
});
```

## Key Interfaces

The IReform<T> interface includes the following methods:

- IDbConnection GetConnection()
- TransactionScope GetScope()
- int Count(Query<T> query)
- bool Exists(Query<T> query)
- void Insert(T item)
- void Insert(List<T> items)
- void Update(T item, Query<T> query)
- void Update(T item, Expression<Func<T, bool>> predicate)
- void Delete(Query<T> query)
- T SelectSingle(Query<T> query)
- IEnumerable<T> Select(Query<T> query)
- void Truncate()
- void BulkInsert(List<T> list)
- void Merge(List<T> list, Query<T> query)

The Reform<T> implementation supports various lifecycle hooks:

- IDbConnection OnGetConnection()
- TransactionScope OnGetScope()
- void OnValidate(IDbConnection connection, T item)
- void OnBeforeInsert(IDbConnection connection, T item)
- void OnAfterInsert(IDbConnection connection, T item)
- void OnBeforeUpdate(IDbConnection connection, T item)
- void OnAfterUpdate(IDbConnection connection, T item)
- void OnBeforeDelete(IDbConnection connection, T item)
- void OnAfterDelete(IDbConnection connection, T item)
- void OnTruncate(IDbConnection connection)
- void OnBulkInsert(IDbConnection connection, List<T> list)
- void OnMerge(IDbConnection connection, List<T> list, Query<T> query)

## Customization

Reform uses dependency injection for customization. You can register your own implementations of any Reform interface using the Reformer.RegisterType method:

```csharp
Reformer.RegisterType(typeof(IConnectionStringProvider), typeof(MyConnectionStringProvider));
Reformer.RegisterType(typeof(IColumnNameFormatter), typeof(MyColumnNameFormatter));
```

## Sample Usage

```csharp
using System;
using System.Collections.Generic;
using Reform.Attributes;
using Reform.Interfaces;
using Reform.Logic;

namespace ReformSample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Register your connection string provider
            Reformer.RegisterType(typeof(IConnectionStringProvider), typeof(ConnectionStringProvider));

            // Get a Reform instance for your entity
            IReform<User> reform = Reformer.Reform<User>();

            // Create a query using LINQ expressions
            var query = new Query<User>()
                .Where(u => u.LastName.StartsWith("S"))
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName);

            // Execute the query
            var users = reform.Select(query);

            foreach (var user in users)
                Console.WriteLine($"{user.LastName}, {user.FirstName}");
        }
    }

    [EntityMetadata(SchemaName = "dbo", TableName = "Users")]
    class User
    {
        [PropertyMetadata(ColumnName = "id", IsPrimaryKey = true)]
        public int Id { get; set; }

        [PropertyMetadata(ColumnName = "first_name")]
        public string FirstName { get; set; }

        [PropertyMetadata(ColumnName = "last_name")]
        public string LastName { get; set; }
    }

    class ConnectionStringProvider : IConnectionStringProvider
    {
        public string GetConnectionString(string databaseName)
        {
            return $"Server=localhost;Database={databaseName};Uid=myuser;Pwd=mypassword;";
        }
    }
}
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

Key areas for contribution:
- Additional database provider support
- Performance optimizations
- Additional query features
- Documentation improvements

Reform is an ORM that's extremely easy to use and extend. Stop writing SQL! Use the IReform interface instead. It includes methods like Count, Exists, Select, Insert, Update, Delete, Merge, and even BulkInsert. It supports Symmetric Key entryption and it's fully customizable allowing it to be used even without SQL Server. 

Reform puts the C# developer back in control of how data is shaped and manipulated. Define POCOs, add Reform code attributes, and you're done. It includes support for validation and transactions and it's the perfect tool for ETL applications. Forget all about Martin Fowler's silly IRepository pattern. Use IReform instead. You'll be glad you did!

The IReform<T> interface includes the following methods:

- SqlConnection GetConnection()
- TransactionScope GetScope();
- int Count();
- int Count(SqlConnection connection);
- int Count(List<Filter> filters);
- int Count(SqlConnection connection, List<Filter> filters);
- bool Exists(Filter filter);
- bool Exists(List<Filter> filters);
- bool Exists(SqlConnection connection, List<Filter> filters);
- void Insert(T item);
- void Insert(List<T> items);
- void Insert(SqlConnection connection, T item);
- void Insert(SqlConnection connection, List<T> items);
- void Update(T item);
- void Update(List<T> list);
- void Update(SqlConnection connection, T item);
- void Update(SqlConnection connection, List<T> list);
- void Delete(T item);
- void Delete(List<T> list);
- void Delete(SqlConnection connection, T item);
- void Delete(SqlConnection connection, List<T> list);
- T SelectSingle(List<Filter> filters);
- T SelectSingle(List<Filter> filters, T defaultObject);
- T SelectSingle(SqlConnection connection, List<Filter> filters);
- T SelectSingle(SqlConnection connection, List<Filter> filters, T defaultObject);
- IEnumerable<T> Select();
- IEnumerable<T> Select(Filter filter);
- IEnumerable<T> Select(List<Filter> filters);
- IEnumerable<T> Select(QueryCriteria queryCriteria);
- IEnumerable<T> Select(QueryCriteria queryCriteria, out int totalCount);
- IEnumerable<T> Select(SqlConnection connection, List<Filter> filters);
- IEnumerable<T> Select(SqlConnection connection, QueryCriteria queryCriteria);
- void Truncate();
- void Truncate(SqlConnection connection);
- void BulkInsert(List<T> list);
- void BulkInsert(SqlConnection connection, List<T> list);
- void Merge(List<T> list);
- void Merge(List<T> list, Filter filter);
- void Merge(List<T> list, List<Filter> filters);
- void Merge(SqlConnection connection, List<T> list, List<Filter> filters);

In addition, the Reform<T> implementation supports the following overrides:
- SqlConnection OnGetConnection()
- TransactionScope OnGetScope()
- void OnValidate(SqlConnection connection, T item)
- int OnCount(SqlConnection connection, List<Filter> filters)
- bool OnExists(SqlConnection connection, List<Filter> filters)
- IEnumerable<T> OnSelect(SqlConnection connection, QueryCriteria queryCriteria)
- void OnInsert(SqlConnection connection, T item)
- void OnUpdate(SqlConnection connection, T item)
- void OnDelete(SqlConnection connection, T item)
- void OnBeforeInsert(SqlConnection connection, T item)
- void OnBeforeUpdate(SqlConnection connection, T item)
- void OnAfterInsert(SqlConnection connection, T item)
- void OnAfterUpdate(SqlConnection connection, T item)
- void OnBeforeDelete(SqlConnection connection, T item)
- void OnAfterDelete(SqlConnection connection, T item)
- void OnTruncate(SqlConnection connection)
- void OnBulkInsert(SqlConnection connection, List<T> list)
- void OnMerge(SqlConnection connection, List<T> list, List<Filter> filters)

The Reform implementation is also customizable.  You can register your own types to replace portions of Reform functionality.  Just use the Reformer.RegisterType method to register your own custom implementation of any of the Reform interfaces.

Sample usage

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

                IReform<SysObjects> logic = Reformer.Reform<SysObjects>();

                IEnumerable<SysObjects> list = logic.Select(new Filter(nameof(SysObjects.Name), Operator.Like, "SYS%"));

                foreach (var item in list)
                    Console.WriteLine(item.Name);

                Console.ReadKey();
            }
        }

        [EntityMetadata(DatabaseName="master", TableName="sysobjects")]
        class SysObjects
        {
            [PropertyMetadata(ColumnName = "id")]
            public int Id { get; set; }

            [PropertyMetadata(ColumnName = "name")]
            public string Name { get; set; }
        }

        class ConnectionStringProvider : IConnectionStringProvider
        {
            public string GetConnectionString(string databaseName)
            {
                return $@"Server=.;Database={databaseName};Trusted_Connection=True;";
            }
        }
    }

 
