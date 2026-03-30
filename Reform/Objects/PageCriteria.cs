// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System.Runtime.Serialization;

namespace Reform.Objects;

public class PageCriteria(int page, int pageSize)
{
    public PageCriteria() : this(0, 0)
    {
    }

    public PageCriteria(int page) : this(page, 0)
    {
    }

    [DataMember]
    public int Page { get; set; } = page;

    [DataMember]
    public int PageSize { get; set; } = pageSize;

    public static PageCriteria All()
    {
        return new PageCriteria(0);
    }
}
