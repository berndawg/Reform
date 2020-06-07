// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System.Runtime.Serialization;
using Reform.Enum;

namespace Reform.Objects
{
    public class SortCriterion
    {
        public SortCriterion(string propertyName, SortDirection sortDirection)
        {
            PropertyName = propertyName;
            Direction = sortDirection;
        }

        public SortCriterion(string propertyName)
        {
            PropertyName = propertyName;
            Direction = SortDirection.Ascending;
        }

        [DataMember]
        public string PropertyName { get; set; }

        [DataMember]
        public SortDirection Direction { get; set; }

        public static SortCriterion Ascending(string propertyName)
        {
            return new SortCriterion(propertyName, SortDirection.Ascending);
        }

        public static SortCriterion Descending(string propertyName)
        {
            return new SortCriterion(propertyName, SortDirection.Descending);
        }
    }
}