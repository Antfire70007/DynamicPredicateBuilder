using System;
using DynamicPredicateBuilder;
using DynamicPredicateBuilder.Models;
using Newtonsoft.Json;

namespace DynamicPredicate.Tests.Examples
{
    /// <summary>
    /// Expression 語法使用示範
    /// </summary>
    public class ExpressionSyntaxDemo
    {
        public class LandDTO
        {
            public string LandNo { get; set; }
            public string CityCode { get; set; }
            public string CaseOwner { get; set; }
            public decimal Price { get; set; }
            public DateTime CreateDate { get; set; }
        }

        /// <summary>
        /// 展示 Expression 語法的強大功能
        /// </summary>
        public static void DemoExpressionSyntax()
        {
            Console.WriteLine("=== FilterDictionaryBuilder Expression 語法示範 ===\n");

            // 符合用戶原始需求：支援 x => x.Property 語法
            var query = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .Compare(LogicalOperator.Or, rules => rules
                    .Like(x => x.LandNo, "A123")        // ? 強型別支援
                    .Like(x => x.CityCode, "TPE")       // ? IntelliSense 支援
                    .Compare(LogicalOperator.And, innerRules => innerRules
                        .Equal(x => x.CaseOwner, "John Doe")    // ? 重構安全
                        .GreaterThan(x => x.Price, 1000000)     // ? 編譯時期檢查
                    )
                )
                .Build();

            Console.WriteLine("Expression 語法結果:");
            Console.WriteLine(JsonConvert.SerializeObject(query, Formatting.Indented));

            Console.WriteLine("\n=== 語法比較 ===\n");

            // 對比: 原始寫法 vs Expression 語法
            Console.WriteLine("原始寫法 (繁瑣):");
            Console.WriteLine("nameof(LandDTO.LandNo) - 需要手動維護");
            
            Console.WriteLine("\nExpression 語法 (簡潔):");
            Console.WriteLine("x => x.LandNo - 強型別、自動完成、重構安全");

            Console.WriteLine("\n=== 支援的所有運算子 ===\n");

            var allOperators = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .Equal(x => x.CaseOwner, "John")
                .Like(x => x.LandNo, "A%")
                .Contains(x => x.CityCode, "TPE")
                .StartsWith(x => x.LandNo, "A")
                .EndsWith(x => x.LandNo, "001")
                .In(x => x.CityCode, ["TPE", "KHH"])
                .Between(x => x.Price, 1000000, 5000000)
                .GreaterThan(x => x.CreateDate, DateTime.Now.AddDays(-30))
                .LessThan(x => x.Price, 10000000)
                .GreaterThanOrEqual(x => x.Price, 500000)
                .LessThanOrEqual(x => x.Price, 8000000)
                .Build();

            Console.WriteLine("所有運算子示範:");
            Console.WriteLine(JsonConvert.SerializeObject(allOperators, Formatting.Indented));
        }
    }
}