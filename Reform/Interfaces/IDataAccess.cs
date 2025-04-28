// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using Reform.Objects;
// ReSharper disable UnusedMemberInSuper.Global

namespace Reform.Interfaces
{
    public interface IDataAccess<T> where T : class
    {
        int Count(IDbConnection connection, Query<T> query);
        bool Exists(IDbConnection connection, Query<T> query);
        
        void Insert(IDbConnection connection, T instance);
        
        void Update(IDbConnection connection, T instance);
        void Update(IDbConnection connection, T instance, Query<T> query);
        
        void Delete(IDbConnection connection, T instance);
        void Delete(IDbConnection connection, Query<T> query);
        
        IEnumerable<T> Select(IDbConnection connection, Query<T> query);
        
        void Truncate(IDbConnection connection);
        void BulkInsert(IDbConnection connection, List<T> list);
        void Merge(IDbConnection connection, List<T> list, Query<T> query);
    }
}