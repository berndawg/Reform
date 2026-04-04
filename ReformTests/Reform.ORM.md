# Reform

Reform is a lightweight repository framework for .NET 8 that supports any backing store via virtual method overrides. Define POCOs, add Reform attributes, and you're done. It includes support for validation, transactions, and async operations, and supports SQLite, SQL Server, and MySQL out of the box.

## Getting Started

```csharp
using Reform;
using Reform.Attributes;
using Reform.Interfaces;

// Configure and build
var factory = new Reformer()
    .UseSqlite("Data Source=mydb.db")
    .Build();

// Use it
IReform<Country> countryLogic = factory.For<Country>();

countryLogic.Insert(new Country { CountryName = "Morocco" });

var countries = countryLogic.Select(x => x.CountryName.StartsWith("M")).ToList();

foreach (var country in countries)
    Console.WriteLine(country.CountryName);

// Clean up
factory.Dispose();
```

## Defining Entities

```csharp
[EntityMetadata(DatabaseName = "MyDb", TableName = "Country")]
public class Country
{
    [PropertyMetadata(ColumnName = "CountryId", IsPrimaryKey = true, IsIdentity = true)]
    public int CountryId { get; set; }

    [PropertyMetadata(ColumnName = "CountryName", IsRequired = true)]
    public string CountryName { get; set; }
}
```

### PropertyMetadata Options

| Property      | Description                                    |
|---------------|------------------------------------------------|
| ColumnName    | Database column name                           |
| DisplayName   | Friendly name used in validation messages      |
| IsPrimaryKey  | Marks the property as the primary key          |
| IsIdentity    | Auto-incremented by the database               |
| IsReadOnly    | Excluded from INSERT and UPDATE statements     |
| IsRequired    | Validated as non-empty before insert/update    |

## IReform&lt;T&gt; Interface

### Queries
- `int Count()`
- `int Count(Expression<Func<T, bool>> predicate)`
- `bool Exists(Expression<Func<T, bool>> predicate)`
- `T SelectSingle(Expression<Func<T, bool>> predicate)`
- `T SelectSingleOrDefault(Expression<Func<T, bool>> predicate)`
- `IEnumerable<T> Select()`
- `IEnumerable<T> Select(Expression<Func<T, bool>> predicate)`
- `IEnumerable<T> Select(QueryCriteria<T> queryCriteria)`

### Commands
- `void Insert(T item)`
- `void Insert(List<T> items)`
- `void Update(T item)`
- `void Update(List<T> list)`
- `void Delete(T item)`
- `void Delete(List<T> list)`
- `void Merge(List<T> list)`

### Connection & Transaction Overloads
- `IDbConnection GetConnection()`
- `void Insert(IDbConnection connection, T item)`
- `void Insert(IDbConnection connection, IDbTransaction transaction, T item)`
- `void Update(IDbConnection connection, T item)`
- `void Update(IDbConnection connection, IDbTransaction transaction, T item)`
- `void Delete(IDbConnection connection, T item)`
- `void Delete(IDbConnection connection, IDbTransaction transaction, T item)`

### Async Variants

All query and command methods have async counterparts (e.g. `CountAsync`, `InsertAsync`, `SelectAsync`, etc.), including transaction overloads.

## User-Controlled Transactions

```csharp
IReform<Country> countryLogic = factory.For<Country>();

using var connection = countryLogic.GetConnection();
using var transaction = connection.BeginTransaction();
try
{
    countryLogic.Insert(connection, transaction, new Country { CountryName = "Norway" });
    countryLogic.Insert(connection, transaction, new Country { CountryName = "Sweden" });
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

## Supported Databases

| Database   | Configuration          |
|------------|------------------------|
| SQLite     | `new Reformer().UseSqlite(connectionString)`     |
| SQL Server | `new Reformer().UseSqlServer(connectionString)`  |
| MySQL      | `new Reformer().UseMySql(connectionString)`      |

## Customization

### Registering Custom Types

Replace any internal service with your own implementation:

```csharp
var factory = new Reformer()
    .UseSqlite("Data Source=mydb.db")
    .Register(typeof(IDebugLogger), typeof(MyCustomLogger))
    .Build();
```

### Overrideable Methods

The `Reform<T>` base class exposes protected virtual methods for deep customization:

**Connection**
- `OnGetConnection()` -- control how connections are obtained

**Validation**
- `OnValidate(IDbConnection, T)` -- custom validation logic

**Operations** (each receives connection + transaction)
- `OnCount`, `OnCountAsync`
- `OnExists`, `OnExistsAsync`
- `OnSelect`, `OnSelectAsync`
- `OnInsert`, `OnInsertAsync`
- `OnUpdate`, `OnUpdateAsync`
- `OnDelete`, `OnDeleteAsync`

**Lifecycle Hooks** (each receives connection + transaction + item)
- `OnBeforeInsert`, `OnBeforeInsertAsync`
- `OnAfterInsert`, `OnAfterInsertAsync`
- `OnBeforeUpdate`, `OnBeforeUpdateAsync`
- `OnAfterUpdate`, `OnAfterUpdateAsync`
- `OnBeforeDelete`, `OnBeforeDeleteAsync`
- `OnAfterDelete`, `OnAfterDeleteAsync`

## Architecture

```
IReform<T>  -->  Reform<T>  -->  IDataAccess<T>  -->  ICommandBuilder<T>  -->  ISqlBuilder<T>
                                                                                    |
                                                                          WhereClauseBuilder<T>
```

Each layer can be replaced via the DI container by calling `Register()` on the `Reformer` builder.
