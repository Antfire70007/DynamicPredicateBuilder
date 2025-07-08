using Newtonsoft.Json;

namespace DynamicPredicateBuilder.Models;

public class FilterGroup
{
    public LogicalOperator LogicalOperator { get; set; } = LogicalOperator.And;
    /// <summary>這一組與下一組要用 AND / OR 連接。</summary>
    public LogicalOperator InterOperator { get; set; } = LogicalOperator.Or;

    /// <summary>這整組要不要 NOT。</summary>
    public bool IsNegated { get; set; } = false;


    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<object> Rules { get; set; } = new List<object>();
}
