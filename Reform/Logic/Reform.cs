// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;
using Reform.Extensions;
using Reform.Interfaces;
using Reform.Objects;
using Unity;

namespace Reform.Logic
{
    public class Reform<T> : IReform<T> where T : class
    {
        #region Fields

        private readonly IConnectionProvider<T> _connectionProvider;
        private readonly IDataAccess<T> _dataAccess;
        private readonly IValidator<T> _validator;
        private readonly IScopeProvider _scopeProvider;
        private readonly ICommandBuilder<T> _commandBuilder;
        private readonly IMapper _mapper;

        #endregion

        #region Constructors

        // ReSharper disable once UnusedMember.Global
        public Reform() : this(Reformer.Resolve<IConnectionProvider<T>>(),
                               Reformer.Resolve<IDataAccess<T>>(),
                               Reformer.Resolve<IValidator<T>>(),
                               Reformer.Resolve<IScopeProvider>(),
                               Reformer.Resolve<ICommandBuilder<T>>(),
                               Reformer.Resolve<IMapper>())
        {
        }

        public Reform(IConnectionProvider<T> connectionProvider, IDataAccess<T> dataAccess, IValidator<T> validator, IScopeProvider scopeProvider, ICommandBuilder<T> commandBuilder, IMapper mapper)
        {
            _connectionProvider = connectionProvider;
            _dataAccess = dataAccess;
            _validator = validator;
            _scopeProvider = scopeProvider;
            _commandBuilder = commandBuilder;
            _mapper = mapper;
        }

        #endregion

        #region Interface Implementation

        #region Connection related

        public IDbConnection GetConnection()
        {
            return OnGetConnection();
        }

        #endregion

        #endregion

        #region TransactionScope related

        public TransactionScope GetScope()
        {
            return OnGetScope();
        }

        #endregion

        #region Insert

        public void Insert(T item)
        {
            using (var scope = GetScope())
            {
                using (var connection = GetConnection())
                {
                    Insert(connection, item);
                }
                scope.Complete();
            }
        }

        public void Insert(List<T> items)
        {
            using (var scope = GetScope())
            {
                using (var connection = GetConnection())
                {
                    Insert(connection, items);
                }
                scope.Complete();
            }
        }

        public void Insert(IDbConnection connection, T item)
        {
            OnBeforeInsert(connection, item);
            OnValidate(connection, item);
            _dataAccess.Insert(connection, item);
            OnAfterInsert(connection, item);
        }

        public void Insert(IDbConnection connection, List<T> items)
        {
            foreach (var item in items)
            {
                Insert(connection, item);
            }
        }

        #endregion

        #region Update

        public void Update(T item)
        {
            using (var scope = GetScope())
            {
                using (var connection = GetConnection())
                {
                    Update(connection, item);
                }
                scope.Complete();
            }
        }

        public void Update(List<T> list)
        {
            using (var scope = GetScope())
            {
                using (var connection = GetConnection())
                {
                    Update(connection, list);
                }
                scope.Complete();
            }
        }

        public void Update(IDbConnection connection, T item)
        {
            OnBeforeUpdate(connection, item);
            OnValidate(connection, item);
            _dataAccess.Update(connection, item);
            OnAfterUpdate(connection, item);
        }

        public void Update(IDbConnection connection, List<T> list)
        {
            foreach (var item in list)
            {
                Update(connection, item);
            }
        }

        public void Update(T item, Expression<Func<T, bool>> predicate)
        {
            using (var scope = GetScope())
            {
                using (var connection = GetConnection())
                {
                    Update(connection, item, predicate);
                }
                scope.Complete();
            }
        }

        public void Update(IDbConnection connection, T item, Expression<Func<T, bool>> predicate)
        {
            var query = new Query<T>().Where(predicate);
            OnBeforeUpdate(connection, item);
            OnValidate(connection, item);
            _dataAccess.Update(connection, item, query);
            OnAfterUpdate(connection, item);
        }

        #endregion

        #region Delete

        public void Delete(T item)
        {
            using (var scope = GetScope())
            {
                using (var connection = GetConnection())
                {
                    Delete(connection, item);
                }
                scope.Complete();
            }
        }

        public void Delete(List<T> list)
        {
            using (var scope = GetScope())
            {
                using (var connection = GetConnection())
                {
                    Delete(connection, list);
                }
                scope.Complete();
            }
        }

        public void Delete(IDbConnection connection, T item)
        {
            OnBeforeDelete(connection, item);
            _dataAccess.Delete(connection, item);
            OnAfterDelete(connection, item);
        }

        public void Delete(IDbConnection connection, List<T> list)
        {
            foreach (var item in list)
            {
                Delete(connection, item);
            }
        }

        public void Delete(Expression<Func<T, bool>> predicate)
        {
            using (var scope = GetScope())
            {
                using (var connection = GetConnection())
                {
                    Delete(connection, predicate);
                }
                scope.Complete();
            }
        }

        public void Delete(IDbConnection connection, Expression<Func<T, bool>> predicate)
        {
            var query = new Query<T>().Where(predicate);
            _dataAccess.Delete(connection, query);
        }

        #endregion

        #region Count

        public int Count()
        {
            using (var connection = GetConnection())
            {
                return _dataAccess.Count(connection, new Query<T>());
            }
        }

        public int Count(IDbConnection connection)
        {
            return _dataAccess.Count(connection, new Query<T>());
        }

        public int Count(Expression<Func<T, bool>> predicate)
        {
            using (var connection = GetConnection())
            {
                return Count(connection, predicate);
            }
        }

        public int Count(IDbConnection connection, Expression<Func<T, bool>> predicate)
        {
            var query = new Query<T>().Where(predicate);
            return _dataAccess.Count(connection, query);
        }

        #endregion

        #region Exists

        public bool Exists(Expression<Func<T, bool>> predicate)
        {
            using (var connection = GetConnection())
            {
                return Exists(connection, predicate);
            }
        }

        public bool Exists(IDbConnection connection, Expression<Func<T, bool>> predicate)
        {
            var query = new Query<T>().Where(predicate);
            return _dataAccess.Exists(connection, query);
        }

        #endregion

        #region Select

        public T SelectSingle(Expression<Func<T, bool>> predicate)
        {
            return SelectSingle(predicate, null);
        }

        public T SelectSingle(Expression<Func<T, bool>> predicate, T defaultObject)
        {
            using (var connection = GetConnection())
            {
                return SelectSingle(connection, predicate, defaultObject);
            }
        }

        public T SelectSingle(IDbConnection connection, Expression<Func<T, bool>> predicate)
        {
            return SelectSingle(connection, predicate, null);
        }

        public T SelectSingle(IDbConnection connection, Expression<Func<T, bool>> predicate, T defaultObject)
        {
            var query = new Query<T>().Where(predicate);
            var list = _dataAccess.Select(connection, query).ToList();

            if (list.Count > 1)
                throw new ApplicationException($"Expected to find 0 or 1 {typeof(T).Name} but found {list.Count}");

            return list.Count == 0 ? defaultObject : list[0];
        }

        public IEnumerable<T> Select()
        {
            return Select(new Query<T>());
        }

        public IEnumerable<T> Select(Expression<Func<T, bool>> predicate)
        {
            using (var connection = GetConnection())
            {
                return Select(connection, predicate);
            }
        }

        public IEnumerable<T> Select(Query<T> query)
        {
            using (var connection = GetConnection())
            {
                return Select(connection, query);
            }
        }

        public IEnumerable<T> Select(IDbConnection connection, Expression<Func<T, bool>> predicate)
        {
            var query = new Query<T>().Where(predicate);
            return _dataAccess.Select(connection, query);
        }

        public IEnumerable<T> Select(IDbConnection connection, Query<T> query)
        {
            return _dataAccess.Select(connection, query);
        }

        #endregion

        #region Truncate

        public void Truncate()
        {
            using (var connection = GetConnection())
            {
                Truncate(connection);
            }
        }

        public void Truncate(IDbConnection connection)
        {
            _dataAccess.Truncate(connection);
        }

        #endregion

        #region BulkInsert

        public void BulkInsert(List<T> list)
        {
            using (var scope = GetScope())
            {
                using (var connection = GetConnection())
                {
                    BulkInsert(connection, list);
                }
                scope.Complete();
            }
        }

        public void BulkInsert(IDbConnection connection, List<T> list)
        {
            foreach (var item in list)
            {
                OnValidate(connection, item);
            }
            _dataAccess.BulkInsert(connection, list);
        }

        #endregion

        #region Merge

        public void Merge(List<T> list)
        {
            using (var scope = GetScope())
            {
                using (var connection = GetConnection())
                {
                    Merge(connection, list);
                }
                scope.Complete();
            }
        }

        public void Merge(IDbConnection connection, List<T> list)
        {
            foreach (var item in list)
            {
                OnValidate(connection, item);
            }
            _dataAccess.Merge(connection, list, new Query<T>());
        }

        public void Merge(List<T> list, Expression<Func<T, bool>> predicate)
        {
            using (var scope = GetScope())
            {
                using (var connection = GetConnection())
                {
                    Merge(connection, list, predicate);
                }
                scope.Complete();
            }
        }

        public void Merge(IDbConnection connection, List<T> list, Expression<Func<T, bool>> predicate)
        {
            var query = new Query<T>().Where(predicate);
            foreach (var item in list)
            {
                OnValidate(connection, item);
            }
            _dataAccess.Merge(connection, list, query);
        }

        #endregion

        #region Overrideables

        protected virtual IDbConnection OnGetConnection()
        {
            return _connectionProvider.GetConnection();
        }

        public TransactionScope OnGetScope()
        {
            return _scopeProvider.GetScope();
        }

        protected virtual void OnValidate(IDbConnection connection, T item)
        {
            _validator.Validate(item);
        }

        protected virtual void OnBeforeInsert(IDbConnection connection, T item)
        {
        }

        protected virtual void OnBeforeUpdate(IDbConnection connection, T item)
        {
        }

        protected virtual void OnBeforeDelete(IDbConnection connection, T item)
        {
        }

        protected virtual void OnAfterInsert(IDbConnection connection, T item)
        {
        }

        protected virtual void OnAfterUpdate(IDbConnection connection, T item)
        {
        }

        protected virtual void OnAfterDelete(IDbConnection connection, T item)
        {
        }

        #endregion
    }
}