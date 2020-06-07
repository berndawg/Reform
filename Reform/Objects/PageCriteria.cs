// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System.Runtime.Serialization;

namespace Reform.Objects
{
    public class PageCriteria
    {
        public PageCriteria()
        {
            Page = 0;
            PageSize = 0;
        }

        public PageCriteria(int page)
        {
            Page = page;
            PageSize = 0;
        }

        public PageCriteria(int page, int pageSize)
        {
            Page = page;
            PageSize = pageSize;
        }

        [DataMember]
        public int Page { get; set; }

        [DataMember]
        public int PageSize { get; set; }

        public static PageCriteria All()
        {
            return new PageCriteria(0);
        }
    }
}