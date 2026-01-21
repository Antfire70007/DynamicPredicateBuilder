using System.Text.Json;
using DynamicPredicateBuilder.Models;

namespace DynamicPredicateBuilder;

public static class FilterGroupFactory
{
    public static FilterGroup FromJsonElement(JsonElement json)
    {
        var group = new FilterGroup();

        if (json.TryGetProperty("LogicalOperator", out var logicalOp))
        {
            group.LogicalOperator = Enum.TryParse<LogicalOperator>(logicalOp.GetString(), out var result)
                ? result
                : LogicalOperator.And;
        }

        if (json.TryGetProperty("Rules", out var rulesElement) && rulesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var ruleOrGroup in rulesElement.EnumerateArray())
            {
                if (ruleOrGroup.TryGetProperty("Property", out var propElement))
                {
                    // 單一條件
                    FilterRule rule = new()
                    {
                        Property = propElement.GetString()
                    };

                    if (ruleOrGroup.TryGetProperty("Operator", out var opElement))
                    {
                        rule.Operator = Enum.TryParse<FilterOperator>(opElement.GetString(), out var opResult)
                            ? opResult
                            : FilterOperator.Equal;
                    }

                    if (ruleOrGroup.TryGetProperty("Value", out var valElement))
                    {
                        rule.Value = GetJsonValue(valElement);
                    }

                    group.Rules.Add(rule);
                }
                else
                {
                    // 子群組
                    var subGroup = FromJsonElement(ruleOrGroup);
                    group.Rules.Add(subGroup);
                }
            }
        }

        return group;
    }

    private static object GetJsonValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var l))
                    return l;
                if (element.TryGetDouble(out var d))
                    return d;
                return null;
            case JsonValueKind.True:
            case JsonValueKind.False:
                return element.GetBoolean();
            case JsonValueKind.Array:
                var list = new List<object>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(GetJsonValue(item));
                }
                return list;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
            default:
                return null;
        }
    }

    public static FilterGroup FromDictionary(Dictionary<string, object> dict)
    {
        var group = new FilterGroup();

        if (dict.TryGetValue("LogicalOperator", out var logicalOp))
        {
            group.LogicalOperator = Enum.TryParse<LogicalOperator>(logicalOp.ToString(), out var result)
                ? result
                : LogicalOperator.And;
        }

        if (dict.TryGetValue("InterOperator", out var interOp))
        {
            group.InterOperator = Enum.TryParse<LogicalOperator>(interOp.ToString(), out var interResult)
                ? interResult
                : LogicalOperator.And;
        }

        if (dict.TryGetValue("IsNegated", out var isNegated))
        {
            group.IsNegated = Convert.ToBoolean(isNegated);
        }

        if (dict.TryGetValue("Rules", out var rulesObj))
        {
            var rules = ConvertToList(rulesObj);

            foreach (var ruleObj in rules)
            {
                var ruleDict = ConvertToDictionary(ruleObj);

                if (ruleDict.ContainsKey("Property"))
                {
                    group.Rules.Add(CreateRule(ruleDict));
                }
                else
                {
                    group.Rules.Add(FromDictionary(ruleDict));
                }
            }
        }

        return group;
    }

    private static Dictionary<string, object> ConvertToDictionary(object obj)
    {
        return obj as Dictionary<string, object> ?? new Dictionary<string, object>();
    }

    private static List<object> ConvertToList(object obj)
    {
        return obj as List<object> ?? new List<object>();
    }

    private static FilterRule CreateRule(Dictionary<string, object> dict)
    {
        var rule = new FilterRule();

        if (dict.TryGetValue("Property", out var prop))
            rule.Property = prop.ToString();

        if (dict.TryGetValue("Operator", out var op))
            rule.Operator = Enum.TryParse<FilterOperator>(op.ToString(), out var opResult)
                ? opResult
                : FilterOperator.Equal;

        if (dict.TryGetValue("Value", out var value))
        {
            if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            {
                var list = new List<object>();
                foreach (var item in jsonElement.EnumerateArray())
                {
                    list.Add(GetJsonValue(item));
                }
                rule.Value = list;
            }
            else
            {
                rule.Value = value;
            }
        }

        if (dict.TryGetValue("CompareToProperty", out var compareToProperty))
            rule.CompareToProperty = compareToProperty.ToString();

        if (dict.TryGetValue("IsNegated", out var isNegated))
            rule.IsNegated = Convert.ToBoolean(isNegated);

        return rule;
    }
}
