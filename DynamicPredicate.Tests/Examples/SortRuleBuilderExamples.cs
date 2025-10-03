using System;
using System.Linq;
using DynamicPredicateBuilder;
using DynamicPredicateBuilder.Models;
using DynamicPredicate.Tests;

namespace DynamicPredicate.Tests.Examples
{
    /// <summary>
    /// SortRuleBuilder �ϥνd��
    /// </summary>
    public class SortRuleBuilderExamples
    {
        public void BasicSortingExamples()
        {
            // ²��Ƨ� - �r�ꪩ��
            var sortRules1 = SortRuleBuilder.SortBuilder<User>()
                .Ascending(nameof(User.Name))
                .Descending(nameof(User.Age))
                .Build();

            // ²��Ƨ� - Expression ����
            var sortRules2 = SortRuleBuilder.SortBuilder<User>()
                .Ascending(x => x.Name)
                .Descending(x => x.Age)
                .Build();

            // �h�h�űƧ�
            var sortRules3 = SortRuleBuilder.SortBuilder<User>()
                .Ascending(x => x.Status)
                .ThenBy(x => x.Name)
                .ThenByDescending(x => x.Age)
                .Build();

            // �ϥ� nullable decimal ���Ƨ�
            var sortRules4 = SortRuleBuilder.SortBuilder<User>()
                .Descending(x => x.Salary)
                .ThenBy(x => x.Name)
                .Build();

            // �����ഫ
            SortRule[] sortArray = SortRuleBuilder.SortBuilder<User>()
                .Ascending(x => x.Name)
                .Descending(x => x.Age);
        }

        public void CombinedWithFilteringExample()
        {
            // ���X�ƧǩM�L�o�d��
            var query = FilterDictionaryBuilder.QueryBuilder<User>()
                .WithLogicalOperator(LogicalOperator.Or)
                .Like(x => x.Name, "john")
                .GreaterThan(x => x.Age, 21)
                .Build();

            var sortRules = SortRuleBuilder.SortBuilder<User>()
                .Descending(x => x.Age)
                .ThenBy(x => x.Name)
                .Build();

            // �غc�d�߽ШD
            var request = new QueryRequest
            {
                Filter = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(
                    System.Text.Json.JsonSerializer.Serialize(query)),
                Sort = sortRules,
                Page = 1,
                PageSize = 20
            };

            // �b������Τ��A�o�|�ǻ����A�ȼh
            // var results = _userService.Query(request);
        }

        public void NullableDecimalSortingExample()
        {
            // �w�� nullable decimal ��쪺�Ƨǽd��
            var sortRules = SortRuleBuilder.SortBuilder<User>()
                .Descending(x => x.Salary)  // nullable decimal ���ǱƦC
                .ThenBy(x => x.Name)        // �ۦP�~��ɫ��m�W�ɧ�
                .ThenByDescending(x => x.Age) // �̫���~�֭���
                .Build();

            // �~��ƧǲզX�d��
            var salaryFirstSort = SortRuleBuilder.SortBuilder<User>()
                .Ascending(x => x.Salary)   // �~��ɧ� (null �ȷ|�Ʀb�e���Ϋ᭱�A�̹�@�өw)
                .ThenBy(x => x.Status)      // �ۦP�~��ɫ����A�Ƨ�
                .Build();
        }

        // �ϥΦۭq���O���d��
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

        public void NestedPropertySortingExample()
        {
            // �ϥα_���ݩʱƧ�
            var sortRules = SortRuleBuilder.SortBuilder<UserWithProfile>()
                .Ascending(x => x.Profile.FirstName)
                .ThenBy(x => x.Profile.LastName)
                .ThenByDescending(x => x.CreatedDate)
                .Build();
        }

        public void UseWithLinqExample()
        {
            // ���]���@�ӨϥΪ̶��X
            var users = new[]
            {
                new User { Name = "alice", Age = 25, Salary = 50000.50m },
                new User { Name = "bob", Age = 30, Salary = null },
                new User { Name = "charlie", Age = 25, Salary = 75000.00m }
            }.AsQueryable();

            // �إ߱ƧǳW�h
            var sortRules = SortRuleBuilder.SortBuilder<User>()
                .Ascending(x => x.Age)
                .ThenBy(x => x.Name)
                .Build();

            // �]�t�~��ƧǪ��d��
            var sortWithSalary = SortRuleBuilder.SortBuilder<User>()
                .Descending(x => x.Salary)
                .ThenBy(x => x.Age)
                .ThenBy(x => x.Name)
                .Build();

            // ���]���@���X�R��k�i�H�M�αƧǳW�h�� IQueryable
            // var sortedUsers = users.ApplySorting(sortRules);
            
            // �Τ�ʹ�{�Ƨ��޿�
            IQueryable<User> query = users;
            foreach (var rule in sortRules)
            {
                // �o�̻ݭn��ڪ��Ƨ��޿�A�ܨҶȰ��ܷN
                // query = rule.Descending 
                //     ? query.AppendOrderByDescending(rule.Property) 
                //     : query.AppendOrderBy(rule.Property);
            }
        }
    }
}