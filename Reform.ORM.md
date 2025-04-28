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