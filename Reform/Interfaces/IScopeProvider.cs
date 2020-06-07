// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System.Transactions;

namespace Reform.Interfaces
{
    public interface IScopeProvider
    {
        TransactionScope GetScope();
    }
}