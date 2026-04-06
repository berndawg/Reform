namespace Reform.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class EntityMetadataAttribute : Attribute
{
    public string? DatabaseName { get; init; }
    public string? TableName { get; init; }
}
