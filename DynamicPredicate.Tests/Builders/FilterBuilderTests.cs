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

            fn(new User { Name = "Snake", Status = "Active" }).Should().BeFalse();  // !Snake && !Retired → false
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

            predicate(new User { Name = "Snake", Age = 20 }).Should().BeTrue();   // Group1 成立
            predicate(new User { Name = "Otacon", Age = 50 }).Should().BeTrue(); // Group2 成立
            predicate(new User { Name = "Otacon", Age = 30 }).Should().BeFalse();
        }
    }
}
