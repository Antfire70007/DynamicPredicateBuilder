using System.ComponentModel.DataAnnotations;
using System.Reflection;
using DynamicPredicateBuilder.Attributes;

namespace DynamicPredicateBuilder;
public class QueryableField
{
    public string Field { get; set; }
    public string Display { get; set; }
    public bool AllowQuery { get; set; }
}
public static class QueryableFieldHelper
{
    public static List<QueryableField> GetFields<T>()
    {
        return GetFields(typeof(T));
    }

    private static List<QueryableField> GetFields(Type type, string parentPath = "", string parentDisplay = "")
    {
        var metadataTypeAttr = type.GetCustomAttribute<MetadataTypeAttribute>();
        Type metadataType = metadataTypeAttr?.MetadataClassType;

        var properties = type.GetProperties();

        var result = new List<QueryableField>();

        foreach (var prop in properties)
        {
            var metaProperty = metadataType?.GetProperty(prop.Name);

            var displayAttr = (metaProperty != null)
                ? metaProperty.GetCustomAttribute<DisplayAttribute>()
                : prop.GetCustomAttribute<DisplayAttribute>();

            var queryableAttr = (metaProperty != null)
                ? metaProperty.GetCustomAttribute<QueryableAttribute>()
                : prop.GetCustomAttribute<QueryableAttribute>();

            var fieldPath = string.IsNullOrEmpty(parentPath) ? prop.Name : $"{parentPath}.{prop.Name}";
            var displayName = string.IsNullOrEmpty(parentDisplay)
                ? (displayAttr?.Name ?? prop.Name)
                : $"{parentDisplay} → {(displayAttr?.Name ?? prop.Name)}";

            if (queryableAttr != null && !IsSimpleType(prop.PropertyType))
            {
                // 導覽屬性 → 遞迴
                result.AddRange(GetFields(prop.PropertyType, fieldPath, displayName));
            }
            else
            {
                result.Add(new QueryableField
                {
                    Field = fieldPath,
                    Display = displayName,
                    AllowQuery = queryableAttr != null
                });
            }
        }

        return result;
    }

    private static bool IsSimpleType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType.IsPrimitive ||
               underlyingType.IsEnum ||
               underlyingType == typeof(string) ||
               underlyingType == typeof(decimal) ||
               underlyingType == typeof(DateTime) ||
               underlyingType == typeof(Guid);
    }

    public static HashSet<string> GetAllowedFields<T>()
    {
        return GetFields<T>()
            .Where(f => f.AllowQuery)
            .Select(f => f.Field)
            .ToHashSet();
    }
}