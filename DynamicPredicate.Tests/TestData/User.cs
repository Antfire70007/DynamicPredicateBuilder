using System.Collections.Generic;
namespace DynamicPredicate.Tests
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal? Salary { get; set; }
        public List<string> Tags { get; set; } = [];
        public List<int> Numbers { get; set; } = [];
    }
}
