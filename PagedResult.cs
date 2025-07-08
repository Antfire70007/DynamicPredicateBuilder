namespace DynamicPredicateBuilder;

public class PagedResult<T>
{
    public int TotalCount { get; set; }
    public List<T> Items { get; set; }

    public int Page { get; set; }
    public int PageSize { get; set; }

    public int TotalPages
    {
        get
        {
            if (PageSize == 0) return 0;
            return (int)Math.Ceiling(TotalCount / (double)PageSize);
        }
    }

    public PagedResult()
    {
        Items = new List<T>();
    }
}
