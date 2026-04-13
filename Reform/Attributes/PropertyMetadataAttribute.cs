namespace Reform.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class PropertyMetadataAttribute : Attribute
{
    public string? ColumnName { get; init; }
    public string? DisplayName { get; init; }
    public bool IsPrimaryKey { get; init; }
    public bool IsIdentity { get; init; }
    public bool IsReadOnly { get; init; }
    public bool IsRequired { get; init; }
}
