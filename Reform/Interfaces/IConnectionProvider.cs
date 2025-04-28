// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.

using System.Data;

namespace Reform.Interfaces
{
    // ReSharper disable once UnusedTypeParameter
    public interface IConnectionProvider<T> where T : class
    {
        IDbConnection GetConnection();
    }
}