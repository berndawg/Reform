// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Transactions;
using Reform.Objects;

namespace Reform.Interfaces
{
    public interface IReform<T> where T : class
    {
        SqlConnection GetConnection();

        TransactionScope GetScope();

        int Count();
        int Count(SqlConnection connection);
        int Count(List<Filter> filters);
        int Count(SqlConnection connection, List<Filter> filters);

        bool Exists(Filter filter);
        bool Exists(List<Filter> filters);
        bool Exists(SqlConnection connection, List<Filter> filters);

        void Insert(T item);
        void Insert(List<T> items);
        void Insert(SqlConnection connection, T item);
        void Insert(SqlConnection connection, List<T> items);

        void Update(T item);
        void Update(List<T> list);
        void Update(SqlConnection connection, T item);
        void Update(SqlConnection connection, List<T> list);

        void Delete(T item);
        void Delete(List<T> list);
        void Delete(SqlConnection connection, T item);
        void Delete(SqlConnection connection, List<T> list);

        T SelectSingle(List<Filter> filters);
        T SelectSingle(List<Filter> filters, T defaultObject);
        T SelectSingle(SqlConnection connection, List<Filter> filters);
        T SelectSingle(SqlConnection connection, List<Filter> filters, T defaultObject);

        IEnumerable<T> Select();
        IEnumerable<T> Select(Filter filter);
        IEnumerable<T> Select(List<Filter> filters);
        IEnumerable<T> Select(QueryCriteria queryCriteria);
        IEnumerable<T> Select(QueryCriteria queryCriteria, out int totalCount);
        IEnumerable<T> Select(SqlConnection connection, List<Filter> filters);
        IEnumerable<T> Select(SqlConnection connection, QueryCriteria queryCriteria);

        void Truncate();
        void Truncate(SqlConnection connection);

        void BulkInsert(List<T> list);
        void BulkInsert(SqlConnection connection, List<T> list);

        void Merge(List<T> list);
        void Merge(List<T> list, Filter filter);
        void Merge(List<T> list, List<Filter> filters);
        void Merge(SqlConnection connection, List<T> list, List<Filter> filters);
    }
}