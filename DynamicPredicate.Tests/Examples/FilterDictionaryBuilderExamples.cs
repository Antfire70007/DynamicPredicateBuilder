using System;
using System.Collections.Generic;
using DynamicPredicateBuilder;
using DynamicPredicateBuilder.Models;
using Newtonsoft.Json;

namespace DynamicPredicate.Tests.Examples
{
    /// <summary>
    /// FilterDictionaryBuilder 使用範例
    /// </summary>
    public class FilterDictionaryBuilderExamples
    {
        // 測試用的 DTO 類別
        public class LandDTO
        {
            public string LandNo { get; set; }
            public string CityCode { get; set; }
            public string CaseOwner { get; set; }
            public decimal Price { get; set; }
            public decimal? EstimatedValue { get; set; }  // 新增的 nullable decimal 欄位
            public DateTime CreateDate { get; set; }
            public List<string> Tags { get; set; }
        }

        /// <summary>
        /// 簡單查詢範例 - 符合用戶原本的需求
        /// </summary>
        public static void SimpleQueryExample()
        {
            var landNo = "A123";
            var city = "TPE";

            // 使用 FilterDictionaryBuilder 的新語法
            var queryDict = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.Or)
                .Like(nameof(LandDTO.LandNo), landNo)
                .Like(nameof(LandDTO.CityCode), city)
                .Build();

            Console.WriteLine("=== 簡單查詢範例 ===");
            Console.WriteLine(JsonConvert.SerializeObject(queryDict, Formatting.Indented));

            // 對比原本的寫法
            var originalDict = new List<object>();
            originalDict.Add(new Dictionary<string, object>
            {
                { "Property", nameof(LandDTO.LandNo) },
                { "Operator", FilterOperator.Like },
                { "Value", landNo }
            });

            originalDict.Add(new Dictionary<string, object>
            {
                { "Property", nameof(LandDTO.CityCode) },
                { "Operator", FilterOperator.Like },
                { "Value", city }
            });

            var rawDict = new Dictionary<string, object>
            {
                { "LogicalOperator", LogicalOperator.Or},
                { "Rules", originalDict }
            };

            Console.WriteLine("\n=== 原本的寫法 ===");
            Console.WriteLine(JsonConvert.SerializeObject(rawDict, Formatting.Indented));
        }

        /// <summary>
        /// Nullable Decimal 查詢範例 - 展示如何處理可為 null 的 decimal 欄位
        /// </summary>
        public static void NullableDecimalQueryExample()
        {
            Console.WriteLine("=== Nullable Decimal 查詢範例 ===");

            // 範例 1: 查詢有估價值的土地
            var queryWithValue = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .GreaterThan(x => x.EstimatedValue, 0)  // 估價值大於 0
                .LessThanOrEqual(x => x.EstimatedValue, 10000000)  // 估價值小於等於 1000 萬
                .Build();

            Console.WriteLine("查詢有估價值的土地 (0 < EstimatedValue <= 10,000,000):");
            Console.WriteLine(JsonConvert.SerializeObject(queryWithValue, Formatting.Indented));

            // 範例 2: 查詢沒有估價值的土地 (null 值)
            var queryWithoutValue = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .Equal(x => x.EstimatedValue, null)  // 估價值為 null
                .GreaterThan(x => x.Price, 500000)   // 但實際價格大於 50 萬
                .Build();

            Console.WriteLine("\n查詢沒有估價值的土地 (EstimatedValue is null but Price > 500,000):");
            Console.WriteLine(JsonConvert.SerializeObject(queryWithoutValue, Formatting.Indented));

            // 範例 3: 複合條件 - 有估價值或高價格
            var queryComplexCondition = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.Or)
                .Compare(LogicalOperator.And, rules => rules
                    .GreaterThan(x => x.EstimatedValue, 5000000)  // 估價值大於 500 萬
                    .LessThan(x => x.EstimatedValue, 20000000)    // 且小於 2000 萬
                )
                .GreaterThan(x => x.Price, 15000000)  // 或實際價格大於 1500 萬
                .Build();

            Console.WriteLine("\n複合條件查詢 (高估價值範圍 OR 高實際價格):");
            Console.WriteLine(JsonConvert.SerializeObject(queryComplexCondition, Formatting.Indented));

            // 範例 4: 估價值與實際價格的比較
            var queryPriceComparison = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .Compare(LogicalOperator.Or, priceRules => priceRules
                    .Equal(x => x.EstimatedValue, null)  // 沒有估價值
                    .AddPropertyComparison(x => x.EstimatedValue, FilterOperator.GreaterThan, x => x.Price)  // 或估價值高於實際價格
                )
                .In(x => x.CityCode, new[] { "TPE", "KHH", "TCH" })  // 限定在主要城市
                .Build();

            Console.WriteLine("\n價格比較查詢 (無估價值 OR 估價值 > 實際價格，且在主要城市):");
            Console.WriteLine(JsonConvert.SerializeObject(queryPriceComparison, Formatting.Indented));

            // 範例 5: Between 操作符與 nullable decimal
            var queryBetweenRange = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .Between(x => x.EstimatedValue, 1000000, 5000000)  // 估價值在 100-500 萬之間
                .Like(x => x.LandNo, "A%")  // 地號以 A 開頭
                .Contains(x => x.Tags, "優質地段")  // 包含優質地段標籤
                .Build();

            Console.WriteLine("\n範圍查詢 (估價值在 100-500 萬之間，地號以 A 開頭，優質地段):");
            Console.WriteLine(JsonConvert.SerializeObject(queryBetweenRange, Formatting.Indented));

            // 範例 6: 否定查詢 - 排除特定估價值範圍
            var queryNegation = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .Between(x => x.EstimatedValue, 8000000, 12000000, isNegated: true)  // 排除 800-1200 萬範圍
                .GreaterThan(x => x.Price, 0)  // 實際價格大於 0
                .Build();

            Console.WriteLine("\n否定查詢 (排除估價值 800-1200 萬範圍):");
            Console.WriteLine(JsonConvert.SerializeObject(queryNegation, Formatting.Indented));
        }

        /// <summary>
        /// 巢狀查詢範例 - 符合用戶的進階需求
        /// </summary>
        public static void NestedQueryExample()
        {
            var landNo = "A123";
            var city = "TPE";
            var owner = "John Doe";

            // 多層次的查詢方式
            var queryDict = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.Or)
                .Like(nameof(LandDTO.LandNo), landNo)
                .Like(nameof(LandDTO.CityCode), city)
                .Compare(LogicalOperator.And, rules => rules
                    .Equal(nameof(LandDTO.CaseOwner), owner)
                    .GreaterThan(nameof(LandDTO.Price), 1000000)
                )
                .Build();

            Console.WriteLine("=== 巢狀查詢範例 ===");
            Console.WriteLine(JsonConvert.SerializeObject(queryDict, Formatting.Indented));
        }

        /// <summary>
        /// 複雜查詢範例 - 展示各種運算子和巢狀結構
        /// </summary>
        public static void ComplexQueryExample()
        {
            var queryDict = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .Compare(LogicalOperator.Or, mainRules => mainRules
                    // 條件組 1: 特定地號或城市
                    .Like(nameof(LandDTO.LandNo), "A123")
                    .In(nameof(LandDTO.CityCode), new[] { "TPE", "KHH", "TCH" })
                    // 條件組 2: 特殊業主且高價
                    .Compare(LogicalOperator.And, specialRules => specialRules
                        .Equal(nameof(LandDTO.CaseOwner), "VIP Owner")
                        .Between(nameof(LandDTO.Price), 5000000, 50000000)
                    )
                )
                // 必須符合的時間範圍
                .Compare(LogicalOperator.And, timeRules => timeRules
                    .GreaterThan(nameof(LandDTO.CreateDate), DateTime.Now.AddMonths(-6))
                    .LessThan(nameof(LandDTO.CreateDate), DateTime.Now)
                )
                // 必須有特定標籤
                .Contains(nameof(LandDTO.Tags), "優質地段")
                .Build();

            Console.WriteLine("=== 複雜查詢範例 ===");
            Console.WriteLine(JsonConvert.SerializeObject(queryDict, Formatting.Indented));
        }

        /// <summary>
        /// 否定查詢範例
        /// </summary>
        public static void NegationQueryExample()
        {
            var queryDict = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                // 不是特定業主
                .Equal(nameof(LandDTO.CaseOwner), "Blacklisted Owner", isNegated: true)
                // 不在指定價格範圍
                .Compare(LogicalOperator.Or, priceRules => priceRules
                    .LessThan(nameof(LandDTO.Price), 500000)
                    .GreaterThan(nameof(LandDTO.Price), 10000000),
                    isNegated: true)
                // 不包含特定標籤
                .Contains(nameof(LandDTO.Tags), "問題地段", isNegated: true)
                .Build();

            Console.WriteLine("=== 否定查詢範例 ===");
            Console.WriteLine(JsonConvert.SerializeObject(queryDict, Formatting.Indented));
        }

        /// <summary>
        /// 屬性比較範例
        /// </summary>
        public static void PropertyComparisonExample()
        {
            var queryDict = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                // 地號必須與城市代碼相同 (假設的業務邏輯)
                .AddPropertyComparison(nameof(LandDTO.LandNo), FilterOperator.Equal, nameof(LandDTO.CityCode))
                // 其他條件
                .GreaterThan(nameof(LandDTO.Price), 1000000)
                .Build();

            Console.WriteLine("=== 屬性比較範例 ===");
            Console.WriteLine(JsonConvert.SerializeObject(queryDict, Formatting.Indented));
        }

        /// <summary>
        /// 隱式轉換範例
        /// </summary>
        public static void ImplicitConversionExample()
        {
            // 隱式轉換為 Dictionary
            Dictionary<string, object> dict = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .Like(nameof(LandDTO.LandNo), "test")
                .Equal(nameof(LandDTO.CaseOwner), "John");

            // 隱式轉換為 FilterGroup
            FilterGroup group = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .Contains(nameof(LandDTO.CityCode), "taipei")
                .GreaterThan(nameof(LandDTO.Price), 1000000);

            Console.WriteLine("=== 隱式轉換範例 ===");
            Console.WriteLine("轉換為 Dictionary:");
            Console.WriteLine(JsonConvert.SerializeObject(dict, Formatting.Indented));
            Console.WriteLine("\n轉換為 FilterGroup:");
            Console.WriteLine(JsonConvert.SerializeObject(group, Formatting.Indented));
        }

        /// <summary>
        /// Expression 語法範例 - 強型別的屬性選擇
        /// </summary>
        public static void ExpressionSyntaxExample()
        {
            var landNo = "A123";
            var city = "TPE";

            // 使用 Expression 語法的簡單查詢
            var queryDict = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.Or)
                .Like(x => x.LandNo, landNo)
                .Like(x => x.CityCode, city)
                .Build();

            Console.WriteLine("=== Expression 語法範例 ===");
            Console.WriteLine(JsonConvert.SerializeObject(queryDict, Formatting.Indented));
        }

        /// <summary>
        /// Expression 巢狀查詢範例
        /// </summary>
        public static void ExpressionNestedQueryExample()
        {
            var landNo = "A123";
            var city = "TPE";
            var owner = "John Doe";

            // Expression 版本的多層次查詢
            var queryDict = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.Or)
                .Like(x => x.LandNo, landNo)
                .Like(x => x.CityCode, city)
                .Compare(LogicalOperator.And, rules => rules
                    .Equal(x => x.CaseOwner, owner)
                    .GreaterThan(x => x.Price, 1000000)
                    .Between(x => x.CreateDate, DateTime.Now.AddMonths(-6), DateTime.Now)
                )
                .Build();

            Console.WriteLine("=== Expression 巢狀查詢範例 ===");
            Console.WriteLine(JsonConvert.SerializeObject(queryDict, Formatting.Indented));
        }

        /// <summary>
        /// Expression 與字串混合使用範例
        /// </summary>
        public static void MixedSyntaxExample()
        {
            var queryDict = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                // Expression 版本 - 有編譯時期型別檢查
                .Like(x => x.LandNo, "A123")
                .GreaterThan(x => x.Price, 1000000)
                .In(x => x.Tags, new[] { "優質", "推薦" })
                // 字串版本 - 適用於動態屬性名稱
                .Equal(nameof(LandDTO.CaseOwner), "John Doe")
                .Contains("DynamicProperty", "value")  // 假設有動態屬性
                .Build();

            Console.WriteLine("=== Expression 與字串混合使用範例 ===");
            Console.WriteLine(JsonConvert.SerializeObject(queryDict, Formatting.Indented));
        }

        /// <summary>
        /// Expression 版本的所有運算子範例
        /// </summary>
        public static void ExpressionAllOperatorsExample()
        {
            var queryDict = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .Equal(x => x.CaseOwner, "John Doe")
                .Like(x => x.LandNo, "A%")
                .Contains(x => x.CityCode, "TPE")
                .StartsWith(x => x.LandNo, "A")
                .EndsWith(x => x.LandNo, "001")
                .In(x => x.Tags, new[] { "優質", "推薦", "熱門" })
                .Between(x => x.Price, 1000000, 5000000)
                .GreaterThan(x => x.CreateDate, DateTime.Now.AddMonths(-3))
                .LessThanOrEqual(x => x.Price, 10000000)
                .GreaterThanOrEqual(x => x.Price, 500000)
                .Build();

            Console.WriteLine("=== Expression 版本的所有運算子範例 ===");
            Console.WriteLine(JsonConvert.SerializeObject(queryDict, Formatting.Indented));
        }

        /// <summary>
        /// Expression 屬性比較範例
        /// </summary>
        public static void ExpressionPropertyComparisonExample()
        {
            var queryDict = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                // 屬性對屬性比較 (Expression 版本)
                .AddPropertyComparison(x => x.LandNo, FilterOperator.Equal, x => x.CityCode)
                // 混合其他條件
                .GreaterThan(x => x.Price, 1000000)
                .Like(x => x.CaseOwner, "VIP%")
                .Build();

            Console.WriteLine("=== Expression 屬性比較範例 ===");
            Console.WriteLine(JsonConvert.SerializeObject(queryDict, Formatting.Indented));
        }

        /// <summary>
        /// 各種工廠方法範例
        /// </summary>
        public static void FactoryMethodsExample()
        {
            // 方法1: 靜態工廠 
            var query1 = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .Like(nameof(LandDTO.LandNo), "test");

            // 方法2: 另一個靜態工廠
            var query2 = FilterDictionaryBuilder.Create<LandDTO>()
                .Equal(nameof(LandDTO.CaseOwner), "John");

            // 方法3: 泛型類別的靜態方法
            var query3 = FilterDictionaryBuilder<LandDTO>.Create()
                .Contains(nameof(LandDTO.CityCode), "taipei");

            Console.WriteLine("=== 工廠方法範例 ===");
            Console.WriteLine("方法1 結果:");
            Console.WriteLine(JsonConvert.SerializeObject(query1.Build(), Formatting.Indented));
            Console.WriteLine("\n方法2 結果:");
            Console.WriteLine(JsonConvert.SerializeObject(query2.Build(), Formatting.Indented));
            Console.WriteLine("\n方法3 結果:");
            Console.WriteLine(JsonConvert.SerializeObject(query3.Build(), Formatting.Indented));
        }

        /// <summary>
        /// 執行所有範例
        /// </summary>
        public static void RunAllExamples()
        {
            SimpleQueryExample();
            Console.WriteLine("\n" + new string('=', 50) + "\n");

            NullableDecimalQueryExample();
            Console.WriteLine("\n" + new string('=', 50) + "\n");

            NestedQueryExample();
            Console.WriteLine("\n" + new string('=', 50) + "\n");

            ComplexQueryExample();
            Console.WriteLine("\n" + new string('=', 50) + "\n");

            NegationQueryExample();
            Console.WriteLine("\n" + new string('=', 50) + "\n");

            PropertyComparisonExample();
            Console.WriteLine("\n" + new string('=', 50) + "\n");

            ImplicitConversionExample();
            Console.WriteLine("\n" + new string('=', 50) + "\n");

            ExpressionSyntaxExample();
            Console.WriteLine("\n" + new string('=', 50) + "\n");

            ExpressionNestedQueryExample();
            Console.WriteLine("\n" + new string('=', 50) + "\n");

            MixedSyntaxExample();
            Console.WriteLine("\n" + new string('=', 50) + "\n");

            ExpressionAllOperatorsExample();
            Console.WriteLine("\n" + new string('=', 50) + "\n");

            ExpressionPropertyComparisonExample();
            Console.WriteLine("\n" + new string('=', 50) + "\n");

            FactoryMethodsExample();
        }
    }
}