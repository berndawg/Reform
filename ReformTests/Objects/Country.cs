using Reform.Attributes;

namespace ReformTests.Objects
{
    [EntityMetadata(DatabaseName = "ReformDB", TableName = "Country")]
    public class Country
    {
        [PropertyMetadata(ColumnName = "CountryId", DisplayName ="Country ID", IsPrimaryKey = true, IsIdentity = true)]
        public int CountryId { get; set; }

        [PropertyMetadata(ColumnName = "CountryName", DisplayName ="Country Name")]
        public string CountryName { get; set; }
    }
}
