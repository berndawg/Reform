// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Transactions;
using Reform.Objects;

namespace Reform.Interfaces
{
    public interface IReform<T> where T : class
    {
        IDbConnection GetConnection();
        TransactionScope GetScope();

        int Count();
        int Count(IDbConnection connection);
        int Count(Expression<Func<T, bool>> predicate);
        int Count(IDbConnection connection, Expression<Func<T, bool>> predicate);

        bool Exists(Expression<Func<T, bool>> predicate);
        bool Exists(IDbConnection connection, Expression<Func<T, bool>> predicate);

        void Insert(T item);
        void Insert(List<T> items);
        void Insert(IDbConnection connection, T item);
        void Insert(IDbConnection connection, List<T> items);

        void Update(T item);
        void Update(List<T> list);
        void Update(IDbConnection connection, T item);
        void Update(IDbConnection connection, List<T> list);
        void Update(T item, Expression<Func<T, bool>> predicate);
        void Update(IDbConnection connection, T item, Expression<Func<T, bool>> predicate);

        void Delete(T item);
        void Delete(List<T> list);
        void Delete(IDbConnection connection, T item);
        void Delete(IDbConnection connection, List<T> list);
        void Delete(Expression<Func<T, bool>> predicate);
        void Delete(IDbConnection connection, Expression<Func<T, bool>> predicate);

        T SelectSingle(Expression<Func<T, bool>> predicate);
        T SelectSingle(Expression<Func<T, bool>> predicate, T defaultObject);
        T SelectSingle(IDbConnection connection, Expression<Func<T, bool>> predicate);
        T SelectSingle(IDbConnection connection, Expression<Func<T, bool>> predicate, T defaultObject);

        IEnumerable<T> Select();
        IEnumerable<T> Select(Expression<Func<T, bool>> predicate);
        IEnumerable<T> Select(Query<T> query);
        IEnumerable<T> Select(IDbConnection connection, Expression<Func<T, bool>> predicate);
        IEnumerable<T> Select(IDbConnection connection, Query<T> query);

        void Truncate();
        void Truncate(IDbConnection connection);

        void BulkInsert(List<T> list);
        void BulkInsert(IDbConnection connection, List<T> list);

        void Merge(List<T> list);
        void Merge(List<T> list, Expression<Func<T, bool>> predicate);
        void Merge(IDbConnection connection, List<T> list, Expression<Func<T, bool>> predicate);
    }
}