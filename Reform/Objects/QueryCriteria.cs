// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Reform.Objects
{
    public class QueryCriteria
    {
        #region Constructor

        public QueryCriteria()
        {
            Filters = new List<Filter>();
            SortCriteria = new SortCriteria();
            PageCriteria = new PageCriteria();
        }

        #endregion

        [DataMember]
        public List<Filter> Filters { get; set; }

        [DataMember]
        public SortCriteria SortCriteria { get; set; }

        [DataMember]
        public PageCriteria PageCriteria { get; set; }
    }
}