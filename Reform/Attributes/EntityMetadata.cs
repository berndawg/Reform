using System;

namespace Reform.Attributes
{
    public class EntityMetadata : Attribute
    {
        public string DatabaseName { get; set; }
        public string TableName { get; set; }
    }
}
