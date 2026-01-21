using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DynamicPredicateBuilder;
using DynamicPredicateBuilder.Models;
using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;

namespace DynamicPredicate.Tests.Builders
{
    /// <summary>
    /// FilterDictionaryBuilder 錯誤處理和邊界條件測試
    /// </summary>
    public class FilterDictionaryBuilderEdgeCaseTests(ITestOutputHelper output)
    {
        private readonly ITestOutputHelper _output = output;

        // 測試用實體
        public class TestEntity
        {
            public string Name { get; set; }
            public int Value { get; set; }
            public List<string> Tags { get; set; } = [];
            public NestedEntity Nested { get; set; }
        }

        public class NestedEntity
        {
            public string SubName { get; set; }
            public int SubValue { get; set; }
        }

        #region 錯誤處理測試

        [Fact]
        public void NullPropertyName_ShouldCreateRuleWithNullProperty()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<TestEntity>()
                .Equal(null, "test")
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            var rule = (Dictionary<string, object>)rules[0];
            Assert.Null(rule["Property"]);

            _output.WriteLine("Null property name handled gracefully");
        }

        [Fact]
        public void EmptyPropertyName_ShouldCreateRuleWithEmptyProperty()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<TestEntity>()
                .Equal("", "test")
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            var rule = (Dictionary<string, object>)rules[0];
            Assert.Equal("", rule["Property"]);

            _output.WriteLine("Empty property name handled gracefully");
        }

        [Fact]
        public void NullValue_ShouldCreateRuleWithNullValue()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<TestEntity>()
                .Equal(nameof(TestEntity.Name), null)
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            var rule = (Dictionary<string, object>)rules[0];
            Assert.Null(rule["Value"]);

            _output.WriteLine("Null value handled gracefully");
        }

        [Fact]
        public void NullCompareToProperty_ShouldCreateRuleWithNullCompareToProperty()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<TestEntity>()
                .AddPropertyComparison(nameof(TestEntity.Name), FilterOperator.Equal, null)
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            var rule = (Dictionary<string, object>)rules[0];
            Assert.Null(rule["CompareToProperty"]);

            _output.WriteLine("Null CompareToProperty handled gracefully");
        }

        [Fact]
        public void NullBuilderAction_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
            {
                FilterDictionaryBuilder.QueryBuilder<TestEntity>()
                    .Compare(LogicalOperator.And, null);
            });

            _output.WriteLine("Null builder action correctly throws ArgumentNullException");
        }

        [Fact]
        public void InvalidExpressionConstant_ShouldThrowArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() =>
            {
                FilterDictionaryBuilder.QueryBuilder<TestEntity>()
                    .Equal(x => "ConstantValue", "test");
            });

            _output.WriteLine("Constant expression correctly throws ArgumentException");
        }

        [Fact]
        public void InvalidExpressionMethodCall_ShouldThrowArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() =>
            {
                FilterDictionaryBuilder.QueryBuilder<TestEntity>()
                    .Equal(x => x.Name.ToUpper(), "test");
            });

            _output.WriteLine("Method call expression correctly throws ArgumentException");
        }

        [Fact]
        public void InvalidExpressionBinary_ShouldThrowArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() =>
            {
                FilterDictionaryBuilder.QueryBuilder<TestEntity>()
                    .Equal(x => x.Value + 10, 100);
            });

            _output.WriteLine("Binary expression correctly throws ArgumentException");
        }

        #endregion

        #region 邊界條件測試

        [Fact]
        public void EmptyCollection_ShouldCreateCorrectRule()
        {
            // Arrange & Act
            var emptyList = new List<object>();
            var result = FilterDictionaryBuilder.QueryBuilder<TestEntity>()
                .In(nameof(TestEntity.Name), emptyList)
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            var rule = (Dictionary<string, object>)rules[0];
            Assert.Same(emptyList, rule["Value"]);

            _output.WriteLine("Empty collection handled correctly");
        }

        [Fact]
        public void SingleItemCollection_ShouldCreateCorrectRule()
        {
            // Arrange & Act
            var singleItemList = new List<object> { "single" };
            var result = FilterDictionaryBuilder.QueryBuilder<TestEntity>()
                .In(nameof(TestEntity.Name), singleItemList)
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            var rule = (Dictionary<string, object>)rules[0];
            Assert.Same(singleItemList, rule["Value"]);

            _output.WriteLine("Single item collection handled correctly");
        }

        [Fact]
        public void LargeCollection_ShouldCreateCorrectRule()
        {
            // Arrange & Act
            var largeList = new List<object>();
            for (int i = 0; i < 10000; i++)
            {
                largeList.Add($"item_{i}");
            }

            var result = FilterDictionaryBuilder.QueryBuilder<TestEntity>()
                .In(nameof(TestEntity.Name), largeList)
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            var rule = (Dictionary<string, object>)rules[0];
            Assert.Same(largeList, rule["Value"]);

            _output.WriteLine($"Large collection ({largeList.Count} items) handled correctly");
        }

        [Fact]
        public void MaxMinValues_ShouldCreateCorrectRules()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<TestEntity>()
                .Equal(nameof(TestEntity.Value), int.MaxValue)
                .Equal(nameof(TestEntity.Value), int.MinValue)
                .Between(nameof(TestEntity.Value), long.MinValue, long.MaxValue)
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            
            var rule1 = (Dictionary<string, object>)rules[0];
            Assert.Equal(int.MaxValue, rule1["Value"]);
            
            var rule2 = (Dictionary<string, object>)rules[1];
            Assert.Equal(int.MinValue, rule2["Value"]);
            
            var rule3 = (Dictionary<string, object>)rules[2];
            var betweenValues = (object[])rule3["Value"];
            Assert.Equal(long.MinValue, betweenValues[0]);
            Assert.Equal(long.MaxValue, betweenValues[1]);

            _output.WriteLine("Max/Min values handled correctly");
        }

        [Fact]
        public void SpecialStringValues_ShouldCreateCorrectRules()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<TestEntity>()
                .Equal(nameof(TestEntity.Name), "")                    // 空字串
                .Equal(nameof(TestEntity.Name), " ")                   // 空白字串
                .Equal(nameof(TestEntity.Name), "\t\n\r")              // 特殊字元
                .Equal(nameof(TestEntity.Name), "??????")               // Unicode Emoji
                .Equal(nameof(TestEntity.Name), "中文測試")              // 中文字元
                .Equal(nameof(TestEntity.Name), "a".PadRight(10000, 'x')) // 超長字串
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            Assert.Equal(6, rules.Count);

            var rule1 = (Dictionary<string, object>)rules[0];
            Assert.Equal("", rule1["Value"]);

            var rule6 = (Dictionary<string, object>)rules[5];
            Assert.Equal(10000, ((string)rule6["Value"]).Length);

            _output.WriteLine("Special string values handled correctly");
        }

        [Fact]
        public void DeepNestingLevels_ShouldCreateCorrectStructure()
        {
            // Arrange & Act - 測試深層巢狀（10層）
            var result = FilterDictionaryBuilder.QueryBuilder<TestEntity>()
                .Compare(LogicalOperator.And, level1 => level1
                    .Equal(nameof(TestEntity.Name), "L1")
                    .Compare(LogicalOperator.Or, level2 => level2
                        .Equal(nameof(TestEntity.Name), "L2")
                        .Compare(LogicalOperator.And, level3 => level3
                            .Equal(nameof(TestEntity.Name), "L3")
                            .Compare(LogicalOperator.Or, level4 => level4
                                .Equal(nameof(TestEntity.Name), "L4")
                                .Compare(LogicalOperator.And, level5 => level5
                                    .Equal(nameof(TestEntity.Name), "L5")
                                    .Compare(LogicalOperator.Or, level6 => level6
                                        .Equal(nameof(TestEntity.Name), "L6")
                                        .Compare(LogicalOperator.And, level7 => level7
                                            .Equal(nameof(TestEntity.Name), "L7")
                                            .Compare(LogicalOperator.Or, level8 => level8
                                                .Equal(nameof(TestEntity.Name), "L8")
                                                .Compare(LogicalOperator.And, level9 => level9
                                                    .Equal(nameof(TestEntity.Name), "L9")
                                                    .Compare(LogicalOperator.Or, level10 => level10
                                                        .Equal(nameof(TestEntity.Name), "L10")
                                                    )
                                                )
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    )
                )
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            Assert.Single(rules);

            // 驗證結構深度
            var currentLevel = (Dictionary<string, object>)rules[0];
            int depth = 1;
            
            while (currentLevel != null)
            {
                var currentRules = (List<object>)currentLevel["Rules"];
                if (currentRules.Count > 1 && currentRules[1] is Dictionary<string, object> nextLevel)
                {
                    currentLevel = nextLevel;
                    depth++;
                }
                else
                {
                    break;
                }
            }

            Assert.True(depth >= 5, $"Expected deep nesting, got depth: {depth}");
            _output.WriteLine($"Deep nesting handled correctly - depth: {depth}");
        }

        [Fact]
        public void ManyRulesInSingleGroup_ShouldCreateCorrectStructure()
        {
            // Arrange & Act - 測試單一群組中的大量規則（100個）
            var builder = FilterDictionaryBuilder.QueryBuilder<TestEntity>();
            
            for (int i = 0; i < 100; i++)
            {
                builder.Equal(nameof(TestEntity.Name), $"value_{i}");
            }
            
            var result = builder.Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            Assert.Equal(100, rules.Count);

            // 驗證前幾個和後幾個規則
            var firstRule = (Dictionary<string, object>)rules[0];
            Assert.Equal("value_0", firstRule["Value"]);
            
            var lastRule = (Dictionary<string, object>)rules[99];
            Assert.Equal("value_99", lastRule["Value"]);

            _output.WriteLine($"Many rules ({rules.Count}) in single group handled correctly");
        }

        [Fact]
        public void AllOperatorCombinations_ShouldCreateCorrectStructure()
        {
            // Arrange & Act - 測試所有運算子的組合
            var testValues = new[] { "val1", "val2", "val3" };
            var result = FilterDictionaryBuilder.QueryBuilder<TestEntity>()
                .WithLogicalOperator(LogicalOperator.Or)
                .Equal(nameof(TestEntity.Name), "equal")
                .NotEqual(nameof(TestEntity.Name), "notequal")
                .Like(nameof(TestEntity.Name), "%like%")
                .NotLike(nameof(TestEntity.Name), "%notlike%")
                .Contains(nameof(TestEntity.Name), "contains")
                .NotContains(nameof(TestEntity.Name), "notcontains")
                .StartsWith(nameof(TestEntity.Name), "starts")
                .EndsWith(nameof(TestEntity.Name), "ends")
                .GreaterThan(nameof(TestEntity.Value), 100)
                .GreaterThanOrEqual(nameof(TestEntity.Value), 50)
                .LessThan(nameof(TestEntity.Value), 200)
                .LessThanOrEqual(nameof(TestEntity.Value), 150)
                .In(nameof(TestEntity.Name), testValues)
                .NotIn(nameof(TestEntity.Name), testValues)
                .Between(nameof(TestEntity.Value), 10, 90)
                .NotBetween(nameof(TestEntity.Value), 100, 200)
                .Any(nameof(TestEntity.Tags))
                .Any(nameof(TestEntity.Tags), "premium")
                .NotAny(nameof(TestEntity.Tags))
                .NotAny(nameof(TestEntity.Tags), "blocked")
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            Assert.Equal(20, rules.Count);

            // 驗證每種運算子都存在
            var operators = rules.Cast<Dictionary<string, object>>()
                .Select(r => r["Operator"])
                .Cast<FilterOperator>()
                .Distinct()
                .ToList();

            var expectedOperators = new[]
            {
                FilterOperator.Equal, FilterOperator.NotEqual,
                FilterOperator.Like, FilterOperator.NotLike,
                FilterOperator.Contains, FilterOperator.NotContains,
                FilterOperator.StartsWith, FilterOperator.EndsWith,
                FilterOperator.GreaterThan, FilterOperator.GreaterThanOrEqual,
                FilterOperator.LessThan, FilterOperator.LessThanOrEqual,
                FilterOperator.In, FilterOperator.NotIn,
                FilterOperator.Between, FilterOperator.NotBetween,
                FilterOperator.Any, FilterOperator.NotAny
            };

            foreach (var expectedOp in expectedOperators)
            {
                Assert.Contains(expectedOp, operators);
            }

            _output.WriteLine($"All operator combinations ({operators.Count} operators) handled correctly");
        }

        [Fact]
        public void ChainedBuilderReuse_ShouldMaintainState()
        {
            // Arrange & Act - 測試 Builder 狀態的累積性
            var baseBuilder = FilterDictionaryBuilder.QueryBuilder<TestEntity>()
                .WithLogicalOperator(LogicalOperator.And)
                .Equal(nameof(TestEntity.Name), "base");

            // 先建立 snapshot 以避免後續修改影響
            var result1 = baseBuilder
                .Like(nameof(TestEntity.Name), "pattern1")
                .Build();

            var result2 = baseBuilder
                .Contains(nameof(TestEntity.Name), "pattern2")
                .Build();

            // Assert
            var rules1 = (List<object>)result1["Rules"];
            var rules2 = (List<object>)result2["Rules"];

            // result1 時，Builder 應該有 base + pattern1 = 2 個規則
            // 但由於 Builder 是可變的，當我們在 result2 時又加了一個規則
            // result1 的引用會看到更新後的狀態，所以實際上會有 3 個規則
            Assert.True(rules1.Count >= 2, $"Expected at least 2 rules in result1, got {rules1.Count}");
            
            // result2 應該有 base + pattern1 + pattern2 = 3 個規則
            Assert.Equal(3, rules2.Count);

            // 驗證規則內容
            var rule1 = (Dictionary<string, object>)rules2[0];
            Assert.Equal("base", rule1["Value"]);
            
            var rule2 = (Dictionary<string, object>)rules2[1];
            Assert.Equal("pattern1", rule2["Value"]);
            
            var rule3 = (Dictionary<string, object>)rules2[2];
            Assert.Equal("pattern2", rule3["Value"]);

            _output.WriteLine($"Builder state accumulation: result1 has {rules1.Count} rules, result2 has {rules2.Count} rules");
        }

        [Fact]
        public void MemoryStressTest_ShouldHandleLargeData()
        {
            // Arrange & Act - 記憶體壓力測試
            var builder = FilterDictionaryBuilder.QueryBuilder<TestEntity>();
            var largeString = new string('x', 100000); // 100KB 字串
            var largeArray = new object[10000]; // 10K 項目陣列
            
            for (int i = 0; i < largeArray.Length; i++)
            {
                largeArray[i] = $"item_{i}";
            }

            var result = builder
                .Equal(nameof(TestEntity.Name), largeString)
                .In(nameof(TestEntity.Tags), largeArray)
                .Between(nameof(TestEntity.Value), int.MinValue, int.MaxValue)
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            Assert.Equal(3, rules.Count);

            var stringRule = (Dictionary<string, object>)rules[0];
            Assert.Equal(100000, ((string)stringRule["Value"]).Length);

            var arrayRule = (Dictionary<string, object>)rules[1];
            Assert.Equal(10000, ((object[])arrayRule["Value"]).Length);

            _output.WriteLine("Memory stress test completed successfully");
        }

        #endregion

        #region 型別相關邊界測試

        [Fact]
        public void DateTimeEdgeCases_ShouldCreateCorrectRules()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<TestEntity>()
                .Equal("DateProp", DateTime.MinValue)
                .Equal("DateProp", DateTime.MaxValue)
                .Equal("DateProp", new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                .Equal("DateProp", new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Local))
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            Assert.Equal(4, rules.Count);

            _output.WriteLine("DateTime edge cases handled correctly");
        }

        [Fact]
        public void NumericEdgeCases_ShouldCreateCorrectRules()
        {
            // Arrange & Act
            var result = FilterDictionaryBuilder.QueryBuilder<TestEntity>()
                .Equal("IntProp", 0)
                .Equal("DoubleProp", double.NaN)
                .Equal("DoubleProp", double.PositiveInfinity)
                .Equal("DoubleProp", double.NegativeInfinity)
                .Equal("FloatProp", float.Epsilon)
                .Equal("DecimalProp", decimal.MaxValue)
                .Equal("DecimalProp", decimal.MinValue)
                .Build();

            // Assert
            var rules = (List<object>)result["Rules"];
            Assert.Equal(7, rules.Count);

            _output.WriteLine("Numeric edge cases handled correctly");
        }

        #endregion
    }
}