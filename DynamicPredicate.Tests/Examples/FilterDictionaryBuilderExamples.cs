using System;
using System.Collections.Generic;
using DynamicPredicateBuilder;
using DynamicPredicateBuilder.Models;
using Newtonsoft.Json;

namespace DynamicPredicate.Tests.Examples
{
    /// <summary>
    /// FilterDictionaryBuilder �ϥνd��
    /// </summary>
    public class FilterDictionaryBuilderExamples
    {
        // ���եΪ� DTO ���O
        public class LandDTO
        {
            public string LandNo { get; set; }
            public string CityCode { get; set; }
            public string CaseOwner { get; set; }
            public decimal Price { get; set; }
            public decimal? EstimatedValue { get; set; }  // �s�W�� nullable decimal ���
            public DateTime CreateDate { get; set; }
            public List<string> Tags { get; set; }
        }

        /// <summary>
        /// ²��d�߽d�� - �ŦX�Τ�쥻���ݨD
        /// </summary>
        public static void SimpleQueryExample()
        {
            var landNo = "A123";
            var city = "TPE";

            // �ϥ� FilterDictionaryBuilder ���s�y�k
            var queryDict = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.Or)
                .Like(nameof(LandDTO.LandNo), landNo)
                .Like(nameof(LandDTO.CityCode), city)
                .Build();

            Console.WriteLine("=== ²��d�߽d�� ===");
            Console.WriteLine(JsonConvert.SerializeObject(queryDict, Formatting.Indented));

            // ���쥻���g�k
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

            Console.WriteLine("\n=== �쥻���g�k ===");
            Console.WriteLine(JsonConvert.SerializeObject(rawDict, Formatting.Indented));
        }

        /// <summary>
        /// Nullable Decimal �d�߽d�� - �i�ܦp��B�z�i�� null �� decimal ���
        /// </summary>
        public static void NullableDecimalQueryExample()
        {
            Console.WriteLine("=== Nullable Decimal �d�߽d�� ===");

            // �d�� 1: �d�ߦ������Ȫ��g�a
            var queryWithValue = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .GreaterThan(x => x.EstimatedValue, 0)  // �����Ȥj�� 0
                .LessThanOrEqual(x => x.EstimatedValue, 10000000)  // �����Ȥp�󵥩� 1000 �U
                .Build();

            Console.WriteLine("�d�ߦ������Ȫ��g�a (0 < EstimatedValue <= 10,000,000):");
            Console.WriteLine(JsonConvert.SerializeObject(queryWithValue, Formatting.Indented));

            // �d�� 2: �d�ߨS�������Ȫ��g�a (null ��)
            var queryWithoutValue = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .Equal(x => x.EstimatedValue, null)  // �����Ȭ� null
                .GreaterThan(x => x.Price, 500000)   // ����ڻ���j�� 50 �U
                .Build();

            Console.WriteLine("\n�d�ߨS�������Ȫ��g�a (EstimatedValue is null but Price > 500,000):");
            Console.WriteLine(JsonConvert.SerializeObject(queryWithoutValue, Formatting.Indented));

            // �d�� 3: �ƦX���� - �������ȩΰ�����
            var queryComplexCondition = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.Or)
                .Compare(LogicalOperator.And, rules => rules
                    .GreaterThan(x => x.EstimatedValue, 5000000)  // �����Ȥj�� 500 �U
                    .LessThan(x => x.EstimatedValue, 20000000)    // �B�p�� 2000 �U
                )
                .GreaterThan(x => x.Price, 15000000)  // �ι�ڻ���j�� 1500 �U
                .Build();

            Console.WriteLine("\n�ƦX����d�� (�������Ƚd�� OR ����ڻ���):");
            Console.WriteLine(JsonConvert.SerializeObject(queryComplexCondition, Formatting.Indented));

            // �d�� 4: �����ȻP��ڻ��檺���
            var queryPriceComparison = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .Compare(LogicalOperator.Or, priceRules => priceRules
                    .Equal(x => x.EstimatedValue, null)  // �S��������
                    .AddPropertyComparison(x => x.EstimatedValue, FilterOperator.GreaterThan, x => x.Price)  // �Φ����Ȱ����ڻ���
                )
                .In(x => x.CityCode, new[] { "TPE", "KHH", "TCH" })  // ���w�b�D�n����
                .Build();

            Console.WriteLine("\n�������d�� (�L������ OR ������ > ��ڻ���A�B�b�D�n����):");
            Console.WriteLine(JsonConvert.SerializeObject(queryPriceComparison, Formatting.Indented));

            // �d�� 5: Between �ާ@�ŻP nullable decimal
            var queryBetweenRange = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .Between(x => x.EstimatedValue, 1000000, 5000000)  // �����Ȧb 100-500 �U����
                .Like(x => x.LandNo, "A%")  // �a���H A �}�Y
                .Contains(x => x.Tags, "�u��a�q")  // �]�t�u��a�q����
                .Build();

            Console.WriteLine("\n�d��d�� (�����Ȧb 100-500 �U�����A�a���H A �}�Y�A�u��a�q):");
            Console.WriteLine(JsonConvert.SerializeObject(queryBetweenRange, Formatting.Indented));

            // �d�� 6: �_�w�d�� - �ư��S�w�����Ƚd��
            var queryNegation = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .Between(x => x.EstimatedValue, 8000000, 12000000, isNegated: true)  // �ư� 800-1200 �U�d��
                .GreaterThan(x => x.Price, 0)  // ��ڻ���j�� 0
                .Build();

            Console.WriteLine("\n�_�w�d�� (�ư������� 800-1200 �U�d��):");
            Console.WriteLine(JsonConvert.SerializeObject(queryNegation, Formatting.Indented));
        }

        /// <summary>
        /// �_���d�߽d�� - �ŦX�Τ᪺�i���ݨD
        /// </summary>
        public static void NestedQueryExample()
        {
            var landNo = "A123";
            var city = "TPE";
            var owner = "John Doe";

            // �h�h�����d�ߤ覡
            var queryDict = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.Or)
                .Like(nameof(LandDTO.LandNo), landNo)
                .Like(nameof(LandDTO.CityCode), city)
                .Compare(LogicalOperator.And, rules => rules
                    .Equal(nameof(LandDTO.CaseOwner), owner)
                    .GreaterThan(nameof(LandDTO.Price), 1000000)
                )
                .Build();

            Console.WriteLine("=== �_���d�߽d�� ===");
            Console.WriteLine(JsonConvert.SerializeObject(queryDict, Formatting.Indented));
        }

        /// <summary>
        /// �����d�߽d�� - �i�ܦU�عB��l�M�_�����c
        /// </summary>
        public static void ComplexQueryExample()
        {
            var queryDict = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                .Compare(LogicalOperator.Or, mainRules => mainRules
                    // ����� 1: �S�w�a���Ϋ���
                    .Like(nameof(LandDTO.LandNo), "A123")
                    .In(nameof(LandDTO.CityCode), new[] { "TPE", "KHH", "TCH" })
                    // ����� 2: �S��~�D�B����
                    .Compare(LogicalOperator.And, specialRules => specialRules
                        .Equal(nameof(LandDTO.CaseOwner), "VIP Owner")
                        .Between(nameof(LandDTO.Price), 5000000, 50000000)
                    )
                )
                // �����ŦX���ɶ��d��
                .Compare(LogicalOperator.And, timeRules => timeRules
                    .GreaterThan(nameof(LandDTO.CreateDate), DateTime.Now.AddMonths(-6))
                    .LessThan(nameof(LandDTO.CreateDate), DateTime.Now)
                )
                // �������S�w����
                .Contains(nameof(LandDTO.Tags), "�u��a�q")
                .Build();

            Console.WriteLine("=== �����d�߽d�� ===");
            Console.WriteLine(JsonConvert.SerializeObject(queryDict, Formatting.Indented));
        }

        /// <summary>
        /// �_�w�d�߽d��
        /// </summary>
        public static void NegationQueryExample()
        {
            var queryDict = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                // ���O�S�w�~�D
                .Equal(nameof(LandDTO.CaseOwner), "Blacklisted Owner", isNegated: true)
                // ���b���w����d��
                .Compare(LogicalOperator.Or, priceRules => priceRules
                    .LessThan(nameof(LandDTO.Price), 500000)
                    .GreaterThan(nameof(LandDTO.Price), 10000000),
                    isNegated: true)
                // ���]�t�S�w����
                .Contains(nameof(LandDTO.Tags), "���D�a�q", isNegated: true)
                .Build();

            Console.WriteLine("=== �_�w�d�߽d�� ===");
            Console.WriteLine(JsonConvert.SerializeObject(queryDict, Formatting.Indented));
        }

        /// <summary>
        /// �ݩʤ���d��
        /// </summary>
        public static void PropertyComparisonExample()
        {
            var queryDict = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                // �a�������P�����N�X�ۦP (���]���~���޿�)
                .AddPropertyComparison(nameof(LandDTO.LandNo), FilterOperator.Equal, nameof(LandDTO.CityCode))
                // ��L����
                .GreaterThan(nameof(LandDTO.Price), 1000000)
                .Build();

            Console.WriteLine("=== �ݩʤ���d�� ===");
            Console.WriteLine(JsonConvert.SerializeObject(queryDict, Formatting.Indented));
        }

        /// <summary>
        /// �����ഫ�d��
        /// </summary>
        public static void ImplicitConversionExample()
        {
            // �����ഫ�� Dictionary
            Dictionary<string, object> dict = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .Like(nameof(LandDTO.LandNo), "test")
                .Equal(nameof(LandDTO.CaseOwner), "John");

            // �����ഫ�� FilterGroup
            FilterGroup group = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .Contains(nameof(LandDTO.CityCode), "taipei")
                .GreaterThan(nameof(LandDTO.Price), 1000000);

            Console.WriteLine("=== �����ഫ�d�� ===");
            Console.WriteLine("�ഫ�� Dictionary:");
            Console.WriteLine(JsonConvert.SerializeObject(dict, Formatting.Indented));
            Console.WriteLine("\n�ഫ�� FilterGroup:");
            Console.WriteLine(JsonConvert.SerializeObject(group, Formatting.Indented));
        }

        /// <summary>
        /// Expression �y�k�d�� - �j���O���ݩʿ��
        /// </summary>
        public static void ExpressionSyntaxExample()
        {
            var landNo = "A123";
            var city = "TPE";

            // �ϥ� Expression �y�k��²��d��
            var queryDict = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.Or)
                .Like(x => x.LandNo, landNo)
                .Like(x => x.CityCode, city)
                .Build();

            Console.WriteLine("=== Expression �y�k�d�� ===");
            Console.WriteLine(JsonConvert.SerializeObject(queryDict, Formatting.Indented));
        }

        /// <summary>
        /// Expression �_���d�߽d��
        /// </summary>
        public static void ExpressionNestedQueryExample()
        {
            var landNo = "A123";
            var city = "TPE";
            var owner = "John Doe";

            // Expression �������h�h���d��
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

            Console.WriteLine("=== Expression �_���d�߽d�� ===");
            Console.WriteLine(JsonConvert.SerializeObject(queryDict, Formatting.Indented));
        }

        /// <summary>
        /// Expression �P�r��V�X�ϥνd��
        /// </summary>
        public static void MixedSyntaxExample()
        {
            var queryDict = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                // Expression ���� - ���sĶ�ɴ����O�ˬd
                .Like(x => x.LandNo, "A123")
                .GreaterThan(x => x.Price, 1000000)
                .In(x => x.Tags, new[] { "�u��", "����" })
                // �r�ꪩ�� - �A�Ω�ʺA�ݩʦW��
                .Equal(nameof(LandDTO.CaseOwner), "John Doe")
                .Contains("DynamicProperty", "value")  // ���]���ʺA�ݩ�
                .Build();

            Console.WriteLine("=== Expression �P�r��V�X�ϥνd�� ===");
            Console.WriteLine(JsonConvert.SerializeObject(queryDict, Formatting.Indented));
        }

        /// <summary>
        /// Expression �������Ҧ��B��l�d��
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
                .In(x => x.Tags, new[] { "�u��", "����", "����" })
                .Between(x => x.Price, 1000000, 5000000)
                .GreaterThan(x => x.CreateDate, DateTime.Now.AddMonths(-3))
                .LessThanOrEqual(x => x.Price, 10000000)
                .GreaterThanOrEqual(x => x.Price, 500000)
                .Build();

            Console.WriteLine("=== Expression �������Ҧ��B��l�d�� ===");
            Console.WriteLine(JsonConvert.SerializeObject(queryDict, Formatting.Indented));
        }

        /// <summary>
        /// Expression �ݩʤ���d��
        /// </summary>
        public static void ExpressionPropertyComparisonExample()
        {
            var queryDict = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .WithLogicalOperator(LogicalOperator.And)
                // �ݩʹ��ݩʤ�� (Expression ����)
                .AddPropertyComparison(x => x.LandNo, FilterOperator.Equal, x => x.CityCode)
                // �V�X��L����
                .GreaterThan(x => x.Price, 1000000)
                .Like(x => x.CaseOwner, "VIP%")
                .Build();

            Console.WriteLine("=== Expression �ݩʤ���d�� ===");
            Console.WriteLine(JsonConvert.SerializeObject(queryDict, Formatting.Indented));
        }

        /// <summary>
        /// �U�ؤu�t��k�d��
        /// </summary>
        public static void FactoryMethodsExample()
        {
            // ��k1: �R�A�u�t 
            var query1 = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
                .Like(nameof(LandDTO.LandNo), "test");

            // ��k2: �t�@���R�A�u�t
            var query2 = FilterDictionaryBuilder.Create<LandDTO>()
                .Equal(nameof(LandDTO.CaseOwner), "John");

            // ��k3: �x�����O���R�A��k
            var query3 = FilterDictionaryBuilder<LandDTO>.Create()
                .Contains(nameof(LandDTO.CityCode), "taipei");

            Console.WriteLine("=== �u�t��k�d�� ===");
            Console.WriteLine("��k1 ���G:");
            Console.WriteLine(JsonConvert.SerializeObject(query1.Build(), Formatting.Indented));
            Console.WriteLine("\n��k2 ���G:");
            Console.WriteLine(JsonConvert.SerializeObject(query2.Build(), Formatting.Indented));
            Console.WriteLine("\n��k3 ���G:");
            Console.WriteLine(JsonConvert.SerializeObject(query3.Build(), Formatting.Indented));
        }

        /// <summary>
        /// ����Ҧ��d��
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