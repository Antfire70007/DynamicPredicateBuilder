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
        private static TestDbContext CreateInMemoryContext()
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

        private static TestDbContext CreateSqliteContext()
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

        private static ExtendedTestDbContext CreateExtendedInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ExtendedTestDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .LogTo(System.Console.WriteLine, LogLevel.Information)
                .Options;

            var context = new ExtendedTestDbContext(options);

            // Seed complex test data
            SeedComplexTestData(context);
            
            return context;
        }

        private static ExtendedTestDbContext CreateExtendedSqliteContext()
        {
            var options = new DbContextOptionsBuilder<ExtendedTestDbContext>()
                .UseSqlite("Data Source=:memory:")
                .EnableSensitiveDataLogging()
                .LogTo(Console.WriteLine, LogLevel.Information)
                .Options;

            var context = new ExtendedTestDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();

            // Seed complex test data
            SeedComplexTestData(context);
            
            return context;
        }

        private static void SeedComplexTestData(ExtendedTestDbContext context)
        {
            // 創建公司
            var company1 = new Company { Id = 1, Name = "TechCorp", Address = "台北市信義區" };
            var company2 = new Company { Id = 2, Name = "SoftwareInc", Address = "台中市西屯區" };
            
            context.Companies.AddRange(company1, company2);
            
            // 創建部門
            var dept1 = new Department { Id = 1, Name = "Engineering", CompanyId = 1 };
            var dept2 = new Department { Id = 2, Name = "Marketing", CompanyId = 1 };
            var dept3 = new Department { Id = 3, Name = "Development", CompanyId = 2 };
            
            context.Departments.AddRange(dept1, dept2, dept3);
            
            // 創建管理者
            var manager1 = new Manager { Id = 1, Name = "Alice Manager", Title = "CTO", Bonus = 100000m, DepartmentId = 1 };
            var manager2 = new Manager { Id = 2, Name = "Bob Manager", Title = "CMO", Bonus = 80000m, DepartmentId = 2 };
            var manager3 = new Manager { Id = 3, Name = "Carol Manager", Title = "Lead", Bonus = 90000m, DepartmentId = 3 };
            
            context.Managers.AddRange(manager1, manager2, manager3);
            
            // 創建員工
            var emp1 = new Employee { Id = 1, Name = "John Dev", Email = "john@techcorp.com", Salary = 80000m, HireDate = DateTime.Now.AddYears(-2), DepartmentId = 1 };
            var emp2 = new Employee { Id = 2, Name = "Jane Dev", Email = "jane@techcorp.com", Salary = 85000m, HireDate = DateTime.Now.AddYears(-1), DepartmentId = 1 };
            var emp3 = new Employee { Id = 3, Name = "Mike Marketing", Email = "mike@techcorp.com", Salary = 70000m, HireDate = DateTime.Now.AddMonths(-6), DepartmentId = 2 };
            var emp4 = new Employee { Id = 4, Name = "Sarah Coder", Email = "sarah@softwareinc.com", Salary = 90000m, HireDate = DateTime.Now.AddYears(-3), DepartmentId = 3 };
            
            context.Employees.AddRange(emp1, emp2, emp3, emp4);
            
            // 創建員工檔案
            var profile1 = new EmployeeProfile { Id = 1, EmployeeId = 1, Skills = "C#,JavaScript", YearsOfExperience = 5, Education = "Bachelor CS" };
            var profile2 = new EmployeeProfile { Id = 2, EmployeeId = 2, Skills = "Python,React", YearsOfExperience = 3, Education = "Master CS" };
            var profile3 = new EmployeeProfile { Id = 3, EmployeeId = 3, Skills = "Marketing,Analytics", YearsOfExperience = 4, Education = "MBA" };
            var profile4 = new EmployeeProfile { Id = 4, EmployeeId = 4, Skills = "Java,Spring", YearsOfExperience = 7, Education = "PhD CS" };
            
            context.EmployeeProfiles.AddRange(profile1, profile2, profile3, profile4);
            
            // 創建專案
            var project1 = new Project { Id = 1, Name = "WebApp", Description = "E-commerce platform", StartDate = DateTime.Now.AddMonths(-6), Budget = 500000m, CompanyId = 1 };
            var project2 = new Project { Id = 2, Name = "MobileApp", Description = "Mobile companion", StartDate = DateTime.Now.AddMonths(-3), Budget = 300000m, CompanyId = 1 };
            var project3 = new Project { Id = 3, Name = "DataPlatform", Description = "Big data solution", StartDate = DateTime.Now.AddMonths(-12), Budget = 800000m, CompanyId = 2 };
            
            context.Projects.AddRange(project1, project2, project3);
            
            // 創建專案詳細資料
            var detail1 = new ProjectDetail { Id = 1, ProjectId = 1, TechnicalStack = "ASP.NET Core,React", EstimatedHours = 2000, Priority = "High" };
            var detail2 = new ProjectDetail { Id = 2, ProjectId = 2, TechnicalStack = "React Native", EstimatedHours = 1200, Priority = "Medium" };
            var detail3 = new ProjectDetail { Id = 3, ProjectId = 3, TechnicalStack = "Apache Spark,Kafka", EstimatedHours = 3000, Priority = "High" };
            
            context.ProjectDetails.AddRange(detail1, detail2, detail3);
            
            // 創建專案分配
            var assignment1 = new ProjectAssignment { Id = 1, EmployeeId = 1, ProjectId = 1, Role = "Backend Developer", HoursPerWeek = 40, AssignedDate = DateTime.Now.AddMonths(-6) };
            var assignment2 = new ProjectAssignment { Id = 2, EmployeeId = 2, ProjectId = 1, Role = "Frontend Developer", HoursPerWeek = 35, AssignedDate = DateTime.Now.AddMonths(-5) };
            var assignment3 = new ProjectAssignment { Id = 3, EmployeeId = 2, ProjectId = 2, Role = "Lead Developer", HoursPerWeek = 30, AssignedDate = DateTime.Now.AddMonths(-3) };
            var assignment4 = new ProjectAssignment { Id = 4, EmployeeId = 4, ProjectId = 3, Role = "Senior Developer", HoursPerWeek = 40, AssignedDate = DateTime.Now.AddMonths(-12) };
            
            context.ProjectAssignments.AddRange(assignment1, assignment2, assignment3, assignment4);
            
            context.SaveChanges();
        }

        // 合約測試資料種子方法
        private static ExtendedTestDbContext CreateContractTestContext()
        {
            var options = new DbContextOptionsBuilder<ExtendedTestDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .LogTo(System.Console.WriteLine, LogLevel.Information)
                .Options;

            var context = new ExtendedTestDbContext(options);

            // Seed 合約測試資料
            SeedContractTestData(context);
            
            return context;
        }

        private static void SeedContractTestData(ExtendedTestDbContext context)
        {
            // 創建建案
            var build1 = new Build { Id = 1, Name = "信義豪宅", AptId = 1001L, Location = "台北市信義區", Price = 50000000m };
            var build2 = new Build { Id = 2, Name = "大安名邸", AptId = 1002L, Location = "台北市大安區", Price = 45000000m };
            var build3 = new Build { Id = 3, Name = "南港新城", AptId = null, Location = "台北市南港區", Price = null }; // null AptId
            var build4 = new Build { Id = 4, Name = "內湖花園", AptId = 1004L, Location = "台北市內湖區", Price = 38000000m };

            context.Builds.AddRange(build1, build2, build3, build4);

            // 創建合約
            var contract1 = new Contract { Id = 1, Name = "豪宅購買合約", CreatedDate = DateTime.Now.AddMonths(-6) };
            var contract2 = new Contract { Id = 2, Name = "投資購買合約", CreatedDate = DateTime.Now.AddMonths(-3) };
            var contract3 = new Contract { Id = 3, Name = "自住購買合約", CreatedDate = DateTime.Now.AddMonths(-1) };

            context.Contracts.AddRange(contract1, contract2, contract3);

            // 創建建案合約關聯
            var buildContract1 = new BuildContract 
            { 
                Id = 1, ContractId = 1, BuildId = 1, 
                ContractType = "購買", Amount = 50000000m, 
                SignedDate = DateTime.Now.AddMonths(-6) 
            };
            var buildContract2 = new BuildContract 
            { 
                Id = 2, ContractId = 1, BuildId = 2, 
                ContractType = "購買", Amount = 45000000m, 
                SignedDate = DateTime.Now.AddMonths(-5) 
            };
            var buildContract3 = new BuildContract 
            { 
                Id = 3, ContractId = 2, BuildId = 2, 
                ContractType = "投資", Amount = 45000000m, 
                SignedDate = DateTime.Now.AddMonths(-3) 
            };
            var buildContract4 = new BuildContract 
            { 
                Id = 4, ContractId = 2, BuildId = 4, 
                ContractType = "投資", Amount = 38000000m, 
                SignedDate = DateTime.Now.AddMonths(-2) 
            };
            var buildContract5 = new BuildContract 
            { 
                Id = 5, ContractId = 3, BuildId = 3, 
                ContractType = "自住", Amount = 35000000m, 
                SignedDate = DateTime.Now.AddMonths(-1) 
            };

            context.BuildContracts.AddRange(buildContract1, buildContract2, buildContract3, buildContract4, buildContract5);

            context.SaveChanges();
        }

        [Fact]
        public void FilterBuilder_DecimalComparison_ShouldGenerateCorrectSQL()
        {
            using var context = CreateInMemoryContext();
            
            var groups = new List<FilterGroup>
            {
                new() {
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

        #region Contract.BuildContracts[].Build.AptId 測試案例

        [Fact]
        public void FilterBuilder_ArrayNavigation_ContractBuildContractsAptId_Equal_ShouldWork()
        {
            using var context = CreateContractTestContext();

            var groups = new List<FilterGroup>
            {
                new() {
                    LogicalOperator = LogicalOperator.And,
                    Rules =
                    [
                        new FilterRule { Property = "BuildContracts[].Build.AptId", Operator = FilterOperator.Equal, Value = 1001L }
                    ]
                }
            };

            var predicate = FilterBuilder.Build<Contract>(groups);
            var query = context.Contracts
                .Include(c => c.BuildContracts)
                .ThenInclude(bc => bc.Build)
                .Where(predicate);
            var results = query.ToList();

            // 應該找到包含 AptId = 1001 的合約 (豪宅購買合約)
            results.Should().HaveCount(1);
            results.Should().Contain(c => c.Name == "豪宅購買合約");
        }

        [Fact]
        public void FilterBuilder_ExpressionVersion_ArrayNavigation_ContractBuildContractsAptId_MixedSyntax_ShouldWork()
        {
            using var context = CreateContractTestContext();

            // 方案 1: 混合使用字串和 Expression 語法（推薦）
            var filterGroup = FilterDictionaryBuilder.QueryBuilder<Contract>()
                .WithLogicalOperator(LogicalOperator.And)
                .Equal("BuildContracts[].Build.AptId", 1001L) // 陣列導覽用字串語法
                .Contains(c => c.Name, "豪宅") // 一般屬性用 Expression 語法
                .ToFilterGroup();

            var predicate = FilterBuilder.Build<Contract>(filterGroup);
            var query = context.Contracts
                .Include(c => c.BuildContracts)
                .ThenInclude(bc => bc.Build)
                .Where(predicate);
            var results = query.ToList();

            // 應該找到符合條件的合約
            results.Should().HaveCount(1);
            results.Should().Contain(c => c.Name == "豪宅購買合約");
        }

        [Fact]
        public void FilterBuilder_ExpressionVersion_ArrayNavigation_ContractBuildContractsAptId_ArrayExtensions_ShouldWork()
        {
            using var context = CreateContractTestContext();

            // 方案 2: 使用新的陣列導覽 Expression 方法
            var filterGroup = FilterDictionaryBuilder.QueryBuilder<Contract>()
                .WithLogicalOperator(LogicalOperator.And)
                .ArrayEqual(c => c.BuildContracts, bc => bc.Build.AptId, 1001L)
                .Contains(c => c.Name, "豪宅")
                .ToFilterGroup();

            var predicate = FilterBuilder.Build<Contract>(filterGroup);
            var query = context.Contracts
                .Include(c => c.BuildContracts)
                .ThenInclude(bc => bc.Build)
                .Where(predicate);
            var results = query.ToList();

            // 應該找到符合條件的合約
            results.Should().HaveCount(1);
            results.Should().Contain(c => c.Name == "豪宅購買合約");
        }

        [Fact]
        public void FilterBuilder_ExpressionVersion_ArrayNavigation_AllArrayMethods_ShouldWork()
        {
            using var context = CreateContractTestContext();

            // 測試所有陣列導覽方法
            var filterGroup1 = FilterDictionaryBuilder.QueryBuilder<Contract>()
                .WithLogicalOperator(LogicalOperator.And)
                .ArrayIn(c => c.BuildContracts, bc => bc.Build.AptId, [1001L, 1002L])
                .ToFilterGroup();

            var predicate1 = FilterBuilder.Build<Contract>(filterGroup1);
            var results1 = context.Contracts
                .Include(c => c.BuildContracts)
                .ThenInclude(bc => bc.Build)
                .Where(predicate1)
                .ToList();

            results1.Should().HaveCount(2);
            results1.Should().Contain(c => c.Name == "豪宅購買合約");
            results1.Should().Contain(c => c.Name == "投資購買合約");

            // 測試 GreaterThan
            var filterGroup2 = FilterDictionaryBuilder.QueryBuilder<Contract>()
                .WithLogicalOperator(LogicalOperator.And)
                .ArrayGreaterThan(c => c.BuildContracts, bc => bc.Build.AptId, 1002L)
                .ToFilterGroup();

            var predicate2 = FilterBuilder.Build<Contract>(filterGroup2);
            var results2 = context.Contracts
                .Include(c => c.BuildContracts)
                .ThenInclude(bc => bc.Build)
                .Where(predicate2)
                .ToList();

            results2.Should().HaveCount(1);
            results2.Should().Contain(c => c.Name == "投資購買合約");

            // 測試 Between
            var filterGroup3 = FilterDictionaryBuilder.QueryBuilder<Contract>()
                .WithLogicalOperator(LogicalOperator.And)
                .ArrayBetween(c => c.BuildContracts, bc => bc.Build.AptId, 1001L, 1003L)
                .ToFilterGroup();

            var predicate3 = FilterBuilder.Build<Contract>(filterGroup3);
            var results3 = context.Contracts
                .Include(c => c.BuildContracts)
                .ThenInclude(bc => bc.Build)
                .Where(predicate3)
                .ToList();

            results3.Should().HaveCount(2);
            results3.Should().Contain(c => c.Name == "豪宅購買合約");
            results3.Should().Contain(c => c.Name == "投資購買合約");
        }

        [Fact]
        public void FilterBuilder_ExpressionVersion_ArrayLike_ShouldWork()
        {
            using var context = CreateContractTestContext();

            // 測試 ArrayLike 功能 - 使用簡單的字串匹配
            var filterGroup = FilterDictionaryBuilder.QueryBuilder<Contract>()
                .WithLogicalOperator(LogicalOperator.And)
                .ArrayLike(c => c.BuildContracts, bc => bc.Build.Name, "豪宅")
                .ToFilterGroup();

            var predicate = FilterBuilder.Build<Contract>(filterGroup);
            var query = context.Contracts
                .Include(c => c.BuildContracts)
                .ThenInclude(bc => bc.Build)
                .Where(predicate);
            var results = query.ToList();

            // 應該找到包含建案名稱符合 Like 條件的合約
            results.Should().HaveCount(1);
            results.Should().Contain(c => c.Name == "豪宅購買合約");
        }

        [Fact]
        public void FilterBuilder_ExpressionVersion_ArrayStringOperations_ShouldWork()
        {
            using var context = CreateContractTestContext();

            // 測試 ArrayContains
            var filterGroup1 = FilterDictionaryBuilder.QueryBuilder<Contract>()
                .WithLogicalOperator(LogicalOperator.And)
                .ArrayContains(c => c.BuildContracts, bc => bc.Build.Name, "豪宅")
                .ToFilterGroup();

            var predicate1 = FilterBuilder.Build<Contract>(filterGroup1);
            var results1 = context.Contracts
                .Include(c => c.BuildContracts)
                .ThenInclude(bc => bc.Build)
                .Where(predicate1)
                .ToList();

            results1.Should().HaveCount(1);
            results1.Should().Contain(c => c.Name == "豪宅購買合約");

            // 測試 ArrayStartsWith
            var filterGroup2 = FilterDictionaryBuilder.QueryBuilder<Contract>()
                .WithLogicalOperator(LogicalOperator.And)
                .ArrayStartsWith(c => c.BuildContracts, bc => bc.Build.Location, "台北市")
                .ToFilterGroup();

            var predicate2 = FilterBuilder.Build<Contract>(filterGroup2);
            var results2 = context.Contracts
                .Include(c => c.BuildContracts)
                .ThenInclude(bc => bc.Build)
                .Where(predicate2)
                .ToList();

            // 應該找到所有台北市的建案合約
            results2.Should().HaveCount(3); // 所有建案都在台北市
            
            // 測試 ArrayNotContains
            var filterGroup3 = FilterDictionaryBuilder.QueryBuilder<Contract>()
                .WithLogicalOperator(LogicalOperator.And)
                .ArrayNotContains(c => c.BuildContracts, bc => bc.Build.Name, "不存在的")
                .ToFilterGroup();

            var predicate3 = FilterBuilder.Build<Contract>(filterGroup3);
            var results3 = context.Contracts
                .Include(c => c.BuildContracts)
                .ThenInclude(bc => bc.Build)
                .Where(predicate3)
                .ToList();

            results3.Should().HaveCount(3); // 所有合約都不包含"不存在的"
        }

        [Fact]
        public void FilterBuilder_ExpressionVersion_ArrayComparisonOperations_ShouldWork()
        {
            using var context = CreateContractTestContext();

            // 測試 ArrayNotEqual
            var filterGroup1 = FilterDictionaryBuilder.QueryBuilder<Contract>()
                .WithLogicalOperator(LogicalOperator.And)
                .ArrayNotEqual(c => c.BuildContracts, bc => bc.Build.AptId, 1001L)
                .ToFilterGroup();

            var predicate1 = FilterBuilder.Build<Contract>(filterGroup1);
            var results1 = context.Contracts
                .Include(c => c.BuildContracts)
                .ThenInclude(bc => bc.Build)
                .Where(predicate1)
                .ToList();

            results1.Should().HaveCount(2);
            results1.Should().Contain(c => c.Name == "投資購買合約");
            results1.Should().Contain(c => c.Name == "自住購買合約");

            // 測試 ArrayLessThan
            var filterGroup2 = FilterDictionaryBuilder.QueryBuilder<Contract>()
                .WithLogicalOperator(LogicalOperator.And)
                .ArrayLessThan(c => c.BuildContracts, bc => bc.Build.AptId, 1003L)
                .ToFilterGroup();

            var predicate2 = FilterBuilder.Build<Contract>(filterGroup2);
            var results2 = context.Contracts
                .Include(c => c.BuildContracts)
                .ThenInclude(bc => bc.Build)
                .Where(predicate2)
                .ToList();

            results2.Should().HaveCount(2);
            results2.Should().Contain(c => c.Name == "豪宅購買合約");
            results2.Should().Contain(c => c.Name == "投資購買合約");

            // 測試 ArrayGreaterThanOrEqual
            var filterGroup3 = FilterDictionaryBuilder.QueryBuilder<Contract>()
                .WithLogicalOperator(LogicalOperator.And)
                .ArrayGreaterThanOrEqual(c => c.BuildContracts, bc => bc.Build.AptId, 1002L)
                .ToFilterGroup();

            var predicate3 = FilterBuilder.Build<Contract>(filterGroup3);
            var queryResults3 = context.Contracts
                .Include(c => c.BuildContracts)
                .ThenInclude(bc => bc.Build)
                .Where(predicate3)
                .ToList();

            // 應該找到豪宅購買合約和投資購買合約
            queryResults3.Should().HaveCount(2);
            queryResults3.Should().Contain(c => c.Name == "豪宅購買合約");
            queryResults3.Should().Contain(c => c.Name == "投資購買合約");
        }

        [Fact]
        public void FilterBuilder_ExpressionVersion_ArrayAdvancedOperations_ShouldWork()
        {
            using var context = CreateContractTestContext();

            // 測試 ArrayNotIn - 尋找不包含特定 AptId 的合約
            var filterGroup1 = FilterDictionaryBuilder.QueryBuilder<Contract>()
                .WithLogicalOperator(LogicalOperator.And)
                .ArrayNotIn(c => c.BuildContracts, bc => bc.Build.AptId, [1001L, 1004L])
                .ToFilterGroup();

            var predicate1 = FilterBuilder.Build<Contract>(filterGroup1);
            var results1 = context.Contracts
                .Include(c => c.BuildContracts)
                .ThenInclude(bc => bc.Build)
                .Where(predicate1)
                .ToList();

            // 調整預期結果：只有 "自住購買合約" 的 AptId 為 null（不在 [1001L, 1004L] 中）
            results1.Should().HaveCount(1);
            results1.Should().Contain(c => c.Name == "自住購買合約"); // 包含 null

            // 測試 ArrayNotBetween - 尋找不在 1001L-1002L 範圍內的合約
            var filterGroup2 = FilterDictionaryBuilder.QueryBuilder<Contract>()
                .WithLogicalOperator(LogicalOperator.And)
                .ArrayNotBetween(c => c.BuildContracts, bc => bc.Build.AptId, 1001L, 1002L)
                .ToFilterGroup();

            var predicate2 = FilterBuilder.Build<Contract>(filterGroup2);
            var results2 = context.Contracts
                .Include(c => c.BuildContracts)
                .ThenInclude(bc => bc.Build)
                .Where(predicate2)
                .ToList();

            // 調整預期結果：只有 "自住購買合約"（AptId 為 null，不在範圍內）
            // "投資購買合約" 包含 1002L（在範圍內）和 1004L（不在範圍內），可能被排除
            results2.Should().HaveCount(1);
            results2.Should().Contain(c => c.Name == "自住購買合約"); // 包含 null

            // 測試 ArrayNotLike - 使用簡單字串
            var filterGroup3 = FilterDictionaryBuilder.QueryBuilder<Contract>()
                .WithLogicalOperator(LogicalOperator.And)
                .ArrayNotLike(c => c.BuildContracts, bc => bc.Build.Name, "豪宅")
                .ToFilterGroup();

            var predicate3 = FilterBuilder.Build<Contract>(filterGroup3);
            var results3 = context.Contracts
                .Include(c => c.BuildContracts)
                .ThenInclude(bc => bc.Build)
                .Where(predicate3)
                .ToList();

            results3.Should().HaveCount(2);
            results3.Should().Contain(c => c.Name == "投資購買合約");
            results3.Should().Contain(c => c.Name == "自住購買合約");
        }

        [Fact]
        public void FilterBuilder_ExpressionVersion_ArrayMixedOperations_ComplexQuery_ShouldWork()
        {
            using var context = CreateContractTestContext();

            // 複雜查詢：結合多種陣列導覽方法
            var filterGroup = FilterDictionaryBuilder.QueryBuilder<Contract>()
                .WithLogicalOperator(LogicalOperator.And)
                .ArrayGreaterThan(c => c.BuildContracts, bc => bc.Build.AptId, 1000L)
                .ArrayLike(c => c.BuildContracts, bc => bc.Build.Location, "台北市")
                .ArrayNotEqual(c => c.BuildContracts, bc => bc.ContractType, "自住")
                .Contains(c => c.Name, "購買")
                .ToFilterGroup();

            var predicate = FilterBuilder.Build<Contract>(filterGroup);
            var query = context.Contracts
                .Include(c => c.BuildContracts)
                .ThenInclude(bc => bc.Build)
                .Where(predicate);
            var results = query.ToList();

            // 應該找到符合所有條件的合約
            results.Should().HaveCount(2);
            results.Should().Contain(c => c.Name == "豪宅購買合約");
            results.Should().Contain(c => c.Name == "投資購買合約");
        }

        #endregion
    }
}