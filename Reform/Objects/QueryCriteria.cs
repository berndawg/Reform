using System;
using System.Linq.Expressions;

namespace Reform.Objects
{
    public class QueryCriteria<T> where T : class
    {
        public QueryCriteria()
        {
            SortCriteria = new SortCriteria();
            PageCriteria = new PageCriteria();
        }

        public Expression<Func<T, bool>> Predicate { get; set; }
        public SortCriteria SortCriteria { get; set; }
        public PageCriteria PageCriteria { get; set; }
    }
}
