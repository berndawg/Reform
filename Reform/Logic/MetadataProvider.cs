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
        public Type Type { get; }
        public IEnumerable<PropertyMap> AllProperties { get; }
        public IEnumerable<PropertyMap> RequiredProperties { get; }
        public IEnumerable<PropertyMap> UpdateableProperties { get; }
        public string DatabaseName { get; }
        public string TableName { get; }

        private readonly PropertyMap _primaryKeyPropertyMap;
        private readonly Dictionary<string, PropertyMap> _propertyMapLookupByPropertyName;
        private readonly Dictionary<string, PropertyMap> _propertyMapLookupByColumnName;

        internal MetadataProvider() : this(typeof(T))
        {
        }

        private MetadataProvider(Type type)
        {
            Type = type;

            var entityMetadataArray = (EntityMetadata[])Type.GetCustomAttributes(typeof(EntityMetadata), false);

            if (entityMetadataArray.Length > 0)
            {
                EntityMetadata entityMetadata = entityMetadataArray[0];

                DatabaseName = string.IsNullOrEmpty(entityMetadata.DatabaseName)
                    ? ""
                    : entityMetadata.DatabaseName;

                TableName = entityMetadata.TableName;
            }
            else
            {
                DatabaseName = string.Empty;
                TableName = string.Empty;
            }

            List<PropertyMap> allProperties = GetProperties(Type).ToList();

            AllProperties = allProperties;
            RequiredProperties = allProperties.Where(x => x.IsRequired);
            UpdateableProperties = allProperties.Where(x => !x.IsReadOnly && !x.IsIdentity);

            _primaryKeyPropertyMap = allProperties.FirstOrDefault(x => x.IsPrimaryKey);

            _propertyMapLookupByPropertyName = allProperties.ToDictionary(p => p.PropertyName, p => p);
            _propertyMapLookupByColumnName = allProperties.ToDictionary(p => p.ColumnName, p => p);
        }

        public PropertyMap GetPropertyMapByPropertyName(string propertyName)
        {
            return _propertyMapLookupByPropertyName.ContainsKey(propertyName) ? _propertyMapLookupByPropertyName[propertyName] : null;
        }

        public PropertyMap GetPropertyMapByColumnName(string columnName)
        {
            return _propertyMapLookupByColumnName.ContainsKey(columnName) ? _propertyMapLookupByColumnName[columnName] : null;
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
    }
}
