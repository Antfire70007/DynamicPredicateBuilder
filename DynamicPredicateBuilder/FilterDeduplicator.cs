//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;

//namespace DynamicPredicateBuilder;

//public static class FilterDeduplicator
//{
//    public static Expression<Func<T, bool>> Deduplicate<T>(IEnumerable<Expression<Func<T, bool>>> expressions)
//    {
//        var uniqueExpressions = new HashSet<string>();
//        var dedupedExpressions = new List<Expression>();

//        foreach (var expr in expressions)
//        {
//            string key = expr.Body.ToString();
//            if (uniqueExpressions.Add(key))
//            {
//                dedupedExpressions.Add(expr.Body);
//            }
//        }

//        if (!dedupedExpressions.Any())
//            return x => true;

//        var param = Expression.Parameter(typeof(T), "x");
//        Expression? body = null;

//        foreach (var expr in dedupedExpressions)
//        {
//            var replaced = new ParameterReplacer(param).Visit(expr);
//            body = body == null ? replaced : Expression.AndAlso(body, replaced);
//        }

//        return Expression.Lambda<Func<T, bool>>(body!, param);
//    }

//    private class ParameterReplacer : ExpressionVisitor
//    {
//        private readonly ParameterExpression _parameter;

//        public ParameterReplacer(ParameterExpression parameter)
//        {
//            _parameter = parameter;
//        }

//        protected override Expression VisitParameter(ParameterExpression node)
//        {
//            return _parameter;
//        }
//    }
//}