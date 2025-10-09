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
    /// Any �M NotAny �ާ@�Ū��ϥνd��
    /// </summary>
    public class AnyNotAnyExamples
    {
        private readonly ITestOutputHelper _output;

        public AnyNotAnyExamples(ITestOutputHelper output)
        {
            _output = output;
        }

        // ���եΪ��������O
        public class Product
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public List<string> Tags { get; set; } = new();
            public List<string> Categories { get; set; } = new();
            public List<int> Ratings { get; set; } = new();
            public decimal Price { get; set; }
        }

        [Fact]
        public void Example_AnyOperator_CheckCollectionHasElements()
        {
            _output.WriteLine("=== Any �ާ@�šG�ˬd���X�O�_�����󤸯� ===");

            // �إߴ��ո��
            var products = new List<Product>
            {
                new() { Id = 1, Name = "Laptop", Tags = new() { "Electronics", "Computer" } },
                new() { Id = 2, Name = "Book", Tags = new() },  // �Ŷ��X
                new() { Id = 3, Name = "Phone", Tags = null }    // null ���X
            };

            // �ϥ� FilterDictionaryBuilder�G�d�ߦ�������Ҫ����~
            var filterGroup = FilterDictionaryBuilder.QueryBuilder<Product>()
                .Any(x => x.Tags)  // value �� null�A�ˬd���X�O�_�����󤸯�
                .ToFilterGroup();

            var predicate = FilterBuilder.Build<Product>(filterGroup).Compile();
            var results = products.Where(predicate).ToList();

            // �u�� Laptop ������
            results.Should().HaveCount(1);
            results[0].Name.Should().Be("Laptop");

            _output.WriteLine($"��� {results.Count} �Ӧ����Ҫ����~�G");
            foreach (var product in results)
            {
                _output.WriteLine($"- {product.Name}: [{string.Join(", ", product.Tags ?? new())}]");
            }
        }

        [Fact]
        public void Example_AnyOperator_CheckCollectionContainsSpecificValue()
        {
            _output.WriteLine("=== Any �ާ@�šG�ˬd���X�O�_�]�t�S�w�� ===");

            // �إߴ��ո��
            var products = new List<Product>
            {
                new() { Id = 1, Name = "Gaming Laptop", Tags = new() { "Electronics", "Gaming", "Computer" } },
                new() { Id = 2, Name = "Office Laptop", Tags = new() { "Electronics", "Business", "Computer" } },
                new() { Id = 3, Name = "Gaming Mouse", Tags = new() { "Gaming", "Accessories" } },
                new() { Id = 4, Name = "Office Chair", Tags = new() { "Furniture", "Business" } }
            };

            // �ϥ� FilterDictionaryBuilder�G�d�ߥ]�t "Gaming" ���Ҫ����~
            var filterGroup = FilterDictionaryBuilder.QueryBuilder<Product>()
                .Any(x => x.Tags, "Gaming")  // �ˬd Tags ���X�O�_�]�t "Gaming"
                .ToFilterGroup();

            var predicate = FilterBuilder.Build<Product>(filterGroup).Compile();
            var results = products.Where(predicate).ToList();

            // ���ӧ�� 2 �ӹC���������~
            results.Should().HaveCount(2);
            results.Should().Contain(p => p.Name == "Gaming Laptop");
            results.Should().Contain(p => p.Name == "Gaming Mouse");

            _output.WriteLine($"��� {results.Count} �ӥ]�t 'Gaming' ���Ҫ����~�G");
            foreach (var product in results)
            {
                _output.WriteLine($"- {product.Name}: [{string.Join(", ", product.Tags)}]");
            }
        }

        [Fact]
        public void Example_NotAnyOperator_CheckCollectionIsEmpty()
        {
            _output.WriteLine("=== NotAny �ާ@�šG�ˬd���X�O�_���� ===");

            // �إߴ��ո��
            var products = new List<Product>
            {
                new() { Id = 1, Name = "New Product", Categories = new() },     // �Ŷ��X
                new() { Id = 2, Name = "Draft Product", Categories = null },    // null ���X
                new() { Id = 3, Name = "Published Product", Categories = new() { "Electronics" } }
            };

            // �ϥ� FilterDictionaryBuilder�G�d�ߨS�����������~
            var filterGroup = FilterDictionaryBuilder.QueryBuilder<Product>()
                .NotAny(x => x.Categories)  // value �� null�A�ˬd���X�O�_�S�����󤸯�
                .ToFilterGroup();

            var predicate = FilterBuilder.Build<Product>(filterGroup).Compile();
            var results = products.Where(predicate).ToList();

            // ���ӧ�� 2 �ӨS�����������~
            results.Should().HaveCount(2);
            results.Should().Contain(p => p.Name == "New Product");
            results.Should().Contain(p => p.Name == "Draft Product");

            _output.WriteLine($"��� {results.Count} �ӨS�����������~�G");
            foreach (var product in results)
            {
                var categories = product.Categories?.Count > 0 ? string.Join(", ", product.Categories) : "�L";
                _output.WriteLine($"- {product.Name}: [{categories}]");
            }
        }

        [Fact]
        public void Example_NotAnyOperator_CheckCollectionDoesNotContainValue()
        {
            _output.WriteLine("=== NotAny �ާ@�šG�ˬd���X�O�_���]�t�S�w�� ===");

            // �إߴ��ո��
            var products = new List<Product>
            {
                new() { Id = 1, Name = "Safe Product", Tags = new() { "Electronics", "Safe" } },
                new() { Id = 2, Name = "Another Safe Product", Tags = new() { "Furniture" } },
                new() { Id = 3, Name = "Dangerous Product", Tags = new() { "Electronics", "Dangerous" } },
                new() { Id = 4, Name = "No Tags Product", Tags = new() }
            };

            // �ϥ� FilterDictionaryBuilder�G�d�ߤ��]�t "Dangerous" ���Ҫ����~
            var filterGroup = FilterDictionaryBuilder.QueryBuilder<Product>()
                .NotAny(x => x.Tags, "Dangerous")  // �ˬd Tags ���X�O�_���]�t "Dangerous"
                .ToFilterGroup();

            var predicate = FilterBuilder.Build<Product>(filterGroup).Compile();
            var results = products.Where(predicate).ToList();

            // ���ӧ�� 3 �Ӧw�����~
            results.Should().HaveCount(3);
            results.Should().NotContain(p => p.Name == "Dangerous Product");

            _output.WriteLine($"��� {results.Count} �Ӥ��]�t 'Dangerous' ���Ҫ����~�G");
            foreach (var product in results)
            {
                var tags = product.Tags?.Count > 0 ? string.Join(", ", product.Tags) : "�L";
                _output.WriteLine($"- {product.Name}: [{tags}]");
            }
        }

        [Fact]
        public void Example_ComplexAnyNotAnyConditions()
        {
            _output.WriteLine("=== ������ Any/NotAny ����զX ===");

            // �إߴ��ո��
            var products = new List<Product>
            {
                new() { Id = 1, Name = "Premium Gaming Laptop", 
                        Tags = new() { "Electronics", "Gaming", "Premium" },
                        Categories = new() { "Computer", "Gaming" },
                        Ratings = new() { 5, 4, 5 } },
                new() { Id = 2, Name = "Budget Office Laptop", 
                        Tags = new() { "Electronics", "Budget" },
                        Categories = new() { "Computer", "Office" },
                        Ratings = new() { 3, 3, 4 } },
                new() { Id = 3, Name = "Dangerous Gadget", 
                        Tags = new() { "Electronics", "Dangerous" },
                        Categories = new() { "Gadget" },
                        Ratings = new() { 1, 2 } }
            };

            // �����d�ߡG
            // 1. ���������š]Ratings �����š^
            // 2. �����]�t "Premium" ����
            // 3. ����]�t "Dangerous" ����
            // 4. ���Ť������� 5 ��
            var filterGroup = FilterDictionaryBuilder.QueryBuilder<Product>()
                .WithLogicalOperator(LogicalOperator.And)
                .Any(x => x.Ratings)                    // ������
                .Any(x => x.Tags, "Premium")            // �]�t Premium ����
                .NotAny(x => x.Tags, "Dangerous")       // ���]�t Dangerous ����
                .Any(x => x.Ratings, 5)                 // ���Ť��� 5 ��
                .ToFilterGroup();

            var predicate = FilterBuilder.Build<Product>(filterGroup).Compile();
            var results = products.Where(predicate).ToList();

            // �u�� Premium Gaming Laptop �ŦX�Ҧ�����
            results.Should().HaveCount(1);
            results[0].Name.Should().Be("Premium Gaming Laptop");

            _output.WriteLine($"��� {results.Count} �ӲŦX�������󪺲��~�G");
            foreach (var product in results)
            {
                _output.WriteLine($"- {product.Name}:");
                _output.WriteLine($"  ����: [{string.Join(", ", product.Tags)}]");
                _output.WriteLine($"  ����: [{string.Join(", ", product.Categories)}]");
                _output.WriteLine($"  ����: [{string.Join(", ", product.Ratings)}]");
            }
        }

        [Fact]
        public void Example_UsingFilterRules_DirectlyWithAnyNotAny()
        {
            _output.WriteLine("=== �����ϥ� FilterRule �� Any/NotAny �ާ@�� ===");

            // �إߴ��ո��
            var products = new List<Product>
            {
                new() { Id = 1, Name = "Electronics", Tags = new() { "Tech", "Popular" } },
                new() { Id = 2, Name = "Book", Tags = new() { "Education" } },
                new() { Id = 3, Name = "Empty Product", Tags = new() }
            };

            // �ϥ� FilterRule �����إ߱���
            var filterGroup = new FilterGroup
            {
                LogicalOperator = LogicalOperator.Or,
                Rules = new List<object>
                {
                    // �ˬd Tags �O�_�����󤸯�
                    new FilterRule 
                    { 
                        Property = "Tags", 
                        Operator = FilterOperator.Any, 
                        Value = null 
                    },
                    // �ˬd Tags �O�_�]�t "Popular"
                    new FilterRule 
                    { 
                        Property = "Tags", 
                        Operator = FilterOperator.Any, 
                        Value = "Popular" 
                    }
                }
            };

            var predicate = FilterBuilder.Build<Product>(filterGroup).Compile();
            var results = products.Where(predicate).ToList();

            // �Ҧ������Ҫ����~���|�Q���
            results.Should().HaveCount(2);
            results.Should().Contain(p => p.Name == "Electronics");
            results.Should().Contain(p => p.Name == "Book");

            _output.WriteLine($"��� {results.Count} �ӲŦX���󪺲��~�G");
            foreach (var product in results)
            {
                var tags = product.Tags?.Count > 0 ? string.Join(", ", product.Tags) : "�L";
                _output.WriteLine($"- {product.Name}: [{tags}]");
            }
        }
    }
}