namespace Reform.Attributes;

public class EntityMetadata : Attribute
{
    public string? DatabaseName { get; init; }
    public string TableName { get; init; }
}
