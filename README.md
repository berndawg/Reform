# Reform

Reform is a lightweight repository framework for .NET. Define POCOs, add Reform attributes, and use the `IReform<T>` interface for all your data operations. It supports SQLite, SQL Server, MySQL, and PostgreSQL out of the box, and is fully extensible to any backing store via virtual method overrides.

## Getting Started

```csharp
using Reform;
using Reform.Attributes;
using Reform.Interfaces;

// Define your entity
[EntityMetadata(DatabaseName = "MyDb", TableName = "Country")]
public class Country
{
    [PropertyMetadata(ColumnName = "CountryId", IsPrimaryKey = true, IsIdentity = true)]
    public int CountryId { get; set; }

    [PropertyMetadata(ColumnName = "CountryName")]
    public string CountryName { get; set; }
}

// Configure and build
ReformFactory factory = new Reformer()
    .UseSqlite("Data Source=mydb.db")
    .Build();

// Use it
IReform<Country> countryLogic = factory.For<Country>();

countryLogic.Insert(new Country { CountryName = "France" });

IEnumerable<Country> countries = countryLogic.Select();
```

## Supported Dialects

| Method | Dialect |
|---|---|
| `UseSqlite(connectionString)` | SQLite |
| `UseSqlServer(connectionString)` | SQL Server |
| `UseMySql(connectionString)` | MySQL |
| `UsePostgreSql(connectionString)` | PostgreSQL |

## IReform&lt;T&gt; Interface

### Sync

```csharp
IDbConnection GetConnection();

int Count();
int Count(Expression<Func<T, bool>> predicate);

bool Exists(Expression<Func<T, bool>> predicate);

void Insert(T item);
void Insert(List<T> items);
void Insert(IDbConnection connection, T item);
void Insert(IDbConnection connection, IDbTransaction transaction, T item);

void Update(T item);
void Update(List<T> list);
void Update(IDbConnection connection, T item);
void Update(IDbConnection connection, IDbTransaction transaction, T item);

void Delete(T item);
void Delete(List<T> list);
void Delete(IDbConnection connection, T item);
void Delete(IDbConnection connection, IDbTransaction transaction, T item);

void Merge(List<T> list);

void Truncate();

T SelectSingle(Expression<Func<T, bool>> predicate);
T SelectSingleOrDefault(Expression<Func<T, bool>> predicate);

IEnumerable<T> Select();
IEnumerable<T> Select(Expression<Func<T, bool>> predicate);
IEnumerable<T> Select(QueryCriteria<T> queryCriteria);
```

### Async

```csharp
Task<int> CountAsync();
Task<int> CountAsync(Expression<Func<T, bool>> predicate);

Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

Task InsertAsync(T item);
Task InsertAsync(List<T> items);
Task InsertAsync(IDbConnection connection, IDbTransaction transaction, T item);

Task UpdateAsync(T item);
Task UpdateAsync(List<T> list);
Task UpdateAsync(IDbConnection connection, IDbTransaction transaction, T item);

Task DeleteAsync(T item);
Task DeleteAsync(List<T> list);
Task DeleteAsync(IDbConnection connection, IDbTransaction transaction, T item);

Task MergeAsync(List<T> list);

Task TruncateAsync();

Task<T> SelectSingleAsync(Expression<Func<T, bool>> predicate);
Task<T> SelectSingleOrDefaultAsync(Expression<Func<T, bool>> predicate);

Task<IEnumerable<T>> SelectAsync();
Task<IEnumerable<T>> SelectAsync(Expression<Func<T, bool>> predicate);
Task<IEnumerable<T>> SelectAsync(QueryCriteria<T> queryCriteria);
```

## Attributes

### EntityMetadata

| Property | Description |
|---|---|
| `DatabaseName` | Database name |
| `TableName` | Table name |

### PropertyMetadata

| Property | Description |
|---|---|
| `ColumnName` | Column name |
| `DisplayName` | Display name (for validation messages) |
| `IsPrimaryKey` | Marks the primary key column |
| `IsIdentity` | Column is auto-incremented |
| `IsReadOnly` | Column is excluded from inserts and updates |
| `IsRequired` | Validation: value must not be null/empty |

## Extensibility

Reform is built on Microsoft.Extensions.DependencyInjection. Use `Register` to replace any internal service:

```csharp
ReformFactory factory = new Reformer()
    .UseSqlite("Data Source=mydb.db")
    .Register(typeof(IDataAccess<>), typeof(MyCustomDataAccess<>))
    .Build();
```

You can also subclass `Reform<T>` and override its virtual methods to customise connection handling and individual operations.
