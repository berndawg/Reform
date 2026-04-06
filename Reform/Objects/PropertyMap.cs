using System.Linq.Expressions;
using System.Reflection;
using Reform.Attributes;

namespace Reform.Objects
{
    public class PropertyMap
    {
        private readonly Func<object, object> _getter;
        private readonly Action<object, object> _setter;

        public PropertyMap(PropertyInfo propertyInfo, PropertyMetadataAttribute propertyMetadata)
        {
            PropertyInfo = propertyInfo;
            PropertyMetadata = propertyMetadata;

            _getter = BuildGetter(propertyInfo);
            _setter = BuildSetter(propertyInfo);
        }

        public PropertyInfo PropertyInfo { get; }
        public PropertyMetadataAttribute PropertyMetadata { get; }

        public string DisplayName => string.IsNullOrEmpty(PropertyMetadata.DisplayName) ? PropertyMetadata.ColumnName! : PropertyMetadata.DisplayName!;
        public bool IsRequired => PropertyMetadata.IsRequired;
        public bool IsReadOnly => PropertyMetadata.IsReadOnly;
        public bool IsPrimaryKey => PropertyMetadata.IsPrimaryKey;
        public bool IsIdentity => PropertyMetadata.IsIdentity;
        public string PropertyName => PropertyInfo.Name;
        public string ColumnName => PropertyMetadata.ColumnName!;
        public Type PropertyType => PropertyInfo.PropertyType;

        public object GetPropertyValue(object instance)
        {
            return _getter(instance);
        }

        public void SetPropertyValue(object instance, object value)
        {
            _setter(instance, value);
        }

        private static Func<object, object> BuildGetter(PropertyInfo propertyInfo)
        {
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var castInstance = Expression.Convert(instanceParam, propertyInfo.DeclaringType!);
            var propertyAccess = Expression.Property(castInstance, propertyInfo);
            var castResult = Expression.Convert(propertyAccess, typeof(object));
            return Expression.Lambda<Func<object, object>>(castResult, instanceParam).Compile();
        }

        private static Action<object, object> BuildSetter(PropertyInfo propertyInfo)
        {
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var valueParam = Expression.Parameter(typeof(object), "value");
            var castInstance = Expression.Convert(instanceParam, propertyInfo.DeclaringType!);
            var castValue = Expression.Convert(valueParam, propertyInfo.PropertyType);
            var propertyAccess = Expression.Property(castInstance, propertyInfo);
            var assign = Expression.Assign(propertyAccess, castValue);
            return Expression.Lambda<Action<object, object>>(assign, instanceParam, valueParam).Compile();
        }
    }
}
