using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Reform.Interfaces;

namespace Reform.Logic
{
    public class Mapper : IMapper
    {
        private readonly Dictionary<Type, Dictionary<string, PropertyInfo>> _propertyCache = new Dictionary<Type, Dictionary<string, PropertyInfo>>();

        public TDestination Map<TSource, TDestination>(TSource source) where TDestination : class, new()
        {
            if (source == null)
                return null;

            var destination = new TDestination();
            Map(source, destination);
            return destination;
        }

        public void Map<TSource, TDestination>(TSource source, TDestination destination) where TDestination : class
        {
            if (source == null || destination == null)
                return;

            var sourceProperties = GetTypeProperties(typeof(TSource));
            var destinationProperties = GetTypeProperties(typeof(TDestination));

            foreach (var sourceProperty in sourceProperties.Values)
            {
                if (!destinationProperties.TryGetValue(sourceProperty.Name, out var destinationProperty))
                    continue;

                if (!destinationProperty.CanWrite)
                    continue;

                if (!IsCompatibleType(sourceProperty.PropertyType, destinationProperty.PropertyType))
                    continue;

                var value = sourceProperty.GetValue(source, null);
                destinationProperty.SetValue(destination, value, null);
            }
        }

        private Dictionary<string, PropertyInfo> GetTypeProperties(Type type)
        {
            if (_propertyCache.TryGetValue(type, out var properties))
                return properties;

            properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                           .ToDictionary(p => p.Name, p => p);

            _propertyCache[type] = properties;
            return properties;
        }

        private bool IsCompatibleType(Type sourceType, Type destinationType)
        {
            if (destinationType.IsAssignableFrom(sourceType))
                return true;

            // Handle nullable types
            if (Nullable.GetUnderlyingType(destinationType) != null)
                return IsCompatibleType(sourceType, Nullable.GetUnderlyingType(destinationType));

            // Handle numeric type conversions
            if (IsNumericType(sourceType) && IsNumericType(destinationType))
                return true;

            return false;
        }

        private bool IsNumericType(Type type)
        {
            if (type == null) return false;

            // Handle nullable numeric types
            type = Nullable.GetUnderlyingType(type) ?? type;

            return type == typeof(byte)
                || type == typeof(sbyte)
                || type == typeof(short)
                || type == typeof(ushort)
                || type == typeof(int)
                || type == typeof(uint)
                || type == typeof(long)
                || type == typeof(ulong)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(decimal);
        }
    }
} 