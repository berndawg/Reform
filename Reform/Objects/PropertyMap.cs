using System;
using System.Reflection;
using Reform.Attributes;

namespace Reform.Objects
{
    public class PropertyMap
    {
        public PropertyMap(PropertyInfo propertyInfo, PropertyMetadata propertyMetadata)
        {
            PropertyInfo = propertyInfo;
            PropertyMetadata = propertyMetadata;
        }

        public PropertyInfo PropertyInfo { get; }
        public PropertyMetadata PropertyMetadata { get; }

        public string DisplayName => string.IsNullOrEmpty(PropertyMetadata.DisplayName) ? PropertyMetadata.ColumnName : PropertyMetadata.DisplayName;
        public bool IsRequired => PropertyMetadata.IsRequired;
        public bool IsReadOnly => PropertyMetadata.IsReadOnly;
        public bool IsPrimaryKey => PropertyMetadata.IsPrimaryKey;
        public bool IsIdentity => PropertyMetadata.IsIdentity;
        public string PropertyName => PropertyInfo.Name;
        public string ColumnName => PropertyMetadata.ColumnName;
        public Type PropertyType => PropertyInfo.PropertyType;

        public object GetPropertyValue(object instance)
        {
            return PropertyInfo.GetValue(instance, null);
        }

        public void SetPropertyValue(object instance, object value)
        {
            PropertyInfo.SetValue(instance, value, null);
        }
    }
}
