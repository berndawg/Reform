using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Reform.Interfaces;
using Reform.Objects;

namespace Reform.Logic
{
    public class Reform<T> : IReform<T> where T : class
    {
        private readonly IConnectionProvider<T> _connectionProvider;
        private readonly IDataAccess<T> _dataAccess;
        private readonly IValidator<T> _validator;
        private readonly IMetadataProvider<T>? _metadataProvider;

        public Reform(IConnectionProvider<T> connectionProvider, IDataAccess<T> dataAccess, IValidator<T> validator, IMetadataProvider<T> metadataProvider)
        {
            _connectionProvider = connectionProvider;
            _dataAccess = dataAccess;
            _validator = validator;
            _metadataProvider = metadataProvider;
        }

        /// <summary>
        /// Initializes a <see cref="Reform{T}"/> for derived types that supply their own persistence (for example, a non-database store).
        /// Database-oriented APIs throw <see cref="InvalidOperationException"/> unless those methods are overridden.
        /// </summary>
        protected Reform(IValidator<T> validator, IMetadataProvider<T>? metadataProvider = null)
        {
            _connectionProvider = null!;
            _dataAccess = null!;
            _validator = validator;
            _metadataProvider = metadataProvider;
        }

        [MemberNotNull(nameof(_connectionProvider))]
        [MemberNotNull(nameof(_dataAccess))]
        private void EnsureDataLayer()
        {
            if (_connectionProvider == null || _dataAccess == null)
            {
                throw new InvalidOperationException(
                    $"Reform<{typeof(T).Name}> was constructed with the constructor intended for custom storage backends (validator only). " +
                    "Use the constructor that accepts IConnectionProvider<T> and IDataAccess<T>, or override every method that performs database access.");
            }
        }

        private async Task<DbConnection> GetOpenedConnectionAsync()
        {
            EnsureDataLayer();
            return await _connectionProvider.GetConnectionAsync();
        }

        #region Connection

        public IDbConnection GetConnection()
        {
            return OnGetConnection();
        }

        #endregion

        #region Count

        public virtual int Count()
        {
            using var connection = GetConnection();
            return OnCount(connection, null);
        }

        public virtual int Count(Expression<Func<T, bool>> predicate)
        {
            using var connection = GetConnection();
            return OnCount(connection, predicate);
        }

        public virtual async Task<int> CountAsync()
        {
            await using var connection = await GetOpenedConnectionAsync();
            return await OnCountAsync(connection, null);
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            await using var connection = await GetOpenedConnectionAsync();
            return await OnCountAsync(connection, predicate);
        }

        #endregion

        #region Exists

        public virtual bool Exists(Expression<Func<T, bool>> predicate)
        {
            using var connection = GetConnection();
            return OnExists(connection, predicate);
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            await using var connection = await GetOpenedConnectionAsync();
            return await OnExistsAsync(connection, predicate);
        }

        #endregion

        #region Insert

        public virtual void Insert(T item)
        {
            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                InsertInternal(connection, transaction, item);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public virtual void Insert(List<T> items)
        {
            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                foreach (var item in items)
                    InsertInternal(connection, transaction, item);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void Insert(IDbConnection connection, T item)
        {
            InsertInternal(connection, null!, item);
        }

        public void Insert(IDbConnection connection, IDbTransaction transaction, T item)
        {
            InsertInternal(connection, transaction, item);
        }

        public async Task InsertAsync(T item)
        {
            await using var connection = await GetOpenedConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                await InsertInternalAsync(connection, transaction, item);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task InsertAsync(List<T> items)
        {
            await using var connection = await GetOpenedConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                foreach (var item in items)
                    await InsertInternalAsync(connection, transaction, item);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task InsertAsync(IDbConnection connection, IDbTransaction transaction, T item)
        {
            await InsertInternalAsync(connection, transaction, item);
        }

        private void InsertInternal(IDbConnection connection, IDbTransaction transaction, T item)
        {
            OnBeforeInsert(connection, transaction, item);
            OnValidate(connection, item);
            OnInsert(connection, transaction, item);
            OnAfterInsert(connection, transaction, item);
        }

        private async Task InsertInternalAsync(IDbConnection connection, IDbTransaction transaction, T item)
        {
            await OnBeforeInsertAsync(connection, transaction, item);
            OnValidate(connection, item);
            await OnInsertAsync(connection, transaction, item);
            await OnAfterInsertAsync(connection, transaction, item);
        }

        #endregion

        #region Update

        public virtual void Update(T item)
        {
            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                UpdateInternal(connection, transaction, item);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public virtual void Update(List<T> list)
        {
            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                foreach (var item in list)
                    UpdateInternal(connection, transaction, item);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void Update(IDbConnection connection, T item)
        {
            UpdateInternal(connection, null!, item);
        }

        public void Update(IDbConnection connection, IDbTransaction transaction, T item)
        {
            UpdateInternal(connection, transaction, item);
        }

        public async Task UpdateAsync(T item)
        {
            await using var connection = await GetOpenedConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                await UpdateInternalAsync(connection, transaction, item);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateAsync(List<T> list)
        {
            await using var connection = await GetOpenedConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                foreach (var item in list)
                    await UpdateInternalAsync(connection, transaction, item);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateAsync(IDbConnection connection, IDbTransaction transaction, T item)
        {
            await UpdateInternalAsync(connection, transaction, item);
        }

        private void UpdateInternal(IDbConnection connection, IDbTransaction transaction, T item)
        {
            OnBeforeUpdate(connection, transaction, item);
            OnValidate(connection, item);
            OnUpdate(connection, transaction, item);
            OnAfterUpdate(connection, transaction, item);
        }

        private async Task UpdateInternalAsync(IDbConnection connection, IDbTransaction transaction, T item)
        {
            await OnBeforeUpdateAsync(connection, transaction, item);
            OnValidate(connection, item);
            await OnUpdateAsync(connection, transaction, item);
            await OnAfterUpdateAsync(connection, transaction, item);
        }

        #endregion

        #region Delete

        public virtual void Delete(T item)
        {
            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                DeleteInternal(connection, transaction, item);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public virtual void Delete(List<T> list)
        {
            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                foreach (var item in list)
                    DeleteInternal(connection, transaction, item);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void Delete(IDbConnection connection, T item)
        {
            DeleteInternal(connection, null!, item);
        }

        public void Delete(IDbConnection connection, IDbTransaction transaction, T item)
        {
            DeleteInternal(connection, transaction, item);
        }

        public async Task DeleteAsync(T item)
        {
            await using var connection = await GetOpenedConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                await DeleteInternalAsync(connection, transaction, item);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteAsync(List<T> list)
        {
            await using var connection = await GetOpenedConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                foreach (var item in list)
                    await DeleteInternalAsync(connection, transaction, item);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteAsync(IDbConnection connection, IDbTransaction transaction, T item)
        {
            await DeleteInternalAsync(connection, transaction, item);
        }

        private void DeleteInternal(IDbConnection connection, IDbTransaction transaction, T item)
        {
            OnBeforeDelete(connection, transaction, item);
            OnDelete(connection, transaction, item);
            OnAfterDelete(connection, transaction, item);
        }

        private async Task DeleteInternalAsync(IDbConnection connection, IDbTransaction transaction, T item)
        {
            await OnBeforeDeleteAsync(connection, transaction, item);
            await OnDeleteAsync(connection, transaction, item);
            await OnAfterDeleteAsync(connection, transaction, item);
        }

        #endregion

        #region Merge

        public virtual void Merge(List<T> list)
        {
            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                MergeInternal(connection, transaction, list);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public virtual async Task MergeAsync(List<T> list)
        {
            await using var connection = await GetOpenedConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                await MergeInternalAsync(connection, transaction, list);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private void MergeInternal(IDbConnection connection, IDbTransaction transaction, List<T> list)
        {
            if (list.Count == 0)
                throw new ArgumentException("Cannot merge an empty list. Use Delete to remove all rows.", nameof(list));

            if (_metadataProvider == null)
                throw new InvalidOperationException("Merge requires IMetadataProvider<T>. Pass it to the Reform<T> constructor.");

            var existing = OnSelect(connection, transaction, new QueryCriteria<T>()).ToList();
            var existingByPk = new Dictionary<object, T>();
            foreach (var record in existing)
                existingByPk[_metadataProvider.GetPrimaryKeyValue(record)] = record;

            var accountedPks = new HashSet<object>();

            foreach (var item in list)
            {
                if (IsDefaultPrimaryKey(item))
                {
                    InsertInternal(connection, transaction, item);
                }
                else
                {
                    var pk = _metadataProvider.GetPrimaryKeyValue(item);
                    accountedPks.Add(pk);

                    if (existingByPk.ContainsKey(pk))
                        UpdateInternal(connection, transaction, item);
                    else
                        InsertInternal(connection, transaction, item);
                }
            }

            foreach (var kvp in existingByPk.Where(kvp => !accountedPks.Contains(kvp.Key)))
            {
                DeleteInternal(connection, transaction, kvp.Value);
            }
        }

        private async Task MergeInternalAsync(IDbConnection connection, IDbTransaction transaction, List<T> list)
        {
            if (list.Count == 0)
                throw new ArgumentException("Cannot merge an empty list. Use Delete to remove all rows.", nameof(list));

            if (_metadataProvider == null)
                throw new InvalidOperationException("Merge requires IMetadataProvider<T>. Pass it to the Reform<T> constructor.");

            var existing = (await OnSelectAsync(connection, transaction, new QueryCriteria<T>())).ToList();
            var existingByPk = new Dictionary<object, T>();
            foreach (var record in existing)
                existingByPk[_metadataProvider.GetPrimaryKeyValue(record)] = record;

            var accountedPks = new HashSet<object>();

            foreach (var item in list)
            {
                if (IsDefaultPrimaryKey(item))
                {
                    await InsertInternalAsync(connection, transaction, item);
                }
                else
                {
                    var pk = _metadataProvider.GetPrimaryKeyValue(item);
                    accountedPks.Add(pk);

                    if (existingByPk.ContainsKey(pk))
                        await UpdateInternalAsync(connection, transaction, item);
                    else
                        await InsertInternalAsync(connection, transaction, item);
                }
            }

            foreach (var kvp in existingByPk.Where(kvp => !accountedPks.Contains(kvp.Key)))
            {
                await DeleteInternalAsync(connection, transaction, kvp.Value);
            }
        }

        #endregion

        #region Truncate

        public virtual void Truncate()
        {
            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                OnTruncate(connection, transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public virtual async Task TruncateAsync()
        {
            await using var connection = await GetOpenedConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                await OnTruncateAsync(connection, transaction);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion

        private bool IsDefaultPrimaryKey(T item)
        {
            var pkValue = _metadataProvider!.GetPrimaryKeyValue(item);
            var pkType = _metadataProvider.PrimaryKeyPropertyType;
            return pkType.IsValueType && pkValue.Equals(Activator.CreateInstance(pkType));
        }

        #region SelectSingle

        public virtual T SelectSingle(Expression<Func<T, bool>> predicate)
        {
            var list = Select(predicate).ToList();

            if (list.Count == 1)
                return list[0];

            throw new InvalidOperationException($"Expected to find 1 {typeof(T).Name} but found {list.Count}.");
        }

        public virtual T SelectSingleOrDefault(Expression<Func<T, bool>> predicate)
        {
            var list = Select(predicate).ToList();

            if (list.Count > 1)
                throw new InvalidOperationException($"Expected to find 1 or 0 {typeof(T).Name} but found {list.Count}.");

            return list.FirstOrDefault()!;
        }

        public async Task<T> SelectSingleAsync(Expression<Func<T, bool>> predicate)
        {
            var list = (await SelectAsync(predicate)).ToList();

            if (list.Count == 1)
                return list[0];

            throw new InvalidOperationException($"Expected to find 1 {typeof(T).Name} but found {list.Count}.");
        }

        public async Task<T> SelectSingleOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            var list = (await SelectAsync(predicate)).ToList();

            if (list.Count > 1)
                throw new InvalidOperationException($"Expected to find 1 or 0 {typeof(T).Name} but found {list.Count}.");

            return list.FirstOrDefault()!;
        }

        #endregion

        #region Select

        public virtual IEnumerable<T> Select()
        {
            return Select(new QueryCriteria<T>());
        }

        public virtual IEnumerable<T> Select(Expression<Func<T, bool>> predicate)
        {
            return Select(new QueryCriteria<T> { Predicate = predicate });
        }

        public virtual IEnumerable<T> Select(QueryCriteria<T> queryCriteria)
        {
            using var connection = GetConnection();
            return OnSelect(connection, queryCriteria);
        }

        public Task<IEnumerable<T>> SelectAsync()
        {
            return SelectAsync(new QueryCriteria<T>());
        }

        public Task<IEnumerable<T>> SelectAsync(Expression<Func<T, bool>> predicate)
        {
            return SelectAsync(new QueryCriteria<T> { Predicate = predicate });
        }

        public async Task<IEnumerable<T>> SelectAsync(QueryCriteria<T> queryCriteria)
        {
            await using var connection = await GetOpenedConnectionAsync();
            return await OnSelectAsync(connection, queryCriteria);
        }

        #endregion

        #region Overrideables

        protected virtual IDbConnection OnGetConnection()
        {
            EnsureDataLayer();
            return _connectionProvider.GetConnection();
        }

        protected virtual void OnValidate(IDbConnection connection, T item)
        {
            _validator.Validate(item);
        }

        protected virtual int OnCount(IDbConnection connection, Expression<Func<T, bool>>? predicate)
        {
            EnsureDataLayer();
            return _dataAccess.Count(connection, null, predicate);
        }

        protected virtual Task<int> OnCountAsync(IDbConnection connection, Expression<Func<T, bool>>? predicate)
        {
            EnsureDataLayer();
            return _dataAccess.CountAsync(connection, null, predicate);
        }

        protected virtual bool OnExists(IDbConnection connection, Expression<Func<T, bool>> predicate)
        {
            EnsureDataLayer();
            return _dataAccess.Exists(connection, null, predicate);
        }

        protected virtual Task<bool> OnExistsAsync(IDbConnection connection, Expression<Func<T, bool>> predicate)
        {
            EnsureDataLayer();
            return _dataAccess.ExistsAsync(connection, null, predicate);
        }

        protected virtual IEnumerable<T> OnSelect(IDbConnection connection, QueryCriteria<T> queryCriteria)
        {
            EnsureDataLayer();
            return _dataAccess.Select(connection, null, queryCriteria);
        }

        protected virtual IEnumerable<T> OnSelect(IDbConnection connection, IDbTransaction transaction, QueryCriteria<T> queryCriteria)
        {
            EnsureDataLayer();
            return _dataAccess.Select(connection, transaction, queryCriteria);
        }

        protected virtual Task<IEnumerable<T>> OnSelectAsync(IDbConnection connection, QueryCriteria<T> queryCriteria)
        {
            EnsureDataLayer();
            return _dataAccess.SelectAsync(connection, null, queryCriteria);
        }

        protected virtual Task<IEnumerable<T>> OnSelectAsync(IDbConnection connection, IDbTransaction transaction, QueryCriteria<T> queryCriteria)
        {
            EnsureDataLayer();
            return _dataAccess.SelectAsync(connection, transaction, queryCriteria);
        }

        protected virtual void OnInsert(IDbConnection connection, IDbTransaction transaction, T item)
        {
            EnsureDataLayer();
            _dataAccess.Insert(connection, transaction, item);
        }

        protected virtual Task OnInsertAsync(IDbConnection connection, IDbTransaction transaction, T item)
        {
            EnsureDataLayer();
            return _dataAccess.InsertAsync(connection, transaction, item);
        }

        protected virtual void OnUpdate(IDbConnection connection, IDbTransaction transaction, T item)
        {
            EnsureDataLayer();
            _dataAccess.Update(connection, transaction, item);
        }

        protected virtual Task OnUpdateAsync(IDbConnection connection, IDbTransaction transaction, T item)
        {
            EnsureDataLayer();
            return _dataAccess.UpdateAsync(connection, transaction, item);
        }

        protected virtual void OnDelete(IDbConnection connection, IDbTransaction transaction, T item)
        {
            EnsureDataLayer();
            _dataAccess.Delete(connection, transaction, item);
        }

        protected virtual Task OnDeleteAsync(IDbConnection connection, IDbTransaction transaction, T item)
        {
            EnsureDataLayer();
            return _dataAccess.DeleteAsync(connection, transaction, item);
        }

        protected virtual void OnTruncate(IDbConnection connection, IDbTransaction transaction)
        {
            EnsureDataLayer();
            _dataAccess.Truncate(connection, transaction);
        }

        protected virtual Task OnTruncateAsync(IDbConnection connection, IDbTransaction transaction)
        {
            EnsureDataLayer();
            return _dataAccess.TruncateAsync(connection, transaction);
        }

        protected virtual void OnBeforeInsert(IDbConnection connection, IDbTransaction transaction, T item) { }
        protected virtual void OnBeforeUpdate(IDbConnection connection, IDbTransaction transaction, T item) { }
        protected virtual void OnAfterInsert(IDbConnection connection, IDbTransaction transaction, T item) { }
        protected virtual void OnAfterUpdate(IDbConnection connection, IDbTransaction transaction, T item) { }
        protected virtual void OnBeforeDelete(IDbConnection connection, IDbTransaction transaction, T item) { }
        protected virtual void OnAfterDelete(IDbConnection connection, IDbTransaction transaction, T item) { }

        protected virtual Task OnBeforeInsertAsync(IDbConnection connection, IDbTransaction transaction, T item) { OnBeforeInsert(connection, transaction, item); return Task.CompletedTask; }
        protected virtual Task OnBeforeUpdateAsync(IDbConnection connection, IDbTransaction transaction, T item) { OnBeforeUpdate(connection, transaction, item); return Task.CompletedTask; }
        protected virtual Task OnAfterInsertAsync(IDbConnection connection, IDbTransaction transaction, T item) { OnAfterInsert(connection, transaction, item); return Task.CompletedTask; }
        protected virtual Task OnAfterUpdateAsync(IDbConnection connection, IDbTransaction transaction, T item) { OnAfterUpdate(connection, transaction, item); return Task.CompletedTask; }
        protected virtual Task OnBeforeDeleteAsync(IDbConnection connection, IDbTransaction transaction, T item) { OnBeforeDelete(connection, transaction, item); return Task.CompletedTask; }
        protected virtual Task OnAfterDeleteAsync(IDbConnection connection, IDbTransaction transaction, T item) { OnAfterDelete(connection, transaction, item); return Task.CompletedTask; }

        #endregion
    }
}
