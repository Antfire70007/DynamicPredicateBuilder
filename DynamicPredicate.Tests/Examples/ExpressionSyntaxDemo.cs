using System;
using DynamicPredicateBuilder;
using DynamicPredicateBuilder.Models;
using Newtonsoft.Json;

namespace DynamicPredicate.Tests.Examples
{
    /// <summary>
    /// Expression �y�k�ϥΥܽd
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
        /// �i�� Expression �y�k���j�j�\��
        /// </summary>
        public static void DemoExpressionSyntax()
        {
            Console.WriteLine("=== FilterDictionaryBuilder Expression �y�k�ܽd ===\n");

            // �ŦX�Τ��l�ݨD�G�䴩 x => x.Property �y�k
            var query = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .Compare(LogicalOperator.Or, rules => rules
                    .Like(x => x.LandNo, "A123")        // ? �j���O�䴩
                    .Like(x => x.CityCode, "TPE")       // ? IntelliSense �䴩
                    .Compare(LogicalOperator.And, innerRules => innerRules
                        .Equal(x => x.CaseOwner, "John Doe")    // ? ���c�w��
                        .GreaterThan(x => x.Price, 1000000)     // ? �sĶ�ɴ��ˬd
                    )
                )
                .Build();

            Console.WriteLine("Expression �y�k���G:");
            Console.WriteLine(JsonConvert.SerializeObject(query, Formatting.Indented));

            Console.WriteLine("\n=== �y�k��� ===\n");

            // ���: ��l�g�k vs Expression �y�k
            Console.WriteLine("��l�g�k (�c��):");
            Console.WriteLine("nameof(LandDTO.LandNo) - �ݭn��ʺ��@");
            
            Console.WriteLine("\nExpression �y�k (²��):");
            Console.WriteLine("x => x.LandNo - �j���O�B�۰ʧ����B���c�w��");

            Console.WriteLine("\n=== �䴩���Ҧ��B��l ===\n");

            var allOperators = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .Equal(x => x.CaseOwner, "John")
                .Like(x => x.LandNo, "A%")
                .Contains(x => x.CityCode, "TPE")
                .StartsWith(x => x.LandNo, "A")
                .EndsWith(x => x.LandNo, "001")
                .In(x => x.CityCode, new[] { "TPE", "KHH" })
                .Between(x => x.Price, 1000000, 5000000)
                .GreaterThan(x => x.CreateDate, DateTime.Now.AddDays(-30))
                .LessThan(x => x.Price, 10000000)
                .GreaterThanOrEqual(x => x.Price, 500000)
                .LessThanOrEqual(x => x.Price, 8000000)
                .Build();

            Console.WriteLine("�Ҧ��B��l�ܽd:");
            Console.WriteLine(JsonConvert.SerializeObject(allOperators, Formatting.Indented));
        }
    }
}