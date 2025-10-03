using System.Collections.Generic;
using System.Linq;
using DynamicPredicateBuilder;
using DynamicPredicateBuilder.Models;
using Xunit;

namespace DynamicPredicate.Tests.Builders
{
    public class SortRuleBuilderTests
    {
        private class TestEntity
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public decimal Salary { get; set; }
            public NestedEntity Department { get; set; }
        }

        private class NestedEntity
        {
            public string Code { get; set; }
            public string Name { get; set; }
        }

        [Fact]
        public void SortBuilder_StringVersion_BuildsCorrectly()
        {
            // Arrange & Act
            var sortRules = SortRuleBuilder.SortBuilder<TestEntity>()
                .Ascending("Name")
                .Descending("Age")
                .Build();

            // Assert
            Assert.Equal(2, sortRules.Count);
            Assert.Equal("Name", sortRules[0].Property);
            Assert.False(sortRules[0].Descending);
            Assert.Equal("Age", sortRules[1].Property);
            Assert.True(sortRules[1].Descending);
        }

        [Fact]
        public void SortBuilder_ExpressionVersion_BuildsCorrectly()
        {
            // Arrange & Act
            var sortRules = SortRuleBuilder.SortBuilder<TestEntity>()
                .Ascending(x => x.Name)
                .Descending(x => x.Age)
                .Build();

            // Assert
            Assert.Equal(2, sortRules.Count);
            Assert.Equal("Name", sortRules[0].Property);
            Assert.False(sortRules[0].Descending);
            Assert.Equal("Age", sortRules[1].Property);
            Assert.True(sortRules[1].Descending);
        }

        [Fact]
        public void SortBuilder_ThenByMethods_BuildsCorrectly()
        {
            // Arrange & Act
            var sortRules = SortRuleBuilder.SortBuilder<TestEntity>()
                .Ascending(x => x.Name)
                .ThenBy(x => x.Age)
                .ThenByDescending(x => x.Salary)
                .Build();

            // Assert
            Assert.Equal(3, sortRules.Count);
            Assert.Equal("Name", sortRules[0].Property);
            Assert.False(sortRules[0].Descending);
            Assert.Equal("Age", sortRules[1].Property);
            Assert.False(sortRules[1].Descending);
            Assert.Equal("Salary", sortRules[2].Property);
            Assert.True(sortRules[2].Descending);
        }

        [Fact]
        public void SortBuilder_NestedProperties_BuildsCorrectly()
        {
            // Arrange & Act
            var sortRules = SortRuleBuilder.SortBuilder<TestEntity>()
                .Ascending(x => x.Department.Name)
                .Descending(x => x.Department.Code)
                .Build();

            // Assert
            Assert.Equal(2, sortRules.Count);
            Assert.Equal("Department.Name", sortRules[0].Property);
            Assert.False(sortRules[0].Descending);
            Assert.Equal("Department.Code", sortRules[1].Property);
            Assert.True(sortRules[1].Descending);
        }

        [Fact]
        public void SortBuilder_ImplicitConversion_WorksCorrectly()
        {
            // Arrange
            var builder = SortRuleBuilder.SortBuilder<TestEntity>()
                .Ascending(x => x.Name)
                .Descending(x => x.Age);

            // Act
            List<SortRule> listConversion = builder;
            SortRule[] arrayConversion = builder;

            // Assert
            Assert.Equal(2, listConversion.Count);
            Assert.Equal(2, arrayConversion.Length);
            Assert.Equal("Name", listConversion[0].Property);
            Assert.Equal("Age", arrayConversion[1].Property);
        }

        [Fact]
        public void SortBuilder_AlternativeCreationMethods_WorkCorrectly()
        {
            // Method 1
            var builder1 = SortRuleBuilder.SortBuilder<TestEntity>();
            
            // Method 2
            var builder2 = SortRuleBuilder.Create<TestEntity>();
            
            // Method 3
            var builder3 = SortRuleBuilder<TestEntity>.Create();

            // Act
            builder1.Ascending(x => x.Name);
            builder2.Ascending(x => x.Age);
            builder3.Ascending(x => x.Salary);

            // Assert
            Assert.Single(builder1.Build());
            Assert.Single(builder2.Build());
            Assert.Single(builder3.Build());
            Assert.Equal("Name", builder1.Build()[0].Property);
            Assert.Equal("Age", builder2.Build()[0].Property);
            Assert.Equal("Salary", builder3.Build()[0].Property);
        }

        [Fact]
        public void SortBuilder_AddMethod_BuildsCorrectly()
        {
            // Arrange & Act - String version
            var sortRules1 = SortRuleBuilder.SortBuilder<TestEntity>()
                .Add("Name", false)
                .Add("Age", true)
                .Build();

            // Arrange & Act - Expression version
            var sortRules2 = SortRuleBuilder.SortBuilder<TestEntity>()
                .Add(x => x.Name, false)
                .Add(x => x.Age, true)
                .Build();

            // Assert
            Assert.Equal(2, sortRules1.Count);
            Assert.Equal("Name", sortRules1[0].Property);
            Assert.False(sortRules1[0].Descending);
            Assert.Equal("Age", sortRules1[1].Property);
            Assert.True(sortRules1[1].Descending);

            Assert.Equal(2, sortRules2.Count);
            Assert.Equal("Name", sortRules2[0].Property);
            Assert.False(sortRules2[0].Descending);
            Assert.Equal("Age", sortRules2[1].Property);
            Assert.True(sortRules2[1].Descending);
        }
    }
}