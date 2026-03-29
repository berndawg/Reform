using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;
using Reform.Interfaces;
using Reform.Objects;

namespace Reform.Logic
{
    public class Reform<T> : IReform<T> where T : class
    {
        private readonly IConnectionProvider<T> _connectionProvider;
        private readonly IDataAccess<T> _dataAccess;
        private readonly IValidator<T> _validator;
        private readonly IScopeProvider _scopeProvider;

        public Reform() : this(Reformer.Resolve<IConnectionProvider<T>>(),
                               Reformer.Resolve<IDataAccess<T>>(),
                               Reformer.Resolve<IValidator<T>>(),
                               Reformer.Resolve<IScopeProvider>())
        {
        }

        public Reform(IConnectionProvider<T> connectionProvider, IDataAccess<T> dataAccess, IValidator<T> validator, IScopeProvider scopeProvider)
        {
            _connectionProvider = connectionProvider;
            _dataAccess = dataAccess;
            _validator = validator;
            _scopeProvider = scopeProvider;
        }

        #region Connection/Scope

        public IDbConnection GetConnection()
        {
            return OnGetConnection();
        }

        public TransactionScope GetScope()
        {
            return OnGetScope();
        }

        #endregion

        #region Count

        public int Count()
        {
            using (IDbConnection connection = GetConnection())
            {
                return OnCount(connection, null);
            }
        }

        public int Count(Expression<Func<T, bool>> predicate)
        {
            using (IDbConnection connection = GetConnection())
            {
                return OnCount(connection, predicate);
            }
        }

        #endregion

        #region Exists

        public bool Exists(Expression<Func<T, bool>> predicate)
        {
            using (IDbConnection connection = GetConnection())
            {
                return OnExists(connection, predicate);
            }
        }

        #endregion

        #region Insert

        public void Insert(T item)
        {
            using (TransactionScope scope = GetScope())
            {
                using (IDbConnection connection = GetConnection())
                {
                    Insert(connection, item);
                    scope.Complete();
                }
            }
        }

        public void Insert(List<T> items)
        {
            using (TransactionScope scope = GetScope())
            {
                using (IDbConnection connection = GetConnection())
                {
                    foreach (T item in items)
                        Insert(connection, item);
                }

                scope.Complete();
            }
        }

        public void Insert(IDbConnection connection, T item)
        {
            OnBeforeInsert(connection, item);
            OnValidate(connection, item);
            OnInsert(connection, item);
            OnAfterInsert(connection, item);
        }

        #endregion

        #region Update

        public void Update(T item)
        {
            using (TransactionScope scope = GetScope())
            {
                using (IDbConnection connection = GetConnection())
                    Update(connection, item);

                scope.Complete();
            }
        }

        public void Update(List<T> list)
        {
            using (TransactionScope scope = GetScope())
            {
                using (IDbConnection connection = GetConnection())
                {
                    foreach (T item in list)
                        Update(connection, item);
                }

                scope.Complete();
            }
        }

        public void Update(IDbConnection connection, T item)
        {
            OnBeforeUpdate(connection, item);
            OnValidate(connection, item);
            OnUpdate(connection, item);
            OnAfterUpdate(connection, item);
        }

        #endregion

        #region Delete

        public void Delete(T item)
        {
            using (TransactionScope scope = GetScope())
            {
                using (IDbConnection connection = GetConnection())
                {
                    OnBeforeDelete(connection, item);
                    OnDelete(connection, item);
                    OnAfterDelete(connection, item);
                }

                scope.Complete();
            }
        }

        public void Delete(List<T> list)
        {
            using (TransactionScope scope = GetScope())
            {
                using (IDbConnection connection = GetConnection())
                {
                    foreach (T item in list)
                    {
                        OnBeforeDelete(connection, item);
                        OnDelete(connection, item);
                        OnAfterDelete(connection, item);
                    }
                }

                scope.Complete();
            }
        }

        public void Delete(IDbConnection connection, T item)
        {
            OnBeforeDelete(connection, item);
            OnDelete(connection, item);
            OnAfterDelete(connection, item);
        }

        #endregion

        #region SelectSingle

        public T SelectSingle(Expression<Func<T, bool>> predicate)
        {
            IEnumerable<T> list = Select(predicate).ToList();

            if (list.Count() == 1)
                return list.First();

            throw new ApplicationException($"Expected to find 1 {typeof(T).Name} but found {list.Count()}");
        }

        public T SelectSingleOrDefault(Expression<Func<T, bool>> predicate)
        {
            IEnumerable<T> list = Select(predicate).ToList();

            if (list.Count() > 1)
                throw new ApplicationException($"Expected to find 1 or 0 {typeof(T).Name} but found {list.Count()}");

            return list.FirstOrDefault();
        }

        #endregion

        #region Select

        public IEnumerable<T> Select()
        {
            return Select(new QueryCriteria<T>());
        }

        public IEnumerable<T> Select(Expression<Func<T, bool>> predicate)
        {
            return Select(new QueryCriteria<T> { Predicate = predicate });
        }

        public IEnumerable<T> Select(QueryCriteria<T> queryCriteria)
        {
            using (IDbConnection connection = GetConnection())
            {
                return OnSelect(connection, queryCriteria);
            }
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

        protected virtual int OnCount(IDbConnection connection, Expression<Func<T, bool>> predicate)
        {
            return _dataAccess.Count(connection, predicate);
        }

        protected virtual bool OnExists(IDbConnection connection, Expression<Func<T, bool>> predicate)
        {
            return _dataAccess.Exists(connection, predicate);
        }

        protected virtual IEnumerable<T> OnSelect(IDbConnection connection, QueryCriteria<T> queryCriteria)
        {
            return _dataAccess.Select(connection, queryCriteria);
        }

        protected virtual void OnInsert(IDbConnection connection, T item)
        {
            _dataAccess.Insert(connection, item);
        }

        protected virtual void OnUpdate(IDbConnection connection, T item)
        {
            _dataAccess.Update(connection, item);
        }

        protected virtual void OnDelete(IDbConnection connection, T item)
        {
            _dataAccess.Delete(connection, item);
        }

        protected virtual void OnBeforeInsert(IDbConnection connection, T item) { }
        protected virtual void OnBeforeUpdate(IDbConnection connection, T item) { }
        protected virtual void OnAfterInsert(IDbConnection connection, T item) { }
        protected virtual void OnAfterUpdate(IDbConnection connection, T item) { }
        protected virtual void OnBeforeDelete(IDbConnection connection, T item) { }
        protected virtual void OnAfterDelete(IDbConnection connection, T item) { }

        #endregion
    }
}
