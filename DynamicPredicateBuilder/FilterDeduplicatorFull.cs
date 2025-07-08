using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DynamicPredicateBuilder.Models;

namespace DynamicPredicateBuilder;

public static class FilterDeduplicator
{
    // 主函式：從 Expression 清單去重合併
    public static Expression<Func<T, bool>> Deduplicate<T>(IEnumerable<Expression<Func<T, bool>>> expressions)
    {
        var uniqueSet = new HashSet<string>();
        var duplicates = new List<string>();
        var merged = new List<Expression>();

        foreach (var expr in expressions)
        {
            string key = expr.Body.ToString();
            if (uniqueSet.Add(key))
            {
                merged.Add(expr.Body);
            }
            else
            {
                duplicates.Add(key);
            }
        }

        if (duplicates.Count > 0)
        {
            Console.WriteLine("🔁 重複條件：");
            foreach (var dup in duplicates.Distinct())
                Console.WriteLine($" - {dup}");
        }

        if (!merged.Any())
            return x => true;

        var param = Expression.Parameter(typeof(T), "x");
        Expression? body = null;

        foreach (var expr in merged)
        {
            var replaced = new ParameterReplacer(param).Visit(expr);
            body = body == null ? replaced : Expression.AndAlso(body, replaced);
        }

        return Expression.Lambda<Func<T, bool>>(body!, param);
    }

    // 從 FilterRule 去重
    public static List<FilterRule> DeduplicateRules(IEnumerable<FilterRule> rules)
    {
        var seen = new HashSet<string>();
        return rules.Where(rule =>
        {
            string key = $"{rule.Property}|{rule.Operator}|{rule.Value}";
            return seen.Add(key);
        }).ToList();
    }

    private class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _parameter;
        public ParameterReplacer(ParameterExpression parameter) => _parameter = parameter;
        protected override Expression VisitParameter(ParameterExpression node) => _parameter;
    }
}

