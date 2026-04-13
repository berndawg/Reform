using Reform.Objects;

namespace Reform.Interfaces;

public interface IMetadataProvider<T> where T : class
{
    Type Type { get; }
    IEnumerable<PropertyMap> AllProperties { get; }
    IEnumerable<PropertyMap> RequiredProperties { get; }
    IEnumerable<PropertyMap> UpdateableProperties { get; }
    string DatabaseName { get; }
    string TableName { get; }
    string PrimaryKeyPropertyName { get; }
    string PrimaryKeyColumnName { get; }
    Type PrimaryKeyPropertyType { get; }
    PropertyMap? GetPropertyMapByPropertyName(string propertyName);
    PropertyMap? GetPropertyMapByColumnName(string columnName);
    object GetPrimaryKeyValue(T instance);
    void SetPrimaryKeyValue(T instance, object? id);
}
