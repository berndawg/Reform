// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System;
using System.Reflection;
using Reform.Attributes;

namespace Reform.Objects
{
    public class PropertyMap
    {
        #region Constructor

        public PropertyMap(PropertyInfo propertyInfo, PropertyMetadata propertyMetadata)
        {
            PropertyInfo = propertyInfo;
            PropertyMetadata = propertyMetadata;
        }

        #endregion

        #region Properties

        public PropertyInfo PropertyInfo { get; }

        public PropertyMetadata PropertyMetadata { get; }

        public string DisplayName
        {
            get { return string.IsNullOrEmpty(PropertyMetadata.DisplayName)? PropertyMetadata.ColumnName : PropertyMetadata.DisplayName; }
        }

        public bool IsRequired
        {
            get { return PropertyMetadata.IsRequired; }
        }

        public bool IsReadOnly
        {
            get { return PropertyMetadata.IsReadOnly; }
        }

        public bool IsPrimaryKey
        {
            get { return PropertyMetadata.IsPrimaryKey; }
        }

        public bool IsIdentity
        {
            get { return PropertyMetadata.IsIdentity; }
        }

        public bool IsEncrypted
        {
            get { return PropertyMetadata.IsEncrypted; }
        }

        public string PropertyName
        {
            get { return PropertyInfo.Name; }
        }

        public string ColumnName
        {
            get { return PropertyMetadata.ColumnName; }
        }

        public Type PropertyType
        {
            get { return PropertyInfo.PropertyType; }
        }

        #endregion

        #region Methods

        public object GetPropertyValue(object instance)
        {
            return PropertyInfo.GetValue(instance, null);
        }

        public void SetPropertyValue(object instance, object value)
        {
            PropertyInfo.SetValue(instance, value, null);
        }

        #endregion
    }
}