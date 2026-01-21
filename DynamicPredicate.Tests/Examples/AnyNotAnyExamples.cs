using System;
using System.Collections.Generic;
using System.Linq;
using DynamicPredicateBuilder;
using DynamicPredicateBuilder.Models;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;

namespace DynamicPredicate.Tests.Examples
{
    /// <summary>
    /// Any 和 NotAny 操作符的使用範例
    /// </summary>
    public class AnyNotAnyExamples(ITestOutputHelper output)
    {

        // 測試用的實體類別
        public class Product
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public List<string> Tags { get; set; } = [];
            public List<string> Categories { get; set; } = [];
            public List<int> Ratings { get; set; } = [];
            public decimal Price { get; set; }
        }

        [Fact]
        public void Example_AnyOperator_CheckCollectionHasElements()
        {
            output.WriteLine("=== Any 操作符：檢查集合是否有任何元素 ===");

            // 建立測試資料
            var products = new List<Product>
            {
                new() { Id = 1, Name = "Laptop", Tags = ["Electronics", "Computer"] },
                new() { Id = 2, Name = "Book", Tags = [] },  // 空集合
                new() { Id = 3, Name = "Phone", Tags = null }    // null 集合
            };

            // 使用 FilterDictionaryBuilder：查詢有任何標籤的產品
            var filterGroup = FilterDictionaryBuilder.QueryBuilder<Product>()
                .Any(x => x.Tags)  // value 為 null，檢查集合是否有任何元素
                .ToFilterGroup();

            var predicate = FilterBuilder.Build<Product>(filterGroup).Compile();
            var results = products.Where(predicate).ToList();

            // 只有 Laptop 有標籤
            results.Should().HaveCount(1);
            results[0].Name.Should().Be("Laptop");

            output.WriteLine($"找到 {results.Count} 個有標籤的產品：");
            foreach (var product in results)
            {
                output.WriteLine($"- {product.Name}: [{string.Join(", ", product.Tags ?? [])}]");
            }
        }

        [Fact]
        public void Example_AnyOperator_CheckCollectionContainsSpecificValue()
        {
            output.WriteLine("=== Any 操作符：檢查集合是否包含特定值 ===");

            // 建立測試資料
            var products = new List<Product>
            {
                new() { Id = 1, Name = "Gaming Laptop", Tags = ["Electronics", "Gaming", "Computer"] },
                new() { Id = 2, Name = "Office Laptop", Tags = ["Electronics", "Business", "Computer"] },
                new() { Id = 3, Name = "Gaming Mouse", Tags = ["Gaming", "Accessories"] },
                new() { Id = 4, Name = "Office Chair", Tags = ["Furniture", "Business"] }
            };

            // 使用 FilterDictionaryBuilder：查詢包含 "Gaming" 標籤的產品
            var filterGroup = FilterDictionaryBuilder.QueryBuilder<Product>()
                .Any(x => x.Tags, "Gaming")  // 檢查 Tags 集合是否包含 "Gaming"
                .ToFilterGroup();

            var predicate = FilterBuilder.Build<Product>(filterGroup).Compile();
            var results = products.Where(predicate).ToList();

            // 應該找到 2 個遊戲相關產品
            results.Should().HaveCount(2);
            results.Should().Contain(p => p.Name == "Gaming Laptop");
            results.Should().Contain(p => p.Name == "Gaming Mouse");

            output.WriteLine($"找到 {results.Count} 個包含 'Gaming' 標籤的產品：");
            foreach (var product in results)
            {
                output.WriteLine($"- {product.Name}: [{string.Join(", ", product.Tags)}]");
            }
        }

        [Fact]
        public void Example_NotAnyOperator_CheckCollectionIsEmpty()
        {
            output.WriteLine("=== NotAny 操作符：檢查集合是否為空 ===");

            // 建立測試資料
            var products = new List<Product>
            {
                new() { Id = 1, Name = "New Product", Categories = [] },     // 空集合
                new() { Id = 2, Name = "Draft Product", Categories = null },    // null 集合
                new() { Id = 3, Name = "Published Product", Categories = ["Electronics"] }
            };

            // 使用 FilterDictionaryBuilder：查詢沒有分類的產品
            var filterGroup = FilterDictionaryBuilder.QueryBuilder<Product>()
                .NotAny(x => x.Categories)  // value 為 null，檢查集合是否沒有任何元素
                .ToFilterGroup();

            var predicate = FilterBuilder.Build<Product>(filterGroup).Compile();
            var results = products.Where(predicate).ToList();

            // 應該找到 2 個沒有分類的產品
            results.Should().HaveCount(2);
            results.Should().Contain(p => p.Name == "New Product");
            results.Should().Contain(p => p.Name == "Draft Product");

            output.WriteLine($"找到 {results.Count} 個沒有分類的產品：");
            foreach (var product in results)
            {
                var categories = product.Categories?.Count > 0 ? string.Join(", ", product.Categories) : "無";
                output.WriteLine($"- {product.Name}: [{categories}]");
            }
        }

        [Fact]
        public void Example_NotAnyOperator_CheckCollectionDoesNotContainValue()
        {
            output.WriteLine("=== NotAny 操作符：檢查集合是否不包含特定值 ===");

            // 建立測試資料
            var products = new List<Product>
            {
                new() { Id = 1, Name = "Safe Product", Tags = ["Electronics", "Safe"] },
                new() { Id = 2, Name = "Another Safe Product", Tags = ["Furniture"] },
                new() { Id = 3, Name = "Dangerous Product", Tags = ["Electronics", "Dangerous"] },
                new() { Id = 4, Name = "No Tags Product", Tags = [] }
            };

            // 使用 FilterDictionaryBuilder：查詢不包含 "Dangerous" 標籤的產品
            var filterGroup = FilterDictionaryBuilder.QueryBuilder<Product>()
                .NotAny(x => x.Tags, "Dangerous")  // 檢查 Tags 集合是否不包含 "Dangerous"
                .ToFilterGroup();

            var predicate = FilterBuilder.Build<Product>(filterGroup).Compile();
            var results = products.Where(predicate).ToList();

            // 應該找到 3 個安全產品
            results.Should().HaveCount(3);
            results.Should().NotContain(p => p.Name == "Dangerous Product");

            output.WriteLine($"找到 {results.Count} 個不包含 'Dangerous' 標籤的產品：");
            foreach (var product in results)
            {
                var tags = product.Tags?.Count > 0 ? string.Join(", ", product.Tags) : "無";
                output.WriteLine($"- {product.Name}: [{tags}]");
            }
        }

        [Fact]
        public void Example_ComplexAnyNotAnyConditions()
        {
            output.WriteLine("=== 複雜的 Any/NotAny 條件組合 ===");

            // 建立測試資料
            var products = new List<Product>
            {
                new() { Id = 1, Name = "Premium Gaming Laptop", 
                        Tags = ["Electronics", "Gaming", "Premium"],
                        Categories = ["Computer", "Gaming"],
                        Ratings = [5, 4, 5] },
                new() { Id = 2, Name = "Budget Office Laptop", 
                        Tags = ["Electronics", "Budget"],
                        Categories = ["Computer", "Office"],
                        Ratings = [3, 3, 4] },
                new() { Id = 3, Name = "Dangerous Gadget", 
                        Tags = ["Electronics", "Dangerous"],
                        Categories = ["Gadget"],
                        Ratings = [1, 2] }
            };

            // 複雜查詢：
            // 1. 必須有評級（Ratings 不為空）
            // 2. 必須包含 "Premium" 標籤
            // 3. 不能包含 "Dangerous" 標籤
            // 4. 評級中必須有 5 分
            var filterGroup = FilterDictionaryBuilder.QueryBuilder<Product>()
                .WithLogicalOperator(LogicalOperator.And)
                .Any(x => x.Ratings)                    // 有評級
                .Any(x => x.Tags, "Premium")            // 包含 Premium 標籤
                .NotAny(x => x.Tags, "Dangerous")       // 不包含 Dangerous 標籤
                .Any(x => x.Ratings, 5)                 // 評級中有 5 分
                .ToFilterGroup();

            var predicate = FilterBuilder.Build<Product>(filterGroup).Compile();
            var results = products.Where(predicate).ToList();

            // 只有 Premium Gaming Laptop 符合所有條件
            results.Should().HaveCount(1);
            results[0].Name.Should().Be("Premium Gaming Laptop");

            output.WriteLine($"找到 {results.Count} 個符合複雜條件的產品：");
            foreach (var product in results)
            {
                output.WriteLine($"- {product.Name}:");
                output.WriteLine($"  標籤: [{string.Join(", ", product.Tags)}]");
                output.WriteLine($"  分類: [{string.Join(", ", product.Categories)}]");
                output.WriteLine($"  評級: [{string.Join(", ", product.Ratings)}]");
            }
        }

        [Fact]
        public void Example_UsingFilterRules_DirectlyWithAnyNotAny()
        {
            output.WriteLine("=== 直接使用 FilterRule 的 Any/NotAny 操作符 ===");

            // 建立測試資料
            var products = new List<Product>
            {
                new() { Id = 1, Name = "Electronics", Tags = ["Tech", "Popular"] },
                new() { Id = 2, Name = "Book", Tags = ["Education"] },
                new() { Id = 3, Name = "Empty Product", Tags = [] }
            };

            // 使用 FilterRule 直接建立條件
            var filterGroup = new FilterGroup
            {
                LogicalOperator = LogicalOperator.Or,
                Rules =
                [
                    // 檢查 Tags 是否有任何元素
                    new FilterRule 
                    { 
                        Property = "Tags", 
                        Operator = FilterOperator.Any, 
                        Value = null 
                    },
                    // 檢查 Tags 是否包含 "Popular"
                    new FilterRule 
                    { 
                        Property = "Tags", 
                        Operator = FilterOperator.Any, 
                        Value = "Popular" 
                    }
                ]
            };

            var predicate = FilterBuilder.Build<Product>(filterGroup).Compile();
            var results = products.Where(predicate).ToList();

            // 所有有標籤的產品都會被找到
            results.Should().HaveCount(2);
            results.Should().Contain(p => p.Name == "Electronics");
            results.Should().Contain(p => p.Name == "Book");

            output.WriteLine($"找到 {results.Count} 個符合條件的產品：");
            foreach (var product in results)
            {
                var tags = product.Tags?.Count > 0 ? string.Join(", ", product.Tags) : "無";
                output.WriteLine($"- {product.Name}: [{tags}]");
            }
        }
    }
}