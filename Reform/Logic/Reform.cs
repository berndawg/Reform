using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Reform.Interfaces;
using Reform.Objects;

namespace Reform.Logic
{
    public class Reform<T> : IReform<T> where T : class
    {
        private readonly IConnectionProvider<T> _connectionProvider;
        private readonly IDataAccess<T> _dataAccess;
        private readonly IValidator<T> _validator;
        private readonly IMetadataProvider<T> _metadataProvider;

        public Reform(IConnectionProvider<T> connectionProvider, IDataAccess<T> dataAccess, IValidator<T> validator, IMetadataProvider<T> metadataProvider)
        {
            _connectionProvider = connectionProvider;
            _dataAccess = dataAccess;
            _validator = validator;
            _metadataProvider = metadataProvider;
        }

        protected Reform(IValidator<T> validator, IMetadataProvider<T> metadataProvider = null)
        {
            _validator = validator;
            _metadataProvider = metadataProvider;
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
            using (IDbConnection connection = GetConnection())
            {
                return OnCount(connection, null);
            }
        }

        public virtual int Count(Expression<Func<T, bool>> predicate)
        {
            using (IDbConnection connection = GetConnection())
            {
                return OnCount(connection, predicate);
            }
        }

        public virtual async Task<int> CountAsync()
        {
            await using (DbConnection connection = await _connectionProvider.GetConnectionAsync())
            {
                return await OnCountAsync(connection, null);
            }
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            await using (DbConnection connection = await _connectionProvider.GetConnectionAsync())
            {
                return await OnCountAsync(connection, predicate);
            }
        }

        #endregion

        #region Exists

        public virtual bool Exists(Expression<Func<T, bool>> predicate)
        {
            using (IDbConnection connection = GetConnection())
            {
                return OnExists(connection, predicate);
            }
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            await using (DbConnection connection = await _connectionProvider.GetConnectionAsync())
            {
                return await OnExistsAsync(connection, predicate);
            }
        }

        #endregion

        #region Insert

        public virtual void Insert(T item)
        {
            using (IDbConnection connection = GetConnection())
            {
                using (IDbTransaction transaction = connection.BeginTransaction())
                {
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
            }
        }

        public virtual void Insert(List<T> items)
        {
            using (IDbConnection connection = GetConnection())
            {
                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (T item in items)
                            InsertInternal(connection, transaction, item);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void Insert(IDbConnection connection, T item)
        {
            InsertInternal(connection, null, item);
        }

        public async Task InsertAsync(T item)
        {
            await using (DbConnection connection = await _connectionProvider.GetConnectionAsync())
            {
                await using (DbTransaction transaction = await connection.BeginTransactionAsync())
                {
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
            }
        }

        public async Task InsertAsync(List<T> items)
        {
            await using (DbConnection connection = await _connectionProvider.GetConnectionAsync())
            {
                await using (DbTransaction transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        foreach (T item in items)
                            await InsertInternalAsync(connection, transaction, item);

                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        private void InsertInternal(IDbConnection connection, IDbTransaction transaction, T item)
        {
            OnBeforeInsert(connection, item);
            OnValidate(connection, item);
            OnInsert(connection, transaction, item);
            OnAfterInsert(connection, item);
        }

        private async Task InsertInternalAsync(IDbConnection connection, IDbTransaction transaction, T item)
        {
            await OnBeforeInsertAsync(connection, item);
            OnValidate(connection, item);
            await OnInsertAsync(connection, transaction, item);
            await OnAfterInsertAsync(connection, item);
        }

        #endregion

        #region Update

        public virtual void Update(T item)
        {
            using (IDbConnection connection = GetConnection())
            {
                using (IDbTransaction transaction = connection.BeginTransaction())
                {
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
            }
        }

        public virtual void Update(List<T> list)
        {
            using (IDbConnection connection = GetConnection())
            {
                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (T item in list)
                            UpdateInternal(connection, transaction, item);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void Update(IDbConnection connection, T item)
        {
            UpdateInternal(connection, null, item);
        }

        public async Task UpdateAsync(T item)
        {
            await using (DbConnection connection = await _connectionProvider.GetConnectionAsync())
            {
                await using (DbTransaction transaction = await connection.BeginTransactionAsync())
                {
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
            }
        }

        public async Task UpdateAsync(List<T> list)
        {
            await using (DbConnection connection = await _connectionProvider.GetConnectionAsync())
            {
                await using (DbTransaction transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        foreach (T item in list)
                            await UpdateInternalAsync(connection, transaction, item);

                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        private void UpdateInternal(IDbConnection connection, IDbTransaction transaction, T item)
        {
            OnBeforeUpdate(connection, item);
            OnValidate(connection, item);
            OnUpdate(connection, transaction, item);
            OnAfterUpdate(connection, item);
        }

        private async Task UpdateInternalAsync(IDbConnection connection, IDbTransaction transaction, T item)
        {
            await OnBeforeUpdateAsync(connection, item);
            OnValidate(connection, item);
            await OnUpdateAsync(connection, transaction, item);
            await OnAfterUpdateAsync(connection, item);
        }

        #endregion

        #region Delete

        public virtual void Delete(T item)
        {
            using (IDbConnection connection = GetConnection())
            {
                using (IDbTransaction transaction = connection.BeginTransaction())
                {
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
            }
        }

        public virtual void Delete(List<T> list)
        {
            using (IDbConnection connection = GetConnection())
            {
                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (T item in list)
                            DeleteInternal(connection, transaction, item);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void Delete(IDbConnection connection, T item)
        {
            DeleteInternal(connection, null, item);
        }

        public async Task DeleteAsync(T item)
        {
            await using (DbConnection connection = await _connectionProvider.GetConnectionAsync())
            {
                await using (DbTransaction transaction = await connection.BeginTransactionAsync())
                {
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
            }
        }

        public async Task DeleteAsync(List<T> list)
        {
            await using (DbConnection connection = await _connectionProvider.GetConnectionAsync())
            {
                await using (DbTransaction transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        foreach (T item in list)
                            await DeleteInternalAsync(connection, transaction, item);

                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        private void DeleteInternal(IDbConnection connection, IDbTransaction transaction, T item)
        {
            OnBeforeDelete(connection, item);
            OnDelete(connection, transaction, item);
            OnAfterDelete(connection, item);
        }

        private async Task DeleteInternalAsync(IDbConnection connection, IDbTransaction transaction, T item)
        {
            await OnBeforeDeleteAsync(connection, item);
            await OnDeleteAsync(connection, transaction, item);
            await OnAfterDeleteAsync(connection, item);
        }

        #endregion

        #region Merge

        public virtual void Merge(List<T> list)
        {
            using (IDbConnection connection = GetConnection())
            {
                using (IDbTransaction transaction = connection.BeginTransaction())
                {
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
            }
        }

        public virtual async Task MergeAsync(List<T> list)
        {
            await using (DbConnection connection = await _connectionProvider.GetConnectionAsync())
            {
                await using (DbTransaction transaction = await connection.BeginTransactionAsync())
                {
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
            }
        }

        private void MergeInternal(IDbConnection connection, IDbTransaction transaction, List<T> list)
        {
            if (_metadataProvider == null)
                throw new InvalidOperationException("Merge requires IMetadataProvider<T>. Pass it to the Reform<T> constructor.");

            var existing = OnSelect(connection, transaction, new QueryCriteria<T>()).ToList();
            var existingByPk = new Dictionary<object, T>();
            foreach (T record in existing)
                existingByPk[_metadataProvider.GetPrimaryKeyValue(record)] = record;

            var accountedPks = new HashSet<object>();

            foreach (T item in list)
            {
                if (IsDefaultPrimaryKey(item))
                {
                    InsertInternal(connection, transaction, item);
                }
                else
                {
                    object pk = _metadataProvider.GetPrimaryKeyValue(item);
                    accountedPks.Add(pk);

                    if (existingByPk.ContainsKey(pk))
                        UpdateInternal(connection, transaction, item);
                    else
                        InsertInternal(connection, transaction, item);
                }
            }

            foreach (var kvp in existingByPk)
            {
                if (!accountedPks.Contains(kvp.Key))
                    DeleteInternal(connection, transaction, kvp.Value);
            }
        }

        private async Task MergeInternalAsync(IDbConnection connection, IDbTransaction transaction, List<T> list)
        {
            if (_metadataProvider == null)
                throw new InvalidOperationException("Merge requires IMetadataProvider<T>. Pass it to the Reform<T> constructor.");

            var existing = (await OnSelectAsync(connection, transaction, new QueryCriteria<T>())).ToList();
            var existingByPk = new Dictionary<object, T>();
            foreach (T record in existing)
                existingByPk[_metadataProvider.GetPrimaryKeyValue(record)] = record;

            var accountedPks = new HashSet<object>();

            foreach (T item in list)
            {
                if (IsDefaultPrimaryKey(item))
                {
                    await InsertInternalAsync(connection, transaction, item);
                }
                else
                {
                    object pk = _metadataProvider.GetPrimaryKeyValue(item);
                    accountedPks.Add(pk);

                    if (existingByPk.ContainsKey(pk))
                        await UpdateInternalAsync(connection, transaction, item);
                    else
                        await InsertInternalAsync(connection, transaction, item);
                }
            }

            foreach (var kvp in existingByPk)
            {
                if (!accountedPks.Contains(kvp.Key))
                    await DeleteInternalAsync(connection, transaction, kvp.Value);
            }
        }

        private bool IsDefaultPrimaryKey(T item)
        {
            object pkValue = _metadataProvider.GetPrimaryKeyValue(item);
            if (pkValue == null) return true;

            Type pkType = _metadataProvider.PrimaryKeyPropertyType;
            if (pkType.IsValueType)
                return pkValue.Equals(Activator.CreateInstance(pkType));

            return false;
        }

        #endregion

        #region SelectSingle

        public virtual T SelectSingle(Expression<Func<T, bool>> predicate)
        {
            IEnumerable<T> list = Select(predicate).ToList();

            if (list.Count() == 1)
                return list.First();

            throw new ApplicationException($"Expected to find 1 {typeof(T).Name} but found {list.Count()}");
        }

        public virtual T SelectSingleOrDefault(Expression<Func<T, bool>> predicate)
        {
            IEnumerable<T> list = Select(predicate).ToList();

            if (list.Count() > 1)
                throw new ApplicationException($"Expected to find 1 or 0 {typeof(T).Name} but found {list.Count()}");

            return list.FirstOrDefault();
        }

        public async Task<T> SelectSingleAsync(Expression<Func<T, bool>> predicate)
        {
            var list = (await SelectAsync(predicate)).ToList();

            if (list.Count == 1)
                return list[0];

            throw new ApplicationException($"Expected to find 1 {typeof(T).Name} but found {list.Count}");
        }

        public async Task<T> SelectSingleOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            var list = (await SelectAsync(predicate)).ToList();

            if (list.Count > 1)
                throw new ApplicationException($"Expected to find 1 or 0 {typeof(T).Name} but found {list.Count}");

            return list.FirstOrDefault();
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
            using (IDbConnection connection = GetConnection())
            {
                return OnSelect(connection, queryCriteria);
            }
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
            await using (DbConnection connection = await _connectionProvider.GetConnectionAsync())
            {
                return await OnSelectAsync(connection, queryCriteria);
            }
        }

        #endregion

        #region Overrideables

        protected virtual IDbConnection OnGetConnection()
        {
            return _connectionProvider.GetConnection();
        }

        protected virtual void OnValidate(IDbConnection connection, T item)
        {
            _validator.Validate(item);
        }

        protected virtual int OnCount(IDbConnection connection, Expression<Func<T, bool>> predicate)
        {
            return _dataAccess.Count(connection, null, predicate);
        }

        protected virtual Task<int> OnCountAsync(IDbConnection connection, Expression<Func<T, bool>> predicate)
        {
            return _dataAccess.CountAsync(connection, null, predicate);
        }

        protected virtual bool OnExists(IDbConnection connection, Expression<Func<T, bool>> predicate)
        {
            return _dataAccess.Exists(connection, null, predicate);
        }

        protected virtual Task<bool> OnExistsAsync(IDbConnection connection, Expression<Func<T, bool>> predicate)
        {
            return _dataAccess.ExistsAsync(connection, null, predicate);
        }

        protected virtual IEnumerable<T> OnSelect(IDbConnection connection, QueryCriteria<T> queryCriteria)
        {
            return _dataAccess.Select(connection, null, queryCriteria);
        }

        protected virtual Task<IEnumerable<T>> OnSelectAsync(IDbConnection connection, QueryCriteria<T> queryCriteria)
        {
            return _dataAccess.SelectAsync(connection, null, queryCriteria);
        }

        protected virtual IEnumerable<T> OnSelect(IDbConnection connection, IDbTransaction transaction, QueryCriteria<T> queryCriteria)
        {
            return _dataAccess.Select(connection, transaction, queryCriteria);
        }

        protected virtual Task<IEnumerable<T>> OnSelectAsync(IDbConnection connection, IDbTransaction transaction, QueryCriteria<T> queryCriteria)
        {
            return _dataAccess.SelectAsync(connection, transaction, queryCriteria);
        }

        protected virtual void OnInsert(IDbConnection connection, IDbTransaction transaction, T item)
        {
            _dataAccess.Insert(connection, transaction, item);
        }

        protected virtual Task OnInsertAsync(IDbConnection connection, IDbTransaction transaction, T item)
        {
            return _dataAccess.InsertAsync(connection, transaction, item);
        }

        protected virtual void OnUpdate(IDbConnection connection, IDbTransaction transaction, T item)
        {
            _dataAccess.Update(connection, transaction, item);
        }

        protected virtual Task OnUpdateAsync(IDbConnection connection, IDbTransaction transaction, T item)
        {
            return _dataAccess.UpdateAsync(connection, transaction, item);
        }

        protected virtual void OnDelete(IDbConnection connection, IDbTransaction transaction, T item)
        {
            _dataAccess.Delete(connection, transaction, item);
        }

        protected virtual Task OnDeleteAsync(IDbConnection connection, IDbTransaction transaction, T item)
        {
            return _dataAccess.DeleteAsync(connection, transaction, item);
        }

        protected virtual void OnBeforeInsert(IDbConnection connection, T item) { }
        protected virtual void OnBeforeUpdate(IDbConnection connection, T item) { }
        protected virtual void OnAfterInsert(IDbConnection connection, T item) { }
        protected virtual void OnAfterUpdate(IDbConnection connection, T item) { }
        protected virtual void OnBeforeDelete(IDbConnection connection, T item) { }
        protected virtual void OnAfterDelete(IDbConnection connection, T item) { }

        protected virtual Task OnBeforeInsertAsync(IDbConnection connection, T item) { OnBeforeInsert(connection, item); return Task.CompletedTask; }
        protected virtual Task OnBeforeUpdateAsync(IDbConnection connection, T item) { OnBeforeUpdate(connection, item); return Task.CompletedTask; }
        protected virtual Task OnAfterInsertAsync(IDbConnection connection, T item) { OnAfterInsert(connection, item); return Task.CompletedTask; }
        protected virtual Task OnAfterUpdateAsync(IDbConnection connection, T item) { OnAfterUpdate(connection, item); return Task.CompletedTask; }
        protected virtual Task OnBeforeDeleteAsync(IDbConnection connection, T item) { OnBeforeDelete(connection, item); return Task.CompletedTask; }
        protected virtual Task OnAfterDeleteAsync(IDbConnection connection, T item) { OnAfterDelete(connection, item); return Task.CompletedTask; }

        #endregion
    }
}
