// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System.Collections.Generic;
using Reform.Objects;

namespace Reform.Interfaces
{
    // ReSharper disable once UnusedTypeParameter
    public interface ISqlBuilder<T>
    {
        string GetCountSql(List<Filter> filters, out Dictionary<string, object> parameterDictionary);
        string GetExistsSql(List<Filter> filters, out Dictionary<string, object> parameterDictionary);
        string GetSelectSql(QueryCriteria queryCriteria, ref Dictionary<string, object> parameterDictionary);
        string GetInsertSql(T instance, ref Dictionary<string, object> parameterDictionary);
        string GetUpdateSql(T instance, object original, ref Dictionary<string, object> parameterDictionary, List<Filter> filters);
        string GetDeleteSql(List<Filter> filters, ref Dictionary<string, object> parameterDictionary);
        string GetMergeSql(string tempTableName, List<Filter> filters, ref Dictionary<string, object> parameterDictionary);
    }
}