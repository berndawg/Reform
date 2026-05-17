# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Reform is a lightweight .NET 8 repository framework. Users decorate POCOs with `EntityMetadata`/`PropertyMetadata` attributes and consume `IReform<T>` for all CRUD. Ships as two NuGet packages:

- `Reform` (`Reform/Reform.csproj`, v5.0) — core library. Public surface is `IReform<T>`. Supports SQLite, SQL Server, MySQL, PostgreSQL via swappable `IDialect`.
- `Reform.Excel` (`Reform.Excel/Reform.Excel.csproj`, v1.0) — `IReform<T>` over .xlsx files via ClosedXML. Adds `Reformer.UseExcel<T>(path)` as an extension method.

## Solution Layout

- `Reform/` — the library. All core public surface lives here.
- `Reform.Excel/` — Excel-backed `IReform<T>` implementation.
- `ReformTests/` — xUnit unit tests for core. Uses a shared in-memory SQLite connection.
- `Reform.Excel.Tests/` — xUnit unit tests for the Excel backend.
- `ReformIntegrationTests/` — a **console Exe** (not a test project) that runs integration tests against a real MySQL server.
- `ReformDB/` — SQL Server database project (`.sqlproj`) with schema objects under `dbo/`. Schema source for integration tests; not part of the runtime library and not referenced by `Reform.sln`.

`Directory.Build.props` sets `LangVersion=latest`, `Nullable=enable`, and `ImplicitUsings=enable` for every project.

## Commands

```powershell
dotnet build Reform.sln
dotnet test  Reform.sln                                                                # all xUnit projects
dotnet test  ReformTests/ReformTests.csproj                                            # core tests only
dotnet test  Reform.Excel.Tests/Reform.Excel.Tests.csproj                              # Excel-backend tests only
dotnet test  ReformTests/ReformTests.csproj --filter "FullyQualifiedName~UnitTests.Insert_And_Count"
```

Integration tests (requires a running MySQL instance — the Exe creates the database and tables itself):

```
dotnet run --project ReformIntegrationTests -- --connection-string "Server=127.0.0.1;Port=33063;Database=ReformIntegrationTest;uid=root;pwd=pwd;"
```

Connection string can also come from `REFORM_TEST_CONNECTION_STRING`. Default port is **33063**, not 3306. `ReformIntegrationTests` drives both sync (`SqlServerTests`) and async (`SqlServerAsyncTests`) suites through a hand-rolled `TestRunner` and returns a non-zero exit code on failure. Don't try to run it with `dotnet test`.

## Architecture

**Composition is via `Microsoft.Extensions.DependencyInjection`.** `Reformer` is a fluent builder that records dialect + connection-string + custom registrations, then `Build()` constructs a `ServiceCollection`, registers all open generics as singletons, applies user overrides last so they win, and returns a `ReformFactory` wrapping the `ServiceProvider`. `factory.For<T>()` resolves `IReform<T>`. `factory.Resolve<T>()` pulls any registered service. `factory.CodeGen(tableName)` runs the schema-driven POCO generator (see `ICodeGenerator` below).

Three `Register` overloads, in increasing capability:
- `Register(Type, Type)` — open-generic substitutions.
- `Register<TService>(instance)` — fixed instance.
- `Register<TService>(Func<IServiceProvider, TService>)` — per-entity-type factory wiring that can resolve other services. This is what `UseExcel<T>` uses.

**Call stack for a SQL operation:**

```
IReform<T> (Reform<T>)        ← public API + transaction orchestration (sealed, no virtuals)
  └─ IDataAccess<T>           ← ADO.NET execution + reader → entity mapping
       └─ ICommandBuilder<T>  ← composes IDbCommand from SQL + parameters
            ├─ ISqlBuilder<T> ← builds SQL strings (uses WhereClauseBuilder<T> for predicates)
            └─ IDialect       ← per-DB quoting, identity SQL, paging SQL, parameter prefix, column-metadata query
```

Cross-cutting singletons: `IDialect`, `IMetadataProvider<T>`, `IConnectionProvider<T>`, `IValidator<T>`, `IDebugLogger`, `IConnectionStringProvider`, `ICodeGenerator`.

**`Reform<T>` is sealed.** Extension goes through composition, not inheritance:

- Different SQL flavour / connection strategy → swap `IDataAccess<T>`, `ISqlBuilder<T>`, or `IConnectionProvider<T>` via `Reformer.Register`.
- Different validation → swap `IValidator<T>`.
- Cross-cutting behaviour (logging, auditing, retries) → decorate `IReform<T>` or `IDataAccess<T>`.
- Non-database backend → implement `IReform<T>` directly. `Reform.Excel.ExcelReform<T>` is the worked example.

The pipeline inside `Reform<T>` is now just `validator.Validate(item)` → `dataAccess.Op(c, t, item)`, factored through `InsertOne` / `UpdateOne` helpers and a `Read` / `Write` / `ReadAsync` / `WriteAsync` transaction wrapper. There are no `On*` hooks — they were unused dead weight prior to v5.0.

### Key design points that aren't obvious from one file

- **Dialect abstraction**: all SQL-string quoting, paging (`OFFSET/LIMIT` vs `OFFSET/FETCH` vs `TOP`), identity retrieval, LIKE-escaping, boolean literals, truncate syntax, and column-metadata queries (for `ICodeGenerator`) are owned by `IDialect`. When adding a feature that emits SQL, route it through the dialect rather than hardcoding syntax in `SqlBuilder`. Each dialect implements its own metadata query; the default throws `NotSupportedException` so unsupported backends fail loudly.
- **`ICodeGenerator`**: `factory.CodeGen(tableName)` reads live column metadata via `IDialect.GetColumnMetadataSql` and emits a POCO with Reform attributes. Tests live in `ReformTests/CodeGenTests.cs`.
- **`WhereClauseBuilder<T>` is stateless per-call**: `Build` constructs a fresh private `Visitor` each call so the outer builder is thread-safe. Preserve this pattern — do not add mutable fields to the outer class. When chaining a WHERE onto an UPDATE/INSERT that already has parameters (e.g. `@p0..@pN` from SET values), pass `startingIndex` so generated names don't collide.
- **Identity handling**: `DataAccess<T>.Insert` runs the insert command as `ExecuteScalar` — dialects append an identity-returning statement via `IDialect.IdentitySql` — and writes the returned value back onto the instance via `IMetadataProvider.SetPrimaryKeyValue`.
- **`QueryCriteria<T>`** bundles predicate + `SortCriteria` + `PageCriteria`. `CommandBuilder.GetSelectCommand` auto-adds a PK ascending sort when paging is requested without an explicit sort — required because `LIMIT/OFFSET` (or `OFFSET ... FETCH`) is undefined without `ORDER BY`.
- **Parameterless `Insert(T)`/`Update(T)`/`Delete(T)`** wrap their work in a transaction owned by the `Write` helper. The `(IDbConnection, T)` and `(IDbConnection, IDbTransaction, T)` overloads do **not** — they assume the caller owns the lifecycle.
- **Excel backend leaks `IReform<T>`'s shape**: `IReform<T>` exposes `GetConnection()` and `Insert/Update/Delete` overloads taking `IDbConnection`/`IDbTransaction` — these are leaky for non-DB backends. `ExcelReform<T>` throws `NotSupportedException` on all of them. If you ever add a second non-DB backend and that throw starts to hurt, the principled fix is to split the interface (`IReform<T>` connection-free + `IDbReform<T> : IReform<T>` adding the overloads); avoid it until pain is concrete.

### Attributes

- `[EntityMetadata(DatabaseName, TableName)]` on the class.
- `[PropertyMetadata(ColumnName, DisplayName, IsPrimaryKey, IsIdentity, IsReadOnly, IsRequired)]` on properties. `IsReadOnly` excludes from INSERT/UPDATE; `IsRequired` is enforced by `Validator<T>` before insert/update.

## Conventions to match

- Nullable reference types are enabled solution-wide. Do not suppress with `null!` to hide a design gap — if a parameter is genuinely nullable downstream, change the signature.
- Sync and async methods exist as pairs throughout the stack. When adding an operation, add both, and route both through the same `*One` helper so the validate-then-execute order stays identical.
- Logging goes through `IDebugLogger` (default writes nothing). Tests register a custom logger via `.Register(typeof(IDebugLogger), typeof(TestDebugLogger))` to capture SQL — follow that pattern instead of `Console.WriteLine`.
- `Reform<T>` uses four private helpers — `Read`/`Write`/`ReadAsync`/`WriteAsync` — for connection/transaction lifecycle. Any new public CRUD method should route through them rather than duplicating the `using/try/commit/rollback` block.
- `ReformTests/UnitTests.cs` relies on `Data Source=InMemoryTest;Mode=Memory;Cache=Shared` and keeps `_sharedConnection` open for the test's lifetime so the shared-cache in-memory DB survives. Follow the same pattern for new SQLite-backed tests.
