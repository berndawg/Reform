# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Reform is a lightweight .NET 8 repository/ORM framework shipped as a NuGet package (`Reform`, currently v4.0). Users decorate POCOs with `EntityMetadata`/`PropertyMetadata` attributes and consume `IReform<T>` for all CRUD. Supports SQLite, SQL Server, MySQL, and PostgreSQL.

## Solution Layout

- `Reform/` — the library. All public surface lives here.
- `ReformTests/` — xUnit unit tests. Uses a shared in-memory SQLite connection.
- `ReformIntegrationTests/` — a **console Exe** (not a test project) that runs integration tests against a real MySQL server.
- `ReformDB/` — SQL Server database project (`.sqlproj`) with schema objects under `dbo/`.

`Directory.Build.props` sets `LangVersion=latest`, `Nullable=enable`, and `ImplicitUsings=enable` for every project.

## Commands

Build the solution:
```
dotnet build Reform.sln
```

Run unit tests (xUnit, SQLite in-memory, no external deps):
```
dotnet test ReformTests/ReformTests.csproj
```

Run a single unit test:
```
dotnet test ReformTests/ReformTests.csproj --filter "FullyQualifiedName~UnitTests.Insert_And_Count"
```

Run integration tests (requires a running MySQL instance — the Exe creates the database and tables itself):
```
dotnet run --project ReformIntegrationTests -- --connection-string "Server=127.0.0.1;Port=33063;Database=ReformIntegrationTest;uid=root;pwd=pwd;"
```
The connection string can also come from the `REFORM_TEST_CONNECTION_STRING` env var. Default port is 33063, not 3306. `ReformIntegrationTests` drives both sync (`SqlServerTests`) and async (`SqlServerAsyncTests`) suites through a hand-rolled `TestRunner` (not xUnit) and returns a non-zero exit code on failure.

## Architecture

Pipeline (each stage resolved from DI and replaceable via `Reformer.Register`):

```
IReform<T>  →  Reform<T>  →  IDataAccess<T>  →  ICommandBuilder<T>  →  ISqlBuilder<T>
                                                                            │
                                                                   WhereClauseBuilder<T>
```

Cross-cutting singletons: `IDialect` (one of `SqliteDialect`/`SqlServerDialect`/`MySqlDialect`/`PostgreSqlDialect`), `IMetadataProvider<T>`, `IConnectionProvider<T>`, `IValidator<T>`, `IDebugLogger`, `IConnectionStringProvider`, `ICodeGenerator`.

Entry point is `Reformer` (builder) → `ReformFactory` (DI container wrapper). `factory.For<T>()` returns `IReform<T>`. `factory.Resolve<T>()` pulls any registered service; `factory.CodeGen(tableName)` generates a POCO from live schema via `ICodeGenerator`.

### Key design points that aren't obvious from one file

- **Dialect abstraction**: all SQL-string quoting, paging (`OFFSET/LIMIT` vs `OFFSET/FETCH` vs `TOP`), identity retrieval, LIKE-escaping, boolean literals, and truncate syntax are owned by `IDialect`. When adding a feature that emits SQL, route it through the dialect rather than hardcoding syntax in `SqlBuilder`.
- **WhereClauseBuilder is stateless per-call**: `WhereClauseBuilder<T>.Build` constructs a fresh private `Visitor` each call so the outer builder is thread-safe. Preserve this pattern — do not add mutable fields to the outer class.
- **`Reform<T>` has two constructors**: the normal DI constructor and a protected `(IValidator<T>, IMetadataProvider<T>?)` overload for subclasses that back a non-database store. `EnsureDataLayer()` throws if a derived class hits a DB-using method without overriding it. When adding new DB methods to `Reform<T>`, call `EnsureDataLayer()` or go through `GetConnection()`/`GetOpenedConnectionAsync()`.
- **Operation hooks**: public methods on `Reform<T>` are thin wrappers around protected `OnXxx` / `OnBeforeXxx` / `OnAfterXxx` virtuals. Override those, not the public methods, for custom behavior.
- **Parameterless `Insert(T)`/`Update(T)`/`Delete(T)` wrap their work in a transaction** that they begin and commit/rollback themselves. The `(IDbConnection, T)` and `(IDbConnection, IDbTransaction, T)` overloads do **not** — they assume the caller owns the lifecycle. `Insert(IDbConnection, T)` forwards `transaction: null!` internally (this is the documented pattern, not a bug to "fix").
- **Identity handling**: `DataAccess<T>.Insert` runs the insert command as `ExecuteScalar` (dialects append an identity-returning statement via `IDialect.IdentitySql`) and writes the returned value back onto the instance via `IMetadataProvider.SetPrimaryKeyValue`.

### Attributes

- `[EntityMetadata(DatabaseName, TableName)]` on the class.
- `[PropertyMetadata(ColumnName, DisplayName, IsPrimaryKey, IsIdentity, IsReadOnly, IsRequired)]` on properties. `IsReadOnly` excludes from INSERT/UPDATE; `IsRequired` is enforced by `Validator<T>` before insert/update.

## Testing Notes

- `ReformTests/UnitTests.cs` relies on `Data Source=InMemoryTest;Mode=Memory;Cache=Shared` and keeps `_sharedConnection` open for the test's lifetime so the shared-cache in-memory DB survives. Follow the same pattern for new SQLite-backed tests.
- `ExcelReform.cs` / `ExcelTests.cs` demonstrate the non-database subclassing path (override everything, use the protected validator-only constructor). Useful as a reference when documenting extensibility.
- Integration tests against MySQL are **not** xUnit — they are the `TestRunner` Exe. Don't try to run them with `dotnet test`.
