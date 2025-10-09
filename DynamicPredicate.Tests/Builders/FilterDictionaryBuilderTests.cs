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

        // 測試用的複雜實體類別，包含導覽屬性
        public class Contract
        {
            public string ContractNo { get; set; }
            public List<BuildContract> BuildContracts { get; set; } = new();
            public ContractDetail Detail { get; set; }
        }

        public class BuildContract
        {
            public string BuildContractId { get; set; }
            public Build Build { get; set; }
            public List<SubContract> SubContracts { get; set; } = new();
        }

        public class Build
        {
            public string BuildId { get; set; }
            public string HosAddress { get; set; }
            public string BuildName { get; set; }
            public List<Floor> Floors { get; set; } = new();
        }

        public class Floor
        {
            public string FloorName { get; set; }
            public List<Unit> Units { get; set; } = new();
        }

        public class Unit
        {
            public string UnitNo { get; set; }
            public decimal Area { get; set; }
        }

        public class SubContract
        {
            public string SubContractId { get; set; }
            public string ContractorName { get; set; }
        }

        public class ContractDetail
        {
            public decimal TotalAmount { get; set; }
            public DateTime SignDate { get; set; }
        }

        // 測試用的實體類別，包含集合屬性
        public class TestEntityWithCollections
        {
            public string Name { get; set; }
            public List<string> Tags { get; set; } = new();
            public List<string> Categories { get; set; } = new();
            public List<string> Flags { get; set; } = new();
            public List<string> Labels { get; set; } = new();
            public List<int> Numbers { get; set; } = new();
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

        [Fact]
        public void AnyNotAnyOperators_ShouldCreateCorrectDictionary()
        {
            // Arrange & Act - 測試 Any 和 NotAny 操作符
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .Any("Tags")                              // 檢查 Tags 集合是否有任何元素（value 為 null）
                .Any("Categories", "VIP")                 // 檢查 Categories 集合是否包含 "VIP"
                .NotAny("BlacklistTags")                  // 檢查 BlacklistTags 集合是否沒有任何元素
                .NotAny("RestrictedCategories", "Banned") // 檢查 RestrictedCategories 集合是否不包含 "Banned"
                .Build();

            // Assert
            Assert.Equal(LogicalOperator.And, result["LogicalOperator"]);
            var rules = (List<object>)result["Rules"];
            Assert.Equal(4, rules.Count);

            // 檢查第一個 Any 規則（無 value）
            var rule1 = (Dictionary<string, object>)rules[0];
            Assert.Equal("Tags", rule1["Property"]);
            Assert.Equal(FilterOperator.Any, rule1["Operator"]);
            Assert.Null(rule1["Value"]);

            // 檢查第二個 Any 規則（有 value）
            var rule2 = (Dictionary<string, object>)rules[1];
            Assert.Equal("Categories", rule2["Property"]);
            Assert.Equal(FilterOperator.Any, rule2["Operator"]);
            Assert.Equal("VIP", rule2["Value"]);

            // 檢查第一個 NotAny 規則（無 value）
            var rule3 = (Dictionary<string, object>)rules[2];
            Assert.Equal("BlacklistTags", rule3["Property"]);
            Assert.Equal(FilterOperator.NotAny, rule3["Operator"]);
            Assert.Null(rule3["Value"]);

            // 檢查第二個 NotAny 規則（有 value）
            var rule4 = (Dictionary<string, object>)rules[3];
            Assert.Equal("RestrictedCategories", rule4["Property"]);
            Assert.Equal(FilterOperator.NotAny, rule4["Operator"]);
            Assert.Equal("Banned", rule4["Value"]);

            _output.WriteLine($"Any/NotAny Operators Query Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void ExpressionAnyNotAnyOperators_ShouldCreateCorrectDictionary()
        {
            // Arrange - 建立測試用的 DTO
            var result = FilterDictionaryBuilder.QueryBuilder<TestEntityWithCollections>()
                .WithLogicalOperator(LogicalOperator.And)
                .Any(x => x.Tags)                           // 檢查 Tags 集合是否有任何元素
                .Any(x => x.Categories, "Premium")          // 檢查 Categories 集合是否包含 "Premium"
                .NotAny(x => x.Flags)                       // 請檢查 Flags 集合是否沒有任何元素
                .NotAny(x => x.Labels, "Deprecated")        // 檢查 Labels 集合是否不包含 "Deprecated"
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            Assert.Equal(4, rules.Count);

            var rule1 = (Dictionary<string, object>)rules[0];
            Assert.Equal(nameof(TestEntityWithCollections.Tags), rule1["Property"]);
            Assert.Equal(FilterOperator.Any, rule1["Operator"]);
            Assert.Null(rule1["Value"]);

            var rule2 = (Dictionary<string, object>)rules[1];
            Assert.Equal(nameof(TestEntityWithCollections.Categories), rule2["Property"]);
            Assert.Equal(FilterOperator.Any, rule2["Operator"]);
            Assert.Equal("Premium", rule2["Value"]);

            _output.WriteLine($"Expression Any/NotAny Operators Query Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void AllNewOperators_ShouldCreateCorrectDictionary()
        {
            // Arrange & Act - 測試所有新增的操作符
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .NotEqual(x => x.CaseOwner, "TestUser")
                .NotContains(x => x.LandNo, "TEMP")
                .NotIn(x => x.CityCode, new[] { "INVALID", "TEST" })
                .NotLike(x => x.LandNo, "%DRAFT%")
                .NotBetween(x => x.Price, 0, 100)
                .Any(x => x.LandNo)                       // 假設 LandNo 可能是集合
                .NotAny(x => x.CityCode, "BLACKLISTED")   // 假設 CityCode 可能是集合
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            Assert.Equal(7, rules.Count);

            // 檢查 NotEqual 規則
            var rule1 = (Dictionary<string, object>)rules[0];
            Assert.Equal(FilterOperator.NotEqual, rule1["Operator"]);

            // 檢查 NotContains 規則
            var rule2 = (Dictionary<string, object>)rules[1];
            Assert.Equal(FilterOperator.NotContains, rule2["Operator"]);

            // 檢查 NotIn 規則
            var rule3 = (Dictionary<string, object>)rules[2];
            Assert.Equal(FilterOperator.NotIn, rule3["Operator"]);

            // 檢查 NotLike 規則
            var rule4 = (Dictionary<string, object>)rules[3];
            Assert.Equal(FilterOperator.NotLike, rule4["Operator"]);

            // 檢查 NotBetween 規則
            var rule5 = (Dictionary<string, object>)rules[4];
            Assert.Equal(FilterOperator.NotBetween, rule5["Operator"]);

            // 檢查 Any 規則
            var rule6 = (Dictionary<string, object>)rules[5];
            Assert.Equal(FilterOperator.Any, rule6["Operator"]);

            // 檢查 NotAny 規則
            var rule7 = (Dictionary<string, object>)rules[6];
            Assert.Equal(FilterOperator.NotAny, rule7["Operator"]);

            _output.WriteLine($"All New Operators Query Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        #region 新增的測試案例

        [Fact]
        public void ArrayNavigationPropertyQuery_ShouldCreateCorrectDictionary()
        {
            // Arrange & Act - 測試陣列導覽屬性查詢
            var result = FilterDictionaryBuilder.QueryBuilder<Contract>()
                .WithLogicalOperator(LogicalOperator.Or)
                .Like("BuildContracts[].Build.HosAddress", "台北市")
                .Like("BuildContracts[].Build.BuildName", "大樓")
                .Equal("BuildContracts[].Build.Floors[].Units[].UnitNo", "A101")
                .GreaterThan("BuildContracts[].SubContracts[].ContractorName", "ABC公司")
                .Build();

            // Assert
            Assert.Equal(LogicalOperator.Or, result["LogicalOperator"]);
            var rules = (List<object>)result["Rules"];
            Assert.Equal(4, rules.Count);

            var rule1 = (Dictionary<string, object>)rules[0];
            Assert.Equal("BuildContracts[].Build.HosAddress", rule1["Property"]);
            Assert.Equal(FilterOperator.Like, rule1["Operator"]);
            Assert.Equal("台北市", rule1["Value"]);

            var rule4 = (Dictionary<string, object>)rules[3];
            Assert.Equal("BuildContracts[].SubContracts[].ContractorName", rule4["Property"]);
            Assert.Equal(FilterOperator.GreaterThan, rule4["Operator"]);
            Assert.Equal("ABC公司", rule4["Value"]);

            _output.WriteLine($"Array Navigation Property Query Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void MultiLevelArrayNavigation_ShouldCreateCorrectDictionary()
        {
            // Arrange & Act - 測試多層陣列導覽
            var result = FilterDictionaryBuilder.QueryBuilder<Contract>()
                .WithLogicalOperator(LogicalOperator.And)
                .Compare(LogicalOperator.Or, rules => rules
                    .Like("BuildContracts[].Build.Floors[].Units[].UnitNo", "A")
                    .Contains("BuildContracts[].Build.Floors[].FloorName", "1F")
                    .GreaterThan("BuildContracts[].Build.Floors[].Units[].Area", 30.0m)
                )
                .Equal("Detail.TotalAmount", 1000000m)
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            Assert.Equal(2, rules.Count);

            var nestedGroup = (Dictionary<string, object>)rules[0];
            var nestedRules = (List<object>)nestedGroup["Rules"];
            Assert.Equal(3, nestedRules.Count);

            var rule1 = (Dictionary<string, object>)nestedRules[0];
            Assert.Equal("BuildContracts[].Build.Floors[].Units[].UnitNo", rule1["Property"]);

            _output.WriteLine($"Multi-Level Array Navigation Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void InterOperator_ShouldBeSetCorrectly()
        {
            // Arrange & Act - 測試 InterOperator 設定
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .WithInterOperator(LogicalOperator.Or)
                .Equal(nameof(LandDTO.CaseOwner), "John")
                .Like(nameof(LandDTO.LandNo), "A123")
                .Build();

            // Assert
            Assert.Equal(LogicalOperator.And, result["LogicalOperator"]);
            Assert.Equal(LogicalOperator.Or, result["InterOperator"]);

            _output.WriteLine($"InterOperator Query Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void InterOperator_ShouldNotAppearWhenDefault()
        {
            // Arrange & Act - 測試預設 InterOperator 不會出現在結果中
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.Or)
                // 不設定 InterOperator，應該使用預設值 And
                .Equal(nameof(LandDTO.CaseOwner), "John")
                .Build();

            // Assert
            Assert.Equal(LogicalOperator.Or, result["LogicalOperator"]);
            Assert.False(result.ContainsKey("InterOperator")); // 預設值不應該出現

            _output.WriteLine($"Default InterOperator Query Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void GroupNegation_ShouldBeSetCorrectly()
        {
            // Arrange & Act - 測試群組級別的否定
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .Negate(true)
                .Equal(nameof(LandDTO.CaseOwner), "John")
                .Like(nameof(LandDTO.LandNo), "A123")
                .Build();

            // Assert
            Assert.Equal(LogicalOperator.And, result["LogicalOperator"]);
            Assert.True((bool)result["IsNegated"]);

            _output.WriteLine($"Group Negation Query Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void GroupNegation_ShouldNotAppearWhenFalse()
        {
            // Arrange & Act - 測試 IsNegated = false 時不出現
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .Negate(false)
                .Equal(nameof(LandDTO.CaseOwner), "John")
                .Build();

            // Assert
            Assert.False(result.ContainsKey("IsNegated")); // false 時不應該出現

            _output.WriteLine($"No Group Negation Query Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void EmptyBuilder_ShouldCreateMinimalDictionary()
        {
            // Arrange & Act - 測試空的 Builder
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .Build();

            // Assert
            Assert.Equal(LogicalOperator.And, result["LogicalOperator"]); // 預設值
            var rules = (List<object>)result["Rules"];
            Assert.Empty(rules);
            Assert.False(result.ContainsKey("InterOperator")); // 預設值不出現
            Assert.False(result.ContainsKey("IsNegated")); // 預設值不出現

            _output.WriteLine($"Empty Builder Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void PropertyComparisonWithNegation_ShouldCreateCorrectDictionary()
        {
            // Arrange & Act - 測試帶否定的屬性比較
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .AddPropertyComparison(nameof(LandDTO.CreateDate), FilterOperator.GreaterThan, nameof(LandDTO.Price), isNegated: true)
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            var rule = (Dictionary<string, object>)rules[0];
            
            Assert.Equal(nameof(LandDTO.CreateDate), rule["Property"]);
            Assert.Equal(FilterOperator.GreaterThan, rule["Operator"]);
            Assert.Equal(nameof(LandDTO.Price), rule["CompareToProperty"]);
            Assert.True((bool)rule["IsNegated"]);

            _output.WriteLine($"Property Comparison with Negation Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void ExpressionPropertyComparisonWithNegation_ShouldCreateCorrectDictionary()
        {
            // Arrange & Act - 測試 Expression 版本的帶否定屬性比較
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .AddPropertyComparison(x => x.CreateDate, FilterOperator.LessThan, x => x.Price, isNegated: true)
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            var rule = (Dictionary<string, object>)rules[0];
            
            Assert.Equal(nameof(LandDTO.CreateDate), rule["Property"]);
            Assert.Equal(FilterOperator.LessThan, rule["Operator"]);
            Assert.Equal(nameof(LandDTO.Price), rule["CompareToProperty"]);
            Assert.True((bool)rule["IsNegated"]);

            _output.WriteLine($"Expression Property Comparison with Negation Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void InvalidExpression_ShouldThrowException()
        {
            // Arrange & Act & Assert - 測試無效的 Expression
            Assert.Throws<ArgumentException>(() =>
            {
                // 這會產生一個不是 MemberExpression 或 UnaryExpression 的表達式
                FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                    .Add(x => "ConstantString", FilterOperator.Equal, "test");
            });
        }

        [Fact]
        public void DeepNestedCompare_ShouldCreateCorrectStructure()
        {
            // Arrange & Act - 測試深層巢狀的 Compare
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .Compare(LogicalOperator.Or, rules => rules
                    .Equal(nameof(LandDTO.CaseOwner), "Owner1")
                    .Compare(LogicalOperator.And, level2 => level2
                        .Like(nameof(LandDTO.LandNo), "A")
                        .Compare(LogicalOperator.Or, level3 => level3
                            .GreaterThan(nameof(LandDTO.Price), 1000000)
                            .LessThan(nameof(LandDTO.Price), 500000)
                        )
                    )
                )
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            Assert.Single(rules);

            var level1Group = (Dictionary<string, object>)rules[0];
            Assert.Equal(LogicalOperator.Or, level1Group["LogicalOperator"]);
            var level1Rules = (List<object>)level1Group["Rules"];
            Assert.Equal(2, level1Rules.Count);

            // 檢查第二個規則是否為巢狀群組
            var level2Group = (Dictionary<string, object>)level1Rules[1];
            Assert.Equal(LogicalOperator.And, level2Group["LogicalOperator"]);
            var level2Rules = (List<object>)level2Group["Rules"];
            Assert.Equal(2, level2Rules.Count);

            // 檢查第三層巢狀
            var level3Group = (Dictionary<string, object>)level2Rules[1];
            Assert.Equal(LogicalOperator.Or, level3Group["LogicalOperator"]);

            _output.WriteLine($"Deep Nested Compare Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void NestedCompareWithIsNegatedUpdate_ShouldSetCorrectly()
        {
            // Arrange & Act - 測試巢狀 Compare 中 IsNegated 的更新邏輯
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .Compare(LogicalOperator.And, rules => rules
                    .Equal(nameof(LandDTO.CaseOwner), "John")
                    .Like(nameof(LandDTO.LandNo), "A123"), 
                    isNegated: true)
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            var nestedGroup = (Dictionary<string, object>)rules[0];
            Assert.True((bool)nestedGroup["IsNegated"]);

            _output.WriteLine($"Nested Compare with IsNegated Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void ChainedBuilderMethods_ShouldMaintainState()
        {
            // Arrange & Act - 測試鏈式方法調用是否正確維護狀態
            var builder = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.Or)
                .WithInterOperator(LogicalOperator.Or) // 設定為非預設值，確保會出現在結果中
                .Negate(true);

            var result = builder
                .Equal(nameof(LandDTO.CaseOwner), "John")
                .Like(nameof(LandDTO.LandNo), "A123")
                .Build();

            // Assert
            Assert.Equal(LogicalOperator.Or, result["LogicalOperator"]);
            Assert.Equal(LogicalOperator.Or, result["InterOperator"]);
            Assert.True((bool)result["IsNegated"]);

            var rules = (List<object>)result["Rules"];
            Assert.Equal(2, rules.Count);

            _output.WriteLine($"Chained Builder Methods Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void MixedExpressionAndStringNavigation_ShouldWork()
        {
            // Arrange & Act - 測試混合 Expression 和字串導覽屬性
            var result = FilterDictionaryBuilder.QueryBuilder<Contract>()
                .WithLogicalOperator(LogicalOperator.Or)
                .Like("BuildContracts[].Build.HosAddress", "台北")  // 字串版本
                .Equal(x => x.ContractNo, "C001")                  // Expression 版本
                .Contains("BuildContracts[].SubContracts[].ContractorName", "建設") // 字串版本
                .GreaterThan(x => x.Detail.TotalAmount, 500000m)   // Expression 版本
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            Assert.Equal(4, rules.Count);

            var rule1 = (Dictionary<string, object>)rules[0];
            Assert.Equal("BuildContracts[].Build.HosAddress", rule1["Property"]);

            var rule2 = (Dictionary<string, object>)rules[1];
            Assert.Equal(nameof(Contract.ContractNo), rule2["Property"]);

            _output.WriteLine($"Mixed Expression and String Navigation Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        #endregion
    }
}