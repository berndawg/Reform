namespace Reform.Attributes;

public class PropertyMetadata : Attribute
{
    public string? ColumnName { get; init; }
    public string? DisplayName { get; init; }
    public bool IsPrimaryKey { get; init; }
    public bool IsIdentity { get; init; }
    public bool IsReadOnly { get; init; }
    public bool IsRequired { get; init; }
}
