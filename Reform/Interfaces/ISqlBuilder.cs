// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Reform.Objects;

namespace Reform.Interfaces
{
    public interface ISqlBuilder<T> where T : class
    {
        string GetCountSql(Query<T> query, out Dictionary<string, object> parameterDictionary);
        
        string GetExistsSql(Query<T> query, out Dictionary<string, object> parameterDictionary);
        
        string GetSelectSql(Query<T> query, out Dictionary<string, object> parameterDictionary);
        
        string GetInsertSql(T instance, ref Dictionary<string, object> parameterDictionary);
        
        string GetUpdateSql(T instance, T original, Expression<Func<T, bool>> predicate, ref Dictionary<string, object> parameterDictionary);
        string GetUpdateSql(T instance, Query<T> query, out Dictionary<string, object> parameterDictionary);
        
        string GetDeleteSql(Query<T> query, out Dictionary<string, object> parameterDictionary);
        
        string GetMergeSql(T instance, Expression<Func<T, bool>> predicate, ref Dictionary<string, object> parameterDictionary);
        string GetMergeSql(List<T> instances, Query<T> query, out Dictionary<string, object> parameterDictionary);
        
        string GetLastInsertIdSql();
        string GetTruncateTableSql();
    }
}