namespace Graceful.Tests
{
    using Xunit;
    using System;
    using System.Collections.Generic;

    public class ModelTests
    {
        [Fact]
        public void GetAllModelsTest()
        {
            // NOTE: Order is important!
            Assert.Equal
            (
                new List<Type>
                {
                    typeof(Models.CustomContext),
                    typeof(Models.CustomTableName),
                    typeof(Models.LazyManyToOneTestModel1),
                    typeof(Models.LazyManyToOneTestModel2),
                    typeof(Models.LazyManyToOneTestModel3),
                    typeof(Models.LazyOneToOneTestModel1),
                    typeof(Models.LazyOneToOneTestModel2),
                    typeof(Models.LazyOneToOneTestModel3),
                    typeof(Models.MultipleManyToManyTestModel1),
                    typeof(Models.MultipleManyToManyTestModel2),
                    typeof(Models.MultipleManyToOneTestModel1),
                    typeof(Models.MultipleManyToOneTestModel2),
                    typeof(Models.MultipleOneToOneTestModel1),
                    typeof(Models.MultipleOneToOneTestModel2),
                    typeof(Models.SimpleManyToManyTestModel1),
                    typeof(Models.SimpleManyToManyTestModel2),
                    typeof(Models.SimpleManyToOneTestModel1),
                    typeof(Models.SimpleManyToOneTestModel2),
                    typeof(Models.SimpleOneToOneTestModel1),
                    typeof(Models.SimpleOneToOneTestModel2),
                    typeof(Models.User),
                    typeof(Models.Address),
                    typeof(Models.Car),
                    typeof(Models.Group)
                },
                Model.GetAllModels()
            );
        }

        [Theory]
        [InlineData("Graceful.Tests.Models.User")]
        [InlineData("user")]
        [InlineData("User")]
        [InlineData("Users")]
        public void GetModelTest(string value)
        {
            Assert.Equal(typeof(Models.User), Model.GetModel(value));
        }
    }
}
