// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System.Collections.Generic;
using System.Data.SqlClient;
using Reform.Objects;
// ReSharper disable UnusedMemberInSuper.Global

namespace Reform.Interfaces
{
    public interface IDataAccess<T> where T : class
    {
        int Count(SqlConnection connection, List<Filter> filters);
        bool Exists(SqlConnection connection, List<Filter> filters);
        void Insert(SqlConnection connection, T instance);
        void Update(SqlConnection connection, T instance);
        void Update(SqlConnection connection, T instance, List<Filter> filters);
        void Delete(SqlConnection connection, T instance);
        void Delete(SqlConnection connection, List<Filter> filters);
        IEnumerable<T> Select(SqlConnection connection, QueryCriteria queryCriteria);
        IEnumerable<T> Select(SqlConnection connection, List<Filter> filters);
        void Truncate(SqlConnection connection);
        void BulkInsert(SqlConnection connection, List<T> list);
        void Merge(SqlConnection connection, List<T> list, List<Filter> filters);
    }
}