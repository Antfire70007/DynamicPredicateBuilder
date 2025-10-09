using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using DynamicPredicate;
using DynamicPredicateBuilder.Models;
using DynamicPredicateBuilder;

namespace DynamicPredicate.Tests.Builders
{
    public class FilterBuilderTests
    {
        [Fact]
        public void BuildPredicate_WithNotAndNestedGroups_ShouldRespectPrecedence()
        {
            var groups = new List<FilterGroup>
    {
        new()
        {
            LogicalOperator = LogicalOperator.And,
            InterOperator   = LogicalOperator.Or,
            Rules =
            [
                new FilterRule { Property = "Name", Operator = FilterOperator.Equal, Value = "Snake", IsNegated = true }
            ]
        },
        new()
        {
            IsNegated       = true,
            LogicalOperator = LogicalOperator.And,
            Rules =
            [
                new FilterRule { Property = "Status", Operator = FilterOperator.Equal, Value = "Retired" }
            ]
        }
    };

            var fn = FilterBuilder.Build<User>(groups).Compile();

            fn(new User { Name = "Snake", Status = "Active" }).Should().BeFalse();  // !Snake && !Retired �� false
            fn(new User { Name = "Boss", Status = "Active" }).Should().BeTrue();   //  true  ||  true
        }

        [Fact]
        public void BuildPredicate_WithMultipleGroups_ShouldCombineCorrectly()
        {
            var groups = new List<FilterGroup>
    {
        new FilterGroup
        {
            LogicalOperator = LogicalOperator.And,
            InterOperator = LogicalOperator.Or,
            Rules =
            [
                new FilterRule { Property = "Name", Operator = FilterOperator.Equal, Value = "Snake" }
            ]
        },
        new FilterGroup
        {
            LogicalOperator = LogicalOperator.And,
            Rules =
            [
                new FilterRule { Property = "Age", Operator = FilterOperator.GreaterThan, Value = 40 }
            ]
        }
    };

            var predicate = FilterBuilder.Build<User>(groups).Compile();

            predicate(new User { Name = "Snake", Age = 20 }).Should().BeTrue();   // Group1 ����
            predicate(new User { Name = "Otacon", Age = 50 }).Should().BeTrue(); // Group2 ����
            predicate(new User { Name = "Otacon", Age = 30 }).Should().BeFalse();
        }

        [Fact]
        public void BuildPredicate_EqualOperator_ShouldReturnTrueForEqualValue()
        {
            var groups = new List<FilterGroup>
    {
        new FilterGroup
        {
            LogicalOperator = LogicalOperator.And,
            Rules =
            [
                new FilterRule { Property = "Name", Operator = FilterOperator.Equal, Value = "Snake" }
            ]
        }
    };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            predicate(new User { Name = "Snake" }).Should().BeTrue();
            predicate(new User { Name = "Boss" }).Should().BeFalse();
        }
        [Fact]
        public void BuildPredicate_NotEqualOperator_ShouldReturnTrueForDifferentValue()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Name", Operator = FilterOperator.NotEqual, Value = "Snake" }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            predicate(new User { Name = "Boss" }).Should().BeTrue();
            predicate(new User { Name = "Snake" }).Should().BeFalse();
        }

        [Fact]
        public void BuildPredicate_GreaterThanOperator_ShouldReturnTrueForGreaterValue()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Age", Operator = FilterOperator.GreaterThan, Value = 30 }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            predicate(new User { Age = 40 }).Should().BeTrue();
            predicate(new User { Age = 20 }).Should().BeFalse();
        }

        #region Nullable Decimal Tests

        [Fact]
        public void BuildPredicate_NullableDecimal_EqualOperator_WithValue_ShouldWork()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.Equal, Value = 50000.50m }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            
            predicate(new User { Salary = 50000.50m }).Should().BeTrue();
            predicate(new User { Salary = 60000.00m }).Should().BeFalse();
            predicate(new User { Salary = null }).Should().BeFalse();
        }

        [Fact]
        public void BuildPredicate_NullableDecimal_EqualOperator_WithNull_ShouldWork()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.Equal, Value = null }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            
            predicate(new User { Salary = null }).Should().BeTrue();
            predicate(new User { Salary = 50000.50m }).Should().BeFalse();
        }

        [Fact]
        public void BuildPredicate_NullableDecimal_NotEqualOperator_WithValue_ShouldWork()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.NotEqual, Value = 50000.50m }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            
            predicate(new User { Salary = 60000.00m }).Should().BeTrue();
            predicate(new User { Salary = null }).Should().BeTrue();
            predicate(new User { Salary = 50000.50m }).Should().BeFalse();
        }

        [Fact]
        public void BuildPredicate_NullableDecimal_NotEqualOperator_WithNull_ShouldWork()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.NotEqual, Value = null }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            
            predicate(new User { Salary = 50000.50m }).Should().BeTrue();
            predicate(new User { Salary = 60000.00m }).Should().BeTrue();
            predicate(new User { Salary = null }).Should().BeFalse();
        }

        [Fact]
        public void BuildPredicate_NullableDecimal_GreaterThanOperator_ShouldWork()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.GreaterThan, Value = 50000m }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            
            predicate(new User { Salary = 60000.00m }).Should().BeTrue();
            predicate(new User { Salary = 40000.00m }).Should().BeFalse();
            predicate(new User { Salary = null }).Should().BeFalse(); // null 不滿足 > 條件
        }

        [Fact]
        public void BuildPredicate_NullableDecimal_LessThanOperator_ShouldWork()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.LessThan, Value = 50000m }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            
            predicate(new User { Salary = 40000.00m }).Should().BeTrue();
            predicate(new User { Salary = 60000.00m }).Should().BeFalse();
            predicate(new User { Salary = null }).Should().BeFalse(); // null 不滿足 < 條件
        }

        [Fact]
        public void BuildPredicate_NullableDecimal_GreaterThanOrEqualOperator_ShouldWork()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.GreaterThanOrEqual, Value = 50000m }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            
            predicate(new User { Salary = 50000.00m }).Should().BeTrue();
            predicate(new User { Salary = 60000.00m }).Should().BeTrue();
            predicate(new User { Salary = 40000.00m }).Should().BeFalse();
            predicate(new User { Salary = null }).Should().BeFalse();
        }

        [Fact]
        public void BuildPredicate_NullableDecimal_LessThanOrEqualOperator_ShouldWork()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.LessThanOrEqual, Value = 50000m }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            
            predicate(new User { Salary = 50000.00m }).Should().BeTrue();
            predicate(new User { Salary = 40000.00m }).Should().BeTrue();
            predicate(new User { Salary = 60000.00m }).Should().BeFalse();
            predicate(new User { Salary = null }).Should().BeFalse();
        }

        [Fact]
        public void BuildPredicate_NullableDecimal_BetweenOperator_ShouldWork()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.Between, Value = new[] { 40000m, 60000m } }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            
            predicate(new User { Salary = 50000.00m }).Should().BeTrue();
            predicate(new User { Salary = 40000.00m }).Should().BeTrue(); // 邊界值包含
            predicate(new User { Salary = 60000.00m }).Should().BeTrue(); // 邊界值包含
            predicate(new User { Salary = 30000.00m }).Should().BeFalse();
            predicate(new User { Salary = 70000.00m }).Should().BeFalse();
            predicate(new User { Salary = null }).Should().BeFalse();
        }

        [Fact]
        public void BuildPredicate_NullableDecimal_InOperator_ShouldWork()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.In, Value = new[] { 40000m, 50000m, 60000m } }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            
            predicate(new User { Salary = 50000.00m }).Should().BeTrue();
            predicate(new User { Salary = 40000.00m }).Should().BeTrue();
            predicate(new User { Salary = 60000.00m }).Should().BeTrue();
            predicate(new User { Salary = 35000.00m }).Should().BeFalse();
            predicate(new User { Salary = null }).Should().BeFalse();
        }

        [Fact]
        public void BuildPredicate_NullableDecimal_InOperator_WithNull_ShouldWork()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.In, Value = new object[] { 40000m, 50000m, null } }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            
            predicate(new User { Salary = 50000.00m }).Should().BeTrue();
            predicate(new User { Salary = 40000.00m }).Should().BeTrue();
            predicate(new User { Salary = null }).Should().BeTrue();
            predicate(new User { Salary = 35000.00m }).Should().BeFalse();
        }

        [Fact]
        public void BuildPredicate_NullableDecimal_ComplexConditions_ShouldWork()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.Or,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.Equal, Value = null },
                        new FilterRule { Property = "Salary", Operator = FilterOperator.GreaterThan, Value = 80000m }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            
            predicate(new User { Salary = null }).Should().BeTrue(); // 沒有薪資
            predicate(new User { Salary = 90000.00m }).Should().BeTrue(); // 高薪
            predicate(new User { Salary = 50000.00m }).Should().BeFalse(); // 中等薪資
        }

        [Fact]
        public void BuildPredicate_NullableDecimal_WithNegation_ShouldWork()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.Between, Value = new[] { 40000m, 60000m }, IsNegated = true }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            
            predicate(new User { Salary = 30000.00m }).Should().BeTrue(); // 不在範圍內
            predicate(new User { Salary = 70000.00m }).Should().BeTrue(); // 不在範圍內
            predicate(new User { Salary = null }).Should().BeTrue(); // null 不在範圍內
            predicate(new User { Salary = 50000.00m }).Should().BeFalse(); // 在範圍內
        }

        [Fact]
        public void BuildPredicate_NullableDecimal_MultipleGroups_ShouldWork()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    InterOperator = LogicalOperator.Or,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.GreaterThan, Value = 50000m }
                    ]
                },
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Age", Operator = FilterOperator.LessThan, Value = 40 }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            
            predicate(new User { Salary = 60000.00m, Age = 35 }).Should().BeTrue(); // 兩個條件都滿足
            predicate(new User { Salary = 60000.00m, Age = 45 }).Should().BeTrue(); // Group1 滿足
            predicate(new User { Salary = 40000.00m, Age = 35 }).Should().BeTrue(); // Group2 滿足
            predicate(new User { Salary = 40000.00m, Age = 45 }).Should().BeFalse(); // 都不滿足
            predicate(new User { Salary = null, Age = 45 }).Should().BeFalse(); // 都不滿足
        }

        #endregion

        #region Any and NotAny Operator Tests

        [Fact]
        public void BuildPredicate_AnyOperator_WithoutValue_ShouldCheckIfCollectionHasAnyElements()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Tags", Operator = FilterOperator.Any, Value = null }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            
            predicate(new User { Tags = new List<string> { "VIP", "Premium" } }).Should().BeTrue(); // 有元素
            predicate(new User { Tags = new List<string>() }).Should().BeFalse(); // 空集合
            predicate(new User { Tags = null }).Should().BeFalse(); // null 集合
        }

        [Fact]
        public void BuildPredicate_AnyOperator_WithValue_ShouldCheckIfCollectionContainsValue()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Tags", Operator = FilterOperator.Any, Value = "VIP" }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            
            predicate(new User { Tags = new List<string> { "VIP", "Premium" } }).Should().BeTrue(); // 包含 VIP
            predicate(new User { Tags = new List<string> { "Premium", "Standard" } }).Should().BeFalse(); // 不包含 VIP
            predicate(new User { Tags = new List<string>() }).Should().BeFalse(); // 空集合
            predicate(new User { Tags = null }).Should().BeFalse(); // null 集合
        }

        [Fact]
        public void BuildPredicate_NotAnyOperator_WithoutValue_ShouldCheckIfCollectionIsEmpty()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Tags", Operator = FilterOperator.NotAny, Value = null }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            
            predicate(new User { Tags = new List<string>() }).Should().BeTrue(); // 空集合
            predicate(new User { Tags = null }).Should().BeTrue(); // null 集合
            predicate(new User { Tags = new List<string> { "VIP" } }).Should().BeFalse(); // 有元素
        }

        [Fact]
        public void BuildPredicate_NotAnyOperator_WithValue_ShouldCheckIfCollectionDoesNotContainValue()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Tags", Operator = FilterOperator.NotAny, Value = "Banned" }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            
            predicate(new User { Tags = new List<string> { "VIP", "Premium" } }).Should().BeTrue(); // 不包含 Banned
            predicate(new User { Tags = new List<string>() }).Should().BeTrue(); // 空集合
            predicate(new User { Tags = null }).Should().BeTrue(); // null 集合
            predicate(new User { Tags = new List<string> { "Banned", "VIP" } }).Should().BeFalse(); // 包含 Banned
        }

        [Fact]
        public void BuildPredicate_AnyOperator_WithNegation_ShouldWork()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Tags", Operator = FilterOperator.Any, Value = "VIP", IsNegated = true }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            
            predicate(new User { Tags = new List<string> { "Premium", "Standard" } }).Should().BeTrue(); // 不包含 VIP
            predicate(new User { Tags = new List<string>() }).Should().BeTrue(); // 空集合
            predicate(new User { Tags = null }).Should().BeTrue(); // null 集合
            predicate(new User { Tags = new List<string> { "VIP", "Premium" } }).Should().BeFalse(); // 包含 VIP
        }

        [Fact]
        public void BuildPredicate_AnyOperator_WithNumericCollection_ShouldWork()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Numbers", Operator = FilterOperator.Any, Value = 42 }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            
            predicate(new User { Numbers = new List<int> { 1, 42, 100 } }).Should().BeTrue(); // 包含 42
            predicate(new User { Numbers = new List<int> { 1, 2, 3 } }).Should().BeFalse(); // 不包含 42
            predicate(new User { Numbers = new List<int>() }).Should().BeFalse(); // 空集合
        }

        [Fact]
        public void BuildPredicate_ComplexAnyNotAnyConditions_ShouldWork()
        {
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Tags", Operator = FilterOperator.Any, Value = "Premium" },
                        new FilterRule { Property = "Tags", Operator = FilterOperator.NotAny, Value = "Banned" }
                    ]
                }
            };
            var predicate = FilterBuilder.Build<User>(groups).Compile();
            
            predicate(new User { Tags = new List<string> { "Premium", "VIP" } }).Should().BeTrue(); // 有Premium且沒有Banned
            predicate(new User { Tags = new List<string> { "Premium", "Banned" } }).Should().BeFalse(); // 有Premium但也有Banned
            predicate(new User { Tags = new List<string> { "VIP", "Standard" } }).Should().BeFalse(); // 沒有Premium
        }

        [Fact]
        public void BuildPredicate_AnyNotAny_NonCollectionProperty_ShouldReturnCorrectDefault()
        {
            // 測試非集合屬性使用 Any/NotAny 時的預設行為
            var groups1 = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Name", Operator = FilterOperator.Any, Value = null }
                    ]
                }
            };
            var predicate1 = FilterBuilder.Build<User>(groups1).Compile();
            
            predicate1(new User { Name = "John" }).Should().BeFalse(); // 非集合屬性的 Any 應該回傳 false

            var groups2 = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Name", Operator = FilterOperator.NotAny, Value = null }
                    ]
                }
            };
            var predicate2 = FilterBuilder.Build<User>(groups2).Compile();
            
            predicate2(new User { Name = "John" }).Should().BeTrue(); // 非集合屬性的 NotAny 應該回傳 true
        }

        #endregion

        // 其餘 Operator 依此類推...
    }
}
