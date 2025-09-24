using System.Linq.Expressions;

namespace DynamicPredicateBuilder.Core;
public static class PropertyPathHelper
{
    public static Expression BuildPropertyExpression(Expression parameter, string propertyPath)
    {
        Expression current = parameter;
        var members = propertyPath.Split('.');

        for (int i = 0; i < members.Length; i++)
        {
            var memberName = members[i];
            bool isCollection = memberName.EndsWith("[]");
            string actualMemberName = isCollection ? memberName[..^2] : memberName;

            var member = Expression.PropertyOrField(current, actualMemberName);
            bool isLast = i == members.Length - 1;

            if (isCollection)
            {
                Type elementType = member.Type.IsArray
                    ? member.Type.GetElementType()
                    : member.Type.GetGenericArguments().FirstOrDefault();

                if (elementType == null)
                    throw new InvalidOperationException("集合型別無法取得元素型別");

                // 處理下一層
                if (++i >= members.Length)
                    throw new InvalidOperationException("集合型別必須指定元素屬性");

                // 產生巢狀 selector
                var nextPath = string.Join('.', members[i..]);
                var elementParam = Expression.Parameter(elementType, "x");
                var elementBody = BuildPropertyExpression(elementParam, nextPath);
                var selector = Expression.Lambda(elementBody, elementParam);

                var selectMethod = typeof(Enumerable).GetMethods()
                    .First(m => m.Name == "Select" && m.GetParameters().Length == 2)
                    .MakeGenericMethod(elementType, elementBody.Type);

                current = Expression.Call(selectMethod, member, selector);
                break; // 巢狀已遞迴處理，跳出迴圈
            }
            else if (!isLast && (IsNullableType(current.Type) || !current.Type.IsValueType))
            {
                var nullCheck = Expression.Equal(current, Expression.Constant(null, current.Type));
                var defaultValue = Expression.Constant(null, member.Type);
                current = Expression.Condition(nullCheck, defaultValue, member);
            }
            else
            {
                current = member;
            }
        }

        return current;
    }

    //public static Expression BuildPropertyExpression(Expression parameter, string propertyPath)
    //{
    //    Expression current = parameter;
    //    var members = propertyPath.Split('.');

    //    for (int i = 0; i < members.Length; i++)
    //    {
    //        var memberName = members[i];
    //        var member = Expression.PropertyOrField(current, memberName);
    //        bool isLast = i == members.Length - 1;

    //        if (!isLast && (IsNullableType(current.Type) || !current.Type.IsValueType))
    //        {
    //            var nullCheck = Expression.Equal(current, Expression.Constant(null, current.Type));
    //            var defaultValue = Expression.Constant(null, member.Type);
    //            current = Expression.Condition(nullCheck, defaultValue, member);
    //        }
    //        else
    //        {
    //            current = member;
    //        }
    //    }

    //    return current;
    //}

    private static bool IsNullableType(Type type)
    {
        return Nullable.GetUnderlyingType(type) != null;
    }
}
