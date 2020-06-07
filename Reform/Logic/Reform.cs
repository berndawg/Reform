// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
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

        #endregion

        #region Constructors

        // ReSharper disable once UnusedMember.Global
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

        #endregion

        #region Interface Implementation

        #region SqlConnection related

        public SqlConnection GetConnection()
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

        public void Insert(List<T> items)
        {
            using (TransactionScope scope = GetScope())
            {
                using (SqlConnection connection = GetConnection())
                    Insert(connection, items);

                scope.Complete();
            }
        }

        public void Insert(SqlConnection connection, List<T> items)
        {
            foreach (T item in items)
                Insert(connection, item);
        }

        public void Insert(T item)
        {
            using (TransactionScope scope = GetScope())
            {
                using (SqlConnection connection = GetConnection())
                {
                    Insert(connection, item);
                    scope.Complete();
                }
            }
        }

        public void Insert(SqlConnection connection, T item)
        {
            OnBeforeInsert(connection, item);
            OnValidate(connection, item);
            OnInsert(connection, item);
            OnAfterInsert(connection, item);
        }

        #endregion

        #region Update

        public void Update(List<T> list)
        {
            using (TransactionScope scope = GetScope())
            {
                using (SqlConnection connection = GetConnection())
                    Update(connection, list);

                scope.Complete();
            }
        }

        public void Update(SqlConnection connection, List<T> items)
        {
            foreach (T item in items)
            {
                Update(connection, item);
            }
        }

        public void Update(T item)
        {
            using (TransactionScope scope = GetScope())
            {
                using (SqlConnection connection = GetConnection())
                    Update(connection, item);

                scope.Complete();
            }
        }

        public void Update(SqlConnection connection, T item)
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
                using (SqlConnection connection = GetConnection())
                {
                    OnBeforeDelete(connection, item);
                    OnDelete(connection, item);
                    OnAfterDelete(connection, item);
                }

                scope.Complete();
            }
        }

        public void Delete(SqlConnection connection, T item)
        {
            OnBeforeDelete(connection, item);
            OnDelete(connection, item);
            OnAfterDelete(connection, item);
        }

        public void Delete(List<T> list)
        {
            using (SqlConnection connection = GetConnection())
            {
                Delete(connection, list);
            }
        }

        public void Delete(SqlConnection connection, List<T> list)
        {
            using (TransactionScope scope = GetScope())
            {
                foreach (T item in list)
                {
                    OnBeforeDelete(connection, item);
                    OnDelete(connection, item);
                    OnAfterDelete(connection, item);
                }

                scope.Complete();
            }
        }

        #endregion

        #region Count

        public int Count()
        {
            using (SqlConnection connection = GetConnection())
            {
                return Count(connection);
            }
        }

        public int Count(SqlConnection connection)
        {
            return OnCount(connection, new List<Filter>());
        }

        public int Count(List<Filter> filters)
        {
            using (SqlConnection connection = GetConnection())
            {
                return OnCount(connection, filters);
            }
        }

        public int Count(SqlConnection connection, List<Filter> filters)
        {
            return OnCount(connection, filters);
        }

        #endregion

        #region Exists

        public bool Exists(SqlConnection connection, List<Filter> filters)
        {
            return OnExists(connection, filters);
        }

        public bool Exists(Filter filter)
        {
            return Exists(new List<Filter> { filter });
        }

        public bool Exists(List<Filter> filters)
        {
            using (SqlConnection connection = GetConnection())
            {
                return OnExists(connection, filters);
            }
        }

        #endregion

        #region SelectSingle

        public T SelectSingle(List<Filter> filters)
        {
            IEnumerable<T> list = Select(filters).ToList();

            if (list.Count() == 1)
                return list.First();

            throw new ApplicationException($"Expected to find 1 {typeof(T).Name} but found {list.Count()} using the criteria: {filters.ToText()}");
        }

        public T SelectSingle(List<Filter> filters, T defaultObject)
        {
            IEnumerable<T> list = Select(filters).ToList();

            if (list.Count() > 1)
                throw new ApplicationException($"Expected to find 1 or 0 {typeof(T).Name} but found {list.Count()} using the criteria: {filters.ToText()}");

            return list.Count() == 1 ? list.First() : defaultObject;
        }

        public T SelectSingle(SqlConnection connection, List<Filter> filters)
        {
            IEnumerable<T> list = Select(connection, filters).ToList();

            if (list.Count() == 1)
                return list.First();

            throw new ApplicationException($"Expected to find 1 {typeof(T).Name} but found {list.Count()} using the criteria: {filters.ToText()}");
        }

        public T SelectSingle(SqlConnection connection, List<Filter> filters, T defaultObject)
        {
            IEnumerable<T> list = Select(connection, filters).ToList();

            if (list.Count() > 1)
                throw new ApplicationException($"Expected to find 1 or 0 {typeof(T).Name} but found {list.Count()} using the criteria: {filters.ToText()}");

            return list.Count() == 1 ? list.First() : defaultObject;
        }

        #endregion

        #region Select

        public IEnumerable<T> Select()
        {
            return Select(new List<Filter>());
        }

        public IEnumerable<T> Select(QueryCriteria queryCriteria, out int totalCount)
        {
            totalCount = Count(queryCriteria.Filters);
            return Select(queryCriteria);
        }

        public IEnumerable<T> Select(SqlConnection connection, List<Filter> filters)
        {
            return Select(connection, new QueryCriteria { Filters = filters });
        }

        public IEnumerable<T> Select(Filter filter)
        {
            return Select(new List<Filter> { filter });
        }

        public IEnumerable<T> Select(List<Filter> filters)
        {
            return Select(new QueryCriteria { Filters = filters });
        }

        public IEnumerable<T> Select(QueryCriteria queryCriteria)
        {
            using (SqlConnection connection = GetConnection())
            {
                return Select(connection, queryCriteria);
            }
        }

        public IEnumerable<T> Select(SqlConnection connection, QueryCriteria queryCriteria)
        {
            return OnSelect(connection, queryCriteria);
        }

        public void Truncate()
        {
            using (SqlConnection connection = GetConnection())
            {
                Truncate(connection);
            }
        }

        public void Truncate(SqlConnection connection)
        {
            OnTruncate(connection);
        }

        public void BulkInsert(List<T> list)
        {
            using (TransactionScope scope = GetScope())
            {
                using (SqlConnection connection = GetConnection())
                {
                    BulkInsert(connection, list);
                }

                scope.Complete();
            }
        }

        public void BulkInsert(SqlConnection connection, List<T> list)
        {
            foreach (T item in list)
                OnValidate(connection, item);

            OnBulkInsert(connection, list);
        }

        public void Merge(List<T> list)
        {
            using (TransactionScope scope = GetScope())
            {
                using (SqlConnection connection = GetConnection())
                {
                    Merge(connection, list, new List<Filter>());
                }

                scope.Complete();
            }
        }

        public void Merge(List<T> list, Filter filter)
        {
            Merge(list, new List<Filter> { filter });
        }

        public void Merge(List<T> list, List<Filter> filters)
        {
            using (TransactionScope scope = GetScope())
            {
                using (SqlConnection connection = GetConnection())
                {
                    Merge(connection, list, filters);
                }

                scope.Complete();
            }
        }

        public void Merge(SqlConnection connection, List<T> list, List<Filter> filters)
        {
            foreach (T item in list)
                OnValidate(connection, item);

            OnMerge(connection, list, filters);
        }

        #endregion

        #region Overrideables

        protected virtual SqlConnection OnGetConnection()
        {
            return _connectionProvider.GetConnection();
        }

        public TransactionScope OnGetScope()
        {
            return _scopeProvider.GetScope();
        }

        protected virtual void OnValidate(SqlConnection connection, T item)
        {
            _validator.Validate(item);
        }

        protected virtual int OnCount(SqlConnection connection, List<Filter> filters)
        {
            return _dataAccess.Count(connection, filters);
        }

        protected virtual bool OnExists(SqlConnection connection, List<Filter> filters)
        {
            return _dataAccess.Exists(connection, filters);
        }

        protected virtual IEnumerable<T> OnSelect(SqlConnection connection, QueryCriteria queryCriteria)
        {
            return _dataAccess.Select(connection, queryCriteria);
        }

        protected virtual void OnInsert(SqlConnection connection, T item)
        {
            _dataAccess.Insert(connection, item);
        }

        protected virtual void OnUpdate(SqlConnection connection, T item)
        {
            _dataAccess.Update(connection, item);
        }

        protected virtual void OnDelete(SqlConnection connection, T item)
        {
            _dataAccess.Delete(connection, item);
        }

        protected virtual void OnBeforeInsert(SqlConnection connection, T item)
        {
        }

        protected virtual void OnBeforeUpdate(SqlConnection connection, T item)
        {
        }

        protected virtual void OnAfterInsert(SqlConnection connection, T item)
        {
        }

        protected virtual void OnAfterUpdate(SqlConnection connection, T item)
        {
        }

        protected virtual void OnBeforeDelete(SqlConnection connection, T item)
        {
        }

        protected virtual void OnAfterDelete(SqlConnection connection, T item)
        {
        }

        protected virtual void OnTruncate(SqlConnection connection)
        {
            _dataAccess.Truncate(connection);
        }

        protected virtual void OnBulkInsert(SqlConnection connection, List<T> list)
        {
            _dataAccess.BulkInsert(connection, list);
        }

        protected virtual void OnMerge(SqlConnection connection, List<T> list, List<Filter> filters)
        {
            _dataAccess.Merge(connection, list, filters);
        }

        #endregion
    }
}