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
                FilterRule simpleRule => BuildRule(simpleRule, parameter, options),
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

    private static Expression BuildRule(FilterRule rule, ParameterExpression parameter, FilterOptions options)
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

        // 針對 Between/NotBetween 特殊處理，不轉型整個值
        if (rule.Operator == FilterOperator.Between || rule.Operator == FilterOperator.NotBetween)
        {
            Expression betweenBody = BuildBetweenExpression(rule, property);
            return rule.IsNegated ? Expression.Not(betweenBody) : betweenBody;
        }

        // 針對 In/NotIn 特殊處理，不轉型整個值
        if (rule.Operator == FilterOperator.In || rule.Operator == FilterOperator.NotIn)
        {
            Expression inBody = rule.Operator == FilterOperator.In 
                ? BuildIn(property, rule.Value)
                : Expression.Not(BuildIn(property, rule.Value));
            
            return rule.IsNegated ? Expression.Not(inBody) : inBody;
        }

        var convertedValue = ChangeType(rule.Value, property.Type);
        Expression constant;
        
        // 特殊處理 null 值
        if (convertedValue == null)
        {
            constant = Expression.Constant(null, property.Type);
        }
        else
        {
            constant = Expression.Constant(convertedValue, property.Type);
        }
        
        Expression body = null;

        switch (rule.Operator)
        {
            case FilterOperator.Equal:
                if (typeof(IEnumerable).IsAssignableFrom(property.Type) && property.Type != typeof(string))
                {
                    var elementType = property.Type.IsArray
                        ? property.Type.GetElementType()
                        : property.Type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

                    var param = Expression.Parameter(elementType, "x");

                    if (convertedValue is IEnumerable valueList && !(convertedValue is string))
                    {
                        // 多值比對：Any(x => valueList.Contains(x))
                        var containsMethod = typeof(Enumerable).GetMethods()
                            .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                            .MakeGenericMethod(elementType);
                        var valuesExpression = Expression.Constant(valueList);
                        var bodyExpr = Expression.Call(containsMethod, valuesExpression, param);
                        var lambda = Expression.Lambda(bodyExpr, param);

                        var anyMethod = typeof(Enumerable).GetMethods()
                            .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                            .MakeGenericMethod(elementType);

                        body = Expression.Call(anyMethod, property, lambda);
                    }
                    else
                    {
                        // 單值比對：Any(x => x == value)
                        Expression valueExpr;
                        if (convertedValue == null)
                        {
                            valueExpr = Expression.Constant(null, elementType);
                        }
                        else
                        {
                            // 使用 rule.Value 而不是 convertedValue
                            var convertedElementValue = ChangeType(rule.Value, elementType);
                            valueExpr = Expression.Constant(convertedElementValue, elementType);
                        }
                        var eqExpr = Expression.Equal(param, valueExpr);
                        var lambda = Expression.Lambda(eqExpr, param);

                        var anyMethod = typeof(Enumerable).GetMethods()
                            .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                            .MakeGenericMethod(elementType);

                        body = Expression.Call(anyMethod, property, lambda);
                    }
                }
                else
                {
                    body = Expression.Equal(property, constant);
                }
                break;
            case FilterOperator.NotEqual:
                if (typeof(IEnumerable).IsAssignableFrom(property.Type) && property.Type != typeof(string))
                {
                    var elementType = property.Type.IsArray
                        ? property.Type.GetElementType()
                        : property.Type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

                    var param = Expression.Parameter(elementType, "x");

                    if (convertedValue is IEnumerable valueList && !(convertedValue is string))
                    {
                        // 多值比對：!Any(x => valueList.Contains(x))
                        var containsMethod = typeof(Enumerable).GetMethods()
                            .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                            .MakeGenericMethod(elementType);
                        var valuesExpression = Expression.Constant(valueList);
                        var bodyExpr = Expression.Call(containsMethod, valuesExpression, param);
                        var lambda = Expression.Lambda(bodyExpr, param);

                        var anyMethod = typeof(Enumerable).GetMethods()
                            .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                            .MakeGenericMethod(elementType);

                        body = Expression.Not(Expression.Call(anyMethod, property, lambda));
                    }
                    else
                    {
                        // 單值比對：!Any(x => x == value)
                        Expression valueExpr;
                        if (convertedValue == null)
                        {
                            valueExpr = Expression.Constant(null, elementType);
                        }
                        else
                        {
                            // 使用 rule.Value 而不是 convertedValue
                            var convertedElementValue = ChangeType(rule.Value, elementType);
                            valueExpr = Expression.Constant(convertedElementValue, elementType);
                        }
                        var eqExpr = Expression.Equal(param, valueExpr);
                        var lambda = Expression.Lambda(eqExpr, param);

                        var anyMethod = typeof(Enumerable).GetMethods()
                            .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                            .MakeGenericMethod(elementType);

                        body = Expression.Not(Expression.Call(anyMethod, property, lambda));
                    }
                }
                else
                {
                    body = Expression.NotEqual(property, constant);
                }
                break;
            case FilterOperator.GreaterThan:
                if (typeof(IEnumerable).IsAssignableFrom(property.Type) && property.Type != typeof(string))
                {
                    var elementType = property.Type.IsArray
                        ? property.Type.GetElementType()
                        : property.Type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

                    var param = Expression.Parameter(elementType, "x");
                    // 直接使用 rule.Value 而不是 convertedValue
                    var convertedElementValue = ChangeType(rule.Value, elementType);
                    var valueExpr = Expression.Constant(convertedElementValue, elementType);
                    var gtExpr = Expression.GreaterThan(param, valueExpr);
                    var lambda = Expression.Lambda(gtExpr, param);

                    var anyMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                        .MakeGenericMethod(elementType);

                    body = Expression.Call(anyMethod, property, lambda);
                }
                else
                {
                    body = Expression.GreaterThan(property, constant);
                }
                break;
            case FilterOperator.GreaterThanOrEqual:
                if (typeof(IEnumerable).IsAssignableFrom(property.Type) && property.Type != typeof(string))
                {
                    var elementType = property.Type.IsArray
                        ? property.Type.GetElementType()
                        : property.Type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

                    var param = Expression.Parameter(elementType, "x");
                    // 直接使用 rule.Value 而不是 convertedValue
                    var convertedElementValue = ChangeType(rule.Value, elementType);
                    var valueExpr = Expression.Constant(convertedElementValue, elementType);
                    var gteExpr = Expression.GreaterThanOrEqual(param, valueExpr);
                    var lambda = Expression.Lambda(gteExpr, param);

                    var anyMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                        .MakeGenericMethod(elementType);

                    body = Expression.Call(anyMethod, property, lambda);
                }
                else
                {
                    body = Expression.GreaterThanOrEqual(property, constant);
                }
                break;
            case FilterOperator.LessThan:
                if (typeof(IEnumerable).IsAssignableFrom(property.Type) && property.Type != typeof(string))
                {
                    var elementType = property.Type.IsArray
                        ? property.Type.GetElementType()
                        : property.Type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

                    var param = Expression.Parameter(elementType, "x");
                    // 直接使用 rule.Value 而不是 convertedValue
                    var convertedElementValue = ChangeType(rule.Value, elementType);
                    var valueExpr = Expression.Constant(convertedElementValue, elementType);
                    var ltExpr = Expression.LessThan(param, valueExpr);
                    var lambda = Expression.Lambda(ltExpr, param);

                    var anyMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                        .MakeGenericMethod(elementType);

                    body = Expression.Call(anyMethod, property, lambda);
                }
                else
                {
                    body = Expression.LessThan(property, constant);
                }
                break;
            case FilterOperator.LessThanOrEqual:
                if (typeof(IEnumerable).IsAssignableFrom(property.Type) && property.Type != typeof(string))
                {
                    var elementType = property.Type.IsArray
                        ? property.Type.GetElementType()
                        : property.Type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

                    var param = Expression.Parameter(elementType, "x");
                    // 直接使用 rule.Value 而不是 convertedValue
                    var convertedElementValue = ChangeType(rule.Value, elementType);
                    var valueExpr = Expression.Constant(convertedElementValue, elementType);
                    var lteExpr = Expression.LessThanOrEqual(param, valueExpr);
                    var lambda = Expression.Lambda(lteExpr, param);

                    var anyMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                        .MakeGenericMethod(elementType);

                    body = Expression.Call(anyMethod, property, lambda);
                }
                else
                {
                    body = Expression.LessThanOrEqual(property, constant);
                }
                break;
            case FilterOperator.Like:
                if (property.Type == typeof(string))
                {
                    body = BuildLike(property, convertedValue?.ToString());
                }
                else if (typeof(IEnumerable).IsAssignableFrom(property.Type) && property.Type != typeof(string))
                {
                    // 集合型別，元素為字串
                    var elementType = property.Type.IsArray
                        ? property.Type.GetElementType()
                        : property.Type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

                    if (elementType == typeof(string))
                    {
                        var param = Expression.Parameter(elementType, "x");
                        // 這裡要用 term 字串，不是 List<string>
                        var containsExpr = BuildLike(param, rule.Value?.ToString());
                        var lambda = Expression.Lambda(containsExpr, param);

                        var anyMethod = typeof(Enumerable).GetMethods()
                            .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                            .MakeGenericMethod(elementType);

                        body = Expression.Call(anyMethod, property, lambda);
                    }
                }
                break;
            case FilterOperator.StartsWith:
                if (property.Type == typeof(string))
                {
                    body = Expression.Call(property, typeof(string).GetMethod("StartsWith", new[] { typeof(string) }), constant);
                }
                else if (typeof(IEnumerable).IsAssignableFrom(property.Type) && property.Type != typeof(string))
                {
                    var elementType = property.Type.IsArray
                        ? property.Type.GetElementType()
                        : property.Type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

                    if (elementType == typeof(string))
                    {
                        var param = Expression.Parameter(elementType, "x");
                        var startsWithExpr = Expression.Call(param, typeof(string).GetMethod("StartsWith", new[] { typeof(string) }), Expression.Constant(rule.Value?.ToString()));
                        var lambda = Expression.Lambda(startsWithExpr, param);

                        var anyMethod = typeof(Enumerable).GetMethods()
                            .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                            .MakeGenericMethod(elementType);

                        body = Expression.Call(anyMethod, property, lambda);
                    }
                }
                break;
            case FilterOperator.EndsWith:
                if (property.Type == typeof(string))
                {
                    body = Expression.Call(property, typeof(string).GetMethod("EndsWith", new[] { typeof(string) }), constant);
                }
                else if (typeof(IEnumerable).IsAssignableFrom(property.Type) && property.Type != typeof(string))
                {
                    var elementType = property.Type.IsArray
                        ? property.Type.GetElementType()
                        : property.Type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

                    if (elementType == typeof(string))
                    {
                        var param = Expression.Parameter(elementType, "x");
                        var endsWithExpr = Expression.Call(param, typeof(string).GetMethod("EndsWith", new[] { typeof(string) }), Expression.Constant(rule.Value?.ToString()));
                        var lambda = Expression.Lambda(endsWithExpr, param);

                        var anyMethod = typeof(Enumerable).GetMethods()
                            .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                            .MakeGenericMethod(elementType);

                        body = Expression.Call(anyMethod, property, lambda);
                    }
                }
                break;
            case FilterOperator.Contains:
                if (property.Type == typeof(string))
                {
                    body = Expression.Call(property, typeof(string).GetMethod("Contains", new[] { typeof(string) }), constant);
                }
                else if (typeof(IEnumerable).IsAssignableFrom(property.Type) && property.Type != typeof(string))
                {
                    var elementType = property.Type.IsArray
                        ? property.Type.GetElementType()
                        : property.Type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

                    var param = Expression.Parameter(elementType, "x");

                    Expression containsExpr;
                    if (elementType == typeof(string))
                    {
                        // 關鍵字查詢時，直接用 rule.Value?.ToString()，避免 value 被誤轉型
                        containsExpr = Expression.Call(param, typeof(string).GetMethod("Contains", new[] { typeof(string) }), Expression.Constant(rule.Value?.ToString()));
                    }
                    else
                    {
                        // 其他型別直接用等值比對，先轉換值類型
                        var convertedElementValue = ChangeType(rule.Value, elementType);
                        containsExpr = Expression.Equal(param, Expression.Constant(convertedElementValue, elementType));
                    }
                    var lambda = Expression.Lambda(containsExpr, param);

                    var anyMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                        .MakeGenericMethod(elementType);

                    body = Expression.Call(anyMethod, property, lambda);
                }
                break;
            case FilterOperator.NotContains:
                if (property.Type == typeof(string))
                {
                    var containsExpr = Expression.Call(property, typeof(string).GetMethod("Contains", new[] { typeof(string) }), constant);
                    body = Expression.Not(containsExpr);
                }
                else if (typeof(IEnumerable).IsAssignableFrom(property.Type) && property.Type != typeof(string))
                {
                    var elementType = property.Type.IsArray
                        ? property.Type.GetElementType()
                        : property.Type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

                    var param = Expression.Parameter(elementType, "x");

                    Expression containsExpr;
                    if (elementType == typeof(string))
                    {
                        // 關鍵字查詢時，直接用 rule.Value?.ToString()，避免 value 被誤轉型
                        containsExpr = Expression.Call(param, typeof(string).GetMethod("Contains", new[] { typeof(string) }), Expression.Constant(rule.Value?.ToString()));
                    }
                    else
                    {
                        // 其他型別直接用等值比對，先轉換值類型
                        var convertedElementValue = ChangeType(rule.Value, elementType);
                        containsExpr = Expression.Equal(param, Expression.Constant(convertedElementValue, elementType));
                    }
                    var lambda = Expression.Lambda(containsExpr, param);

                    var anyMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                        .MakeGenericMethod(elementType);

                    body = Expression.Not(Expression.Call(anyMethod, property, lambda));
                }
                break;
            case FilterOperator.NotLike:
                if (property.Type == typeof(string))
                {
                    body = Expression.Not(BuildLike(property, convertedValue?.ToString()));
                }
                else if (typeof(IEnumerable).IsAssignableFrom(property.Type) && property.Type != typeof(string))
                {
                    var elementType = property.Type.IsArray
                        ? property.Type.GetElementType()
                        : property.Type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

                    if (elementType == typeof(string))
                    {
                        var param = Expression.Parameter(elementType, "x");
                        // 關鍵字查詢時，直接用 rule.Value?.ToString()，避免 value 被誤轉型
                        var containsExpr = BuildLike(param, rule.Value?.ToString());
                        var lambda = Expression.Lambda(containsExpr, param);

                        var anyMethod = typeof(Enumerable).GetMethods()
                            .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                            .MakeGenericMethod(elementType);

                        body = Expression.Not(Expression.Call(anyMethod, property, lambda));
                    }
                }
                break;
            case FilterOperator.Any:
                if (typeof(IEnumerable).IsAssignableFrom(property.Type) && property.Type != typeof(string))
                {
                    var elementType = property.Type.IsArray
                        ? property.Type.GetElementType()
                        : property.Type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

                    // 如果沒有提供 value 或 value 為 null，則檢查集合是否有任何元素
                    if (rule.Value == null)
                    {
                        var anyMethod = typeof(Enumerable).GetMethods()
                            .First(m => m.Name == "Any" && m.GetParameters().Length == 1)
                            .MakeGenericMethod(elementType);
                        
                        // 需要先檢查集合是否為 null
                        var nullCheck = Expression.NotEqual(property, Expression.Constant(null, property.Type));
                        var anyCall = Expression.Call(anyMethod, property);
                        body = Expression.AndAlso(nullCheck, anyCall);
                    }
                    else
                    {
                        // 如果提供了 value，則檢查集合中是否有任何元素等於該值
                        var param = Expression.Parameter(elementType, "x");
                        var valueExpr = Expression.Constant(ChangeType(rule.Value, elementType), elementType);
                        var equalExpr = Expression.Equal(param, valueExpr);
                        var lambda = Expression.Lambda(equalExpr, param);

                        var anyWithPredicateMethod = typeof(Enumerable).GetMethods()
                            .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                            .MakeGenericMethod(elementType);
                        
                        // 需要先檢查集合是否為 null
                        var nullCheck = Expression.NotEqual(property, Expression.Constant(null, property.Type));
                        var anyCall = Expression.Call(anyWithPredicateMethod, property, lambda);
                        body = Expression.AndAlso(nullCheck, anyCall);
                    }
                }
                else
                {
                    body = Expression.Constant(false);
                }
                break;
            case FilterOperator.NotAny:
                if (typeof(IEnumerable).IsAssignableFrom(property.Type) && property.Type != typeof(string))
                {
                    var elementType = property.Type.IsArray
                        ? property.Type.GetElementType()
                        : property.Type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

                    // 如果沒有提供 value 或 value 為 null，則檢查集合是否沒有任何元素
                    if (rule.Value == null)
                    {
                        var anyMethod = typeof(Enumerable).GetMethods()
                            .First(m => m.Name == "Any" && m.GetParameters().Length == 1)
                            .MakeGenericMethod(elementType);
                        
                        // 檢查集合是否為 null 或空
                        var nullCheck = Expression.Equal(property, Expression.Constant(null, property.Type));
                        var anyCall = Expression.Call(anyMethod, property);
                        var notAnyCall = Expression.Not(anyCall);
                        body = Expression.OrElse(nullCheck, notAnyCall);
                    }
                    else
                    {
                        // 如果提供了 value ，則檢查集合中是否沒有任何元素等於該值
                        var param = Expression.Parameter(elementType, "x");
                        var valueExpr = Expression.Constant(ChangeType(rule.Value, elementType), elementType);
                        var equalExpr = Expression.Equal(param, valueExpr);
                        var lambda = Expression.Lambda(equalExpr, param);

                        var anyWithPredicateMethod = typeof(Enumerable).GetMethods()
                            .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                            .MakeGenericMethod(elementType);
                        
                        // 檢查集合是否為 null 或不包含該值
                        var nullCheck = Expression.Equal(property, Expression.Constant(null, property.Type));
                        var anyCall = Expression.Call(anyWithPredicateMethod, property, lambda);
                        var notAnyCall = Expression.Not(anyCall);
                        body = Expression.OrElse(nullCheck, notAnyCall);
                    }
                }
                else
                {
                    body = Expression.Constant(true);
                }
                break;
        }
        
        body ??= Expression.Constant(true);

        // ★ NOT on single rule
        return rule.IsNegated ? Expression.Not(body) : body;
    }
    private static object ChangeType(object value, Type targetType)
    {
        if (value == null)
            return null;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // 集合型別處理
        if (typeof(IEnumerable).IsAssignableFrom(targetType) && targetType != typeof(string))
        {
            Type elementType = targetType.IsArray
                ? targetType.GetElementType()
                : targetType.GetGenericArguments().FirstOrDefault() ?? typeof(object);

            var elementUnderlyingType = Nullable.GetUnderlyingType(elementType) ?? elementType;

            IEnumerable<object> items;
            if (value is string s)
            {
                items = s.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x));
            }
            else if (value is IEnumerable enumerable && !(value is string))
            {
                items = enumerable.Cast<object>();
            }
            else
            {
                items = new[] { value };
            }

            var converted = items
                .Select(x => x == null ? null : Convert.ChangeType(x, elementUnderlyingType))
                .ToArray();

            if (targetType.IsArray)
            {
                var array = Array.CreateInstance(elementType, converted.Length);
                for (int i = 0; i < converted.Length; i++)
                    array.SetValue(converted[i], i);
                return array;
            }
            else
            {
                var listType = typeof(List<>).MakeGenericType(elementType);
                var list = (IList)Activator.CreateInstance(listType);
                foreach (var item in converted)
                    list.Add(item);
                return list;
            }
        }

        // Enum
        if (underlyingType.IsEnum)
            return Enum.Parse(underlyingType, value.ToString());

        // Guid
        if (underlyingType == typeof(Guid))
            return Guid.Parse(value.ToString());

        // Bool
        if (underlyingType == typeof(bool))
            return value.ToString().Equals("true", StringComparison.OrdinalIgnoreCase) || value.ToString() == "1";

        // String
        if (underlyingType == typeof(string))
            return value?.ToString();

        // Decimal - 明確處理 decimal 類型
        if (underlyingType == typeof(decimal))
        {
            if (value is decimal decimalValue)
                return decimalValue;
            if (decimal.TryParse(value.ToString(), out var parsedDecimal))
                return parsedDecimal;
            return Convert.ToDecimal(value);
        }

        // Double
        if (underlyingType == typeof(double))
        {
            if (value is double doubleValue)
                return doubleValue;
            if (double.TryParse(value.ToString(), out var parsedDouble))
                return parsedDouble;
            return Convert.ToDouble(value);
        }

        // Float
        if (underlyingType == typeof(float))
        {
            if (value is float floatValue)
                return floatValue;
            if (float.TryParse(value.ToString(), out var parsedFloat))
                return parsedFloat;
            return Convert.ToSingle(value);
        }

        // Int
        if (underlyingType == typeof(int))
        {
            if (value is int intValue)
                return intValue;
            if (int.TryParse(value.ToString(), out var parsedInt))
                return parsedInt;
            return Convert.ToInt32(value);
        }

        // Long
        if (underlyingType == typeof(long))
        {
            if (value is long longValue)
                return longValue;
            if (long.TryParse(value.ToString(), out var parsedLong))
                return parsedLong;
            return Convert.ToInt64(value);
        }

        // DateTime
        if (underlyingType == typeof(DateTime))
        {
            if (value is DateTime dateTimeValue)
                return dateTimeValue;
            if (DateTime.TryParse(value.ToString(), out var parsedDateTime))
                return parsedDateTime;
            return Convert.ToDateTime(value);
        }

        // 處理單一 Nullable 型別
        if (Nullable.GetUnderlyingType(targetType) != null)
        {
            if (value == null)
                return null;
            return Convert.ChangeType(value, underlyingType);
        }

        return Convert.ChangeType(value, underlyingType);
    }
    private static object _ChangeType(object value, Type targetType)
    {
        if (value == null)
            return null;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // 集合型別處理
        if (typeof(IEnumerable).IsAssignableFrom(underlyingType) && underlyingType != typeof(string))
        {
            Type elementType = underlyingType.IsArray
                ? underlyingType.GetElementType()
                : underlyingType.GetGenericArguments().FirstOrDefault() ?? typeof(object);

            IEnumerable<object> items;
            if (value is string s)
            {
                items = s.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x));
            }
            else if (value is IEnumerable enumerable && !(value is string))
            {
                items = enumerable.Cast<object>();
            }
            else
            {
                items = new[] { value };
            }

            var converted = items
                .Select(x => ChangeType(x, elementType))
                .ToArray();

            if (underlyingType.IsArray)
            {
                var array = Array.CreateInstance(elementType, converted.Length);
                for (int i = 0; i < converted.Length; i++)
                    array.SetValue(converted[i], i);
                return array;
            }
            else
            {
                var listType = typeof(List<>).MakeGenericType(elementType);
                var list = (IList)Activator.CreateInstance(listType);
                foreach (var item in converted)
                    list.Add(item);
                return list;
            }
        }

        // Enum
        if (underlyingType.IsEnum)
            return Enum.Parse(underlyingType, value.ToString());

        // Guid
        if (underlyingType == typeof(Guid))
            return Guid.Parse(value.ToString());

        // Bool
        if (underlyingType == typeof(bool))
            return value.ToString().Equals("true", StringComparison.OrdinalIgnoreCase) || value.ToString() == "1";

        // String
        if (underlyingType == typeof(string))
            return value?.ToString();

        // 處理陣列型別（如 Between 操作）
        if (value is Array array1 && targetType != typeof(string))
        {
            var elementType = targetType.IsArray ? targetType.GetElementType() : targetType;
            var convertedArray = Array.CreateInstance(elementType, array1.Length);
            for (int i = 0; i < array1.Length; i++)
            {
                convertedArray.SetValue(ChangeType(array1.GetValue(i), elementType), i);
            }
            return convertedArray;
        }

        // Nullable 型別處理（如 decimal?）
        if (Nullable.GetUnderlyingType(targetType) != null)
        {
            // 若 value 已是 underlyingType，直接回傳
            if (value.GetType() == underlyingType)
                return value;
            return Convert.ChangeType(value, underlyingType);
        }

        var result = Convert.ChangeType(value, underlyingType);

        // 型別不符時強制轉型
        if (result != null && !targetType.IsAssignableFrom(result.GetType()))
        {
            return Convert.ChangeType(result, targetType);
        }
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
        if (value is not IEnumerable valueList || value is string)
            return Expression.Constant(true);

        var propertyType = property.Type;
        var elementType = propertyType.IsArray
            ? propertyType.GetElementType()
            : propertyType.GetGenericArguments().FirstOrDefault() ?? propertyType;

        // property 是集合
        if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(string))
        {
            // 轉換 valueList 中的元素為正確的類型
            var convertedValues = valueList.Cast<object>()
                .Select(v => v == null ? null : ChangeType(v, elementType))
                .ToArray();

            // 創建正確類型的集合
            var listType = typeof(List<>).MakeGenericType(elementType);
            var convertedList = (IList)Activator.CreateInstance(listType);
            foreach (var item in convertedValues)
                convertedList.Add(item);

            // x => valueList.Contains(x)
            var param = Expression.Parameter(elementType, "x");
            var containsMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(elementType);
            var valuesExpression = Expression.Constant(convertedList, typeof(IEnumerable<>).MakeGenericType(elementType));
            var body = Expression.Call(containsMethod, valuesExpression, param);
            var lambda = Expression.Lambda(body, param);

            // property.Any(x => valueList.Contains(x))
            var anyMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                .MakeGenericMethod(elementType);
            return Expression.Call(anyMethod, property, lambda);
        }
        else
        {
            // 轉換 valueList 中的元素為屬性的類型
            var convertedValues = valueList.Cast<object>()
                .Select(v => v == null ? null : ChangeType(v, propertyType))
                .ToArray();

            // 創建正確類型的集合
            var listType = typeof(List<>).MakeGenericType(propertyType);
            var convertedList = (IList)Activator.CreateInstance(listType);
            foreach (var item in convertedValues)
                convertedList.Add(item);

            // valueList.Contains(property)
            var containsMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(propertyType);
            var valuesExpression = Expression.Constant(convertedList, typeof(IEnumerable<>).MakeGenericType(propertyType));
            return Expression.Call(containsMethod, valuesExpression, property);
        }
    }

    private static Expression BuildBetweenExpression(FilterRule rule, Expression property)
    {
        if (rule.Value is not IEnumerable betweenList || rule.Value is string)
            return Expression.Constant(true);

        var items = betweenList.Cast<object>().ToList();
        if (items.Count != 2)
            return Expression.Constant(true);

        // 針對集合型別屬性
        if (typeof(IEnumerable).IsAssignableFrom(property.Type) && property.Type != typeof(string))
        {
            var elementType = property.Type.IsArray
                ? property.Type.GetElementType()
                : property.Type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

            var min = Expression.Constant(ChangeType(items[0], elementType), elementType);
            var max = Expression.Constant(ChangeType(items[1], elementType), elementType);

            var param = Expression.Parameter(elementType, "x");
            var geExpr = Expression.GreaterThanOrEqual(param, min);
            var leExpr = Expression.LessThanOrEqual(param, max);
            var betweenExpr = Expression.AndAlso(geExpr, leExpr);
            var lambda = Expression.Lambda(betweenExpr, param);

            var anyMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                .MakeGenericMethod(elementType);

            var result = Expression.Call(anyMethod, property, lambda);
            // 只有 NotBetween 且沒有 IsNegated 時才否定，避免雙重否定
            return rule.Operator == FilterOperator.NotBetween && !rule.IsNegated ? Expression.Not(result) : result;
        }
        else
        {
            // 針對單一值屬性
            var min = Expression.Constant(ChangeType(items[0], property.Type), property.Type);
            var max = Expression.Constant(ChangeType(items[1], property.Type), property.Type);
            var greaterThanOrEqual = Expression.GreaterThanOrEqual(property, min);
            var lessThanOrEqual = Expression.LessThanOrEqual(property, max);
            var betweenExpr = Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);
            // 只有 NotBetween 且沒有 IsNegated 時才否定，避免雙重否定
            return rule.Operator == FilterOperator.NotBetween && !rule.IsNegated ? Expression.Not(betweenExpr) : betweenExpr;
        }
    }
}