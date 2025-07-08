using System.Collections;
using System.Linq.Expressions;
using DynamicPredicateBuilder.Core;
using DynamicPredicateBuilder.Models;
using Newtonsoft.Json;

namespace DynamicPredicateBuilder;


public static class FilterBuilder
{
    public static Expression<Func<T, bool>> Build<T>(FilterGroup group, FilterOptions options = null)
    {
        var optimizedGroup = OptimizeFilterGroup(group);

        var parameter = Expression.Parameter(typeof(T), "x");
        var body = BuildGroup(typeof(T), optimizedGroup, parameter, options);
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
    // FilterGroup除重（將相同的 FilterRule 視為一樣）
    private static FilterGroup OptimizeFilterGroup(FilterGroup group)
    {
        var result = new FilterGroup
        {
            LogicalOperator = group.LogicalOperator,
            Rules = new List<object>()
        };

        // 先處理子Group
        foreach (var rule in group.Rules)
        {
            if (rule is FilterGroup subGroup)
            {
                result.Rules.Add(OptimizeFilterGroup(subGroup));
            }
            else
            {
                result.Rules.Add(rule);
            }
        }

        // 將 FilterRule 做除重（用 json 序列化後比對）
        result.Rules = result.Rules
            .GroupBy(r => JsonConvert.SerializeObject(r))
            .Select(g => g.First())
            .ToList();

        return result;
    }
    private static Expression BuildGroup(Type entityType, FilterGroup group, ParameterExpression parameter, FilterOptions options)
    {
        Expression body = null;

        foreach (var rule in group.Rules)
        {
            Expression exp = null;

            if (rule is FilterRule simpleRule)
            {
                exp = BuildRule(entityType, simpleRule, parameter, options);
            }
            else if (rule is FilterGroup subGroup)
            {
                exp = BuildGroup(entityType, subGroup, parameter, options);
            }

            if (exp == null)
                continue;

            if (body == null)
                body = exp;
            else
                body = group.LogicalOperator == LogicalOperator.And
                    ? Expression.AndAlso(body, exp)
                    : Expression.OrElse(body, exp);
        }

        return body ?? Expression.Constant(true);
    }

    private static Expression BuildRule(Type entityType, FilterRule rule, ParameterExpression parameter, FilterOptions options)
    {
        // 檢查是否允許查詢
        if (options != null && options.AllowedFields != null && !options.AllowedFields.Contains(rule.Property))
            return Expression.Constant(true);

        var property = PropertyPathHelper.BuildPropertyExpression(parameter, rule.Property);


        // 欄位 vs 欄位
        if (!string.IsNullOrEmpty(rule.CompareToProperty))
        {
            var compareProperty = PropertyPathHelper.BuildPropertyExpression(parameter, rule.CompareToProperty);

            switch (rule.Operator)
            {
                case FilterOperator.Equal:
                    return Expression.Equal(property, compareProperty);
                case FilterOperator.NotEqual:
                    return Expression.NotEqual(property, compareProperty);
                case FilterOperator.GreaterThan:
                    return Expression.GreaterThan(property, compareProperty);
                case FilterOperator.GreaterThanOrEqual:
                    return Expression.GreaterThanOrEqual(property, compareProperty);
                case FilterOperator.LessThan:
                    return Expression.LessThan(property, compareProperty);
                case FilterOperator.LessThanOrEqual:
                    return Expression.LessThanOrEqual(property, compareProperty);
            }
        }

        var convertedValue = ChangeType(rule.Value, property.Type);

        var constant = Expression.Constant(convertedValue, property.Type);
        Expression body = null;

        switch (rule.Operator)
        {
            case FilterOperator.Equal:
                body = Expression.Equal(property, constant);
                break;
            case FilterOperator.NotEqual:
                body = Expression.NotEqual(property, constant);
                break;
            case FilterOperator.GreaterThan:
                body = Expression.GreaterThan(property, constant);
                break;
            case FilterOperator.GreaterThanOrEqual:
                body = Expression.GreaterThanOrEqual(property, constant);
                break;
            case FilterOperator.LessThan:
                body = Expression.LessThan(property, constant);
                break;
            case FilterOperator.LessThanOrEqual:
                body = Expression.LessThanOrEqual(property, constant);
                break;
            case FilterOperator.Like:
                body = BuildLike(property, convertedValue?.ToString());
                break;
            case FilterOperator.In:
                body = BuildIn(property, convertedValue);
                break;
            case FilterOperator.StartsWith:
                body = Expression.Call(property, typeof(string).GetMethod("StartsWith", new[] { typeof(string) }), constant);
                break;
            case FilterOperator.EndsWith:
                body = Expression.Call(property, typeof(string).GetMethod("EndsWith", new[] { typeof(string) }), constant);
                break;
        }

        return body ?? Expression.Constant(true);
    }

    private static object ChangeType(object value, Type targetType)
    {
        if (value == null)
            return null;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType.IsEnum)
            return Enum.Parse(underlyingType, value.ToString());

        if (underlyingType == typeof(Guid))
            return Guid.Parse(value.ToString());

        if (underlyingType == typeof(bool))
            return value.ToString().ToLower() == "true" || value.ToString() == "1";

        if (underlyingType == typeof(string))
            return value?.ToString();

        return Convert.ChangeType(value, underlyingType);
    }

    private static Expression BuildLike(Expression property, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return Expression.Constant(true);

        var likeValue = Expression.Constant(pattern);

        return Expression.Call(property, typeof(string).GetMethod("Contains", new[] { typeof(string) }), likeValue);
    }

    private static Expression BuildIn(Expression property, object value)
    {
        if (!(value is IEnumerable list))
            return Expression.Constant(true);

        var containsMethod = typeof(Enumerable).GetMethods()
            .Where(m => m.Name == "Contains" && m.GetParameters().Length == 2)
            .First()
            .MakeGenericMethod(property.Type);

        var valuesExpression = Expression.Constant(list);
        return Expression.Call(containsMethod, valuesExpression, property);
    }
}