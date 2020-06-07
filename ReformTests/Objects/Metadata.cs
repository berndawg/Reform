using Reform.Attributes;

namespace ReformTests.Objects
{
    [EntityMetadata(DatabaseName = "ReformDB", TableName = "Metadata")]
    public class Metadata
    {
        [PropertyMetadata(ColumnName = "ObjectName")]
        public string ObjectName { get; set; }

        [PropertyMetadata(ColumnName = "ColumnName")]
        public string ColumnName { get; set; }
        
        [PropertyMetadata(ColumnName = "TypeName")]
        public string TypeName { get; set; }
        
        [PropertyMetadata(ColumnName = "IsPrimary")]
        public bool IsPrimary { get; set; }
        
        [PropertyMetadata(ColumnName = "IsIdentity")]
        public bool IsIdentity { get; set; }
        
        [PropertyMetadata(ColumnName = "IsNullable")]
        public bool IsNullable { get; set; }
        
        [PropertyMetadata(ColumnName = "Length")]
        public int Length { get; set; }
        
        [PropertyMetadata(ColumnName = "IsEncrypted")]
        public bool IsEncrypted { get; set; }
        
        [PropertyMetadata(ColumnName = "IsRequired")]
        public bool IsRequired { get; set; }
    }
}
