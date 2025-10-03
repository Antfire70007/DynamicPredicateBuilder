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
    public class FilterDictionaryBuilderTests
    {
        private readonly ITestOutputHelper _output;

        public FilterDictionaryBuilderTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // 測試用的 DTO 類別
        public class LandDTO
        {
            public string LandNo { get; set; }
            public string CityCode { get; set; }
            public string CaseOwner { get; set; }
            public decimal Price { get; set; }
            public DateTime CreateDate { get; set; }
        }

        [Fact]
        public void SimpleQuery_ShouldCreateCorrectDictionary()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.Or)
                .Like(nameof(LandDTO.LandNo), "test-land")
                .Like(nameof(LandDTO.CityCode), "test-city")
                .Build();

            // Assert
            Assert.Equal(LogicalOperator.Or, result["LogicalOperator"]);
            var rules = (List<object>)result["Rules"];
            Assert.Equal(2, rules.Count);

            var rule1 = (Dictionary<string, object>)rules[0];
            Assert.Equal(nameof(LandDTO.LandNo), rule1["Property"]);
            Assert.Equal(FilterOperator.Like, rule1["Operator"]);
            Assert.Equal("test-land", rule1["Value"]);

            var rule2 = (Dictionary<string, object>)rules[1];
            Assert.Equal(nameof(LandDTO.CityCode), rule2["Property"]);
            Assert.Equal(FilterOperator.Like, rule2["Operator"]);
            Assert.Equal("test-city", rule2["Value"]);

            _output.WriteLine($"Simple Query Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void NestedQuery_ShouldCreateCorrectDictionary()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.Or)
                .Like(nameof(LandDTO.LandNo), "test-land")
                .Like(nameof(LandDTO.CityCode), "test-city")
                .Compare(LogicalOperator.And, rules => rules
                    .Equal(nameof(LandDTO.CaseOwner), "John Doe")
                    .GreaterThan(nameof(LandDTO.Price), 1000000)
                )
                .Build();

            // Assert
            Assert.Equal(LogicalOperator.Or, result["LogicalOperator"]);
            var rules = (List<object>)result["Rules"];
            Assert.Equal(3, rules.Count);

            // 檢查巢狀群組
            var nestedGroup = (Dictionary<string, object>)rules[2];
            Assert.Equal(LogicalOperator.And, nestedGroup["LogicalOperator"]);
            var nestedRules = (List<object>)nestedGroup["Rules"];
            Assert.Equal(2, nestedRules.Count);

            _output.WriteLine($"Nested Query Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void QueryWithNegation_ShouldCreateCorrectDictionary()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .Equal(nameof(LandDTO.CaseOwner), "John Doe", isNegated: true)
                .Compare(LogicalOperator.Or, rules => rules
                    .Like(nameof(LandDTO.LandNo), "test")
                    .Contains(nameof(LandDTO.CityCode), "taipei"), 
                    isNegated: true)
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            var rule1 = (Dictionary<string, object>)rules[0];
            Assert.True((bool)rule1["IsNegated"]);

            var nestedGroup = (Dictionary<string, object>)rules[1];
            Assert.True((bool)nestedGroup["IsNegated"]);

            _output.WriteLine($"Negated Query Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void QueryWithAllOperators_ShouldCreateCorrectDictionary()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .Equal(nameof(LandDTO.CaseOwner), "John")
                .Like(nameof(LandDTO.LandNo), "test")
                .Contains(nameof(LandDTO.CityCode), "taipei")
                .In(nameof(LandDTO.CityCode), new[] { "taipei", "kaohsiung", "taichung" })
                .Between(nameof(LandDTO.Price), 500000, 2000000)
                .GreaterThan(nameof(LandDTO.CreateDate), DateTime.Now.AddDays(-30))
                .LessThan(nameof(LandDTO.Price), 5000000)
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            Assert.Equal(7, rules.Count);

            _output.WriteLine($"All Operators Query Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void PropertyComparison_ShouldCreateCorrectDictionary()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .AddPropertyComparison(nameof(LandDTO.LandNo), FilterOperator.Equal, nameof(LandDTO.CityCode))
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            var rule = (Dictionary<string, object>)rules[0];
            
            Assert.Equal(nameof(LandDTO.LandNo), rule["Property"]);
            Assert.Equal(FilterOperator.Equal, rule["Operator"]);
            Assert.Equal(nameof(LandDTO.CityCode), rule["CompareToProperty"]);

            _output.WriteLine($"Property Comparison Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void ConvertToFilterGroup_ShouldWork()
        {
            // Arrange & Act
            var filterGroup = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.Or)
                .Like(nameof(LandDTO.LandNo), "test-land")
                .Equal(nameof(LandDTO.CaseOwner), "John Doe")
                .ToFilterGroup();

            // Assert
            Assert.NotNull(filterGroup);
            Assert.Equal(LogicalOperator.Or, filterGroup.LogicalOperator);
            Assert.Equal(2, filterGroup.Rules.Count);

            var rule1 = (FilterRule)filterGroup.Rules[0];
            Assert.Equal(nameof(LandDTO.LandNo), rule1.Property);
            Assert.Equal(FilterOperator.Like, rule1.Operator);
            Assert.Equal("test-land", rule1.Value);

            _output.WriteLine($"FilterGroup Result: {JsonConvert.SerializeObject(filterGroup, Formatting.Indented)}");
        }

        [Fact]
        public void ImplicitConversion_ShouldWork()
        {
            // Arrange & Act
            Dictionary<string, object> dict = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .Like(nameof(LandDTO.LandNo), "test");

            FilterGroup group = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .Equal(nameof(LandDTO.CaseOwner), "John");

            // Assert
            Assert.NotNull(dict);
            Assert.NotNull(group);
            Assert.IsType<Dictionary<string, object>>(dict);
            Assert.IsType<FilterGroup>(group);

            _output.WriteLine($"Implicit Dictionary: {JsonConvert.SerializeObject(dict, Formatting.Indented)}");
            _output.WriteLine($"Implicit FilterGroup: {JsonConvert.SerializeObject(group, Formatting.Indented)}");
        }

        [Fact]
        public void StaticFactory_ShouldWork()
        {
            // Arrange & Act
            var result1 = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .Like(nameof(LandDTO.LandNo), "test")
                .Build();

            var result2 = FilterDictionaryBuilder.Create<LandDTO>()
                .Equal(nameof(LandDTO.CaseOwner), "John")
                .Build();

            var result3 = FilterDictionaryBuilder<LandDTO>.Create()
                .Contains(nameof(LandDTO.CityCode), "taipei")
                .Build();

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.NotNull(result3);

            _output.WriteLine($"Factory Method 1: {JsonConvert.SerializeObject(result1, Formatting.Indented)}");
            _output.WriteLine($"Factory Method 2: {JsonConvert.SerializeObject(result2, Formatting.Indented)}");
            _output.WriteLine($"Factory Method 3: {JsonConvert.SerializeObject(result3, Formatting.Indented)}");
        }

        [Fact]
        public void ComplexNestedQuery_ShouldCreateCorrectStructure()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .Compare(LogicalOperator.Or, rules => rules
                    .Like(nameof(LandDTO.LandNo), "A123")
                    .Like(nameof(LandDTO.LandNo), "B456")
                    .Compare(LogicalOperator.And, innerRules => innerRules
                        .Equal(nameof(LandDTO.CaseOwner), "Special Owner")
                        .GreaterThan(nameof(LandDTO.Price), 10000000)
                    )
                )
                .Compare(LogicalOperator.And, rules => rules
                    .In(nameof(LandDTO.CityCode), new[] { "TPE", "KHH" })
                    .Between(nameof(LandDTO.CreateDate), DateTime.Now.AddYears(-1), DateTime.Now)
                )
                .Build();

            // Assert
            Assert.Equal(LogicalOperator.And, result["LogicalOperator"]);
            var mainRules = (List<object>)result["Rules"];
            Assert.Equal(2, mainRules.Count);

            // 檢查第一個巢狀群組
            var firstGroup = (Dictionary<string, object>)mainRules[0];
            Assert.Equal(LogicalOperator.Or, firstGroup["LogicalOperator"]);
            var firstGroupRules = (List<object>)firstGroup["Rules"];
            Assert.Equal(3, firstGroupRules.Count); // 2個Like + 1個內巢狀群組

            _output.WriteLine($"Complex Nested Query: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void ExpressionQuery_ShouldCreateCorrectDictionary()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.Or)
                .Like(x => x.LandNo, "test-land")
                .Like(x => x.CityCode, "test-city")
                .Build();

            // Assert
            Assert.Equal(LogicalOperator.Or, result["LogicalOperator"]);
            var rules = (List<object>)result["Rules"];
            Assert.Equal(2, rules.Count);

            var rule1 = (Dictionary<string, object>)rules[0];
            Assert.Equal(nameof(LandDTO.LandNo), rule1["Property"]);
            Assert.Equal(FilterOperator.Like, rule1["Operator"]);
            Assert.Equal("test-land", rule1["Value"]);

            var rule2 = (Dictionary<string, object>)rules[1];
            Assert.Equal(nameof(LandDTO.CityCode), rule2["Property"]);
            Assert.Equal(FilterOperator.Like, rule2["Operator"]);
            Assert.Equal("test-city", rule2["Value"]);

            _output.WriteLine($"Expression Query Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void ExpressionNestedQuery_ShouldCreateCorrectStructure()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.Or)
                .Like(x => x.LandNo, "test-land")
                .Like(x => x.CityCode, "test-city")
                .Compare(LogicalOperator.And, rules => rules
                    .Equal(x => x.CaseOwner, "John Doe")
                    .GreaterThan(x => x.Price, 1000000)
                )
                .Build();

            // Assert
            Assert.Equal(LogicalOperator.Or, result["LogicalOperator"]);
            var rules = (List<object>)result["Rules"];
            Assert.Equal(3, rules.Count);

            // 檢查巢狀群組
            var nestedGroup = (Dictionary<string, object>)rules[2];
            Assert.Equal(LogicalOperator.And, nestedGroup["LogicalOperator"]);
            var nestedRules = (List<object>)nestedGroup["Rules"];
            Assert.Equal(2, nestedRules.Count);

            var nestedRule1 = (Dictionary<string, object>)nestedRules[0];
            Assert.Equal(nameof(LandDTO.CaseOwner), nestedRule1["Property"]);
            Assert.Equal(FilterOperator.Equal, nestedRule1["Operator"]);
            Assert.Equal("John Doe", nestedRule1["Value"]);

            _output.WriteLine($"Expression Nested Query Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void ExpressionWithAllOperators_ShouldCreateCorrectDictionary()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .Equal(x => x.CaseOwner, "John")
                .Like(x => x.LandNo, "test")
                .Contains(x => x.CityCode, "taipei")
                .In(x => x.CityCode, new[] { "taipei", "kaohsiung", "taichung" })
                .Between(x => x.Price, 500000, 2000000)
                .GreaterThan(x => x.CreateDate, DateTime.Now.AddDays(-30))
                .LessThan(x => x.Price, 5000000)
                .GreaterThanOrEqual(x => x.Price, 100000)
                .LessThanOrEqual(x => x.Price, 10000000)
                .StartsWith(x => x.LandNo, "A")
                .EndsWith(x => x.LandNo, "001")
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            Assert.Equal(11, rules.Count);

            _output.WriteLine($"Expression All Operators Query Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void ExpressionPropertyComparison_ShouldCreateCorrectDictionary()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .AddPropertyComparison(x => x.LandNo, FilterOperator.Equal, x => x.CityCode)
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            var rule = (Dictionary<string, object>)rules[0];
            
            Assert.Equal(nameof(LandDTO.LandNo), rule["Property"]);
            Assert.Equal(FilterOperator.Equal, rule["Operator"]);
            Assert.Equal(nameof(LandDTO.CityCode), rule["CompareToProperty"]);

            _output.WriteLine($"Expression Property Comparison Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void ExpressionMixedWithStringQuery_ShouldWork()
        {
            // Arrange & Act - 混合使用 Expression 和字串版本
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .Like(x => x.LandNo, "A123")  // Expression 版本
                .Equal(nameof(LandDTO.CaseOwner), "John Doe")  // 字串版本
                .GreaterThan(x => x.Price, 1000000)  // Expression 版本
                .Contains(nameof(LandDTO.CityCode), "taipei")  // 字串版本
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            Assert.Equal(4, rules.Count);

            var rule1 = (Dictionary<string, object>)rules[0];
            Assert.Equal(nameof(LandDTO.LandNo), rule1["Property"]);

            var rule2 = (Dictionary<string, object>)rules[1];
            Assert.Equal(nameof(LandDTO.CaseOwner), rule2["Property"]);

            _output.WriteLine($"Expression Mixed Query Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }
    }
}