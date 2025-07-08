namespace DynamicPredicateBuilder.Models;
public class FilterRule
{
    public string Property { get; set; }
    public FilterOperator Operator { get; set; }
    public object Value { get; set; }

    // 新增：欄位 vs 欄位
    public string CompareToProperty { get; set; }
}
