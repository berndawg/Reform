# Reform

Reform is an ORM that’s extremely easy to use and extend. Stop writing SQL! Use the IReform interface instead. It includes methods like Count, Exists, Select, Insert, Update, and Delete. It supports multiple SQL dialects (SQLite, SQL Server, MySQL) and is fully customizable via dependency injection.

Reform puts the C# developer back in control of how data is shaped and manipulated. Define POCOs, add Reform attributes, and you’re done. It includes support for validation, transactions, and async operations. 

Reform current supports SqLite, SQL Server, and MySQL and can be extended to include support for non-database data sources, e.g. Excel spreadsheets.

## Setup

Use `ReformBuilder` to configure and build a `ReformFactory`:

    var factory = new ReformBuilder()
        .UseSqlite("Data Source=mydb.db")       // or .UseSqlServer(...) or .UseMySql(...)
        .Build();

    IReform<Country> countries = factory.For<Country>();

The `ReformFactory` implements `IDisposable` and should be disposed when no longer needed.

## IReform&lt;T&gt; Interface

The `IReform<T>` interface includes the following methods:

Synchronous:

- IDbConnection GetConnection()
- int Count()
- int Count(Expression&lt;Func&lt;T, bool&gt;&gt; predicate)
- bool Exists(Expression&lt;Func&lt;T, bool&gt;&gt; predicate)
- void Insert(T item)
- void Insert(List&lt;T&gt; items)
- void Insert(IDbConnection connection, T item)
- void Update(T item)
- void Update(List&lt;T&gt; list)
- void Update(IDbConnection connection, T item)
- void Delete(T item)
- void Delete(List&lt;T&gt; list)
- void Delete(IDbConnection connection, T item)
- T SelectSingle(Expression&lt;Func&lt;T, bool&gt;&gt; predicate)
- T SelectSingleOrDefault(Expression&lt;Func&lt;T, bool&gt;&gt; predicate)
- IEnumerable&lt;T&gt; Select()
- IEnumerable&lt;T&gt; Select(Expression&lt;Func&lt;T, bool&gt;&gt; predicate)
- IEnumerable&lt;T&gt; Select(QueryCriteria&lt;T&gt; queryCriteria)

Async:

- Task&lt;int&gt; CountAsync()
- Task&lt;int&gt; CountAsync(Expression&lt;Func&lt;T, bool&gt;&gt; predicate)
- Task&lt;bool&gt; ExistsAsync(Expression&lt;Func&lt;T, bool&gt;&gt; predicate)
- Task InsertAsync(T item)
- Task InsertAsync(List&lt;T&gt; items)
- Task UpdateAsync(T item)
- Task UpdateAsync(List&lt;T&gt; list)
- Task DeleteAsync(T item)
- Task DeleteAsync(List&lt;T&gt; list)
- Task&lt;T&gt; SelectSingleAsync(Expression&lt;Func&lt;T, bool&gt;&gt; predicate)
- Task&lt;T&gt; SelectSingleOrDefaultAsync(Expression&lt;Func&lt;T, bool&gt;&gt; predicate)
- Task&lt;IEnumerable&lt;T&gt;&gt; SelectAsync()
- Task&lt;IEnumerable&lt;T&gt;&gt; SelectAsync(Expression&lt;Func&lt;T, bool&gt;&gt; predicate)
- Task&lt;IEnumerable&lt;T&gt;&gt; SelectAsync(QueryCriteria&lt;T&gt; queryCriteria)

## Overrideable Methods

The `Reform<T>` implementation supports the following overrides (sync and async):

- IDbConnection OnGetConnection()
- void OnValidate(IDbConnection connection, T item)
- int OnCount(IDbConnection connection, Expression&lt;Func&lt;T, bool&gt;&gt; predicate)
- Task&lt;int&gt; OnCountAsync(IDbConnection connection, Expression&lt;Func&lt;T, bool&gt;&gt; predicate)
- bool OnExists(IDbConnection connection, Expression&lt;Func&lt;T, bool&gt;&gt; predicate)
- Task&lt;bool&gt; OnExistsAsync(IDbConnection connection, Expression&lt;Func&lt;T, bool&gt;&gt; predicate)
- IEnumerable&lt;T&gt; OnSelect(IDbConnection connection, QueryCriteria&lt;T&gt; queryCriteria)
- Task&lt;IEnumerable&lt;T&gt;&gt; OnSelectAsync(IDbConnection connection, QueryCriteria&lt;T&gt; queryCriteria)
- void OnInsert(IDbConnection connection, IDbTransaction transaction, T item)
- Task OnInsertAsync(IDbConnection connection, IDbTransaction transaction, T item)
- void OnUpdate(IDbConnection connection, IDbTransaction transaction, T item)
- Task OnUpdateAsync(IDbConnection connection, IDbTransaction transaction, T item)
- void OnDelete(IDbConnection connection, IDbTransaction transaction, T item)
- Task OnDeleteAsync(IDbConnection connection, IDbTransaction transaction, T item)
- void OnBeforeInsert(IDbConnection connection, T item)
- void OnBeforeUpdate(IDbConnection connection, T item)
- void OnAfterInsert(IDbConnection connection, T item)
- void OnAfterUpdate(IDbConnection connection, T item)
- void OnBeforeDelete(IDbConnection connection, T item)
- void OnAfterDelete(IDbConnection connection, T item)
- Task OnBeforeInsertAsync(IDbConnection connection, T item)
- Task OnBeforeUpdateAsync(IDbConnection connection, T item)
- Task OnAfterInsertAsync(IDbConnection connection, T item)
- Task OnAfterUpdateAsync(IDbConnection connection, T item)
- Task OnBeforeDeleteAsync(IDbConnection connection, T item)
- Task OnAfterDeleteAsync(IDbConnection connection, T item)

## Customization

The Reform implementation is customizable. Use the `ReformBuilder.Register` method to replace any of the Reform interfaces with your own implementation:

    var factory = new ReformBuilder()
        .UseSqlite("Data Source=mydb.db")
        .Register(typeof(IDebugLogger), typeof(MyCustomLogger))
        .Build();

## Sample Usage

    using System;
    using System.Collections.Generic;
    using Reform;
    using Reform.Attributes;
    using Reform.Interfaces;

    namespace ReformSample
    {
        class Program
        {
            static void Main(string[] args)
            {
                using var factory = new ReformBuilder()
                    .UseSqlServer("Server=.;Database=master;Trusted_Connection=True;")
                    .Build();

                IReform<SysObjects> logic = factory.For<SysObjects>();

                IEnumerable<SysObjects> list = logic.Select(x => x.Name.StartsWith("sys"));

                foreach (var item in list)
                    Console.WriteLine(item.Name);
            }
        }

        [EntityMetadata(DatabaseName = "master", TableName = "sysobjects")]
        class SysObjects
        {
            [PropertyMetadata(ColumnName = "id", IsPrimaryKey = true)]
            public int Id { get; set; }

            [PropertyMetadata(ColumnName = "name")]
            public string Name { get; set; }
        }
    }