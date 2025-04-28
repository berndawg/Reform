// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using Reform.Objects;

namespace Reform.Interfaces
{
    public interface ICommandBuilder<T> where T : class
    {
        IDbCommand GetCountCommand(IDbConnection connection, Query<T> query);
        
        IDbCommand GetExistsCommand(IDbConnection connection, Query<T> query);
        
        IDbCommand GetSelectCommand(IDbConnection connection, Query<T> query);
        
        IDbCommand GetInsertCommand(IDbConnection connection, T instance);
        
        IDbCommand GetUpdateCommand(IDbConnection connection, T instance);
        IDbCommand GetUpdateCommand(IDbConnection connection, T instance, Query<T> query);
        
        IDbCommand GetDeleteCommand(IDbConnection connection, T instance);
        IDbCommand GetDeleteCommand(IDbConnection connection, Query<T> query);
        
        IDbCommand GetMergeCommand(IDbConnection connection, List<T> list, Query<T> query);
    }
}