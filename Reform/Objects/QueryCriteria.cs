using System.Linq.Expressions;

namespace Reform.Objects;

public class QueryCriteria<T> where T : class
{
    public Expression<Func<T, bool>>? Predicate { get; set; }
    public SortCriteria SortCriteria { get; set; } = new();
    public PageCriteria PageCriteria { get; set; } = new();
}
