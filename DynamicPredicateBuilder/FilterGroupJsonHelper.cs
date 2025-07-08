using DynamicPredicateBuilder.Models;
using Newtonsoft.Json;

namespace DynamicPredicateBuilder;

public static class FilterGroupJsonHelper
{
    public static FilterGroup FromJson(string json)
    {
        var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        return FilterGroupFactory.FromDictionary(ConvertDict(dict));
    }

    private static Dictionary<string, object> ConvertDict(Dictionary<string, object> dict)
    {
        var result = new Dictionary<string, object>();
        foreach (var kv in dict)
        {
            if (kv.Value is Newtonsoft.Json.Linq.JArray array)
            {
                var list = new List<object>();
                foreach (var item in array)
                {
                    if (item is Newtonsoft.Json.Linq.JObject obj)
                        list.Add(ConvertDict(obj.ToObject<Dictionary<string, object>>()));
                    else
                        list.Add(item.ToObject<object>());
                }
                result[kv.Key] = list;
            }
            else if (kv.Value is Newtonsoft.Json.Linq.JObject obj)
            {
                result[kv.Key] = ConvertDict(obj.ToObject<Dictionary<string, object>>());
            }
            else
            {
                result[kv.Key] = kv.Value;
            }
        }

        return result;
    }
}
