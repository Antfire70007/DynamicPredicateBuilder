using System.Text.Json;

namespace DynamicPredicateBuilder.Models;

public class QueryRequest
{
    public JsonElement Filter { get; set; } 
    public List<SortRule> Sort { get; set; } = new List<SortRule>();

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}