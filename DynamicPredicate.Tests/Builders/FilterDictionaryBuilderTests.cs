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

        // ���եΪ� DTO ���O
        public class LandDTO
        {
            public string LandNo { get; set; }
            public string CityCode { get; set; }
            public string CaseOwner { get; set; }
            public decimal Price { get; set; }
            public DateTime CreateDate { get; set; }
        }

        // ���եΪ������������O�A�]�t�����ݩ�
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

        // ���եΪ��������O�A�]�t���X�ݩ�
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

            // �ˬd�_���s��
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

            // �ˬd�Ĥ@�ӱ_���s��
            var firstGroup = (Dictionary<string, object>)mainRules[0];
            Assert.Equal(LogicalOperator.Or, firstGroup["LogicalOperator"]);
            var firstGroupRules = (List<object>)firstGroup["Rules"];
            Assert.Equal(3, firstGroupRules.Count); // 2��Like + 1�Ӥ��_���s��

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

            // �ˬd�_���s��
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
            // Arrange & Act - �V�X�ϥ� Expression �M�r�ꪩ��
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .Like(x => x.LandNo, "A123")  // Expression ����
                .Equal(nameof(LandDTO.CaseOwner), "John Doe")  // �r�ꪩ��
                .GreaterThan(x => x.Price, 1000000)  // Expression ����
                .Contains(nameof(LandDTO.CityCode), "taipei")  // �r�ꪩ��
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
            // Arrange & Act - ���� Any �M NotAny �ާ@��
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .Any("Tags")                              // �ˬd Tags ���X�O�_�����󤸯��]value �� null�^
                .Any("Categories", "VIP")                 // �ˬd Categories ���X�O�_�]�t "VIP"
                .NotAny("BlacklistTags")                  // �ˬd BlacklistTags ���X�O�_�S�����󤸯�
                .NotAny("RestrictedCategories", "Banned") // �ˬd RestrictedCategories ���X�O�_���]�t "Banned"
                .Build();

            // Assert
            Assert.Equal(LogicalOperator.And, result["LogicalOperator"]);
            var rules = (List<object>)result["Rules"];
            Assert.Equal(4, rules.Count);

            // �ˬd�Ĥ@�� Any �W�h�]�L value�^
            var rule1 = (Dictionary<string, object>)rules[0];
            Assert.Equal("Tags", rule1["Property"]);
            Assert.Equal(FilterOperator.Any, rule1["Operator"]);
            Assert.Null(rule1["Value"]);

            // �ˬd�ĤG�� Any �W�h�]�� value�^
            var rule2 = (Dictionary<string, object>)rules[1];
            Assert.Equal("Categories", rule2["Property"]);
            Assert.Equal(FilterOperator.Any, rule2["Operator"]);
            Assert.Equal("VIP", rule2["Value"]);

            // �ˬd�Ĥ@�� NotAny �W�h�]�L value�^
            var rule3 = (Dictionary<string, object>)rules[2];
            Assert.Equal("BlacklistTags", rule3["Property"]);
            Assert.Equal(FilterOperator.NotAny, rule3["Operator"]);
            Assert.Null(rule3["Value"]);

            // �ˬd�ĤG�� NotAny �W�h�]�� value�^
            var rule4 = (Dictionary<string, object>)rules[3];
            Assert.Equal("RestrictedCategories", rule4["Property"]);
            Assert.Equal(FilterOperator.NotAny, rule4["Operator"]);
            Assert.Equal("Banned", rule4["Value"]);

            _output.WriteLine($"Any/NotAny Operators Query Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void ExpressionAnyNotAnyOperators_ShouldCreateCorrectDictionary()
        {
            // Arrange - �إߴ��եΪ� DTO
            var result = FilterDictionaryBuilder.QueryBuilder<TestEntityWithCollections>()
                .WithLogicalOperator(LogicalOperator.And)
                .Any(x => x.Tags)                           // �ˬd Tags ���X�O�_�����󤸯�
                .Any(x => x.Categories, "Premium")          // �ˬd Categories ���X�O�_�]�t "Premium"
                .NotAny(x => x.Flags)                       // ���ˬd Flags ���X�O�_�S�����󤸯�
                .NotAny(x => x.Labels, "Deprecated")        // �ˬd Labels ���X�O�_���]�t "Deprecated"
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
            // Arrange & Act - ���թҦ��s�W���ާ@��
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .NotEqual(x => x.CaseOwner, "TestUser")
                .NotContains(x => x.LandNo, "TEMP")
                .NotIn(x => x.CityCode, new[] { "INVALID", "TEST" })
                .NotLike(x => x.LandNo, "%DRAFT%")
                .NotBetween(x => x.Price, 0, 100)
                .Any(x => x.LandNo)                       // ���] LandNo �i��O���X
                .NotAny(x => x.CityCode, "BLACKLISTED")   // ���] CityCode �i��O���X
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            Assert.Equal(7, rules.Count);

            // �ˬd NotEqual �W�h
            var rule1 = (Dictionary<string, object>)rules[0];
            Assert.Equal(FilterOperator.NotEqual, rule1["Operator"]);

            // �ˬd NotContains �W�h
            var rule2 = (Dictionary<string, object>)rules[1];
            Assert.Equal(FilterOperator.NotContains, rule2["Operator"]);

            // �ˬd NotIn �W�h
            var rule3 = (Dictionary<string, object>)rules[2];
            Assert.Equal(FilterOperator.NotIn, rule3["Operator"]);

            // �ˬd NotLike �W�h
            var rule4 = (Dictionary<string, object>)rules[3];
            Assert.Equal(FilterOperator.NotLike, rule4["Operator"]);

            // �ˬd NotBetween �W�h
            var rule5 = (Dictionary<string, object>)rules[4];
            Assert.Equal(FilterOperator.NotBetween, rule5["Operator"]);

            // �ˬd Any �W�h
            var rule6 = (Dictionary<string, object>)rules[5];
            Assert.Equal(FilterOperator.Any, rule6["Operator"]);

            // �ˬd NotAny �W�h
            var rule7 = (Dictionary<string, object>)rules[6];
            Assert.Equal(FilterOperator.NotAny, rule7["Operator"]);

            _output.WriteLine($"All New Operators Query Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        #region �s�W�����ծר�

        [Fact]
        public void ArrayNavigationPropertyQuery_ShouldCreateCorrectDictionary()
        {
            // Arrange & Act - ���հ}�C�����ݩʬd��
            var result = FilterDictionaryBuilder.QueryBuilder<Contract>()
                .WithLogicalOperator(LogicalOperator.Or)
                .Like("BuildContracts[].Build.HosAddress", "�x�_��")
                .Like("BuildContracts[].Build.BuildName", "�j��")
                .Equal("BuildContracts[].Build.Floors[].Units[].UnitNo", "A101")
                .GreaterThan("BuildContracts[].SubContracts[].ContractorName", "ABC���q")
                .Build();

            // Assert
            Assert.Equal(LogicalOperator.Or, result["LogicalOperator"]);
            var rules = (List<object>)result["Rules"];
            Assert.Equal(4, rules.Count);

            var rule1 = (Dictionary<string, object>)rules[0];
            Assert.Equal("BuildContracts[].Build.HosAddress", rule1["Property"]);
            Assert.Equal(FilterOperator.Like, rule1["Operator"]);
            Assert.Equal("�x�_��", rule1["Value"]);

            var rule4 = (Dictionary<string, object>)rules[3];
            Assert.Equal("BuildContracts[].SubContracts[].ContractorName", rule4["Property"]);
            Assert.Equal(FilterOperator.GreaterThan, rule4["Operator"]);
            Assert.Equal("ABC���q", rule4["Value"]);

            _output.WriteLine($"Array Navigation Property Query Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void MultiLevelArrayNavigation_ShouldCreateCorrectDictionary()
        {
            // Arrange & Act - ���զh�h�}�C����
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
            // Arrange & Act - ���� InterOperator �]�w
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
            // Arrange & Act - ���չw�] InterOperator ���|�X�{�b���G��
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.Or)
                // ���]�w InterOperator�A���Өϥιw�]�� And
                .Equal(nameof(LandDTO.CaseOwner), "John")
                .Build();

            // Assert
            Assert.Equal(LogicalOperator.Or, result["LogicalOperator"]);
            Assert.False(result.ContainsKey("InterOperator")); // �w�]�Ȥ����ӥX�{

            _output.WriteLine($"Default InterOperator Query Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void GroupNegation_ShouldBeSetCorrectly()
        {
            // Arrange & Act - ���ոs�կŧO���_�w
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
            // Arrange & Act - ���� IsNegated = false �ɤ��X�{
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .Negate(false)
                .Equal(nameof(LandDTO.CaseOwner), "John")
                .Build();

            // Assert
            Assert.False(result.ContainsKey("IsNegated")); // false �ɤ����ӥX�{

            _output.WriteLine($"No Group Negation Query Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void EmptyBuilder_ShouldCreateMinimalDictionary()
        {
            // Arrange & Act - ���ժŪ� Builder
            var result = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .Build();

            // Assert
            Assert.Equal(LogicalOperator.And, result["LogicalOperator"]); // �w�]��
            var rules = (List<object>)result["Rules"];
            Assert.Empty(rules);
            Assert.False(result.ContainsKey("InterOperator")); // �w�]�Ȥ��X�{
            Assert.False(result.ContainsKey("IsNegated")); // �w�]�Ȥ��X�{

            _output.WriteLine($"Empty Builder Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void PropertyComparisonWithNegation_ShouldCreateCorrectDictionary()
        {
            // Arrange & Act - ���ձa�_�w���ݩʤ��
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
            // Arrange & Act - ���� Expression �������a�_�w�ݩʤ��
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
            // Arrange & Act & Assert - ���յL�Ī� Expression
            Assert.Throws<ArgumentException>(() =>
            {
                // �o�|���ͤ@�Ӥ��O MemberExpression �� UnaryExpression ����F��
                FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                    .Add(x => "ConstantString", FilterOperator.Equal, "test");
            });
        }

        [Fact]
        public void DeepNestedCompare_ShouldCreateCorrectStructure()
        {
            // Arrange & Act - ���ղ`�h�_���� Compare
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

            // �ˬd�ĤG�ӳW�h�O�_���_���s��
            var level2Group = (Dictionary<string, object>)level1Rules[1];
            Assert.Equal(LogicalOperator.And, level2Group["LogicalOperator"]);
            var level2Rules = (List<object>)level2Group["Rules"];
            Assert.Equal(2, level2Rules.Count);

            // �ˬd�ĤT�h�_��
            var level3Group = (Dictionary<string, object>)level2Rules[1];
            Assert.Equal(LogicalOperator.Or, level3Group["LogicalOperator"]);

            _output.WriteLine($"Deep Nested Compare Result: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }

        [Fact]
        public void NestedCompareWithIsNegatedUpdate_ShouldSetCorrectly()
        {
            // Arrange & Act - ���ձ_�� Compare �� IsNegated ����s�޿�
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
            // Arrange & Act - �����즡��k�եάO�_���T���@���A
            var builder = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.Or)
                .WithInterOperator(LogicalOperator.Or) // �]�w���D�w�]�ȡA�T�O�|�X�{�b���G��
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
            // Arrange & Act - ���ղV�X Expression �M�r������ݩ�
            var result = FilterDictionaryBuilder.QueryBuilder<Contract>()
                .WithLogicalOperator(LogicalOperator.Or)
                .Like("BuildContracts[].Build.HosAddress", "�x�_")  // �r�ꪩ��
                .Equal(x => x.ContractNo, "C001")                  // Expression ����
                .Contains("BuildContracts[].SubContracts[].ContractorName", "�س]") // �r�ꪩ��
                .GreaterThan(x => x.Detail.TotalAmount, 500000m)   // Expression ����
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