using System.Data;
using System.Linq.Expressions;
using ClosedXML.Excel;
using Reform.Interfaces;
using Reform.Objects;

namespace Reform.Excel
{
    public sealed class ExcelReform<T>(string filePath, IMetadataProvider<T> metadataProvider, IValidator<T> validator)
        : IReform<T>
        where T : class
    {
        private readonly object _gate = new();

        private string SheetName => metadataProvider.TableName;

        #region Reads

        public int Count() => ReadAll().Count;

        public int Count(Expression<Func<T, bool>> predicate) => ReadAll().Count(predicate.Compile());

        public bool Exists(Expression<Func<T, bool>> predicate) => ReadAll().Any(predicate.Compile());

        public IEnumerable<T> Select() => ReadAll();

        public IEnumerable<T> Select(Expression<Func<T, bool>> predicate)
            => ReadAll().Where(predicate.Compile()).ToList();

        public IEnumerable<T> Select(QueryCriteria<T> queryCriteria)
            => queryCriteria.Predicate != null
                ? Select(queryCriteria.Predicate)
                : ReadAll();

        public T SelectSingle(Expression<Func<T, bool>> predicate)
        {
            var list = Select(predicate).ToList();
            if (list.Count == 1) return list[0];
            throw new InvalidOperationException($"Expected to find 1 {typeof(T).Name} but found {list.Count}.");
        }

        public T SelectSingleOrDefault(Expression<Func<T, bool>> predicate)
        {
            var list = Select(predicate).ToList();
            if (list.Count > 1)
                throw new InvalidOperationException($"Expected to find 1 or 0 {typeof(T).Name} but found {list.Count}.");
            return list.FirstOrDefault()!;
        }

        #endregion

        #region Writes

        public void Insert(T item)
        {
            validator.Validate(item);
            lock (_gate)
            {
                using var workbook = LoadOrCreateWorkbook();
                var worksheet = GetOrCreateWorksheet(workbook);
                WriteAppendingRow(worksheet, item);
                workbook.SaveAs(filePath);
            }
        }

        public void Insert(List<T> items)
        {
            foreach (var item in items)
                validator.Validate(item);

            lock (_gate)
            {
                using var workbook = LoadOrCreateWorkbook();
                var worksheet = GetOrCreateWorksheet(workbook);
                foreach (var item in items)
                    WriteAppendingRow(worksheet, item);
                workbook.SaveAs(filePath);
            }
        }

        public void Update(T item)
        {
            validator.Validate(item);
            lock (_gate)
            {
                using var workbook = LoadOrCreateWorkbook();
                if (!workbook.Worksheets.Contains(SheetName))
                    throw new InvalidOperationException($"Expected to find 1 {typeof(T).Name} but found 0.");

                var worksheet = workbook.Worksheet(SheetName);
                if (!UpdateRow(worksheet, item))
                    throw new InvalidOperationException($"Expected to find 1 {typeof(T).Name} but found 0.");

                workbook.SaveAs(filePath);
            }
        }

        public void Update(List<T> list)
        {
            foreach (var item in list)
                validator.Validate(item);

            lock (_gate)
            {
                using var workbook = LoadOrCreateWorkbook();
                if (!workbook.Worksheets.Contains(SheetName))
                    return;

                var worksheet = workbook.Worksheet(SheetName);
                foreach (var item in list)
                    UpdateRow(worksheet, item);

                workbook.SaveAs(filePath);
            }
        }

        public void Delete(T item)
        {
            lock (_gate)
            {
                using var workbook = LoadOrCreateWorkbook();
                if (!workbook.Worksheets.Contains(SheetName))
                    return;

                var worksheet = workbook.Worksheet(SheetName);
                if (DeleteRow(worksheet, item))
                    workbook.SaveAs(filePath);
            }
        }

        public void Delete(List<T> list)
        {
            lock (_gate)
            {
                using var workbook = LoadOrCreateWorkbook();
                if (!workbook.Worksheets.Contains(SheetName))
                    return;

                var worksheet = workbook.Worksheet(SheetName);
                var anyDeleted = false;
                foreach (var item in list)
                    anyDeleted |= DeleteRow(worksheet, item);

                if (anyDeleted)
                    workbook.SaveAs(filePath);
            }
        }

        public void Merge(List<T> list)
        {
            if (list.Count == 0)
                throw new ArgumentException("Cannot merge an empty list. Use Delete to remove all rows.", nameof(list));

            foreach (var item in list)
                validator.Validate(item);

            lock (_gate)
            {
                using var workbook = LoadOrCreateWorkbook();
                var worksheet = GetOrCreateWorksheet(workbook);

                var existing = ReadAllFromWorksheet(worksheet);
                var existingByPk = new Dictionary<object, T>();
                foreach (var record in existing)
                    existingByPk[metadataProvider.GetPrimaryKeyValue(record)] = record;

                var accountedPks = new HashSet<object>();

                foreach (var item in list)
                {
                    if (IsDefaultPrimaryKey(item))
                    {
                        WriteAppendingRow(worksheet, item);
                    }
                    else
                    {
                        var pk = metadataProvider.GetPrimaryKeyValue(item);
                        accountedPks.Add(pk);

                        if (existingByPk.ContainsKey(pk))
                            UpdateRow(worksheet, item);
                        else
                            WriteAppendingRow(worksheet, item);
                    }
                }

                foreach (var kvp in existingByPk.Where(kvp => !accountedPks.Contains(kvp.Key)))
                    DeleteRow(worksheet, kvp.Value);

                workbook.SaveAs(filePath);
            }
        }

        public void Truncate()
        {
            lock (_gate)
            {
                if (!File.Exists(filePath))
                    return;

                using var workbook = new XLWorkbook(filePath);
                if (!workbook.Worksheets.Contains(SheetName))
                    return;

                var worksheet = workbook.Worksheet(SheetName);
                var lastRow = worksheet.LastRowUsed();
                if (lastRow == null || lastRow.RowNumber() < 2)
                    return;

                for (var row = lastRow.RowNumber(); row >= 2; row--)
                    worksheet.Row(row).Delete();

                workbook.SaveAs(filePath);
            }
        }

        #endregion

        #region IDbConnection overloads — not supported

        public IDbConnection GetConnection() => throw NotSupported(nameof(GetConnection));

        public void Insert(IDbConnection connection, T item) => throw NotSupported(nameof(Insert));
        public void Insert(IDbConnection connection, IDbTransaction transaction, T item) => throw NotSupported(nameof(Insert));
        public void Update(IDbConnection connection, T item) => throw NotSupported(nameof(Update));
        public void Update(IDbConnection connection, IDbTransaction transaction, T item) => throw NotSupported(nameof(Update));
        public void Delete(IDbConnection connection, T item) => throw NotSupported(nameof(Delete));
        public void Delete(IDbConnection connection, IDbTransaction transaction, T item) => throw NotSupported(nameof(Delete));

        public Task InsertAsync(IDbConnection connection, IDbTransaction transaction, T item) => throw NotSupported(nameof(InsertAsync));
        public Task UpdateAsync(IDbConnection connection, IDbTransaction transaction, T item) => throw NotSupported(nameof(UpdateAsync));
        public Task DeleteAsync(IDbConnection connection, IDbTransaction transaction, T item) => throw NotSupported(nameof(DeleteAsync));

        private NotSupportedException NotSupported(string member) => new(
            $"ExcelReform<{typeof(T).Name}> does not participate in IDbConnection transactions. Use the connection-free {member}(...) overloads instead.");

        #endregion

        #region Async (Excel I/O is sync — honest wrap)

        public Task<int>  CountAsync()                                       => Task.FromResult(Count());
        public Task<int>  CountAsync(Expression<Func<T, bool>> predicate)    => Task.FromResult(Count(predicate));
        public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)   => Task.FromResult(Exists(predicate));

        public Task<IEnumerable<T>> SelectAsync()                                    => Task.FromResult(Select());
        public Task<IEnumerable<T>> SelectAsync(Expression<Func<T, bool>> predicate) => Task.FromResult(Select(predicate));
        public Task<IEnumerable<T>> SelectAsync(QueryCriteria<T> queryCriteria)      => Task.FromResult(Select(queryCriteria));

        public Task<T> SelectSingleAsync(Expression<Func<T, bool>> predicate)          => Task.FromResult(SelectSingle(predicate));
        public Task<T> SelectSingleOrDefaultAsync(Expression<Func<T, bool>> predicate) => Task.FromResult(SelectSingleOrDefault(predicate));

        public Task InsertAsync(T item)        { Insert(item);  return Task.CompletedTask; }
        public Task InsertAsync(List<T> items) { Insert(items); return Task.CompletedTask; }
        public Task UpdateAsync(T item)        { Update(item);  return Task.CompletedTask; }
        public Task UpdateAsync(List<T> list)  { Update(list);  return Task.CompletedTask; }
        public Task DeleteAsync(T item)        { Delete(item);  return Task.CompletedTask; }
        public Task DeleteAsync(List<T> list)  { Delete(list);  return Task.CompletedTask; }
        public Task MergeAsync(List<T> list)   { Merge(list);   return Task.CompletedTask; }
        public Task TruncateAsync()            { Truncate();    return Task.CompletedTask; }

        #endregion

        #region Excel I/O

        private XLWorkbook LoadOrCreateWorkbook()
            => File.Exists(filePath) ? new XLWorkbook(filePath) : new XLWorkbook();

        private IXLWorksheet GetOrCreateWorksheet(XLWorkbook workbook)
        {
            if (workbook.Worksheets.Contains(SheetName))
                return workbook.Worksheet(SheetName);

            var worksheet = workbook.Worksheets.Add(SheetName);
            var col = 1;
            foreach (var prop in metadataProvider.AllProperties)
                worksheet.Cell(1, col++).Value = prop.ColumnName;

            return worksheet;
        }

        private void WriteAppendingRow(IXLWorksheet worksheet, T item)
        {
            var nextRow = (worksheet.LastRowUsed()?.RowNumber() ?? 1) + 1;

            var identityProp = metadataProvider.AllProperties.FirstOrDefault(p => p.IsIdentity);
            if (identityProp != null)
            {
                var nextId = GetMaxIdentityValue(worksheet, identityProp) + 1;
                identityProp.SetPropertyValue(item, nextId);
            }

            WriteRow(worksheet, nextRow, item);
        }

        private bool UpdateRow(IXLWorksheet worksheet, T item)
        {
            var pkProp = metadataProvider.AllProperties.First(p => p.IsPrimaryKey);
            var pkValue = pkProp.GetPropertyValue(item);
            var pkCol = GetColumnIndex(worksheet, pkProp.ColumnName);
            var lastRow = worksheet.LastRowUsed();
            if (lastRow == null) return false;

            for (var row = 2; row <= lastRow.RowNumber(); row++)
            {
                var cellValue = ReadCellValue(worksheet.Cell(row, pkCol), pkProp.PropertyType);
                if (pkValue.Equals(cellValue))
                {
                    WriteRow(worksheet, row, item);
                    return true;
                }
            }

            return false;
        }

        private bool DeleteRow(IXLWorksheet worksheet, T item)
        {
            var pkProp = metadataProvider.AllProperties.First(p => p.IsPrimaryKey);
            var pkValue = pkProp.GetPropertyValue(item);
            var pkCol = GetColumnIndex(worksheet, pkProp.ColumnName);
            var lastRow = worksheet.LastRowUsed();
            if (lastRow == null) return false;

            for (var row = 2; row <= lastRow.RowNumber(); row++)
            {
                var cellValue = ReadCellValue(worksheet.Cell(row, pkCol), pkProp.PropertyType);
                if (pkValue.Equals(cellValue))
                {
                    worksheet.Row(row).Delete();
                    return true;
                }
            }

            return false;
        }

        private void WriteRow(IXLWorksheet worksheet, int rowNumber, T item)
        {
            var col = 1;
            foreach (var prop in metadataProvider.AllProperties)
            {
                var value = prop.GetPropertyValue(item);
                var cell = worksheet.Cell(rowNumber, col++);

                if (value is int i) cell.Value = i;
                else if (value is long l) cell.Value = l;
                else if (value is double d) cell.Value = d;
                else if (value is bool b) cell.Value = b;
                else if (value is DateTime dt) cell.Value = dt;
                else cell.Value = value.ToString();
            }
        }

        private List<T> ReadAll()
        {
            if (!File.Exists(filePath))
                return new List<T>();

            using var workbook = new XLWorkbook(filePath);
            if (!workbook.Worksheets.Contains(SheetName))
                return new List<T>();

            return ReadAllFromWorksheet(workbook.Worksheet(SheetName));
        }

        private List<T> ReadAllFromWorksheet(IXLWorksheet worksheet)
        {
            var lastRow = worksheet.LastRowUsed();
            if (lastRow == null || lastRow.RowNumber() < 2)
                return new List<T>();

            var columnMap = new Dictionary<int, PropertyMap>();
            var headerRow = worksheet.Row(1);
            var lastCol = worksheet.LastColumnUsed();
            if (lastCol == null)
                return new List<T>();

            for (var col = 1; col <= lastCol.ColumnNumber(); col++)
            {
                var columnName = headerRow.Cell(col).GetString();
                var propMap = metadataProvider.GetPropertyMapByColumnName(columnName);
                if (propMap != null)
                    columnMap[col] = propMap;
            }

            var results = new List<T>();
            for (var row = 2; row <= lastRow.RowNumber(); row++)
            {
                var instance = (T)Activator.CreateInstance(typeof(T))!;

                foreach (var (col, propMap) in columnMap)
                {
                    var cell = worksheet.Cell(row, col);
                    if (!cell.IsEmpty())
                    {
                        var value = ReadCellValue(cell, propMap.PropertyType);
                        if (value is not null)
                            propMap.SetPropertyValue(instance, value);
                    }
                }

                results.Add(instance);
            }

            return results;
        }

        private static object? ReadCellValue(IXLCell cell, Type targetType)
        {
            if (cell.IsEmpty()) return null;

            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (cell.Value.IsNumber)   return Convert.ChangeType(cell.Value.GetNumber(),  underlying);
            if (cell.Value.IsBoolean)  return Convert.ChangeType(cell.Value.GetBoolean(), underlying);
            if (cell.Value.IsDateTime) return cell.Value.GetDateTime();
            if (cell.Value.IsText)     return Convert.ChangeType(cell.Value.GetText(),    underlying);

            return null;
        }

        private int GetColumnIndex(IXLWorksheet worksheet, string columnName)
        {
            var headerRow = worksheet.Row(1);
            var lastColUsed = worksheet.LastColumnUsed();
            if (lastColUsed == null)
                throw new InvalidOperationException($"Column '{columnName}' not found in worksheet '{SheetName}'.");

            var lastCol = lastColUsed.ColumnNumber();
            for (var col = 1; col <= lastCol; col++)
            {
                if (headerRow.Cell(col).GetString() == columnName)
                    return col;
            }

            throw new InvalidOperationException($"Column '{columnName}' not found in worksheet '{SheetName}'.");
        }

        private int GetMaxIdentityValue(IXLWorksheet worksheet, PropertyMap identityProp)
        {
            var pkCol = GetColumnIndex(worksheet, identityProp.ColumnName);
            var max = 0;

            var lastRow = worksheet.LastRowUsed();
            if (lastRow == null || lastRow.RowNumber() < 2)
                return max;

            for (var row = 2; row <= lastRow.RowNumber(); row++)
            {
                var cell = worksheet.Cell(row, pkCol);
                if (!cell.IsEmpty() && cell.Value.IsNumber)
                {
                    var val = (int)cell.Value.GetNumber();
                    if (val > max) max = val;
                }
            }

            return max;
        }

        private bool IsDefaultPrimaryKey(T item)
        {
            var pkValue = metadataProvider.GetPrimaryKeyValue(item);
            var pkType = metadataProvider.PrimaryKeyPropertyType;
            return pkType.IsValueType && pkValue.Equals(Activator.CreateInstance(pkType));
        }

        #endregion
    }
}
