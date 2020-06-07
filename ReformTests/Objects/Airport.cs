using Reform.Attributes;

namespace ReformTests.Objects
{
    [EntityMetadata(DatabaseName = "ReformDb", TableName = "Airport")]
    public class Airport
    {
        [PropertyMetadata(ColumnName = "AirportId", DisplayName = "Airport ID", IsPrimaryKey = true, IsIdentity = true)]
        public int AirportId { get; set; }

        [PropertyMetadata(ColumnName = "AirportCode",  DisplayName ="Airport Code", IsRequired = true)]
        public string AirportCode { get; set; }

        [PropertyMetadata(ColumnName = "AirportName", DisplayName = "Airport Name", IsRequired =true)]
        public string AirportName { get; set; }

        [PropertyMetadata(ColumnName = "CountryId", IsRequired = true)]
        public int CountryId { get; set; }
    }
}
