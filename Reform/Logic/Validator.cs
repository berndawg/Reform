using System;
using System.Collections.Generic;
using System.Linq;
using Reform.Enum;
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
            IEnumerable<string> errors = GetErrors(item);

            if (errors.Any())
                throw new ApplicationException(string.Join(Environment.NewLine, errors));
        }

        public IEnumerable<string> GetErrors(T item)
        {
            foreach (PropertyMap propertyMap in _metadataProvider.RequiredProperties)
            {
                string errorMessage = $"'{propertyMap.DisplayName}' is a required field and must not be blank.";

                if (propertyMap.PropertyType == typeof(string))
                    if (string.IsNullOrWhiteSpace((string)propertyMap.GetPropertyValue(item)))
                        yield return errorMessage;

                if (propertyMap.PropertyType == typeof(Guid))
                    if ((Guid)propertyMap.GetPropertyValue(item) == Guid.Empty)
                        yield return errorMessage;

                if (propertyMap.PropertyType == typeof(DateTime))
                    if ((DateTime)propertyMap.GetPropertyValue(item) == DateTime.MinValue)
                        yield return errorMessage;
            }
        }

    }
}
