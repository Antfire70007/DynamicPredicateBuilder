using DynamicPredicateBuilder.Models;

namespace DynamicPredicateBuilder;

public class FilterCondition
{
    public string PropertyName { get; set; } = string.Empty;
    public FilterOperator Operator { get; set; }
    public object? Value { get; set; }
    public object? Value2 { get; set; }   // Between 等雙值情境
}
