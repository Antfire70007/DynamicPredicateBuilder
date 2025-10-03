using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using DynamicPredicateBuilder.Models;

namespace DynamicPredicateBuilder
{
    /// <summary>
    /// �䴩 Fluent API ���L�o����r��غc��
    /// </summary>
    /// <typeparam name="T">�ؼй�������</typeparam>
    public class FilterDictionaryBuilder<T>
    {
        private readonly List<object> _rules = new();
        private LogicalOperator _logicalOperator = LogicalOperator.And;
        private LogicalOperator _interOperator = LogicalOperator.And;
        private bool _isNegated = false;

        /// <summary>
        /// �إ߷s�� QueryBuilder ���
        /// </summary>
        /// <typeparam name="TEntity">�ؼй�������</typeparam>
        /// <returns>FilterDictionaryBuilder ���</returns>
        public static FilterDictionaryBuilder<TEntity> QueryBuilder<TEntity>()
        {
            return new FilterDictionaryBuilder<TEntity>();
        }

        /// <summary>
        /// �إ߷s�� QueryBuilder ��� (�x�����_����)
        /// </summary>
        /// <returns>FilterDictionaryBuilder ���</returns>
        public static FilterDictionaryBuilder<T> Create()
        {
            return new FilterDictionaryBuilder<T>();
        }

        /// <summary>
        /// �]�w��e�s�ժ��޿�B��l
        /// </summary>
        /// <param name="logicalOperator">�޿�B��l (And/Or)</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> WithLogicalOperator(LogicalOperator logicalOperator)
        {
            _logicalOperator = logicalOperator;
            return this;
        }

        /// <summary>
        /// �]�w�P�U�@�Ӹs�ժ��s���B��l
        /// </summary>
        /// <param name="interOperator">�s�ն��B��l</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> WithInterOperator(LogicalOperator interOperator)
        {
            _interOperator = interOperator;
            return this;
        }

        /// <summary>
        /// �]�w�O�_�_�w��e�s��
        /// </summary>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> Negate(bool isNegated = true)
        {
            _isNegated = isNegated;
            return this;
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
        /// �K�[²�檺�L�o����
        /// </summary>
        /// <param name="property">�ݩʦW��</param>
        /// <param name="operator">�L�o�B��l</param>
        /// <param name="value">�����</param>
        /// <param name="isNegated">�O�_�_�w������</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> Add(string property, FilterOperator @operator, object value, bool isNegated = false)
        {
            var rule = new Dictionary<string, object>
            {
                { "Property", property },
                { "Operator", @operator },
                { "Value", value }
            };

            if (isNegated)
                rule["IsNegated"] = true;

            _rules.Add(rule);
            return this;
        }

        /// <summary>
        /// �K�[²�檺�L�o���� (Expression ����)
        /// </summary>
        /// <typeparam name="TProperty">�ݩ�����</typeparam>
        /// <param name="propertyExpression">�ݩʪ�F��</param>
        /// <param name="operator">�L�o�B��l</param>
        /// <param name="value">�����</param>
        /// <param name="isNegated">�O�_�_�w������</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> Add<TProperty>(Expression<Func<T, TProperty>> propertyExpression, FilterOperator @operator, object value, bool isNegated = false)
        {
            var propertyName = GetPropertyName(propertyExpression);
            return Add(propertyName, @operator, value, isNegated);
        }

        /// <summary>
        /// �K�[�ݩʹ��ݩʪ��������
        /// </summary>
        /// <param name="property">�D�ݩʦW��</param>
        /// <param name="operator">����B��l</param>
        /// <param name="compareToProperty">����ؼ��ݩʦW��</param>
        /// <param name="isNegated">�O�_�_�w������</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> AddPropertyComparison(string property, FilterOperator @operator, string compareToProperty, bool isNegated = false)
        {
            var rule = new Dictionary<string, object>
            {
                { "Property", property },
                { "Operator", @operator },
                { "CompareToProperty", compareToProperty }
            };

            if (isNegated)
                rule["IsNegated"] = true;

            _rules.Add(rule);
            return this;
        }

        /// <summary>
        /// �K�[�ݩʹ��ݩʪ�������� (Expression ����)
        /// </summary>
        /// <typeparam name="TProperty1">�Ĥ@���ݩ�����</typeparam>
        /// <typeparam name="TProperty2">�ĤG���ݩ�����</typeparam>
        /// <param name="propertyExpression">�D�ݩʪ�F��</param>
        /// <param name="operator">����B��l</param>
        /// <param name="compareToPropertyExpression">����ؼ��ݩʪ�F��</param>
        /// <param name="isNegated">�O�_�_�w������</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> AddPropertyComparison<TProperty1, TProperty2>(
            Expression<Func<T, TProperty1>> propertyExpression, 
            FilterOperator @operator, 
            Expression<Func<T, TProperty2>> compareToPropertyExpression, 
            bool isNegated = false)
        {
            var propertyName = GetPropertyName(propertyExpression);
            var compareToPropertyName = GetPropertyName(compareToPropertyExpression);
            return AddPropertyComparison(propertyName, @operator, compareToPropertyName, isNegated);
        }

        /// <summary>
        /// �إ߱_���s�լd��
        /// </summary>
        /// <param name="logicalOperator">�s�դ����޿�B��l</param>
        /// <param name="builderAction">�غc�l�s�ժ��ʧ@</param>
        /// <param name="isNegated">�O�_�_�w��Ӹs��</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> Compare(LogicalOperator logicalOperator, Action<FilterDictionaryBuilder<T>> builderAction, bool isNegated = false)
        {
            var subBuilder = new FilterDictionaryBuilder<T>().WithLogicalOperator(logicalOperator);
            builderAction(subBuilder);

            var subGroup = subBuilder.Build();
            if (isNegated && subGroup.ContainsKey("IsNegated"))
                subGroup["IsNegated"] = true;
            else if (isNegated)
                subGroup.Add("IsNegated", true);

            _rules.Add(subGroup);
            return this;
        }

        /// <summary>
        /// �إ� Equal ���󪺧ֱ���k
        /// </summary>
        /// <param name="property">�ݩʦW��</param>
        /// <param name="value">�����</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> Equal(string property, object value, bool isNegated = false)
        {
            return Add(property, FilterOperator.Equal, value, isNegated);
        }

        /// <summary>
        /// �إ� Equal ���󪺧ֱ���k (Expression ����)
        /// </summary>
        /// <typeparam name="TProperty">�ݩ�����</typeparam>
        /// <param name="propertyExpression">�ݩʪ�F��</param>
        /// <param name="value">�����</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> Equal<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.Equal, value, isNegated);
        }

        /// <summary>
        /// �إ� Like ���󪺧ֱ���k
        /// </summary>
        /// <param name="property">�ݩʦW��</param>
        /// <param name="value">�����</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> Like(string property, object value, bool isNegated = false)
        {
            return Add(property, FilterOperator.Like, value, isNegated);
        }

        /// <summary>
        /// �إ� Like ���󪺧ֱ���k (Expression ����)
        /// </summary>
        /// <typeparam name="TProperty">�ݩ�����</typeparam>
        /// <param name="propertyExpression">�ݩʪ�F��</param>
        /// <param name="value">�����</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> Like<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.Like, value, isNegated);
        }

        /// <summary>
        /// �إ� Contains ���󪺧ֱ���k
        /// </summary>
        /// <param name="property">�ݩʦW��</param>
        /// <param name="value">�����</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> Contains(string property, object value, bool isNegated = false)
        {
            return Add(property, FilterOperator.Contains, value, isNegated);
        }

        /// <summary>
        /// �إ� Contains ���󪺧ֱ���k (Expression ����)
        /// </summary>
        /// <typeparam name="TProperty">�ݩ�����</typeparam>
        /// <param name="propertyExpression">�ݩʪ�F��</param>
        /// <param name="value">�����</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> Contains<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.Contains, value, isNegated);
        }

        /// <summary>
        /// �إ� In ���󪺧ֱ���k
        /// </summary>
        /// <param name="property">�ݩʦW��</param>
        /// <param name="values">�ȶ��X</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> In(string property, IEnumerable<object> values, bool isNegated = false)
        {
            return Add(property, FilterOperator.In, values, isNegated);
        }

        /// <summary>
        /// �إ� In ���󪺧ֱ���k (Expression ����)
        /// </summary>
        /// <typeparam name="TProperty">�ݩ�����</typeparam>
        /// <param name="propertyExpression">�ݩʪ�F��</param>
        /// <param name="values">�ȶ��X</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> In<TProperty>(Expression<Func<T, TProperty>> propertyExpression, IEnumerable<object> values, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.In, values, isNegated);
        }

        /// <summary>
        /// �إ� Between ���󪺧ֱ���k
        /// </summary>
        /// <param name="property">�ݩʦW��</param>
        /// <param name="min">�̤p��</param>
        /// <param name="max">�̤j��</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> Between(string property, object min, object max, bool isNegated = false)
        {
            return Add(property, FilterOperator.Between, new[] { min, max }, isNegated);
        }

        /// <summary>
        /// �إ� Between ���󪺧ֱ���k (Expression ����)
        /// </summary>
        /// <typeparam name="TProperty">�ݩ�����</typeparam>
        /// <param name="propertyExpression">�ݩʪ�F��</param>
        /// <param name="min">�̤p��</param>
        /// <param name="max">�̤j��</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> Between<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object min, object max, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.Between, new[] { min, max }, isNegated);
        }

        /// <summary>
        /// �إ� GreaterThan ���󪺧ֱ���k
        /// </summary>
        /// <param name="property">�ݩʦW��</param>
        /// <param name="value">�����</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> GreaterThan(string property, object value, bool isNegated = false)
        {
            return Add(property, FilterOperator.GreaterThan, value, isNegated);
        }

        /// <summary>
        /// �إ� GreaterThan ���󪺧ֱ���k (Expression ����)
        /// </summary>
        /// <typeparam name="TProperty">�ݩ�����</typeparam>
        /// <param name="propertyExpression">�ݩʪ�F��</param>
        /// <param name="value">�����</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> GreaterThan<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.GreaterThan, value, isNegated);
        }

        /// <summary>
        /// �إ� LessThan ���󪺧ֱ���k
        /// </summary>
        /// <param name="property">�ݩʦW��</param>
        /// <param name="value">�����</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> LessThan(string property, object value, bool isNegated = false)
        {
            return Add(property, FilterOperator.LessThan, value, isNegated);
        }

        /// <summary>
        /// �إ� LessThan ���󪺧ֱ���k (Expression ����)
        /// </summary>
        /// <typeparam name="TProperty">�ݩ�����</typeparam>
        /// <param name="propertyExpression">�ݩʪ�F��</param>
        /// <param name="value">�����</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> LessThan<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.LessThan, value, isNegated);
        }

        /// <summary>
        /// �إ� GreaterThanOrEqual ���󪺧ֱ���k
        /// </summary>
        /// <param name="property">�ݩʦW��</param>
        /// <param name="value">�����</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> GreaterThanOrEqual(string property, object value, bool isNegated = false)
        {
            return Add(property, FilterOperator.GreaterThanOrEqual, value, isNegated);
        }

        /// <summary>
        /// �إ� GreaterThanOrEqual ���󪺧ֱ���k (Expression ����)
        /// </summary>
        /// <typeparam name="TProperty">�ݩ�����</typeparam>
        /// <param name="propertyExpression">�ݩʪ�F��</param>
        /// <param name="value">�����</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> GreaterThanOrEqual<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.GreaterThanOrEqual, value, isNegated);
        }

        /// <summary>
        /// �إ� LessThanOrEqual ���󪺧ֱ���k
        /// </summary>
        /// <param name="property">�ݩʦW��</param>
        /// <param name="value">�����</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> LessThanOrEqual(string property, object value, bool isNegated = false)
        {
            return Add(property, FilterOperator.LessThanOrEqual, value, isNegated);
        }

        /// <summary>
        /// �إ� LessThanOrEqual ���󪺧ֱ���k (Expression ����)
        /// </summary>
        /// <typeparam name="TProperty">�ݩ�����</typeparam>
        /// <param name="propertyExpression">�ݩʪ�F��</param>
        /// <param name="value">�����</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> LessThanOrEqual<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.LessThanOrEqual, value, isNegated);
        }

        /// <summary>
        /// �إ� StartsWith ���󪺧ֱ���k
        /// </summary>
        /// <param name="property">�ݩʦW��</param>
        /// <param name="value">�����</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> StartsWith(string property, object value, bool isNegated = false)
        {
            return Add(property, FilterOperator.StartsWith, value, isNegated);
        }

        /// <summary>
        /// �إ� StartsWith ���󪺧ֱ���k (Expression ����)
        /// </summary>
        /// <typeparam name="TProperty">�ݩ�����</typeparam>
        /// <param name="propertyExpression">�ݩʪ�F��</param>
        /// <param name="value">�����</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> StartsWith<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.StartsWith, value, isNegated);
        }

        /// <summary>
        /// �إ� EndsWith ���󪺧ֱ���k
        /// </summary>
        /// <param name="property">�ݩʦW��</param>
        /// <param name="value">�����</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> EndsWith(string property, object value, bool isNegated = false)
        {
            return Add(property, FilterOperator.EndsWith, value, isNegated);
        }

        /// <summary>
        /// �إ� EndsWith ���󪺧ֱ���k (Expression ����)
        /// </summary>
        /// <typeparam name="TProperty">�ݩ�����</typeparam>
        /// <param name="propertyExpression">�ݩʪ�F��</param>
        /// <param name="value">�����</param>
        /// <param name="isNegated">�O�_�_�w</param>
        /// <returns>Builder ���</returns>
        public FilterDictionaryBuilder<T> EndsWith<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.EndsWith, value, isNegated);
        }

        /// <summary>
        /// �غc�̲ת��r�嵲�G
        /// </summary>
        /// <returns>�L�o����r��</returns>
        public Dictionary<string, object> Build()
        {
            var result = new Dictionary<string, object>
            {
                { "LogicalOperator", _logicalOperator },
                { "Rules", _rules }
            };

            if (_interOperator != LogicalOperator.And)
                result["InterOperator"] = _interOperator;

            if (_isNegated)
                result["IsNegated"] = true;

            return result;
        }

        /// <summary>
        /// �غc�̲ת��r�嵲�G���ഫ�� FilterGroup
        /// </summary>
        /// <returns>FilterGroup ���</returns>
        public FilterGroup ToFilterGroup()
        {
            var dict = Build();
            return FilterGroupFactory.FromDictionary(dict);
        }

        /// <summary>
        /// �����ഫ���r��
        /// </summary>
        /// <param name="builder">Builder ���</param>
        public static implicit operator Dictionary<string, object>(FilterDictionaryBuilder<T> builder)
        {
            return builder.Build();
        }

        /// <summary>
        /// �����ഫ�� FilterGroup
        /// </summary>
        /// <param name="builder">Builder ���</param>
        public static implicit operator FilterGroup(FilterDictionaryBuilder<T> builder)
        {
            return builder.ToFilterGroup();
        }
    }

    /// <summary>
    /// �R�A�u�t���O�A����²�ƪ��إߤ�k
    /// </summary>
    public static class FilterDictionaryBuilder
    {
        /// <summary>
        /// �إ߷s�� QueryBuilder ���
        /// </summary>
        /// <typeparam name="T">�ؼй�������</typeparam>
        /// <returns>FilterDictionaryBuilder ���</returns>
        public static FilterDictionaryBuilder<T> QueryBuilder<T>()
        {
            return new FilterDictionaryBuilder<T>();
        }

        /// <summary>
        /// �إ߷s�� QueryBuilder ���
        /// </summary>
        /// <typeparam name="T">�ؼй�������</typeparam>
        /// <returns>FilterDictionaryBuilder ���</returns>
        public static FilterDictionaryBuilder<T> Create<T>()
        {
            return new FilterDictionaryBuilder<T>();
        }
    }
}