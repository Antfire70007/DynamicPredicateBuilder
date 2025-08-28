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

        // 其餘 Operator 依此類推...
    }
}
