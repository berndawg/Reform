// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System.Transactions;
using Reform.Interfaces;

namespace Reform.Logic
{
    internal sealed class ScopeProvider : IScopeProvider
    {
        #region Interface Implementation

        public TransactionScope GetScope()
        {
            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted
            };

            return new TransactionScope(TransactionScopeOption.Required, transactionOptions);
        }

        #endregion
    }
}