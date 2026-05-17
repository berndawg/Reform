using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using Reform.Interfaces;
using Reform.Objects;

namespace Reform.Logic
{
    public sealed class Reform<T>(
        IConnectionProvider<T> connectionProvider,
        IDataAccess<T> dataAccess,
        IValidator<T> validator,
        IMetadataProvider<T> metadataProvider)
        : IReform<T>
        where T : class
    {
        public IDbConnection GetConnection() => connectionProvider.GetConnection();

        #region Reads (sync)

        public int Count() => Read(c => dataAccess.Count(c, null, null));
        public int Count(Expression<Func<T, bool>> predicate) => Read(c => dataAccess.Count(c, null, predicate));
        public bool Exists(Expression<Func<T, bool>> predicate) => Read(c => dataAccess.Exists(c, null, predicate));

        public IEnumerable<T> Select() => Select(new QueryCriteria<T>());
        public IEnumerable<T> Select(Expression<Func<T, bool>> predicate) => Select(new QueryCriteria<T> { Predicate = predicate });
        public IEnumerable<T> Select(QueryCriteria<T> queryCriteria) => Read(c => dataAccess.Select(c, null, queryCriteria));

        public T SelectSingle(Expression<Func<T, bool>> predicate)
        {
            var list = Select(predicate).ToList();
            if (list.Count == 1) return list[0];
            throw new InvalidOperationException($"Expected to find 1 {typeof(T).Name} but found {list.Count}.");
        }

        public T SelectSingleOrDefault(Expression<Func<T, bool>> predicate)
        {
            var list = Select(predicate).ToList();
            if (list.Count > 1) throw new InvalidOperationException($"Expected to find 1 or 0 {typeof(T).Name} but found {list.Count}.");
            return list.FirstOrDefault()!;
        }

        #endregion

        #region Reads (async)

        public Task<int>  CountAsync() => ReadAsync(c => dataAccess.CountAsync(c, null, null));
        public Task<int>  CountAsync(Expression<Func<T, bool>> predicate) => ReadAsync(c => dataAccess.CountAsync(c, null, predicate));
        public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate) => ReadAsync(c => dataAccess.ExistsAsync(c, null, predicate));

        public Task<IEnumerable<T>> SelectAsync() => SelectAsync(new QueryCriteria<T>());
        public Task<IEnumerable<T>> SelectAsync(Expression<Func<T, bool>> predicate) => SelectAsync(new QueryCriteria<T> { Predicate = predicate });
        public Task<IEnumerable<T>> SelectAsync(QueryCriteria<T> queryCriteria) => ReadAsync(c => dataAccess.SelectAsync(c, null, queryCriteria));

        public async Task<T> SelectSingleAsync(Expression<Func<T, bool>> predicate)
        {
            var list = (await SelectAsync(predicate)).ToList();
            if (list.Count == 1) return list[0];
            throw new InvalidOperationException($"Expected to find 1 {typeof(T).Name} but found {list.Count}.");
        }

        public async Task<T> SelectSingleOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            var list = (await SelectAsync(predicate)).ToList();
            if (list.Count > 1) throw new InvalidOperationException($"Expected to find 1 or 0 {typeof(T).Name} but found {list.Count}.");
            return list.FirstOrDefault()!;
        }

        #endregion

        #region Writes (sync)

        public void Insert(T item) => Write((c, t) => InsertOne(c, t, item));
        public void Insert(List<T> items) => Write((c, t) => { foreach (var i in items) InsertOne(c, t, i); });
        public void Insert(IDbConnection connection, T item) => InsertOne(connection, null, item);
        public void Insert(IDbConnection connection, IDbTransaction transaction, T item) => InsertOne(connection, transaction, item);

        public void Update(T item) => Write((c, t) => UpdateOne(c, t, item));
        public void Update(List<T> list) => Write((c, t) => { foreach (var i in list) UpdateOne(c, t, i); });
        public void Update(IDbConnection connection, T item) => UpdateOne(connection, null, item);
        public void Update(IDbConnection connection, IDbTransaction transaction, T item) => UpdateOne(connection, transaction, item);

        public void Delete(T item) => Write((c, t) => dataAccess.Delete(c, t, item));
        public void Delete(List<T> list) => Write((c, t) => { foreach (var i in list) dataAccess.Delete(c, t, i); });
        public void Delete(IDbConnection connection, T item) => dataAccess.Delete(connection, null, item);
        public void Delete(IDbConnection connection, IDbTransaction transaction, T item) => dataAccess.Delete(connection, transaction, item);

        public void Merge(List<T> list) => Write((c, t) => MergeCore(c, t, list));
        public void Truncate() => Write((c, t) => dataAccess.Truncate(c, t));

        private void InsertOne(IDbConnection connection, IDbTransaction? transaction, T item)
        {
            validator.Validate(item);
            dataAccess.Insert(connection, transaction, item);
        }

        private void UpdateOne(IDbConnection connection, IDbTransaction? transaction, T item)
        {
            validator.Validate(item);
            dataAccess.Update(connection, transaction, item);
        }

        #endregion

        #region Writes (async)

        public Task InsertAsync(T item) => WriteAsync((c, t) => InsertOneAsync(c, t, item));
        public Task InsertAsync(List<T> items) => WriteAsync(async (c, t) => { foreach (var i in items) await InsertOneAsync(c, t, i); });
        public Task InsertAsync(IDbConnection connection, IDbTransaction transaction, T item) => InsertOneAsync(connection, transaction, item);

        public Task UpdateAsync(T item) => WriteAsync((c, t) => UpdateOneAsync(c, t, item));
        public Task UpdateAsync(List<T> list) => WriteAsync(async (c, t) => { foreach (var i in list) await UpdateOneAsync(c, t, i); });
        public Task UpdateAsync(IDbConnection connection, IDbTransaction transaction, T item) => UpdateOneAsync(connection, transaction, item);

        public Task DeleteAsync(T item) => WriteAsync((c, t) => dataAccess.DeleteAsync(c, t, item));
        public Task DeleteAsync(List<T> list) => WriteAsync(async (c, t) => { foreach (var i in list) await dataAccess.DeleteAsync(c, t, i); });
        public Task DeleteAsync(IDbConnection connection, IDbTransaction transaction, T item) => dataAccess.DeleteAsync(connection, transaction, item);

        public Task MergeAsync(List<T> list) => WriteAsync((c, t) => MergeCoreAsync(c, t, list));
        public Task TruncateAsync() => WriteAsync((c, t) => dataAccess.TruncateAsync(c, t));

        private async Task InsertOneAsync(IDbConnection connection, IDbTransaction? transaction, T item)
        {
            validator.Validate(item);
            await dataAccess.InsertAsync(connection, transaction, item);
        }

        private async Task UpdateOneAsync(IDbConnection connection, IDbTransaction? transaction, T item)
        {
            validator.Validate(item);
            await dataAccess.UpdateAsync(connection, transaction, item);
        }

        #endregion

        #region Merge

        private void MergeCore(IDbConnection connection, IDbTransaction transaction, List<T> list)
        {
            if (list.Count == 0)
                throw new ArgumentException("Cannot merge an empty list. Use Delete to remove all rows.", nameof(list));

            var existing = dataAccess.Select(connection, transaction, new QueryCriteria<T>()).ToList();
            var existingByPk = new Dictionary<object, T>();
            foreach (var record in existing)
                existingByPk[metadataProvider.GetPrimaryKeyValue(record)] = record;

            var accountedPks = new HashSet<object>();

            foreach (var item in list)
            {
                if (IsDefaultPrimaryKey(item))
                {
                    InsertOne(connection, transaction, item);
                }
                else
                {
                    var pk = metadataProvider.GetPrimaryKeyValue(item);
                    accountedPks.Add(pk);

                    if (existingByPk.ContainsKey(pk)) UpdateOne(connection, transaction, item);
                    else                              InsertOne(connection, transaction, item);
                }
            }

            foreach (var kvp in existingByPk.Where(kvp => !accountedPks.Contains(kvp.Key)))
                dataAccess.Delete(connection, transaction, kvp.Value);
        }

        private async Task MergeCoreAsync(IDbConnection connection, IDbTransaction transaction, List<T> list)
        {
            if (list.Count == 0)
                throw new ArgumentException("Cannot merge an empty list. Use Delete to remove all rows.", nameof(list));

            var existing = (await dataAccess.SelectAsync(connection, transaction, new QueryCriteria<T>())).ToList();
            var existingByPk = new Dictionary<object, T>();
            foreach (var record in existing)
                existingByPk[metadataProvider.GetPrimaryKeyValue(record)] = record;

            var accountedPks = new HashSet<object>();

            foreach (var item in list)
            {
                if (IsDefaultPrimaryKey(item))
                {
                    await InsertOneAsync(connection, transaction, item);
                }
                else
                {
                    var pk = metadataProvider.GetPrimaryKeyValue(item);
                    accountedPks.Add(pk);

                    if (existingByPk.ContainsKey(pk)) await UpdateOneAsync(connection, transaction, item);
                    else                              await InsertOneAsync(connection, transaction, item);
                }
            }

            foreach (var kvp in existingByPk.Where(kvp => !accountedPks.Contains(kvp.Key)))
                await dataAccess.DeleteAsync(connection, transaction, kvp.Value);
        }

        private bool IsDefaultPrimaryKey(T item)
        {
            var pkValue = metadataProvider.GetPrimaryKeyValue(item);
            var pkType = metadataProvider.PrimaryKeyPropertyType;
            return pkType.IsValueType && pkValue.Equals(Activator.CreateInstance(pkType));
        }

        #endregion

        #region Helpers

        private TR Read<TR>(Func<IDbConnection, TR> body)
        {
            using var connection = connectionProvider.GetConnection();
            return body(connection);
        }

        private void Write(Action<IDbConnection, IDbTransaction> body)
        {
            using var connection = connectionProvider.GetConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                body(connection, transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private async Task<TR> ReadAsync<TR>(Func<DbConnection, Task<TR>> body)
        {
            await using var connection = await connectionProvider.GetConnectionAsync();
            return await body(connection);
        }

        private async Task WriteAsync(Func<DbConnection, DbTransaction, Task> body)
        {
            await using var connection = await connectionProvider.GetConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                await body(connection, transaction);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion
    }
}
