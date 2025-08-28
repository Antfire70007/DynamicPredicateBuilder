using System.Collections.Generic;
namespace DynamicPredicate.Tests
{
    public class User
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }
}
