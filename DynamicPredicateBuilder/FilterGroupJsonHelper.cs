using DynamicPredicateBuilder.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DynamicPredicateBuilder;

public static class FilterGroupJsonHelper
{
    public static FilterGroup FromJson(string json)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        return FilterGroupFactory.FromDictionary(ConvertDict(dict));
    }

    private static Dictionary<string, object> ConvertDict(Dictionary<string, JsonElement> dict)
    {
        var result = new Dictionary<string, object>();
        foreach (var kv in dict)
        {
            var value = kv.Value;
            if (value.ValueKind == JsonValueKind.Array)
            {
                var list = new List<object>();
                foreach (var item in value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                        list.Add(ConvertDict(JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(item.GetRawText())));
                    else
                        list.Add(JsonSerializer.Deserialize<object>(item.GetRawText()));
                }
                result[kv.Key] = list;
            }
            else if (value.ValueKind == JsonValueKind.Object)
            {
                result[kv.Key] = ConvertDict(JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value.GetRawText()));
            }
            else if (value.ValueKind == JsonValueKind.String)
            {
                result[kv.Key] = value.GetString();
            }
            else if (value.ValueKind == JsonValueKind.Number)
            {
                if (value.TryGetInt64(out var longValue))
                    result[kv.Key] = longValue;
                else
                    result[kv.Key] = value.GetDouble();
            }
            else if (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
            {
                result[kv.Key] = value.GetBoolean();
            }
            else if (value.ValueKind == JsonValueKind.Null)
            {
                result[kv.Key] = null;
            }
        }

        return result;
    }
}
