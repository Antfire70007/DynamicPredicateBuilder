using System.Collections.Generic;
using System.Linq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using DynamicPredicate.Tests.TestData;
using DynamicPredicateBuilder.Models;
using DynamicPredicateBuilder;
using System;
using Microsoft.Extensions.Logging;

namespace DynamicPredicate.Tests.Builders
{
    public class SqlGenerationTests
    {
        private TestDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .LogTo(System.Console.WriteLine)
                .Options;

            var context = new TestDbContext(options);
            
            // Seed test data
            context.Users.AddRange(
                new User { Id = 1, Name = "Snake", Age = 35, Salary = 50000.50m },
                new User { Id = 2, Name = "Boss", Age = 45, Salary = 75000.00m },
                new User { Id = 3, Name = "Otacon", Age = 28, Salary = null },
                new User { Id = 4, Name = "Gray Fox", Age = 32, Salary = 60000.25m }
            );
            context.SaveChanges();
            
            return context;
        }

        private TestDbContext CreateSqliteContext()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite("Data Source=:memory:")
                .EnableSensitiveDataLogging()
                .LogTo(Console.WriteLine, LogLevel.Information)
                .Options;

            var context = new TestDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();
            
            // Seed test data
            context.Users.AddRange(
                new User { Id = 1, Name = "Snake", Age = 35, Salary = 50000.50m },
                new User { Id = 2, Name = "Boss", Age = 45, Salary = 75000.00m },
                new User { Id = 3, Name = "Otacon", Age = 28, Salary = null },
                new User { Id = 4, Name = "Gray Fox", Age = 32, Salary = 60000.25m }
            );
            context.SaveChanges();
            
            return context;
        }

        [Fact]
        public void FilterBuilder_DecimalComparison_ShouldGenerateCorrectSQL()
        {
            using var context = CreateInMemoryContext();
            
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.GreaterThan, Value = 50000m }
                    ]
                }
            };
            
            var predicate = FilterBuilder.Build<User>(groups);
            
            // Test with Entity Framework
            var query = context.Users.Where(predicate);
            var results = query.ToList();
            
            // Should find Snake (50000.50), Boss (75000), and Gray Fox (60000.25) - all > 50000
            results.Should().HaveCount(3);
            results.Should().Contain(u => u.Name == "Boss");
            results.Should().Contain(u => u.Name == "Gray Fox");
            results.Should().Contain(u => u.Name == "Snake"); // 50000.50 > 50000
            results.Should().NotContain(u => u.Name == "Otacon"); // null salary
        }

        [Fact]
        public void FilterBuilder_DecimalComparison_SQLite_ShouldGenerateRealSQL()
        {
            using var context = CreateSqliteContext();
            
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.GreaterThan, Value = 50000m }
                    ]
                }
            };
            
            var predicate = FilterBuilder.Build<User>(groups);
            
            // This will generate actual SQL
            var query = context.Users.Where(predicate);
            
            // Print the SQL query (for debugging)
            var sql = query.ToQueryString();
            Console.WriteLine($"Generated SQL: {sql}");
            
            var results = query.ToList();
            
            // Verify the SQL contains decimal comparison
            sql.Should().Contain("Salary").And.Contain("50000");
            
            // Should find Snake (50000.50), Boss (75000), and Gray Fox (60000.25) - all > 50000
            results.Should().HaveCount(3);
            results.Should().Contain(u => u.Name == "Boss");
            results.Should().Contain(u => u.Name == "Gray Fox");
            results.Should().Contain(u => u.Name == "Snake");
            results.Should().NotContain(u => u.Name == "Otacon"); // null salary
        }

        [Fact]
        public void FilterBuilder_DecimalBetween_ShouldWork()
        {
            using var context = CreateInMemoryContext();
            
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.Between, Value = new[] { 50000m, 70000m } }
                    ]
                }
            };
            
            var predicate = FilterBuilder.Build<User>(groups);
            var query = context.Users.Where(predicate);
            var results = query.ToList();
            
            // Should find Snake (50000.50) and Gray Fox (60000.25)
            results.Should().HaveCount(2);
            results.Should().Contain(u => u.Name == "Snake");
            results.Should().Contain(u => u.Name == "Gray Fox");
            results.Should().NotContain(u => u.Name == "Boss"); // 75000 > 70000
            results.Should().NotContain(u => u.Name == "Otacon"); // null
        }

        [Fact]
        public void FilterBuilder_DecimalBetween_SQLite_ShouldGenerateRealSQL()
        {
            using var context = CreateSqliteContext();
            
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.Between, Value = new[] { 50000m, 70000m } }
                    ]
                }
            };
            
            var predicate = FilterBuilder.Build<User>(groups);
            var query = context.Users.Where(predicate);
            
            // Print the SQL query
            var sql = query.ToQueryString();
            Console.WriteLine($"Generated Between SQL: {sql}");
            
            var results = query.ToList();
            
            // Verify the SQL contains BETWEEN clause or equivalent conditions
            sql.Should().Contain("Salary");
            
            // Should find Snake (50000.50) and Gray Fox (60000.25)
            results.Should().HaveCount(2);
            results.Should().Contain(u => u.Name == "Snake");
            results.Should().Contain(u => u.Name == "Gray Fox");
        }

        [Fact]
        public void FilterBuilder_DecimalEqual_WithNullableDecimal_ShouldWork()
        {
            using var context = CreateInMemoryContext();
            
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.Equal, Value = 50000.50m }
                    ]
                }
            };
            
            var predicate = FilterBuilder.Build<User>(groups);
            var query = context.Users.Where(predicate);
            var results = query.ToList();
            
            // Should find only Snake
            results.Should().HaveCount(1);
            results.First().Name.Should().Be("Snake");
        }

        [Fact]
        public void FilterBuilder_DecimalIn_ShouldWork()
        {
            using var context = CreateInMemoryContext();
            
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.In, Value = new[] { 50000.50m, 75000.00m } }
                    ]
                }
            };
            
            var predicate = FilterBuilder.Build<User>(groups);
            var query = context.Users.Where(predicate);
            var results = query.ToList();
            
            // Should find Snake and Boss
            results.Should().HaveCount(2);
            results.Should().Contain(u => u.Name == "Snake");
            results.Should().Contain(u => u.Name == "Boss");
            results.Should().NotContain(u => u.Name == "Gray Fox");
            results.Should().NotContain(u => u.Name == "Otacon");
        }

        [Fact]
        public void FilterBuilder_DecimalIn_SQLite_ShouldGenerateRealSQL()
        {
            using var context = CreateSqliteContext();
            
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.In, Value = new[] { 50000.50m, 75000.00m } }
                    ]
                }
            };
            
            var predicate = FilterBuilder.Build<User>(groups);
            var query = context.Users.Where(predicate);
            
            // Print the SQL query
            var sql = query.ToQueryString();
            Console.WriteLine($"Generated IN SQL: {sql}");
            
            var results = query.ToList();
            
            // Verify the SQL contains IN clause
            sql.Should().Contain("Salary").And.Contain("IN");
            
            // Should find Snake and Boss
            results.Should().HaveCount(2);
            results.Should().Contain(u => u.Name == "Snake");
            results.Should().Contain(u => u.Name == "Boss");
        }

        [Fact]
        public void FilterBuilder_MultipleDecimalConditions_ShouldWork()
        {
            using var context = CreateInMemoryContext();
            
            var groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "Salary", Operator = FilterOperator.GreaterThanOrEqual, Value = 50000m },
                        new FilterRule { Property = "Age", Operator = FilterOperator.LessThan, Value = 40 }
                    ]
                }
            };
            
            var predicate = FilterBuilder.Build<User>(groups);
            var query = context.Users.Where(predicate);
            var results = query.ToList();
            
            // Should find Snake (Age=35, Salary=50000.50) and Gray Fox (Age=32, Salary=60000.25)
            results.Should().HaveCount(2);
            results.Should().Contain(u => u.Name == "Snake");
            results.Should().Contain(u => u.Name == "Gray Fox");
            results.Should().NotContain(u => u.Name == "Boss"); // Age 45 >= 40
            results.Should().NotContain(u => u.Name == "Otacon"); // null salary
        }
    }
}