using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using DynamicPredicateBuilder;
using DynamicPredicateBuilder.Models;
using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;

namespace DynamicPredicate.Tests.Builders
{
    /// <summary>
    /// FilterDictionaryBuilder 表達式解析專用測試
    /// </summary>
    public class FilterDictionaryBuilderExpressionTests
    {
        private readonly ITestOutputHelper _output;

        public FilterDictionaryBuilderExpressionTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // 測試用的複雜實體類別
        public class ComplexEntity
        {
            public string Name { get; set; }
            public NestedEntity Nested { get; set; }
            public List<string> Tags { get; set; } = new();
            public int? NullableInt { get; set; }
            public bool? NullableBool { get; set; }
            public DateTime CreatedDate { get; set; }
        }

        public class NestedEntity
        {
            public string SubName { get; set; }
            public DeepNestedEntity Deep { get; set; }
            public List<NestedCollection> Collections { get; set; } = new();
        }

        public class DeepNestedEntity
        {
            public string DeepName { get; set; }
            public int DeepValue { get; set; }
        }

        public class NestedCollection
        {
            public string ItemName { get; set; }
            public decimal ItemValue { get; set; }
        }

        [Fact]
        public void SimplePropertyExpression_ShouldExtractCorrectPath()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<ComplexEntity>()
                .Equal(x => x.Name, "test")
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            var rule = (Dictionary<string, object>)rules[0];
            Assert.Equal(nameof(ComplexEntity.Name), rule["Property"]);

            _output.WriteLine($"Simple Property Expression: {rule["Property"]}");
        }

        [Fact]
        public void NestedPropertyExpression_ShouldExtractCorrectPath()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<ComplexEntity>()
                .Equal(x => x.Nested.SubName, "test")
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            var rule = (Dictionary<string, object>)rules[0];
            Assert.Equal("Nested.SubName", rule["Property"]);

            _output.WriteLine($"Nested Property Expression: {rule["Property"]}");
        }

        [Fact]
        public void DeepNestedPropertyExpression_ShouldExtractCorrectPath()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<ComplexEntity>()
                .Equal(x => x.Nested.Deep.DeepName, "test")
                .GreaterThan(x => x.Nested.Deep.DeepValue, 100)
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            
            var rule1 = (Dictionary<string, object>)rules[0];
            Assert.Equal("Nested.Deep.DeepName", rule1["Property"]);
            
            var rule2 = (Dictionary<string, object>)rules[1];
            Assert.Equal("Nested.Deep.DeepValue", rule2["Property"]);

            _output.WriteLine($"Deep Nested Property Expression 1: {rule1["Property"]}");
            _output.WriteLine($"Deep Nested Property Expression 2: {rule2["Property"]}");
        }

        [Fact]
        public void NullablePropertyExpression_ShouldExtractCorrectPath()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<ComplexEntity>()
                .Equal(x => x.NullableInt, 42)
                .Equal(x => x.NullableBool, true)
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            
            var rule1 = (Dictionary<string, object>)rules[0];
            Assert.Equal(nameof(ComplexEntity.NullableInt), rule1["Property"]);
            
            var rule2 = (Dictionary<string, object>)rules[1];
            Assert.Equal(nameof(ComplexEntity.NullableBool), rule2["Property"]);

            _output.WriteLine($"Nullable Property Expression 1: {rule1["Property"]}");
            _output.WriteLine($"Nullable Property Expression 2: {rule2["Property"]}");
        }

        [Fact]
        public void DateTimePropertyExpression_ShouldExtractCorrectPath()
        {
            // Arrange & Act
            var testDate = DateTime.Now;
            var result = FilterDictionaryBuilder.QueryBuilder<ComplexEntity>()
                .GreaterThan(x => x.CreatedDate, testDate)
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            var rule = (Dictionary<string, object>)rules[0];
            Assert.Equal(nameof(ComplexEntity.CreatedDate), rule["Property"]);
            Assert.Equal(testDate, rule["Value"]);

            _output.WriteLine($"DateTime Property Expression: {rule["Property"]} = {rule["Value"]}");
        }

        [Fact]
        public void CollectionPropertyExpression_ShouldExtractCorrectPath()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<ComplexEntity>()
                .Any(x => x.Tags)
                .Any(x => x.Tags, "Premium")
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            
            var rule1 = (Dictionary<string, object>)rules[0];
            Assert.Equal(nameof(ComplexEntity.Tags), rule1["Property"]);
            Assert.Null(rule1["Value"]);
            
            var rule2 = (Dictionary<string, object>)rules[1];
            Assert.Equal(nameof(ComplexEntity.Tags), rule2["Property"]);
            Assert.Equal("Premium", rule2["Value"]);

            _output.WriteLine($"Collection Property Expression 1: {rule1["Property"]}");
            _output.WriteLine($"Collection Property Expression 2: {rule2["Property"]}");
        }

        [Fact]
        public void NestedCollectionPropertyExpression_ShouldExtractCorrectPath()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<ComplexEntity>()
                .Any(x => x.Nested.Collections)
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            var rule = (Dictionary<string, object>)rules[0];
            Assert.Equal("Nested.Collections", rule["Property"]);

            _output.WriteLine($"Nested Collection Property Expression: {rule["Property"]}");
        }

        [Fact]
        public void PropertyComparisonExpression_ShouldExtractCorrectPaths()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<ComplexEntity>()
                .AddPropertyComparison(x => x.Name, FilterOperator.Equal, x => x.Nested.SubName)
                .AddPropertyComparison(x => x.Nested.Deep.DeepValue, FilterOperator.GreaterThan, x => x.NullableInt)
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            
            var rule1 = (Dictionary<string, object>)rules[0];
            Assert.Equal(nameof(ComplexEntity.Name), rule1["Property"]);
            Assert.Equal("Nested.SubName", rule1["CompareToProperty"]);
            
            var rule2 = (Dictionary<string, object>)rules[1];
            Assert.Equal("Nested.Deep.DeepValue", rule2["Property"]);
            Assert.Equal(nameof(ComplexEntity.NullableInt), rule2["CompareToProperty"]);

            _output.WriteLine($"Property Comparison 1: {rule1["Property"]} vs {rule1["CompareToProperty"]}");
            _output.WriteLine($"Property Comparison 2: {rule2["Property"]} vs {rule2["CompareToProperty"]}");
        }

        [Fact]
        public void UnaryExpressionConversion_ShouldWork()
        {
            // Arrange & Act - 測試 nullable bool 的 UnaryExpression
            var result = FilterDictionaryBuilder.QueryBuilder<ComplexEntity>()
                .Equal(x => x.NullableBool, true)
                .Equal(x => x.NullableInt, null)
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            
            var rule1 = (Dictionary<string, object>)rules[0];
            Assert.Equal(nameof(ComplexEntity.NullableBool), rule1["Property"]);
            
            var rule2 = (Dictionary<string, object>)rules[1];
            Assert.Equal(nameof(ComplexEntity.NullableInt), rule2["Property"]);

            _output.WriteLine($"Unary Expression 1: {rule1["Property"]}");
            _output.WriteLine($"Unary Expression 2: {rule2["Property"]}");
        }

        [Fact]
        public void InvalidExpressionTypes_ShouldThrowException()
        {
            // Test 1: Constant expression
            Assert.Throws<ArgumentException>(() =>
            {
                FilterDictionaryBuilder.QueryBuilder<ComplexEntity>()
                    .Add(x => "ConstantString", FilterOperator.Equal, "test");
            });

            // Test 2: Method call expression
            Assert.Throws<ArgumentException>(() =>
            {
                FilterDictionaryBuilder.QueryBuilder<ComplexEntity>()
                    .Add(x => x.Name.ToString(), FilterOperator.Equal, "test");
            });

            // Test 3: Binary expression
            Assert.Throws<ArgumentException>(() =>
            {
                FilterDictionaryBuilder.QueryBuilder<ComplexEntity>()
                    .Add(x => x.Name + "suffix", FilterOperator.Equal, "test");
            });

            _output.WriteLine("Invalid expression types correctly throw ArgumentException");
        }

        [Fact]
        public void AllOperatorsWithExpressions_ShouldExtractCorrectPaths()
        {
            // Arrange & Act
            var testDate = DateTime.Now;
            var result = FilterDictionaryBuilder.QueryBuilder<ComplexEntity>()
                .Equal(x => x.Name, "test")
                .NotEqual(x => x.Nested.SubName, "exclude")
                .Like(x => x.Name, "%pattern%")
                .NotLike(x => x.Nested.SubName, "%exclude%")
                .Contains(x => x.Name, "substring")
                .NotContains(x => x.Nested.SubName, "exclude")
                .StartsWith(x => x.Name, "prefix")
                .EndsWith(x => x.Name, "suffix")
                .GreaterThan(x => x.Nested.Deep.DeepValue, 100)
                .GreaterThanOrEqual(x => x.NullableInt, 50)
                .LessThan(x => x.CreatedDate, testDate)
                .LessThanOrEqual(x => x.Nested.Deep.DeepValue, 200)
                .In(x => x.Name, new[] { "option1", "option2" })
                .NotIn(x => x.Nested.SubName, new[] { "exclude1", "exclude2" })
                .Between(x => x.Nested.Deep.DeepValue, 10, 90)
                .NotBetween(x => x.NullableInt, 100, 200)
                .Any(x => x.Tags)
                .NotAny(x => x.Nested.Collections, "unwanted")
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            Assert.Equal(18, rules.Count);

            // 驗證幾個關鍵規則
            var equalRule = (Dictionary<string, object>)rules[0];
            Assert.Equal(nameof(ComplexEntity.Name), equalRule["Property"]);
            Assert.Equal(FilterOperator.Equal, equalRule["Operator"]);

            var nestedRule = (Dictionary<string, object>)rules[1];
            Assert.Equal("Nested.SubName", nestedRule["Property"]);
            Assert.Equal(FilterOperator.NotEqual, nestedRule["Operator"]);

            var deepRule = (Dictionary<string, object>)rules[8];
            Assert.Equal("Nested.Deep.DeepValue", deepRule["Property"]);
            Assert.Equal(FilterOperator.GreaterThan, deepRule["Operator"]);

            var collectionRule = (Dictionary<string, object>)rules[16];
            Assert.Equal(nameof(ComplexEntity.Tags), collectionRule["Property"]);
            Assert.Equal(FilterOperator.Any, collectionRule["Operator"]);

            _output.WriteLine($"All operators test - {rules.Count} rules created successfully");
            _output.WriteLine($"Sample rules: {equalRule["Property"]}, {nestedRule["Property"]}, {deepRule["Property"]}, {collectionRule["Property"]}");
        }

        [Fact]
        public void NestedExpressionInCompare_ShouldExtractCorrectPaths()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<ComplexEntity>()
                .Compare(LogicalOperator.Or, rules => rules
                    .Equal(x => x.Name, "test1")
                    .Like(x => x.Nested.SubName, "test2")
                    .Compare(LogicalOperator.And, innerRules => innerRules
                        .GreaterThan(x => x.Nested.Deep.DeepValue, 100)
                        .Any(x => x.Tags, "premium")
                    )
                )
                .Build();

            // Assert
            var mainRules = (List<object>)result["Rules"];
            var nestedGroup = (Dictionary<string, object>)mainRules[0];
            var nestedRules = (List<object>)nestedGroup["Rules"];
            Assert.Equal(3, nestedRules.Count);

            var rule1 = (Dictionary<string, object>)nestedRules[0];
            Assert.Equal(nameof(ComplexEntity.Name), rule1["Property"]);

            var rule2 = (Dictionary<string, object>)nestedRules[1];
            Assert.Equal("Nested.SubName", rule2["Property"]);

            var innerGroup = (Dictionary<string, object>)nestedRules[2];
            var innerGroupRules = (List<object>)innerGroup["Rules"];
            
            var innerRule1 = (Dictionary<string, object>)innerGroupRules[0];
            Assert.Equal("Nested.Deep.DeepValue", innerRule1["Property"]);
            
            var innerRule2 = (Dictionary<string, object>)innerGroupRules[1];
            Assert.Equal(nameof(ComplexEntity.Tags), innerRule2["Property"]);

            _output.WriteLine($"Nested expression in Compare - all property paths extracted correctly");
        }

        [Fact]
        public void MixedStringAndExpressionPaths_ShouldCoexist()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<ComplexEntity>()
                .Equal(x => x.Name, "expression")                    // Expression
                .Equal(nameof(ComplexEntity.Name), "string")         // String
                .Like(x => x.Nested.SubName, "expr_pattern")         // Expression nested
                .Like("Nested.SubName", "string_pattern")            // String nested
                .GreaterThan(x => x.Nested.Deep.DeepValue, 100)      // Expression deep
                .GreaterThan("Nested.Deep.DeepValue", 200)           // String deep
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            Assert.Equal(6, rules.Count);

            // 驗證 Expression 和 String 版本產生相同的屬性路徑
            var exprRule1 = (Dictionary<string, object>)rules[0];
            var stringRule1 = (Dictionary<string, object>)rules[1];
            Assert.Equal(exprRule1["Property"], stringRule1["Property"]);

            var exprRule2 = (Dictionary<string, object>)rules[2];
            var stringRule2 = (Dictionary<string, object>)rules[3];
            Assert.Equal(exprRule2["Property"], stringRule2["Property"]);

            var exprRule3 = (Dictionary<string, object>)rules[4];
            var stringRule3 = (Dictionary<string, object>)rules[5];
            Assert.Equal(exprRule3["Property"], stringRule3["Property"]);

            _output.WriteLine("Mixed string and expression paths coexist successfully");
            _output.WriteLine($"Expression vs String paths: {exprRule1["Property"]}, {exprRule2["Property"]}, {exprRule3["Property"]}");
        }

        [Fact]
        public void ExpressionPropertyComparisonComplexPaths_ShouldWork()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<ComplexEntity>()
                .AddPropertyComparison(
                    x => x.Nested.Deep.DeepValue, 
                    FilterOperator.GreaterThan, 
                    x => x.NullableInt,
                    isNegated: true)
                .AddPropertyComparison(
                    x => x.Name, 
                    FilterOperator.Equal, 
                    x => x.Nested.SubName)
                .AddPropertyComparison(
                    x => x.CreatedDate, 
                    FilterOperator.LessThan, 
                    x => x.CreatedDate) // 自己比較自己（edge case）
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            Assert.Equal(3, rules.Count);

            var rule1 = (Dictionary<string, object>)rules[0];
            Assert.Equal("Nested.Deep.DeepValue", rule1["Property"]);
            Assert.Equal(nameof(ComplexEntity.NullableInt), rule1["CompareToProperty"]);
            Assert.True((bool)rule1["IsNegated"]);

            var rule2 = (Dictionary<string, object>)rules[1];
            Assert.Equal(nameof(ComplexEntity.Name), rule2["Property"]);
            Assert.Equal("Nested.SubName", rule2["CompareToProperty"]);

            var rule3 = (Dictionary<string, object>)rules[2];
            Assert.Equal(nameof(ComplexEntity.CreatedDate), rule3["Property"]);
            Assert.Equal(nameof(ComplexEntity.CreatedDate), rule3["CompareToProperty"]);

            _output.WriteLine("Expression property comparison with complex paths works correctly");
        }
    }
}