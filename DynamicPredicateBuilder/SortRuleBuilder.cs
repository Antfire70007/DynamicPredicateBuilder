using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DynamicPredicateBuilder.Models;

namespace DynamicPredicateBuilder
{
    /// <summary>
    /// 支援 Fluent API 的排序規則建構器
    /// </summary>
    /// <typeparam name="T">目標實體類型</typeparam>
    public class SortRuleBuilder<T>
    {
        private readonly List<SortRule> _sortRules = new();

        /// <summary>
        /// 建立新的 SortBuilder 實例
        /// </summary>
        /// <typeparam name="TEntity">目標實體類型</typeparam>
        /// <returns>SortRuleBuilder 實例</returns>
        public static SortRuleBuilder<TEntity> SortBuilder<TEntity>()
        {
            return new SortRuleBuilder<TEntity>();
        }

        /// <summary>
        /// 建立新的 SortBuilder 實例 (泛型推斷版本)
        /// </summary>
        /// <returns>SortRuleBuilder 實例</returns>
        public static SortRuleBuilder<T> Create()
        {
            return new SortRuleBuilder<T>();
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
        /// 添加排序規則
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <param name="descending">是否降序排序</param>
        /// <returns>Builder 實例</returns>
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
        /// 添加排序規則 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <param name="descending">是否降序排序</param>
        /// <returns>Builder 實例</returns>
        public SortRuleBuilder<T> Add<TProperty>(Expression<Func<T, TProperty>> propertyExpression, bool descending = false)
        {
            var propertyName = GetPropertyName(propertyExpression);
            return Add(propertyName, descending);
        }

        /// <summary>
        /// 添加升序排序規則
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <returns>Builder 實例</returns>
        public SortRuleBuilder<T> Ascending(string property)
        {
            return Add(property, false);
        }

        /// <summary>
        /// 添加升序排序規則 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <returns>Builder 實例</returns>
        public SortRuleBuilder<T> Ascending<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            return Add(propertyExpression, false);
        }

        /// <summary>
        /// 添加降序排序規則
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <returns>Builder 實例</returns>
        public SortRuleBuilder<T> Descending(string property)
        {
            return Add(property, true);
        }

        /// <summary>
        /// 添加降序排序規則 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <returns>Builder 實例</returns>
        public SortRuleBuilder<T> Descending<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            return Add(propertyExpression, true);
        }

        /// <summary>
        /// 添加 ThenBy 排序規則 (與前一個排序規則相同情況下的次要排序)
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <returns>Builder 實例</returns>
        public SortRuleBuilder<T> ThenBy(string property)
        {
            return Add(property, false);
        }

        /// <summary>
        /// 添加 ThenBy 排序規則 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <returns>Builder 實例</returns>
        public SortRuleBuilder<T> ThenBy<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            return Add(propertyExpression, false);
        }

        /// <summary>
        /// 添加 ThenByDescending 排序規則
        /// </summary>
        /// <param name="property">屬性名稱</param>
        /// <returns>Builder 實例</returns>
        public SortRuleBuilder<T> ThenByDescending(string property)
        {
            return Add(property, true);
        }

        /// <summary>
        /// 添加 ThenByDescending 排序規則 (Expression 版本)
        /// </summary>
        /// <typeparam name="TProperty">屬性類型</typeparam>
        /// <param name="propertyExpression">屬性表達式</param>
        /// <returns>Builder 實例</returns>
        public SortRuleBuilder<T> ThenByDescending<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            return Add(propertyExpression, true);
        }

        /// <summary>
        /// 添加陣列導覽屬性排序規則（通用方法）
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <param name="descending">是否降序排序</param>
        /// <returns>Builder 實例</returns>
        public SortRuleBuilder<T> AddArrayNavigation<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression,
            bool descending = false)
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
            
            // 構造陣列導覽語法：CollectionName[].PropertyName
            var navigationProperty = $"{collectionName}[].{targetPropertyName}";
            
            return Add(navigationProperty, descending);
        }

        /// <summary>
        /// 添加陣列導覽屬性升序排序規則
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <returns>Builder 實例</returns>
        public SortRuleBuilder<T> ArrayAscending<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression)
        {
            return AddArrayNavigation(collectionExpression, propertyExpression, false);
        }

        /// <summary>
        /// 添加陣列導覽屬性降序排序規則
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <returns>Builder 實例</returns>
        public SortRuleBuilder<T> ArrayDescending<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression)
        {
            return AddArrayNavigation(collectionExpression, propertyExpression, true);
        }

        /// <summary>
        /// 添加陣列導覽屬性 ThenBy 排序規則
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <returns>Builder 實例</returns>
        public SortRuleBuilder<T> ArrayThenBy<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression)
        {
            return AddArrayNavigation(collectionExpression, propertyExpression, false);
        }

        /// <summary>
        /// 添加陣列導覽屬性 ThenByDescending 排序規則
        /// </summary>
        /// <typeparam name="TCollection">集合類型</typeparam>
        /// <typeparam name="TProperty">目標屬性類型</typeparam>
        /// <param name="collectionExpression">集合屬性表達式</param>
        /// <param name="propertyExpression">目標屬性表達式</param>
        /// <returns>Builder 實例</returns>
        public SortRuleBuilder<T> ArrayThenByDescending<TCollection, TProperty>(
            Expression<Func<T, IEnumerable<TCollection>>> collectionExpression,
            Expression<Func<TCollection, TProperty>> propertyExpression)
        {
            return AddArrayNavigation(collectionExpression, propertyExpression, true);
        }

        /// <summary>
        /// 建立排序規則清單
        /// </summary>
        /// <returns>排序規則清單</returns>
        public List<SortRule> Build()
        {
            return _sortRules.ToList();
        }

        /// <summary>
        /// 隱式轉換為排序規則清單
        /// </summary>
        /// <param name="builder">Builder 實例</param>
        public static implicit operator List<SortRule>(SortRuleBuilder<T> builder)
        {
            return builder.Build();
        }

        /// <summary>
        /// 隱式轉換為排序規則陣列
        /// </summary>
        /// <param name="builder">Builder 實例</param>
        public static implicit operator SortRule[](SortRuleBuilder<T> builder)
        {
            return builder.Build().ToArray();
        }
    }

    /// <summary>
    /// 靜態工廠類別，提供簡化的建立方法
    /// </summary>
    public static class SortRuleBuilder
    {
        /// <summary>
        /// 建立新的 SortBuilder 實例
        /// </summary>
        /// <typeparam name="T">目標實體類型</typeparam>
        /// <returns>SortRuleBuilder 實例</returns>
        public static SortRuleBuilder<T> SortBuilder<T>()
        {
            return new SortRuleBuilder<T>();
        }

        /// <summary>
        /// 建立新的 SortBuilder 實例
        /// </summary>
        /// <typeparam name="T">目標實體類型</typeparam>
        /// <returns>SortRuleBuilder 實例</returns>
        public static SortRuleBuilder<T> Create<T>()
        {
            return new SortRuleBuilder<T>();
        }
    }
}