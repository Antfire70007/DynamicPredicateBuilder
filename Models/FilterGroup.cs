using Newtonsoft.Json;

namespace DynamicPredicateBuilder.Models;

public class FilterGroup
{
    public LogicalOperator LogicalOperator { get; set; } = LogicalOperator.And;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<object> Rules { get; set; } = new List<object>();
}
