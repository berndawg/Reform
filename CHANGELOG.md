# Changelog

## 5.0.0

### Breaking changes

- **`Reform<T>` is now `sealed`.** Subclassing is no longer supported. Migrate any custom subclass to one of:
  - Implementing `IReform<T>` directly (see `Reform.Excel.ExcelReform<T>` for a worked example).
  - Decorating `IReform<T>` for cross-cutting concerns.
  - Replacing `IDataAccess<T>`, `ISqlBuilder<T>`, or other internal services via `Reformer.Register`.
- **Removed the `protected Reform(IValidator<T>, IMetadataProvider<T>?)` constructor.** Non-database backends now implement `IReform<T>` directly. No `EnsureDataLayer()` guard, no `null!`-forwarded connection provider.
- **Removed all `protected virtual On*` hooks** (`OnInsert`, `OnUpdate`, `OnDelete`, `OnSelect`, `OnCount`, `OnExists`, `OnTruncate`, `OnValidate`, `OnGetConnection`, and the `OnBefore*` / `OnAfter*` lifecycle hooks plus their async equivalents — 24+ methods total). Use composition (decorate `IReform<T>` or `IDataAccess<T>`) instead.
- **Removed `virtual` from all public CRUD methods** on `Reform<T>`. The class is sealed, so this is redundant; downstream callers see no API change.

### Additions

- **`Reformer.Register<TService>(Func<IServiceProvider, TService> factory)`** — new overload for per-entity-type registrations that need DI-resolved dependencies. Required by `Reform.Excel.UseExcel<T>()`.
- **`Reform.Excel` package** — `IReform<T>` over .xlsx files via ClosedXML, with a `Reformer.UseExcel<T>(filePath)` extension method. Multiple Excel-backed entity types can coexist with SQL-backed entity types in a single `Reformer`.

### Internal cleanup

- `Reform/Logic/Reform.cs` shrunk from ~778 lines to ~270 lines.
- Connection + transaction lifecycle factored into private `Read` / `Write` / `ReadAsync` / `WriteAsync` helpers.
- `InsertInternal` / `UpdateInternal` / `DeleteInternal` replaced by `InsertOne` / `UpdateOne` (validator + data-access only; no hook pipeline).

### Migration

Most consumers won't need to change anything — the `IReform<T>` interface is unchanged. The breaking changes affect only code that subclassed `Reform<T>` or overrode `On*` hooks (which, per a repo-wide grep at v5 cut time, was zero code paths inside this repo).
