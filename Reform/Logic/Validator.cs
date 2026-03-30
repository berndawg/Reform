using Reform.Interfaces;
using Reform.Objects;

namespace Reform.Logic;

public class Validator<T>(IMetadataProvider<T> metadataProvider) : IValidator<T>
    where T : class
{
    public void Validate(T item)
    {
        var errors = GetErrors(item);

        if (errors.Any())
            throw new ApplicationException(string.Join(Environment.NewLine, errors));
    }

    public IEnumerable<string> GetErrors(T item)
    {
        foreach (var propertyMap in metadataProvider.RequiredProperties)
        {
            var errorMessage = $"'{propertyMap.DisplayName}' is a required field and must not be blank.";

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
