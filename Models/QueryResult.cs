namespace DynamicPredicateBuilder.Models;

public class QueryResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
}
