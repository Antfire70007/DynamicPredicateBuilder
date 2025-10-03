using Xunit;
using DynamicPredicate.Tests.Examples;

namespace DynamicPredicate.Tests.Examples
{
    public class SqlDemonstrationTests
    {
        [Fact]
        public void RunSqlDemonstration()
        {
            // This test demonstrates that decimal comparisons work correctly with SQL generation
            SqlDemonstration.DemonstrateDecimalSqlGeneration();
        }
    }
}