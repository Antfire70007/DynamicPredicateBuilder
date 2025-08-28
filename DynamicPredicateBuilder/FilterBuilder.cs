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
    public static Expression<Func<T, bool>> Build<T>(IEnumerable<FilterGroup> groups, FilterOptions options = null)
    {
        // 沒有任何條件 → 永遠 true
        if (groups is null || !groups.Any())
            return _ => true;

        // 共用一個 Parameter
        var parameter = Expression.Parameter(typeof(T), "x");

        Expression? finalBody = null;
        LogicalOperator interOp = LogicalOperator.Or; // 第一組之前沒運算子，先預設 Or

        foreach (var rawGroup in groups)
        {
            // 先對每組做 Optimize（除重）
            var group = OptimizeFilterGroup(rawGroup);

            // 重用既有 BuildGroup 產生 Expression
            var groupExpr = BuildGroup(typeof(T), group, parameter, options);

            // 把每組 Expression 用 AND / OR 串起來
            if (finalBody is null)
            {
                finalBody = groupExpr;
            }
            else
            {
                finalBody = interOp == LogicalOperator.And
                          ? Expression.AndAlso(finalBody, groupExpr)
                          : Expression.OrElse(finalBody, groupExpr);
            }

            // 決定下一迴圈要用的組間運算子
            interOp = group.InterOperator;
        }

        return Expression.Lambda<Func<T, bool>>(finalBody ?? Expression.Constant(true), parameter);
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
    private static Expression BuildGroup(
    Type entityType,
    FilterGroup group,
    ParameterExpression parameter,
    FilterOptions options)
    {
        Expression? body = null;

        foreach (var rule in group.Rules)
        {
            Expression? exp = rule switch
            {
                FilterRule simpleRule => BuildRule(entityType, simpleRule, parameter, options),
                FilterGroup subGroup => BuildGroup(entityType, subGroup, parameter, options),
                _ => null
            };

            if (exp == null) continue;

            body = body == null
                ? exp
                : group.LogicalOperator == LogicalOperator.And
                    ? Expression.AndAlso(body, exp)
                    : Expression.OrElse(body, exp);
        }

        body ??= Expression.Constant(true);

        // ★ NOT on entire group
        if (group.IsNegated)
            body = Expression.Not(body);

        return body;
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
            case FilterOperator.Contains:
                if (property.Type == typeof(string))
                    body = Expression.Call(property, typeof(string).GetMethod("Contains", new[] { typeof(string) }), constant);
                break;
            case FilterOperator.NotContains:
                if (property.Type == typeof(string))
                {
                    var containsExpr = Expression.Call(property, typeof(string).GetMethod("Contains", new[] { typeof(string) }), constant);
                    body = Expression.Not(containsExpr);
                }
                break;
            case FilterOperator.NotIn:
                body = Expression.Not(BuildIn(property, convertedValue));
                break;
            case FilterOperator.NotLike:
                body = Expression.Not(BuildLike(property, convertedValue?.ToString()));
                break;
            case FilterOperator.Between:
                if (convertedValue is IEnumerable betweenList)
                {
                    var items = betweenList.Cast<object>().ToList();
                    if (items.Count == 2)
                    {
                        var min = Expression.Constant(ChangeType(items[0], property.Type), property.Type);
                        var max = Expression.Constant(ChangeType(items[1], property.Type), property.Type);
                        var greaterThanOrEqual = Expression.GreaterThanOrEqual(property, min);
                        var lessThanOrEqual = Expression.LessThanOrEqual(property, max);
                        body = Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);
                    }
                }
                break;
            case FilterOperator.NotBetween:
                if (convertedValue is IEnumerable notBetweenList)
                {
                    var items = notBetweenList.Cast<object>().ToList();
                    if (items.Count == 2)
                    {
                        var min = Expression.Constant(ChangeType(items[0], property.Type), property.Type);
                        var max = Expression.Constant(ChangeType(items[1], property.Type), property.Type);
                        var greaterThanOrEqual = Expression.GreaterThanOrEqual(property, min);
                        var lessThanOrEqual = Expression.LessThanOrEqual(property, max);
                        var betweenExpr = Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);
                        body = Expression.Not(betweenExpr);
                    }
                }
                break;
            case FilterOperator.Any:
                if (typeof(IEnumerable).IsAssignableFrom(property.Type) && property.Type != typeof(string))
                {
                    var anyMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == "Any" && m.GetParameters().Length == 1)
                        .MakeGenericMethod(property.Type.GetGenericArguments().FirstOrDefault() ?? typeof(object));
                    body = Expression.Call(anyMethod, property);
                }
                else
                {
                    body = Expression.Constant(false);
                }
                break;
        }
        body = body ?? Expression.Constant(true);

        // ★ NOT on single rule
        return rule.IsNegated ? Expression.Not(body) : body;
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