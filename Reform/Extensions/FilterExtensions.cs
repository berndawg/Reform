// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using Reform.Objects;

namespace Reform.Extensions
{
    public static class FilterExtensions
    {
        public static string ToText(this Filter criterion)
        {
            return $"{criterion.PropertyName} {criterion.Operator} '{criterion.PropertyValue}'";
        }

        public static string ToText(this IEnumerable<Filter> filters)
        {
            return string.Join(" AND ", filters.Select(filter => filter.ToText()));
        }
    }
}