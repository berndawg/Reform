// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Reform.Attributes;
using Reform.Interfaces;
using Reform.Objects;

namespace Reform.Logic
{
    internal sealed class MetadataProvider<T> : IMetadataProvider<T> where T : class
    {
        #region Properties and methods

        public Type Type { get; }
        public IEnumerable<PropertyMap> AllProperties { get; }
        public IEnumerable<PropertyMap> RequiredProperties { get; }
        public IEnumerable<PropertyMap> UpdateableProperties { get; }
        public string SymmetricKeyName { get; }
        public string SymmetricKeyCertificate { get; }
        public string DatabaseName { get; }
        public string TableName { get; }
        public string SchemaName { get; }
        public bool HasEncryptedFields { get; }

        private readonly PropertyMap _primaryKeyPropertyMap;
        private readonly Dictionary<string, PropertyMap> _propertyMapLookupByPropertyName;
        private readonly Dictionary<string, PropertyMap> _propertyMapLookupByColumnName;

        #region Constructors

        // ReSharper disable once UnusedMember.Global
        internal MetadataProvider() : this(typeof(T))
        {
        }

        private MetadataProvider(Type type)
        {
            Type = type;

            var entityMetadataArray = (EntityMetadata[])Type.GetCustomAttributes(typeof(EntityMetadata), false);

            if (entityMetadataArray.Length > 0)
            {
                #region The type has an EntityMetadata code attribute

                EntityMetadata entityMetadata = entityMetadataArray[0];

                DatabaseName = string.IsNullOrEmpty(entityMetadata.DatabaseName)
                    ? ""
                    : entityMetadata.DatabaseName;

                SymmetricKeyName = string.IsNullOrEmpty(entityMetadata.SymmetricKeyName)
                    ? ""
                    : entityMetadata.SymmetricKeyName;

                SymmetricKeyCertificate = string.IsNullOrEmpty(entityMetadata.SymmetricKeyCertificate)
                    ? ""
                    : entityMetadata.SymmetricKeyCertificate;

                TableName = entityMetadata.TableName;

                SchemaName = string.IsNullOrEmpty(entityMetadata.SchemaName)
                    ? "dbo"
                    : entityMetadata.SchemaName;

                #endregion
            }
            else
            {
                #region The type does not have an EntityMetadata code attribute

                DatabaseName = string.Empty;
                SymmetricKeyName = string.Empty;
                TableName = string.Empty;
                SchemaName = "dbo";

                #endregion
            }

            List<PropertyMap> allProperties = GetProperties(Type).ToList();

            AllProperties = allProperties;
            RequiredProperties = allProperties.Where(x => x.IsRequired);
            UpdateableProperties = allProperties.Where(x => !x.IsReadOnly && !x.IsIdentity);
            HasEncryptedFields = allProperties.Any(x => x.IsEncrypted);

            _primaryKeyPropertyMap = allProperties.FirstOrDefault(x => x.IsPrimaryKey);

            _propertyMapLookupByPropertyName = allProperties.ToDictionary(propertyMap => propertyMap.PropertyName, propertyMap => propertyMap);
            _propertyMapLookupByColumnName = allProperties.ToDictionary(propertyMap => propertyMap.ColumnName, propertyMap => propertyMap);
        }

        #endregion

        #region Public Methods

        public PropertyMap GetPropertyMapByPropertyName(string propertyName)
        {
            return _propertyMapLookupByPropertyName.ContainsKey(propertyName) ? _propertyMapLookupByPropertyName[propertyName] : null;
        }

        public PropertyMap GetPropertyMapByColumnName(string columnName)
        {
            return _propertyMapLookupByColumnName.ContainsKey(columnName) ? _propertyMapLookupByPropertyName[columnName] : null;
        }

        public object GetPrimaryKeyValue(T instance)
        {
            return _primaryKeyPropertyMap == null ? 0 : _primaryKeyPropertyMap.GetPropertyValue(instance);
        }

        public void SetPrimaryKeyValue(T instance, object id)
        {
            _primaryKeyPropertyMap?.SetPropertyValue(instance, id);
        }

        public string PrimaryKeyPropertyName
        {
            get
            {
                if (_primaryKeyPropertyMap != null)
                    return _primaryKeyPropertyMap.PropertyName;

                throw new ApplicationException($"Type '{Type}' does not have a property marked 'IsPrimaryKey'");
            }
        }

        public string PrimaryKeyColumnName
        {
            get
            {
                if (_primaryKeyPropertyMap != null)
                    return _primaryKeyPropertyMap.ColumnName;

                throw new ApplicationException($"Type '{Type}' does not have a property marked 'IsPrimaryKey'");
            }
        }

        #endregion

        #endregion

        #region Helpers

        private IEnumerable<PropertyMap> GetProperties(Type type)
        {
            foreach (PropertyInfo propertyInfo in type.GetProperties())
            {
                var propertyMetadataArray =
                    (PropertyMetadata[])propertyInfo.GetCustomAttributes(typeof(PropertyMetadata), false);

                if (propertyMetadataArray.Length == 1)
                    yield return new PropertyMap(propertyInfo, propertyMetadataArray[0]);
            }
        }

        #endregion
    }
}