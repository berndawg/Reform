# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Test

```powershell
dotnet build Reform.sln
dotnet test Reform.sln                                                                # all xUnit projects
dotnet test ReformTests/ReformTests.csproj                                            # core tests only
dotnet test Reform.Excel.Tests/Reform.Excel.Tests.csproj                              # Excel-backend tests only
dotnet test ReformTests/ReformTests.csproj --filter "FullyQualifiedName~Insert_And_Count"
```

The integration suite is a **console app**, not an xUnit project — it requires a real SQL Server instance and a runnable `Main`:

```powershell
dotnet run --project ReformIntegrationTests -- --connection-string "Server=.\SQLEXPRESS;Database=ReformIntegrationTest;Trusted_Connection=True;TrustServerCertificate=True;"
# or set REFORM_TEST_CONNECTION_STRING
```

`DatabaseSetup` in that project creates the DB and tables on first run.

`ReformDB` is a SQL Server Database Project (`.sqlproj`) used as a schema source for the integration tests — it is not part of the runtime library and is not referenced by `Reform.sln`.

## Architecture

Reform ships as two NuGet packages:

- `Reform` (`Reform/Reform.csproj`, `net8.0`) — the core library. Public surface is `IReform<T>`.
- `Reform.Excel` (`Reform.Excel/Reform.Excel.csproj`, `net8.0`) — `IReform<T>` over .xlsx files via ClosedXML. Adds `Reformer.UseExcel<T>(path)` as an extension method.

**Composition is via `Microsoft.Extensions.DependencyInjection`.** `Reformer` is a fluent builder that records dialect + connection-string + custom registrations, then `Build()` constructs a `ServiceCollection`, registers all open generics (`IReform<>`, `IDataAccess<>`, `ICommandBuilder<>`, `ISqlBuilder<>`, `IConnectionProvider<>`, `IMetadataProvider<>`, `IValidator<>`) as singletons, applies user overrides last so they win, and returns a `ReformFactory` wrapping the `ServiceProvider`. `factory.For<T>()` resolves `IReform<T>`.

Three `Register` overloads in increasing capability: `Register(Type, Type)` for open-generic substitutions, `Register<TService>(instance)` for fixed instances, `Register<TService>(Func<IServiceProvider, TService>)` for per-entity-type factory wiring that can resolve other services (this is what `UseExcel<T>` uses).

**Call stack for an operation:**

```
IReform<T> (Reform<T>)        ← public API + transaction orchestration (sealed, no virtuals)
  └─ IDataAccess<T>           ← ADO.NET execution + reader → entity mapping
       └─ ICommandBuilder<T>  ← composes IDbCommand from SQL + parameters
            ├─ ISqlBuilder<T> ← builds SQL strings (uses WhereClauseBuilder<T> for predicates)
            └─ IDialect       ← per-DB quoting, identity SQL, paging SQL, parameter prefix
```

**`Reform<T>` is sealed.** Extension goes through composition, not inheritance:

- Different SQL flavour / connection strategy → swap `IDataAccess<T>`, `ISqlBuilder<T>`, or `IConnectionProvider<T>` via `Reformer.Register`.
- Different validation → swap `IValidator<T>`.
- Cross-cutting behaviour (logging, auditing, retries) → decorate `IReform<T>` or `IDataAccess<T>`.
- Non-database backend → implement `IReform<T>` directly. `Reform.Excel.ExcelReform<T>` is the worked example.

The pipeline inside `Reform<T>` is now just `_validator.Validate(item)` → `_dataAccess.Op(c, t, item)`, factored through `InsertOne` / `UpdateOne` helpers and a `Read` / `Write` / `ReadAsync` / `WriteAsync` transaction wrapper. There are no `On*` hooks — they were unused dead weight prior to v5.0.

**Predicates.** `Expression<Func<T, bool>>` is translated to SQL by `WhereClauseBuilder<T>`, which is **stateless** — each `Build()` call instantiates a fresh nested `Visitor` with its own `StringBuilder` and parameter dictionary. Do not add mutable state to `WhereClauseBuilder<T>` itself; thread-safety depends on this. When chaining a WHERE clause onto an UPDATE/INSERT that already has parameters (e.g. `@p0..@pN` from SET values), pass `startingIndex` so generated names don't collide.

**Identity values.** `Insert` issues `INSERT ...; {IDialect.IdentitySql}` as a single command and uses `ExecuteScalar`. The returned value is `Convert.ChangeType`'d to the PK property type and written back to the entity. Dialect-specific quirks live in `Reform/Dialects/` (SQLite returns `Int64` from `last_insert_rowid()`, SQL Server uses `SCOPE_IDENTITY()`, etc.) — when adding a feature, look there before special-casing in `DataAccess<T>`.

**`QueryCriteria<T>`** bundles predicate + `SortCriteria` + `PageCriteria`. `CommandBuilder.GetSelectCommand` auto-adds a PK ascending sort when paging is requested without an explicit sort — required because `LIMIT/OFFSET` (or `OFFSET ... FETCH`) is undefined without `ORDER BY`.

**Excel backend leaks.** `IReform<T>` exposes `GetConnection()` and `Insert/Update/Delete` overloads taking `IDbConnection`/`IDbTransaction` — these are leaky for non-DB backends. `ExcelReform<T>` throws `NotSupportedException` on all of them. If you ever add a second non-DB backend and that throw starts to hurt, the principled fix is to split the interface (`IReform<T>` connection-free + `IDbReform<T> : IReform<T>` adding the overloads); avoid it until pain is concrete.

## Conventions to match

- Nullable reference types are enabled solution-wide (`Directory.Build.props`). Do not suppress with `null!` to hide a design gap — if a parameter is genuinely nullable downstream, change the signature.
- Sync and async methods exist as pairs throughout the stack. When adding an operation, add both, and route both through the same `*One` helper so the validate-then-execute order stays identical.
- Logging goes through `IDebugLogger` (default writes nothing). Tests register a custom logger via `.Register(typeof(IDebugLogger), typeof(TestDebugLogger))` to capture SQL — follow that pattern instead of `Console.WriteLine`.
- `Reform<T>` uses four private helpers — `Read`/`Write`/`ReadAsync`/`WriteAsync` — for connection/transaction lifecycle. Any new public CRUD method should route through them rather than duplicating the `using/try/commit/rollback` block.
