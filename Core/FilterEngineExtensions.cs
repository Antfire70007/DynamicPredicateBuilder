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

    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, List<SortRule> sortRules)
    {
        if (sortRules == null || !sortRules.Any())
            return query;

        IOrderedQueryable<T> orderedQuery = null;

        foreach (var rule in sortRules)
        {
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