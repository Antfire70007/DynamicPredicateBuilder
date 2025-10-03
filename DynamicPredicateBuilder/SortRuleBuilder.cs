using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DynamicPredicateBuilder.Models;

namespace DynamicPredicateBuilder
{
    /// <summary>
    /// �䴩 Fluent API ���ƧǳW�h�غc��
    /// </summary>
    /// <typeparam name="T">�ؼй�������</typeparam>
    public class SortRuleBuilder<T>
    {
        private readonly List<SortRule> _sortRules = new();

        /// <summary>
        /// �إ߷s�� SortBuilder ���
        /// </summary>
        /// <typeparam name="TEntity">�ؼй�������</typeparam>
        /// <returns>SortRuleBuilder ���</returns>
        public static SortRuleBuilder<TEntity> SortBuilder<TEntity>()
        {
            return new SortRuleBuilder<TEntity>();
        }

        /// <summary>
        /// �إ߷s�� SortBuilder ��� (�x�����_����)
        /// </summary>
        /// <returns>SortRuleBuilder ���</returns>
        public static SortRuleBuilder<T> Create()
        {
            return new SortRuleBuilder<T>();
        }

        /// <summary>
        /// �q Expression �������ݩʦW��
        /// </summary>
        /// <param name="propertyExpression">�ݩʪ�F��</param>
        /// <returns>�ݩʦW��</returns>
        private static string GetPropertyName<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            if (propertyExpression.Body is MemberExpression memberExpr)
            {
                return GetMemberPath(memberExpr);
            }
            
            if (propertyExpression.Body is UnaryExpression unaryExpr && 
                unaryExpr.Operand is MemberExpression memberOperand)
            {
                return GetMemberPath(memberOperand);
            }
            
            throw new ArgumentException("Invalid property expression", nameof(propertyExpression));
        }

        /// <summary>
        /// ���o�������|�]�䴩�_���ݩʦp x.User.Name�^
        /// </summary>
        /// <param name="memberExpression">������F��</param>
        /// <returns>�ݩʸ��|</returns>
        private static string GetMemberPath(MemberExpression memberExpression)
        {
            var path = new List<string>();
            var current = memberExpression;
            
            while (current != null)
            {
                path.Add(current.Member.Name);
                current = current.Expression as MemberExpression;
            }
            
            path.Reverse();
            return string.Join(".", path);
        }

        /// <summary>
        /// �K�[�ƧǳW�h
        /// </summary>
        /// <param name="property">�ݩʦW��</param>
        /// <param name="descending">�O�_���ǱƧ�</param>
        /// <returns>Builder ���</returns>
        public SortRuleBuilder<T> Add(string property, bool descending = false)
        {
            _sortRules.Add(new SortRule
            {
                Property = property,
                Descending = descending
            });
            return this;
        }

        /// <summary>
        /// �K�[�ƧǳW�h (Expression ����)
        /// </summary>
        /// <typeparam name="TProperty">�ݩ�����</typeparam>
        /// <param name="propertyExpression">�ݩʪ�F��</param>
        /// <param name="descending">�O�_���ǱƧ�</param>
        /// <returns>Builder ���</returns>
        public SortRuleBuilder<T> Add<TProperty>(Expression<Func<T, TProperty>> propertyExpression, bool descending = false)
        {
            var propertyName = GetPropertyName(propertyExpression);
            return Add(propertyName, descending);
        }

        /// <summary>
        /// �K�[�ɧǱƧǳW�h
        /// </summary>
        /// <param name="property">�ݩʦW��</param>
        /// <returns>Builder ���</returns>
        public SortRuleBuilder<T> Ascending(string property)
        {
            return Add(property, false);
        }

        /// <summary>
        /// �K�[�ɧǱƧǳW�h (Expression ����)
        /// </summary>
        /// <typeparam name="TProperty">�ݩ�����</typeparam>
        /// <param name="propertyExpression">�ݩʪ�F��</param>
        /// <returns>Builder ���</returns>
        public SortRuleBuilder<T> Ascending<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            return Add(propertyExpression, false);
        }

        /// <summary>
        /// �K�[���ǱƧǳW�h
        /// </summary>
        /// <param name="property">�ݩʦW��</param>
        /// <returns>Builder ���</returns>
        public SortRuleBuilder<T> Descending(string property)
        {
            return Add(property, true);
        }

        /// <summary>
        /// �K�[���ǱƧǳW�h (Expression ����)
        /// </summary>
        /// <typeparam name="TProperty">�ݩ�����</typeparam>
        /// <param name="propertyExpression">�ݩʪ�F��</param>
        /// <returns>Builder ���</returns>
        public SortRuleBuilder<T> Descending<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            return Add(propertyExpression, true);
        }

        /// <summary>
        /// �K�[ ThenBy �ƧǳW�h (�P�e�@�ӱƧǳW�h�ۦP���p�U�����n�Ƨ�)
        /// </summary>
        /// <param name="property">�ݩʦW��</param>
        /// <returns>Builder ���</returns>
        public SortRuleBuilder<T> ThenBy(string property)
        {
            return Add(property, false);
        }

        /// <summary>
        /// �K�[ ThenBy �ƧǳW�h (Expression ����)
        /// </summary>
        /// <typeparam name="TProperty">�ݩ�����</typeparam>
        /// <param name="propertyExpression">�ݩʪ�F��</param>
        /// <returns>Builder ���</returns>
        public SortRuleBuilder<T> ThenBy<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            return Add(propertyExpression, false);
        }

        /// <summary>
        /// �K�[ ThenByDescending �ƧǳW�h
        /// </summary>
        /// <param name="property">�ݩʦW��</param>
        /// <returns>Builder ���</returns>
        public SortRuleBuilder<T> ThenByDescending(string property)
        {
            return Add(property, true);
        }

        /// <summary>
        /// �K�[ ThenByDescending �ƧǳW�h (Expression ����)
        /// </summary>
        /// <typeparam name="TProperty">�ݩ�����</typeparam>
        /// <param name="propertyExpression">�ݩʪ�F��</param>
        /// <returns>Builder ���</returns>
        public SortRuleBuilder<T> ThenByDescending<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            return Add(propertyExpression, true);
        }

        /// <summary>
        /// �إ߱ƧǳW�h�M��
        /// </summary>
        /// <returns>�ƧǳW�h�M��</returns>
        public List<SortRule> Build()
        {
            return _sortRules.ToList();
        }

        /// <summary>
        /// �����ഫ���ƧǳW�h�M��
        /// </summary>
        /// <param name="builder">Builder ���</param>
        public static implicit operator List<SortRule>(SortRuleBuilder<T> builder)
        {
            return builder.Build();
        }

        /// <summary>
        /// �����ഫ���ƧǳW�h�}�C
        /// </summary>
        /// <param name="builder">Builder ���</param>
        public static implicit operator SortRule[](SortRuleBuilder<T> builder)
        {
            return builder.Build().ToArray();
        }
    }

    /// <summary>
    /// �R�A�u�t���O�A����²�ƪ��إߤ�k
    /// </summary>
    public static class SortRuleBuilder
    {
        /// <summary>
        /// �إ߷s�� SortBuilder ���
        /// </summary>
        /// <typeparam name="T">�ؼй�������</typeparam>
        /// <returns>SortRuleBuilder ���</returns>
        public static SortRuleBuilder<T> SortBuilder<T>()
        {
            return new SortRuleBuilder<T>();
        }

        /// <summary>
        /// �إ߷s�� SortBuilder ���
        /// </summary>
        /// <typeparam name="T">�ؼй�������</typeparam>
        /// <returns>SortRuleBuilder ���</returns>
        public static SortRuleBuilder<T> Create<T>()
        {
            return new SortRuleBuilder<T>();
        }
    }
}