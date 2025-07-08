using System.Linq.Expressions;
using DynamicPredicateBuilder.Models;

namespace DynamicPredicateBuilder.Core;

public static class FilterEngine
{

    public static Expression<Func<T, bool>> FromJson<T>(string json)
    {
        var filterGroup = FilterGroupJsonHelper.FromJson(json);
        return FilterBuilder.Build<T>(filterGroup);
    }

    public static Expression<Func<T, bool>> FromDictionary<T>(Dictionary<string, object> dict)
    {
        var filterGroup = FilterGroupFactory.FromDictionary(dict);
        return FilterBuilder.Build<T>(filterGroup);
    }

    public static IQueryable<T> ApplyFilterJson<T>(this IQueryable<T> source, string json, List<SortRule> sortRules = null)
    {
        var predicate = FromJson<T>(json);
        var query = source.Where(predicate);

        if (sortRules != null && sortRules.Any())
        {
            query = query.ApplySort(sortRules);
        }

        return query;
    }

    public static IQueryable<T> ApplyFilterDictionary<T>(this IQueryable<T> source, Dictionary<string, object> dict, List<SortRule> sortRules = null)
    {
        var predicate = FromDictionary<T>(dict);
        var query = source.Where(predicate);

        if (sortRules != null && sortRules.Any())
        {
            query = query.ApplySort(sortRules);
        }

        return query;
    }
}