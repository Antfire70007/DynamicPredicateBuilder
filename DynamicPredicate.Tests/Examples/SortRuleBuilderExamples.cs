using System;
using System.Linq;
using DynamicPredicateBuilder;
using DynamicPredicateBuilder.Models;
using DynamicPredicate.Tests;

namespace DynamicPredicate.Tests.Examples
{
    /// <summary>
    /// SortRuleBuilder 使用範例
    /// </summary>
    public class SortRuleBuilderExamples
    {
        public static void BasicSortingExamples()
        {
            // 簡單排序 - 字串版本
            var sortRules1 = SortRuleBuilder.SortBuilder<User>()
                .Ascending(nameof(User.Name))
                .Descending(nameof(User.Age))
                .Build();

            // 簡單排序 - Expression 版本
            var sortRules2 = SortRuleBuilder.SortBuilder<User>()
                .Ascending(x => x.Name)
                .Descending(x => x.Age)
                .Build();

            // 多層級排序
            var sortRules3 = SortRuleBuilder.SortBuilder<User>()
                .Ascending(x => x.Status)
                .ThenBy(x => x.Name)
                .ThenByDescending(x => x.Age)
                .Build();

            // 使用 nullable decimal 欄位排序
            var sortRules4 = SortRuleBuilder.SortBuilder<User>()
                .Descending(x => x.Salary)
                .ThenBy(x => x.Name)
                .Build();

            // 隱式轉換
            SortRule[] sortArray = SortRuleBuilder.SortBuilder<User>()
                .Ascending(x => x.Name)
                .Descending(x => x.Age);
        }

        public static void CombinedWithFilteringExample()
        {
            // 結合排序和過濾範例
            var query = FilterDictionaryBuilder.QueryBuilder<User>()
                .WithLogicalOperator(LogicalOperator.Or)
                .Like(x => x.Name, "john")
                .GreaterThan(x => x.Age, 21)
                .Build();

            var sortRules = SortRuleBuilder.SortBuilder<User>()
                .Descending(x => x.Age)
                .ThenBy(x => x.Name)
                .Build();

            // 建構查詢請求
            var request = new QueryRequest
            {
                Filter = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(
                    System.Text.Json.JsonSerializer.Serialize(query)),
                Sort = sortRules,
                Page = 1,
                PageSize = 20
            };

            // 在實際應用中，這會傳遞給服務層
            // var results = _userService.Query(request);
        }

        public static void NullableDecimalSortingExample()
        {
            // 針對 nullable decimal 欄位的排序範例
            var sortRules = SortRuleBuilder.SortBuilder<User>()
                .Descending(x => x.Salary)  // nullable decimal 降序排列
                .ThenBy(x => x.Name)        // 相同薪資時按姓名升序
                .ThenByDescending(x => x.Age) // 最後按年齡降序
                .Build();

            // 薪資排序組合範例
            var salaryFirstSort = SortRuleBuilder.SortBuilder<User>()
                .Ascending(x => x.Salary)   // 薪資升序 (null 值會排在前面或後面，依實作而定)
                .ThenBy(x => x.Status)      // 相同薪資時按狀態排序
                .Build();
        }

        // 使用自訂類別的範例
        private class UserWithProfile
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public decimal? Salary { get; set; }
            public ProfileInfo Profile { get; set; }
            public DateTime CreatedDate { get; set; }
        }

        private class ProfileInfo
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        public static void NestedPropertySortingExample()
        {
            // 使用巢狀屬性排序
            var sortRules = SortRuleBuilder.SortBuilder<UserWithProfile>()
                .Ascending(x => x.Profile.FirstName)
                .ThenBy(x => x.Profile.LastName)
                .ThenByDescending(x => x.CreatedDate)
                .Build();
        }

        public static void UseWithLinqExample()
        {
            // 假設有一個使用者集合
            var users = new[]
            {
                new User { Name = "alice", Age = 25, Salary = 50000.50m },
                new User { Name = "bob", Age = 30, Salary = null },
                new User { Name = "charlie", Age = 25, Salary = 75000.00m }
            }.AsQueryable();

            // 建立排序規則
            var sortRules = SortRuleBuilder.SortBuilder<User>()
                .Ascending(x => x.Age)
                .ThenBy(x => x.Name)
                .Build();

            // 包含薪資排序的範例
            var sortWithSalary = SortRuleBuilder.SortBuilder<User>()
                .Descending(x => x.Salary)
                .ThenBy(x => x.Age)
                .ThenBy(x => x.Name)
                .Build();

            // 假設有一個擴充方法可以套用排序規則到 IQueryable
            // var sortedUsers = users.ApplySorting(sortRules);
            
            // 或手動實現排序邏輯
            IQueryable<User> query = users;
            foreach (var rule in sortRules)
            {
                // 這裡需要實際的排序邏輯，示例僅做示意
                // query = rule.Descending 
                //     ? query.AppendOrderByDescending(rule.Property) 
                //     : query.AppendOrderBy(rule.Property);
            }
        }
    }
}