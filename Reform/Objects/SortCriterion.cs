// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System.Runtime.Serialization;
using Reform.Enum;

namespace Reform.Objects;

public class SortCriterion(string propertyName, SortDirection sortDirection)
{
    public SortCriterion(string propertyName) : this(propertyName, SortDirection.Ascending)
    {
    }

    [DataMember]
    public string PropertyName { get; set; } = propertyName;

    [DataMember]
    public SortDirection Direction { get; set; } = sortDirection;

    public static SortCriterion Ascending(string propertyName)
    {
        return new SortCriterion(propertyName, SortDirection.Ascending);
    }

    public static SortCriterion Descending(string propertyName)
    {
        return new SortCriterion(propertyName, SortDirection.Descending);
    }
}
