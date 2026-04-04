using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ClosedXML.Excel;
using Reform.Interfaces;
using Reform.Logic;
using Reform.Objects;

namespace ReformTests
{
    public class ExcelReform<T> : Reform<T> where T : class
    {
        private readonly string _filePath;
        private readonly IMetadataProvider<T> _metadataProvider;

        public ExcelReform(string filePath, IMetadataProvider<T> metadataProvider, IValidator<T> validator)
            : base(validator, metadataProvider)
        {
            _filePath = filePath;
            _metadataProvider = metadataProvider;
        }

        private string SheetName => _metadataProvider.TableName;

        #region CRUD overrides

        public override void Insert(T item)
        {
            OnValidate(null, item);

            using (var workbook = LoadOrCreateWorkbook())
            {
                var worksheet = GetOrCreateWorksheet(workbook);
                int nextRow = (worksheet.LastRowUsed()?.RowNumber() ?? 1) + 1;

                // Auto-increment identity
                var identityProp = _metadataProvider.AllProperties
                    .FirstOrDefault(p => p.IsIdentity);

                if (identityProp != null)
                {
                    int nextId = GetMaxIdentityValue(worksheet, identityProp) + 1;
                    identityProp.SetPropertyValue(item, nextId);
                }

                WriteRow(worksheet, nextRow, item);
                workbook.SaveAs(_filePath);
            }
        }

        public override void Insert(List<T> items)
        {
            foreach (T item in items)
                Insert(item);
        }

        public override void Update(T item)
        {
            OnValidate(null, item);

            using (var workbook = LoadOrCreateWorkbook())
            {
                var worksheet = workbook.Worksheet(SheetName);
                var pkProp = _metadataProvider.AllProperties.First(p => p.IsPrimaryKey);
                object pkValue = pkProp.GetPropertyValue(item);
                int pkCol = GetColumnIndex(worksheet, pkProp.ColumnName);

                for (int row = 2; row <= worksheet.LastRowUsed().RowNumber(); row++)
                {
                    var cellValue = ReadCellValue(worksheet.Cell(row, pkCol), pkProp.PropertyType);
                    if (pkValue.Equals(cellValue))
                    {
                        WriteRow(worksheet, row, item);
                        workbook.SaveAs(_filePath);
                        return;
                    }
                }

                throw new ApplicationException($"Expected to find 1 {typeof(T).Name} but found 0");
            }
        }

        public override void Update(List<T> list)
        {
            foreach (T item in list)
                Update(item);
        }

        public override void Delete(T item)
        {
            using (var workbook = LoadOrCreateWorkbook())
            {
                var worksheet = workbook.Worksheet(SheetName);
                var pkProp = _metadataProvider.AllProperties.First(p => p.IsPrimaryKey);
                object pkValue = pkProp.GetPropertyValue(item);
                int pkCol = GetColumnIndex(worksheet, pkProp.ColumnName);

                for (int row = 2; row <= worksheet.LastRowUsed().RowNumber(); row++)
                {
                    var cellValue = ReadCellValue(worksheet.Cell(row, pkCol), pkProp.PropertyType);
                    if (pkValue.Equals(cellValue))
                    {
                        worksheet.Row(row).Delete();
                        workbook.SaveAs(_filePath);
                        return;
                    }
                }
            }
        }

        public override void Delete(List<T> list)
        {
            foreach (T item in list)
                Delete(item);
        }

        public override IEnumerable<T> Select()
        {
            return ReadAll();
        }

        public override IEnumerable<T> Select(Expression<Func<T, bool>> predicate)
        {
            var filter = predicate.Compile();
            return ReadAll().Where(filter).ToList();
        }

        public override IEnumerable<T> Select(QueryCriteria<T> queryCriteria)
        {
            if (queryCriteria.Predicate != null)
                return Select(queryCriteria.Predicate);

            return ReadAll();
        }

        public override T SelectSingle(Expression<Func<T, bool>> predicate)
        {
            var list = Select(predicate).ToList();
            if (list.Count == 1) return list[0];
            throw new ApplicationException($"Expected to find 1 {typeof(T).Name} but found {list.Count}");
        }

        public override T SelectSingleOrDefault(Expression<Func<T, bool>> predicate)
        {
            var list = Select(predicate).ToList();
            if (list.Count > 1)
                throw new ApplicationException($"Expected to find 1 or 0 {typeof(T).Name} but found {list.Count}");
            return list.FirstOrDefault();
        }

        public override int Count()
        {
            return ReadAll().Count;
        }

        public override int Count(Expression<Func<T, bool>> predicate)
        {
            var filter = predicate.Compile();
            return ReadAll().Count(filter);
        }

        public override bool Exists(Expression<Func<T, bool>> predicate)
        {
            var filter = predicate.Compile();
            return ReadAll().Any(filter);
        }

        #endregion

        #region Excel I/O

        private XLWorkbook LoadOrCreateWorkbook()
        {
            return System.IO.File.Exists(_filePath)
                ? new XLWorkbook(_filePath)
                : new XLWorkbook();
        }

        private IXLWorksheet GetOrCreateWorksheet(XLWorkbook workbook)
        {
            if (workbook.Worksheets.Contains(SheetName))
                return workbook.Worksheet(SheetName);

            var worksheet = workbook.Worksheets.Add(SheetName);

            // Write header row
            int col = 1;
            foreach (var prop in _metadataProvider.AllProperties)
            {
                worksheet.Cell(1, col++).Value = prop.ColumnName;
            }

            return worksheet;
        }

        private void WriteRow(IXLWorksheet worksheet, int rowNumber, T item)
        {
            int col = 1;
            foreach (var prop in _metadataProvider.AllProperties)
            {
                var value = prop.GetPropertyValue(item);
                var cell = worksheet.Cell(rowNumber, col++);

                if (value == null)
                {
                    cell.Value = Blank.Value;
                }
                else if (value is int i) cell.Value = i;
                else if (value is long l) cell.Value = l;
                else if (value is double d) cell.Value = d;
                else if (value is bool b) cell.Value = b;
                else if (value is DateTime dt) cell.Value = dt;
                else cell.Value = value.ToString();
            }
        }

        private List<T> ReadAll()
        {
            if (!System.IO.File.Exists(_filePath))
                return new List<T>();

            using (var workbook = new XLWorkbook(_filePath))
            {
                if (!workbook.Worksheets.Contains(SheetName))
                    return new List<T>();

                var worksheet = workbook.Worksheet(SheetName);
                var lastRow = worksheet.LastRowUsed();

                if (lastRow == null || lastRow.RowNumber() < 2)
                    return new List<T>();

                // Build column map from header row
                var columnMap = new Dictionary<int, PropertyMap>();
                var headerRow = worksheet.Row(1);
                int lastCol = worksheet.LastColumnUsed().ColumnNumber();

                for (int col = 1; col <= lastCol; col++)
                {
                    string columnName = headerRow.Cell(col).GetString();
                    var propMap = _metadataProvider.GetPropertyMapByColumnName(columnName);
                    if (propMap != null)
                        columnMap[col] = propMap;
                }

                // Read data rows
                var results = new List<T>();
                for (int row = 2; row <= lastRow.RowNumber(); row++)
                {
                    T instance = (T)Activator.CreateInstance(typeof(T));

                    foreach (var (col, propMap) in columnMap)
                    {
                        var cell = worksheet.Cell(row, col);
                        if (!cell.IsEmpty())
                        {
                            object value = ReadCellValue(cell, propMap.PropertyType);
                            propMap.SetPropertyValue(instance, value);
                        }
                    }

                    results.Add(instance);
                }

                return results;
            }
        }

        private object ReadCellValue(IXLCell cell, Type targetType)
        {
            if (cell.IsEmpty()) return null;

            Type underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (cell.Value.IsNumber)
                return Convert.ChangeType(cell.Value.GetNumber(), underlying);
            if (cell.Value.IsBoolean)
                return Convert.ChangeType(cell.Value.GetBoolean(), underlying);
            if (cell.Value.IsDateTime)
                return cell.Value.GetDateTime();
            if (cell.Value.IsText)
                return Convert.ChangeType(cell.Value.GetText(), underlying);

            return null;
        }

        private int GetColumnIndex(IXLWorksheet worksheet, string columnName)
        {
            var headerRow = worksheet.Row(1);
            int lastCol = worksheet.LastColumnUsed().ColumnNumber();

            for (int col = 1; col <= lastCol; col++)
            {
                if (headerRow.Cell(col).GetString() == columnName)
                    return col;
            }

            throw new ApplicationException($"Column '{columnName}' not found in worksheet '{SheetName}'");
        }

        private int GetMaxIdentityValue(IXLWorksheet worksheet, PropertyMap identityProp)
        {
            int pkCol = GetColumnIndex(worksheet, identityProp.ColumnName);
            int max = 0;

            var lastRow = worksheet.LastRowUsed();
            if (lastRow == null || lastRow.RowNumber() < 2)
                return max;

            for (int row = 2; row <= lastRow.RowNumber(); row++)
            {
                var cell = worksheet.Cell(row, pkCol);
                if (!cell.IsEmpty() && cell.Value.IsNumber)
                {
                    int val = (int)cell.Value.GetNumber();
                    if (val > max) max = val;
                }
            }

            return max;
        }

        #endregion
    }
}
