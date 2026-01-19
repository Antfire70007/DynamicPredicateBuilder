using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using DynamicPredicateBuilder.Models;

namespace DynamicPredicateBuilder
{
    /// <summary>
    /// 支援 Fluent API 的過濾條件字典建構器
    /// </summary>
    /// <typeparam name="T">目標實體類型</typeparam>
    public class FilterDictionaryBuilder<T>
    {
        private readonly List<object> _rules = new();
        private LogicalOperator _logicalOperator = LogicalOperator.And;
        private LogicalOperator _interOperator = LogicalOperator.And;
        private bool _isNegated = false;

        /// <summary>
        /// 建立新的 QueryBuilder 實例
        /// </summary>
        /// <typeparam name="TEntity">目標實體類型</typeparam>
        /// <returns>FilterDictionaryBuilder 實例</returns>
        public static FilterDictionaryBuilder<TEntity> QueryBuilder<TEntity>()
        {
            return new FilterDictionaryBuilder<TEntity>();
        }

        /// <summary>
        /// 建立新的 QueryBuilder 實例 (泛型推斷版本)
        /// </summary>
        /// <returns>FilterDictionaryBuilder 實例</returns>
        public static FilterDictionaryBuilder<T> Create()
        {
            return new FilterDictionaryBuilder<T>();
        }

        /// <summary>
        /// 設定當前群組的邏輯運算子
        /// </summary>
        /// <param name="logicalOperator">邏輯運算子 (And/Or)</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> WithLogicalOperator(LogicalOperator logicalOperator)
        {
            _logicalOperator = logicalOperator;
            return this;
        }

        /// <summary>
        /// 設定與下一個群組的連接運算子
        /// </summary>
        /// <param name="interOperator">群組間運算子</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> WithInterOperator(LogicalOperator interOperator)
        {
            _interOperator = interOperator;
            return this;
        }

        /// <summary>
        /// 設定是否否定當前群組
        /// </summary>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> Negate(bool isNegated = true)
        {
            _isNegated = isNegated;
            return this;
        }

        /// <summary>
        /// 從 Expression 中提取屬性名稱
        /// </summary>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <returns>屬性名稱</returns>
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
        /// 取得成員路徑（支援巢狀屬性如 x.User.Name）
        /// </summary>
        /// <param name="memberExpression">成員表達式</param>
        /// <returns>屬性路徑</returns>
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
        /// 添加簡單的過濾條件
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <param name="operator">過濾運算子</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定此條件</param>
        /// <returns>Builder 實例</returns>
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
        /// 添加簡單的過濾條件 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <param name="operator">過濾運算子</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定此條件</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> Add<TProperty>(Expression<Func<T, TProperty>> propertyExpression, FilterOperator @operator, object value, bool isNegated = false)
        {
            var propertyName = GetPropertyName(propertyExpression);
            return Add(propertyName, @operator, value, isNegated);
        }

        /// <summary>
        /// 添加屬性對屬性的比較條件
        /// </summary>
        /// <param name="property">主屬性名稱</param>
        /// <param name="operator">比較運算子</param>
        /// <param name="compareToProperty">比較目標屬性名稱</param>
        /// <param name="isNegated">是否否定此條件</param>
        /// <returns>Builder 實例</returns>
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
        /// 添加屬性對屬性的比較條件 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty1">第一個屬性類型</typeparam>
        /// <typeparam name="TProperty2">第二個屬性類型</typeparam>
        /// <param name="propertyExpression">主屬性表達式</param>
        /// <param name="operator">比較運算子</param>
        /// <param name="compareToPropertyExpression">比較目標屬性表達式</param>
        /// <param name="isNegated">是否否定此條件</param>
        /// <returns>Builder 實例</returns>
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
        /// 建立巢狀群組查詢
        /// </summary>
        /// <param name="logicalOperator">群組內的邏輯運算子</param>
        /// <param name="builderAction">建構子群組的動作</param>
        /// <param name="isNegated">是否否定整個群組</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> Compare(LogicalOperator logicalOperator, Action<FilterDictionaryBuilder<T>> builderAction, bool isNegated = false)
        {
            if (builderAction == null)
                throw new ArgumentNullException(nameof(builderAction));

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
        /// 建立 Equal 條件的快捷方法
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> Equal(string property, object value, bool isNegated = false)
        {
            return Add(property, FilterOperator.Equal, value, isNegated);
        }

        /// <summary>
        /// 建立 Equal 條件的快捷方法 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> Equal<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.Equal, value, isNegated);
        }

        /// <summary>
        /// 建立 Like 條件的快捷方法
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> Like(string property, object value, bool isNegated = false)
        {
            return Add(property, FilterOperator.Like, value, isNegated);
        }

        /// <summary>
        /// 建立 Like 條件的快捷方法 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> Like<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.Like, value, isNegated);
        }

        /// <summary>
        /// 建立 Contains 條件的快捷方法
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> Contains(string property, object value, bool isNegated = false)
        {
            return Add(property, FilterOperator.Contains, value, isNegated);
        }

        /// <summary>
        /// 建立 Contains 條件的快捷方法 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> Contains<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.Contains, value, isNegated);
        }

        /// <summary>
        /// 建立 In 條件的快捷方法
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <param name="values">值集合</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> In(string property, IEnumerable<object> values, bool isNegated = false)
        {
            return Add(property, FilterOperator.In, values, isNegated);
        }

        /// <summary>
        /// 建立 In 條件的快捷方法 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <param name="values">值集合</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> In<TProperty>(Expression<Func<T, TProperty>> propertyExpression, IEnumerable<object> values, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.In, values, isNegated);
        }

        /// <summary>
        /// 建立 Between 條件的快捷方法
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> Between(string property, object min, object max, bool isNegated = false)
        {
            return Add(property, FilterOperator.Between, new[] { min, max }, isNegated);
        }

        /// <summary>
        /// 建立 Between 條件的快捷方法 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> Between<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object min, object max, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.Between, new[] { min, max }, isNegated);
        }

        /// <summary>
        /// 建立 GreaterThan 條件的快捷方法
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> GreaterThan(string property, object value, bool isNegated = false)
        {
            return Add(property, FilterOperator.GreaterThan, value, isNegated);
        }

        /// <summary>
        /// 建立 GreaterThan 條件的快捷方法 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> GreaterThan<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.GreaterThan, value, isNegated);
        }

        /// <summary>
        /// 建立 LessThan 條件的快捷方法
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> LessThan(string property, object value, bool isNegated = false)
        {
            return Add(property, FilterOperator.LessThan, value, isNegated);
        }

        /// <summary>
        /// 建立 LessThan 條件的快捷方法 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> LessThan<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.LessThan, value, isNegated);
        }

        /// <summary>
        /// 建立 GreaterThanOrEqual 條件的快捷方法
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> GreaterThanOrEqual(string property, object value, bool isNegated = false)
        {
            return Add(property, FilterOperator.GreaterThanOrEqual, value, isNegated);
        }

        /// <summary>
        /// 建立 GreaterThanOrEqual 條件的快捷方法 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> GreaterThanOrEqual<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.GreaterThanOrEqual, value, isNegated);
        }

        /// <summary>
        /// 建立 LessThanOrEqual 條件的快捷方法
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> LessThanOrEqual(string property, object value, bool isNegated = false)
        {
            return Add(property, FilterOperator.LessThanOrEqual, value, isNegated);
        }

        /// <summary>
        /// 建立 LessThanOrEqual 條件的快捷方法 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> LessThanOrEqual<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.LessThanOrEqual, value, isNegated);
        }

        /// <summary>
        /// 建立 StartsWith 條件的快捷方法
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> StartsWith(string property, object value, bool isNegated = false)
        {
            return Add(property, FilterOperator.StartsWith, value, isNegated);
        }

        /// <summary>
        /// 建立 StartsWith 條件的快捷方法 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> StartsWith<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.StartsWith, value, isNegated);
        }

        /// <summary>
        /// 建立 EndsWith 條件的快捷方法
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> EndsWith(string property, object value, bool isNegated = false)
        {
            return Add(property, FilterOperator.EndsWith, value, isNegated);
        }

        /// <summary>
        /// 建立 EndsWith 條件的快捷方法 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> EndsWith<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.EndsWith, value, isNegated);
        }

        /// <summary>
        /// 建立 NotEqual 條件的快捷方法
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> NotEqual(string property, object value, bool isNegated = false)
        {
            return Add(property, FilterOperator.NotEqual, value, isNegated);
        }

        /// <summary>
        /// 建立 NotEqual 條件的快捷方法 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> NotEqual<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.NotEqual, value, isNegated);
        }

        /// <summary>
        /// 建立 NotContains 條件的快捷方法
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> NotContains(string property, object value, bool isNegated = false)
        {
            return Add(property, FilterOperator.NotContains, value, isNegated);
        }

        /// <summary>
        /// 建立 NotContains 條件的快捷方法 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> NotContains<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.NotContains, value, isNegated);
        }

        /// <summary>
        /// 建立 NotIn 條件的快捷方法
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <param name="values">值集合</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> NotIn(string property, IEnumerable<object> values, bool isNegated = false)
        {
            return Add(property, FilterOperator.NotIn, values, isNegated);
        }

        /// <summary>
        /// 建立 NotIn 條件的快捷方法 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <param name="values">值集合</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> NotIn<TProperty>(Expression<Func<T, TProperty>> propertyExpression, IEnumerable<object> values, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.NotIn, values, isNegated);
        }

        /// <summary>
        /// 建立 NotLike 條件的快捷方法
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> NotLike(string property, object value, bool isNegated = false)
        {
            return Add(property, FilterOperator.NotLike, value, isNegated);
        }

        /// <summary>
        /// 建立 NotLike 條件的快捷方法 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> NotLike<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object value, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.NotLike, value, isNegated);
        }

        /// <summary>
        /// 建立 NotBetween 條件的快捷方法
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> NotBetween(string property, object min, object max, bool isNegated = false)
        {
            return Add(property, FilterOperator.NotBetween, new[] { min, max }, isNegated);
        }

        /// <summary>
        /// 建立 NotBetween 條件的快捷方法 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> NotBetween<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object min, object max, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.NotBetween, new[] { min, max }, isNegated);
        }

        /// <summary>
        /// 添加陣列導覽屬性的查詢條件 (Expression 版本)
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <param name="operator">過濾運算子</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> AddCustomArrayNavigation<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression,
            FilterOperator @operator,
            object value,
            bool isNegated = false)
        {
            // 提取集合屬性名稱
            var collectionName = GetPropertyName(collectionExpression);
            
            // 提取目標屬性名稱
            string targetPropertyName;
            if (propertyExpression.Body is MemberExpression memberExpr)
            {
                targetPropertyName = GetMemberPath(memberExpr);
            }
            else if (propertyExpression.Body is UnaryExpression unaryExpr && 
                     unaryExpr.Operand is MemberExpression memberOperand)
            {
                targetPropertyName = GetMemberPath(memberOperand);
            }
            else
            {
                throw new ArgumentException("Invalid property expression", nameof(propertyExpression));
            }
            
            // 構造陣列導覽語法
            var navigationProperty = $"{collectionName}[].{targetPropertyName}";
            
            return Add(navigationProperty, @operator, value, isNegated);
        }

        /// <summary>
        /// 建立陣列導覽 Equal 條件的快捷方法
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> ArrayEqual<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression,
            object value,
            bool isNegated = false)
        {
            return AddCustomArrayNavigation(collectionExpression, propertyExpression, FilterOperator.Equal, value, isNegated);
        }

        /// <summary>
        /// 建立陣列導覽 In 條件的快捷方法
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <param name="values">值集合</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> ArrayIn<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression,
            IEnumerable<object> values,
            bool isNegated = false)
        {
            return AddCustomArrayNavigation(collectionExpression, propertyExpression, FilterOperator.In, values, isNegated);
        }

        /// <summary>
        /// 建立陣列導覽 GreaterThan 條件的快捷方法
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> ArrayGreaterThan<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression,
            object value,
            bool isNegated = false)
        {
            return AddCustomArrayNavigation(collectionExpression, propertyExpression, FilterOperator.GreaterThan, value, isNegated);
        }

        /// <summary>
        /// 建立陣列導覽 Between 條件的快捷方法
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> ArrayBetween<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression,
            object min,
            object max,
            bool isNegated = false)
        {
            return AddCustomArrayNavigation(collectionExpression, propertyExpression, FilterOperator.Between, new[] { min, max }, isNegated);
        }

        /// <summary>
        /// 建立陣列導覽 Like 條件的快捷方法
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> ArrayLike<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression,
            object value,
            bool isNegated = false)
        {
            return AddCustomArrayNavigation(collectionExpression, propertyExpression, FilterOperator.Like, value, isNegated);
        }

        /// <summary>
        /// 建立陣列導覽 NotLike 條件的快捷方法
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> ArrayNotLike<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression,
            object value,
            bool isNegated = false)
        {
            return AddCustomArrayNavigation(collectionExpression, propertyExpression, FilterOperator.NotLike, value, isNegated);
        }

        /// <summary>
        /// 建立陣列導覽 Contains 條件的快捷方法
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> ArrayContains<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression,
            object value,
            bool isNegated = false)
        {
            return AddCustomArrayNavigation(collectionExpression, propertyExpression, FilterOperator.Contains, value, isNegated);
        }

        /// <summary>
        /// 建立陣列導覽 NotContains 條件的快捷方法
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> ArrayNotContains<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression,
            object value,
            bool isNegated = false)
        {
            return AddCustomArrayNavigation(collectionExpression, propertyExpression, FilterOperator.NotContains, value, isNegated);
        }

        /// <summary>
        /// 建立陣列導覽 StartsWith 條件的快捷方法
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> ArrayStartsWith<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression,
            object value,
            bool isNegated = false)
        {
            return AddCustomArrayNavigation(collectionExpression, propertyExpression, FilterOperator.StartsWith, value, isNegated);
        }

        /// <summary>
        /// 建立陣列導覽 EndsWith 條件的快捷方法
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> ArrayEndsWith<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression,
            object value,
            bool isNegated = false)
        {
            return AddCustomArrayNavigation(collectionExpression, propertyExpression, FilterOperator.EndsWith, value, isNegated);
        }

        /// <summary>
        /// 建立陣列導覽 NotEqual 條件的快捷方法
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> ArrayNotEqual<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression,
            object value,
            bool isNegated = false)
        {
            return AddCustomArrayNavigation(collectionExpression, propertyExpression, FilterOperator.NotEqual, value, isNegated);
        }

        /// <summary>
        /// 建立陣列導覽 NotIn 條件的快捷方法
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <param name="values">值集合</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> ArrayNotIn<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression,
            IEnumerable<object> values,
            bool isNegated = false)
        {
            return AddCustomArrayNavigation(collectionExpression, propertyExpression, FilterOperator.NotIn, values, isNegated);
        }

        /// <summary>
        /// 建立陣列導覽 GreaterThanOrEqual 條件的快捷方法
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> ArrayGreaterThanOrEqual<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression,
            object value,
            bool isNegated = false)
        {
            return AddCustomArrayNavigation(collectionExpression, propertyExpression, FilterOperator.GreaterThanOrEqual, value, isNegated);
        }

        /// <summary>
        /// 建立陣列導覽 LessThan 條件的快捷方法
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> ArrayLessThan<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression,
            object value,
            bool isNegated = false)
        {
            return AddCustomArrayNavigation(collectionExpression, propertyExpression, FilterOperator.LessThan, value, isNegated);
        }

        /// <summary>
        /// 建立陣列導覽 LessThanOrEqual 條件的快捷方法
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <param name="value">比較值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> ArrayLessThanOrEqual<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression,
            object value,
            bool isNegated = false)
        {
            return AddCustomArrayNavigation(collectionExpression, propertyExpression, FilterOperator.LessThanOrEqual, value, isNegated);
        }

        /// <summary>
        /// 建立陣列導覽 NotBetween 條件的快捷方法
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> ArrayNotBetween<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression,
            object min,
            object max,
            bool isNegated = false)
        {
            return AddCustomArrayNavigation(collectionExpression, propertyExpression, FilterOperator.NotBetween, new[] { min, max }, isNegated);
        }

        /// <summary>
        /// 建立陣列導覽 Any 條件的快捷方法
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <param name="value">比較值（可為 null，表示檢查集合是否有任何元素）</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> ArrayAny<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression,
            object? value = null,
            bool isNegated = false)
        {
            return AddCustomArrayNavigation(collectionExpression, propertyExpression, FilterOperator.Any, value, isNegated);
        }

        /// <summary>
        /// 建立陣列導覽 NotAny 條件的快捷方法
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <param name="value">比較值（可為 null，表示檢查集合是否沒有任何元素）</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> ArrayNotAny<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression,
            object? value = null,
            bool isNegated = false)
        {
            return AddCustomArrayNavigation(collectionExpression, propertyExpression, FilterOperator.NotAny, value, isNegated);
        }

        /// <summary>
        /// 建立 Any 條件的快捷方法</summary>
        /// <param name="property">屬性名稱</param>
        /// <param name="value">比較值（可為 null，表示檢查集合是否有任何元素）</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> Any(string property, object? value = null, bool isNegated = false)
        {
            return Add(property, FilterOperator.Any, value, isNegated);
        }

        /// <summary>
        /// 建立 Any 條件的快捷方法 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <param name="value">比較值（可為 null，表示檢查集合是否有任何元素）</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> Any<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object? value = null, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.Any, value, isNegated);
        }

        /// <summary>
        /// 建立 NotAny 條件的快捷方法
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <param name="value">比較值（可為 null，表示檢查集合是否沒有任何元素）</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> NotAny(string property, object? value = null, bool isNegated = false)
        {
            return Add(property, FilterOperator.NotAny, value, isNegated);
        }

        /// <summary>
        /// 建立 NotAny 條件的快捷方法 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <param name="value">比較值（可為 null，表示檢查集合是否沒有任何元素）</param>
        /// <param name="isNegated">是否否定</param>
        /// <returns>Builder 實例</returns>
        public FilterDictionaryBuilder<T> NotAny<TProperty>(Expression<Func<T, TProperty>> propertyExpression, object? value = null, bool isNegated = false)
        {
            return Add(propertyExpression, FilterOperator.NotAny, value, isNegated);
        }

        /// <summary>
        /// 建構最終的字典結果
        /// </summary>
        /// <returns>過濾條件字典</returns>
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
        /// 建構最終的字典結果並轉換為 FilterGroup
        /// </summary>
        /// <returns>FilterGroup 實例</returns>
        public FilterGroup ToFilterGroup()
        {
            var dict = Build();
            return FilterGroupFactory.FromDictionary(dict);
        }

        /// <summary>
        /// 隱式轉換為字典
        /// </summary>
        /// <param name="builder">Builder 實例</param>
        public static implicit operator Dictionary<string, object>(FilterDictionaryBuilder<T> builder)
        {
            return builder.Build();
        }

        /// <summary>
        /// 隱式轉換為 FilterGroup
        /// </summary>
        /// <param name="builder">Builder 實例</param>
        public static implicit operator FilterGroup(FilterDictionaryBuilder<T> builder)
        {
            return builder.ToFilterGroup();
        }
    }

    /// <summary>
    /// 靜態工廠類別，提供簡化的建立方法
    /// </summary>
    public static class FilterDictionaryBuilder
    {
        /// <summary>
        /// 建立新的 QueryBuilder 實例
        /// </summary>
        /// <typeparam name="T">目標實體類型</typeparam>
        /// <returns>FilterDictionaryBuilder 實例</returns>
        public static FilterDictionaryBuilder<T> QueryBuilder<T>()
        {
            return new FilterDictionaryBuilder<T>();
        }

        /// <summary>
        /// 建立新的 QueryBuilder 實例
        /// </summary>
        /// <typeparam name="T">目標實體類型</typeparam>
        /// <returns>FilterDictionaryBuilder 實例</returns>
        public static FilterDictionaryBuilder<T> Create<T>()
        {
            return new FilterDictionaryBuilder<T>();
        }
    }
}