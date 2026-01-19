using System;
using System.Collections.Generic;
using System.Linq;
using DynamicPredicateBuilder;
using DynamicPredicateBuilder.Core;
using DynamicPredicateBuilder.Models;
using DynamicPredicate.Tests.TestData;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace DynamicPredicate.Tests.Examples
{
    /// <summary>
    /// SortRuleBuilder 實際應用範例
    /// </summary>
    public class SortRuleBuilderPracticalExamples : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly ExtendedTestDbContext _context;

        public SortRuleBuilderPracticalExamples(ITestOutputHelper output)
        {
            _output = output;
            _context = CreateTestContext();
        }

        private ExtendedTestDbContext CreateTestContext()
        {
            var options = new DbContextOptionsBuilder<ExtendedTestDbContext>()
                .UseInMemoryDatabase(databaseName: $"PracticalSortTest_{Guid.NewGuid()}")
                .Options;

            var context = new ExtendedTestDbContext(options);

            // 建立測試資料
            var company1 = new Company { Id = 1, Name = "科技公司", Address = "台北" };
            var company2 = new Company { Id = 2, Name = "創新企業", Address = "新竹" };

            var dept1 = new Department { Id = 1, Name = "研發部", CompanyId = 1, Company = company1 };
            var dept2 = new Department { Id = 2, Name = "業務部", CompanyId = 1, Company = company1 };
            var dept3 = new Department { Id = 3, Name = "工程部", CompanyId = 2, Company = company2 };

            var emp1 = new Employee { Id = 1, Name = "張三", Salary = 50000, DepartmentId = 1, Department = dept1 };
            var emp2 = new Employee { Id = 2, Name = "李四", Salary = 60000, DepartmentId = 1, Department = dept1 };
            var emp3 = new Employee { Id = 3, Name = "王五", Salary = 55000, DepartmentId = 2, Department = dept2 };
            var emp4 = new Employee { Id = 4, Name = "趙六", Salary = 65000, DepartmentId = 3, Department = dept3 };

            dept1.Employees = new List<Employee> { emp1, emp2 };
            dept2.Employees = new List<Employee> { emp3 };
            dept3.Employees = new List<Employee> { emp4 };

            company1.Departments = new List<Department> { dept1, dept2 };
            company2.Departments = new List<Department> { dept3 };

            context.Companies.AddRange(company1, company2);
            context.Departments.AddRange(dept1, dept2, dept3);
            context.Employees.AddRange(emp1, emp2, emp3, emp4);
            context.SaveChanges();

            return context;
        }

        [Fact]
        public void Example01_Skip_Array_Navigation_Rules()
        {
            _output.WriteLine("=== 範例 1: 自動跳過陣列導覽屬性規則 ===\n");

            // 建立包含陣列導覽和一般屬性的混合排序規則
            var sortRules = SortRuleBuilder.Create<Department>()
                .Ascending(d => d.Company.Name)  // 一般導覽屬性 - 可在 EF 中執行
                .ArrayThenByDescending(d => d.Employees, e => e.Salary)  // 陣列導覽 - 無法在 EF 中執行
                .ThenBy(d => d.Name)  // 一般屬性 - 可在 EF 中執行
                .Build();

            _output.WriteLine("所有排序規則:");
            foreach (var rule in sortRules)
            {
                _output.WriteLine($"  {rule.Property} (降序: {rule.Descending})");
            }
            _output.WriteLine("");

            // 方法 1: 使用 skipArrayNavigation 參數自動跳過陣列導覽屬性
            var query = _context.Departments
                .Include(d => d.Company)
                .Include(d => d.Employees)
                .ApplySort(sortRules, skipArrayNavigation: true);  // 跳過陣列導覽

            var results = query.ToList();

            _output.WriteLine("資料庫排序結果（跳過陣列導覽屬性）:");
            foreach (var dept in results)
            {
                _output.WriteLine($"  {dept.Company.Name} - {dept.Name}");
            }

            Assert.NotEmpty(results);
        }

        [Fact]
        public void Example02_Separate_Rules_Manually()
        {
            _output.WriteLine("=== 範例 2: 手動分離排序規則 ===\n");

            // 建立混合排序規則
            var allSortRules = SortRuleBuilder.Create<Department>()
                .Ascending(d => d.Company.Name)
                .ArrayThenByDescending(d => d.Employees, e => e.Salary)
                .ThenBy(d => d.Name)
                .Build();

            // 分離出非陣列導覽屬性的規則
            var dbSortRules = allSortRules.GetNonArrayNavigationRules();
            var arraySortRules = allSortRules.GetArrayNavigationRules();

            _output.WriteLine("資料庫端排序規則:");
            foreach (var rule in dbSortRules)
            {
                _output.WriteLine($"  {rule.Property} (降序: {rule.Descending})");
            }

            _output.WriteLine("\n陣列導覽排序規則（需在記憶體中處理）:");
            foreach (var rule in arraySortRules)
            {
                _output.WriteLine($"  {rule.Property} (降序: {rule.Descending})");
            }
            _output.WriteLine("");

            // 先在資料庫端排序（只用非陣列導覽屬性）
            var query = _context.Departments
                .Include(d => d.Company)
                .Include(d => d.Employees)
                .ApplySort(dbSortRules);

            var results = query.ToList();

            // 在記憶體中進行陣列導覽屬性的排序
            if (arraySortRules.Any())
            {
                // 依 Employees[].Salary 降序排序
                results = results
                    .OrderByDescending(d => d.Employees.Any() ? d.Employees.Max(e => e.Salary) : 0)
                    .ToList();
            }

            _output.WriteLine("最終排序結果:");
            foreach (var dept in results)
            {
                var maxSalary = dept.Employees.Any() ? dept.Employees.Max(e => e.Salary) : 0;
                _output.WriteLine($"  {dept.Company.Name} - {dept.Name} - 最高薪資: {maxSalary:N0}");
            }

            Assert.NotEmpty(results);
        }

        [Fact]
        public void Example03_Error_When_Array_Navigation_Not_Skipped()
        {
            _output.WriteLine("=== 範例 3: 不跳過陣列導覽屬性時的錯誤處理 ===\n");

            // 建立包含陣列導覽的排序規則
            var sortRules = SortRuleBuilder.Create<Department>()
                .ArrayDescending(d => d.Employees, e => e.Salary)
                .Build();

            _output.WriteLine($"排序規則: {sortRules[0].Property}");

            // 嘗試直接執行會拋出異常
            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                var query = _context.Departments
                    .Include(d => d.Employees)
                    .ApplySort(sortRules);  // 不跳過，會拋出異常

                var results = query.ToList();
            });

            _output.WriteLine($"\n預期的錯誤訊息:\n{exception.Message}");

            Assert.Contains("陣列導覽屬性", exception.Message);
            Assert.Contains("無法在資料庫查詢中排序", exception.Message);
        }

        [Fact]
        public void Example04_Best_Practice_Mixed_Sorting()
        {
            _output.WriteLine("=== 範例 4: 最佳實踐 - 混合排序策略 ===\n");

            // 步驟 1: 建立完整的排序需求
            var allSortRules = SortRuleBuilder.Create<Department>()
                .Ascending(d => d.Company.Name)  // 資料庫排序
                .ThenBy(d => d.Name)  // 資料庫排序
                .ArrayThenByDescending(d => d.Employees, e => e.Salary)  // 記憶體排序
                .Build();

            _output.WriteLine("完整排序需求:");
            foreach (var rule in allSortRules)
            {
                var type = rule.Property.Contains("[]") ? "記憶體" : "資料庫";
                _output.WriteLine($"  [{type}] {rule.Property} (降序: {rule.Descending})");
            }
            _output.WriteLine("");

            // 步驟 2: 在資料庫端先過濾和預排序（減少資料量）
            var query = _context.Departments
                .Include(d => d.Company)
                .Include(d => d.Employees)
                .Where(d => d.Employees.Any())  // 先過濾
                .ApplySort(allSortRules, skipArrayNavigation: true);  // 只用非陣列規則排序

            // 步驟 3: 載入到記憶體
            var departments = query.ToList();

            _output.WriteLine($"從資料庫載入 {departments.Count} 筆資料");

            // 步驟 4: 在記憶體中應用陣列導覽屬性排序
            var arraySortRules = allSortRules.GetArrayNavigationRules();
            if (arraySortRules.Any())
            {
                _output.WriteLine("\n在記憶體中應用陣列導覽屬性排序...");
                
                // 根據 Employees[].Salary 降序排序
                departments = departments
                    .OrderByDescending(d => d.Employees.Max(e => e.Salary))
                    .ToList();
            }

            // 步驟 5: 輸出最終結果
            _output.WriteLine("\n最終排序結果:");
            foreach (var dept in departments)
            {
                var maxSalary = dept.Employees.Max(e => e.Salary);
                var avgSalary = dept.Employees.Average(e => e.Salary);
                _output.WriteLine($"  {dept.Company.Name} - {dept.Name}");
                _output.WriteLine($"    最高薪資: {maxSalary:N0}, 平均薪資: {avgSalary:N0}");
            }

            Assert.NotEmpty(departments);
        }

        [Fact]
        public void Example05_Only_Regular_Properties()
        {
            _output.WriteLine("=== 範例 5: 只使用一般屬性和導覽屬性排序 ===\n");

            // 只使用一般屬性，不使用陣列導覽
            var sortRules = SortRuleBuilder.Create<Employee>()
                .Ascending(e => e.Department.Company.Name)
                .ThenBy(e => e.Department.Name)
                .ThenByDescending(e => e.Salary)
                .Build();

            _output.WriteLine("排序規則:");
            foreach (var rule in sortRules)
            {
                _output.WriteLine($"  {rule.Property} (降序: {rule.Descending})");
            }
            _output.WriteLine("");

            // 可以直接在 EF Core 查詢中執行
            var results = _context.Employees
                .Include(e => e.Department)
                    .ThenInclude(d => d.Company)
                .ApplySort(sortRules)
                .ToList();

            _output.WriteLine("排序結果:");
            foreach (var emp in results)
            {
                _output.WriteLine($"  {emp.Department.Company.Name} - {emp.Department.Name} - {emp.Name} ({emp.Salary:N0})");
            }

            Assert.NotEmpty(results);
        }

        [Fact]
        public void Example06_Helper_Methods_Demo()
        {
            _output.WriteLine("=== 範例 6: 輔助方法展示 ===\n");

            // 建立混合規則
            var sortRules = SortRuleBuilder.Create<Department>()
                .Ascending(d => d.Name)
                .ArrayThenBy(d => d.Employees, e => e.Name)
                .ThenByDescending(d => d.Id)
                .ArrayThenByDescending(d => d.Employees, e => e.Salary)
                .Build();

            _output.WriteLine($"總共 {sortRules.Count} 個排序規則\n");

            // 使用輔助方法分離規則
            var nonArrayRules = sortRules.GetNonArrayNavigationRules();
            var arrayRules = sortRules.GetArrayNavigationRules();

            _output.WriteLine($"非陣列導覽規則 ({nonArrayRules.Count} 個):");
            foreach (var rule in nonArrayRules)
            {
                _output.WriteLine($"  {rule.Property}");
            }

            _output.WriteLine($"\n陣列導覽規則 ({arrayRules.Count} 個):");
            foreach (var rule in arrayRules)
            {
                _output.WriteLine($"  {rule.Property}");
            }

            Assert.Equal(2, nonArrayRules.Count);
            Assert.Equal(2, arrayRules.Count);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
