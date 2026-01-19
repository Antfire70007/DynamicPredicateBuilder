using System.Linq.Expressions;
using System.Text.Json;
using DynamicPredicateBuilder.Models;

namespace DynamicPredicateBuilder.Core;
public static class FilterEngineExtensions
{
    public static IQueryable<T> ApplyFilterJson<T>(this IQueryable<T> source, JsonElement filterJson, List<SortRule> sortRules = null)
    {
        // 解析成 FilterGroup
        var filterGroup = FilterGroupFactory.FromJsonElement(filterJson);

        // 轉成 Expression
        var predicate = FilterBuilder.Build<T>(filterGroup);

        // 加入 Where
        var query = source.Where(predicate);

        // 加入排序
        if (sortRules != null && sortRules.Any())
        {
            query = query.ApplySort(sortRules);
        }

        return query;
    }

    /// <summary>
    /// 應用排序規則到查詢
    /// </summary>
    /// <typeparam name="T">實體類型</typeparam>
    /// <param name="query">查詢</param>
    /// <param name="sortRules">排序規則</param>
    /// <param name="skipArrayNavigation">是否跳過陣列導覽屬性（預設為 false，會拋出異常）</param>
    /// <returns>已排序的查詢</returns>
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, List<SortRule> sortRules, bool skipArrayNavigation = false)
    {
        if (sortRules == null || !sortRules.Any())
            return query;

        IOrderedQueryable<T> orderedQuery = null;

        foreach (var rule in sortRules)
        {
            // 檢查是否為陣列導覽屬性（包含 "[]" 語法）
            if (rule.Property.Contains("[]"))
            {
                if (skipArrayNavigation)
                {
                    // 跳過陣列導覽屬性
                    continue;
                }
                else
                {
                    // 陣列導覽屬性無法在 EF Core 查詢中執行，拋出有意義的錯誤訊息
                    throw new InvalidOperationException(
                        $"陣列導覽屬性 '{rule.Property}' 無法在資料庫查詢中排序。" +
                        "請先使用 ToList() 或 AsEnumerable() 將資料載入記憶體，然後再進行陣列導覽屬性的排序。" +
                        "\n或者使用 ApplySort(sortRules, skipArrayNavigation: true) 跳過陣列導覽屬性。" +
                        "\n範例：var data = query.ApplySort(sortRules, skipArrayNavigation: true).ToList();");
                }
            }

            var parameter = Expression.Parameter(typeof(T), "x");
            var property = PropertyPathHelper.BuildPropertyExpression(parameter, rule.Property);
            var lambda = Expression.Lambda(property, parameter);

            var methodName = (orderedQuery == null)
                ? (rule.Descending ? "OrderByDescending" : "OrderBy")
                : (rule.Descending ? "ThenByDescending" : "ThenBy");

            query = (IQueryable<T>)typeof(Queryable)
                .GetMethods()
                .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), property.Type)
                .Invoke(null, new object[] { query, lambda });

            orderedQuery = (IOrderedQueryable<T>)query;
        }

        return query;
    }

    /// <summary>
    /// 從排序規則中分離出非陣列導覽屬性的規則
    /// </summary>
    /// <param name="sortRules">排序規則</param>
    /// <returns>非陣列導覽屬性的排序規則</returns>
    public static List<SortRule> GetNonArrayNavigationRules(this List<SortRule> sortRules)
    {
        return sortRules?.Where(r => !r.Property.Contains("[]")).ToList() ?? new List<SortRule>();
    }

    /// <summary>
    /// 從排序規則中分離出陣列導覽屬性的規則
    /// </summary>
    /// <param name="sortRules">排序規則</param>
    /// <returns>陣列導覽屬性的排序規則</returns>
    public static List<SortRule> GetArrayNavigationRules(this List<SortRule> sortRules)
    {
        return sortRules?.Where(r => r.Property.Contains("[]")).ToList() ?? new List<SortRule>();
    }

    public static QueryResult<T> ApplyQuery<T>(this IQueryable<T> query, QueryRequest request)
    {
        // 如果有Filter
        if (request.Filter.ValueKind != JsonValueKind.Undefined && request.Filter.ValueKind != JsonValueKind.Null)
        {
            query = query.ApplyFilterJson(request.Filter, request.Sort);
        }
        else
        {
            // 沒Filter但有Sort
            if (request.Sort != null && request.Sort.Any())
            {
                query = query.ApplySort(request.Sort);
            }
        }

        var totalCount = query.Count();

        if (request.PageSize > 0)
        {
            query = query.Skip((request.Page - 1) * request.PageSize)
                         .Take(request.PageSize);
        }

        return new QueryResult<T>
        {
            TotalCount = totalCount,
            Items = query.ToList()
        };
    }
}