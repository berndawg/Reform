// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using Reform.Enum;

namespace Reform.Objects;

public class SortCriteria : List<SortCriterion>
{
    public static SortCriteria GetSortCriteriaFromSortExpression(string sortExpression)
    {
        var list = new SortCriteria();

        if (!string.IsNullOrEmpty(sortExpression))
        {
            var fields = sortExpression.Trim().Split(',');

            foreach (var field in fields)
            {
                var namePairs = field.Trim().Split(' ');

                if (namePairs.Length == 0)
                    continue;

                var isAscending = namePairs.Length == 1 ||
                                  string.Compare(namePairs[1], "ASC", StringComparison.OrdinalIgnoreCase) == 0;

                list.Add(isAscending
                    ? new SortCriterion(namePairs[0], SortDirection.Ascending)
                    : new SortCriterion(namePairs[0], SortDirection.Descending));
            }
        }

        return list;
    }
}
