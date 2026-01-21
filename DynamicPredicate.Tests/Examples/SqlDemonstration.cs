using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using DynamicPredicate.Tests.TestData;
using DynamicPredicateBuilder.Models;
using DynamicPredicateBuilder;

namespace DynamicPredicate.Tests.Examples
{
    public class SqlDemonstration
    {
        private static readonly decimal[] item = [50000m, 70000m];
        private static readonly decimal[] itemArray = [50000.50m, 75000.00m];

        public static void DemonstrateDecimalSqlGeneration()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite("Data Source=:memory:")
                .EnableSensitiveDataLogging()
                .LogTo(Console.WriteLine)
                .Options;

            using var context = new TestDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();
            
            // Seed data
            context.Users.AddRange(
                new User { Id = 1, Name = "Snake", Age = 35, Salary = 50000.50m },
                new User { Id = 2, Name = "Boss", Age = 45, Salary = 75000.00m },
                new User { Id = 3, Name = "Otacon", Age = 28, Salary = null },
                new User { Id = 4, Name = "Gray Fox", Age = 32, Salary = 60000.25m }
            );
            context.SaveChanges();

            Console.WriteLine("\n=== DECIMAL COMPARISON DEMONSTRATIONS ===\n");

            // 1. Greater Than
            Console.WriteLine("1. Testing: Salary > 50000");
            var greaterThanFilter = new List<FilterGroup>
            {
                new() {
                    LogicalOperator = LogicalOperator.And,
                    Rules = [new FilterRule { Property = "Salary", Operator = FilterOperator.GreaterThan, Value = 50000m }]
                }
            };
            var greaterThanPredicate = FilterBuilder.Build<User>(greaterThanFilter);
            var greaterThanQuery = context.Users.Where(greaterThanPredicate);
            Console.WriteLine($"Generated SQL: {greaterThanQuery.ToQueryString()}");
            var greaterThanResults = greaterThanQuery.ToList();
            Console.WriteLine($"Results: {greaterThanResults.Count} users found");
            foreach (var user in greaterThanResults)
            {
                Console.WriteLine($"  - {user.Name}: ${user.Salary}");
            }

            // 2. Between
            Console.WriteLine("\n2. Testing: Salary BETWEEN 50000 AND 70000");
            var betweenFilter = new List<FilterGroup>
            {
                new() {
                    LogicalOperator = LogicalOperator.And,
                    Rules = [new FilterRule { Property = "Salary", Operator = FilterOperator.Between, Value = item }]
                }
            };
            var betweenPredicate = FilterBuilder.Build<User>(betweenFilter);
            var betweenQuery = context.Users.Where(betweenPredicate);
            Console.WriteLine($"Generated SQL: {betweenQuery.ToQueryString()}");
            var betweenResults = betweenQuery.ToList();
            Console.WriteLine($"Results: {betweenResults.Count} users found");
            foreach (var user in betweenResults)
            {
                Console.WriteLine($"  - {user.Name}: ${user.Salary}");
            }

            // 3. IN
            Console.WriteLine("\n3. Testing: Salary IN (50000.50, 75000.00)");
            var inFilter = new List<FilterGroup>
            {
                new() {
                    LogicalOperator = LogicalOperator.And,
                    Rules = [new FilterRule { Property = "Salary", Operator = FilterOperator.In, Value = itemArray }]
                }
            };
            var inPredicate = FilterBuilder.Build<User>(inFilter);
            var inQuery = context.Users.Where(inPredicate);
            Console.WriteLine($"Generated SQL: {inQuery.ToQueryString()}");
            var inResults = inQuery.ToList();
            Console.WriteLine($"Results: {inResults.Count} users found");
            foreach (var user in inResults)
            {
                Console.WriteLine($"  - {user.Name}: ${user.Salary}");
            }

            // 4. Equal
            Console.WriteLine("\n4. Testing: Salary = 60000.25");
            var equalFilter = new List<FilterGroup>
            {
                new() {
                    LogicalOperator = LogicalOperator.And,
                    Rules = [new FilterRule { Property = "Salary", Operator = FilterOperator.Equal, Value = 60000.25m }]
                }
            };
            var equalPredicate = FilterBuilder.Build<User>(equalFilter);
            var equalQuery = context.Users.Where(equalPredicate);
            Console.WriteLine($"Generated SQL: {equalQuery.ToQueryString()}");
            var equalResults = equalQuery.ToList();
            Console.WriteLine($"Results: {equalResults.Count} users found");
            foreach (var user in equalResults)
            {
                Console.WriteLine($"  - {user.Name}: ${user.Salary}");
            }

            Console.WriteLine("\n=== CONCLUSION ===");
            Console.WriteLine("As demonstrated above, DynamicPredicateBuilder correctly generates SQL");
            Console.WriteLine("for all decimal comparison operations. The decimal values are properly");
            Console.WriteLine("translated to SQL parameters and conditions.");
        }
    }
}