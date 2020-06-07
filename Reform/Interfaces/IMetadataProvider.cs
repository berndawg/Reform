// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System;
using System.Collections.Generic;
using Reform.Objects;

namespace Reform.Interfaces
{
    public interface IMetadataProvider<T> where T : class
    {
        Type Type { get; }
        IEnumerable<PropertyMap> AllProperties { get; }
        IEnumerable<PropertyMap> RequiredProperties { get; }
        IEnumerable<PropertyMap> UpdateableProperties { get; }
        string SymmetricKeyName { get; }
        string SymmetricKeyCertificate { get; }
        string DatabaseName { get; }
        string TableName { get; }
        string SchemaName { get; }
        bool HasEncryptedFields { get; }
        string PrimaryKeyPropertyName { get; }
        string PrimaryKeyColumnName { get; }
        PropertyMap GetPropertyMapByPropertyName(string propertyName);
        PropertyMap GetPropertyMapByColumnName(string columnName);
        object GetPrimaryKeyValue(T instance);
        void SetPrimaryKeyValue(T instance, object id);
    }
}