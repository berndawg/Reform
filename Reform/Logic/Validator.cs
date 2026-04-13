using Reform.Interfaces;
using Reform.Objects;

namespace Reform.Logic
{
    public class Validator<T> : IValidator<T> where T : class
    {
        private readonly IMetadataProvider<T> _metadataProvider;

        public Validator(IMetadataProvider<T> metadataProvider)
        {
            _metadataProvider = metadataProvider;
        }

        public void Validate(T item)
        {
            var errors = GetErrors(item).ToList();

            if (errors.Count != 0)
                throw new ArgumentException(string.Join(Environment.NewLine, errors), nameof(item));
        }

        private IEnumerable<string> GetErrors(T item)
        {
            foreach (var propertyMap in _metadataProvider.RequiredProperties)
            {
                var value = propertyMap.GetPropertyValue(item);

                if (IsEmpty(propertyMap.PropertyType, value))
                    yield return $"'{propertyMap.DisplayName}' is a required field and must not be blank.";
            }
        }

        private static bool IsEmpty(Type type, object value)
        {
            if (value == null)
                return true;

            if (type == typeof(string))
                return string.IsNullOrWhiteSpace((string)value);

            if (type == typeof(Guid))
                return (Guid)value == Guid.Empty;

            if (type.IsValueType)
                return value.Equals(Activator.CreateInstance(type));

            return false;
        }
    }
}
