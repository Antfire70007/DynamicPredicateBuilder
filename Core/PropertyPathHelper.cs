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
            var member = Expression.PropertyOrField(current, memberName);
            bool isLast = i == members.Length - 1;

            if (!isLast && (IsNullableType(current.Type) || !current.Type.IsValueType))
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

    private static bool IsNullableType(Type type)
    {
        return Nullable.GetUnderlyingType(type) != null;
    }
}
