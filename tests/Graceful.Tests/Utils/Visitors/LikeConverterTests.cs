////////////////////////////////////////////////////////////////////////////////
//           ________                                _____        __
//          /  _____/_______ _____     ____   ____ _/ ____\__ __ |  |
//         /   \  ___\_  __ \\__  \  _/ ___\_/ __ \\   __\|  |  \|  |
//         \    \_\  \|  | \/ / __ \_\  \___\  ___/ |  |  |  |  /|  |__
//          \______  /|__|   (____  / \___  >\___  >|__|  |____/ |____/
//                 \/             \/      \/     \/
// =============================================================================
//           Designed & Developed by Brad Jones <brad @="bjc.id.au" />
// =============================================================================
////////////////////////////////////////////////////////////////////////////////

namespace Graceful.Tests
{
    using Xunit;
    using System;
    using System.Linq.Expressions;
    using Graceful.Utils.Visitors;

    public class LikeConverterTests
    {
        class GracefulTestModel
        {
            public string Foo { get; set; }
            public int Bar { get; set; }
        }

        [Fact]
        public void BasicTest()
        {
            Expression<Func<GracefulTestModel, bool>> expression =
                e => e.Foo == "Hello";

            var converter = new LikeConverter();
            converter.Visit(expression.Body);

            Assert.Equal("([Foo] LIKE {0})", converter.Sql);
            Assert.Equal(new object[] {"Hello"}, converter.Parameters);
        }

        [Fact]
        public void NotTest()
        {
            Expression<Func<GracefulTestModel, bool>> expression =
                e => e.Foo != "Hello";

            var converter = new LikeConverter();
            converter.Visit(expression.Body);

            Assert.Equal("([Foo] NOT LIKE {0})", converter.Sql);
            Assert.Equal(new object[] {"Hello"}, converter.Parameters);
        }

        [Fact]
        public void AndTest()
        {
            Expression<Func<GracefulTestModel, bool>> expression =
                e => e.Foo == "Hello" && e.Bar == 123;

            var converter = new LikeConverter();
            converter.Visit(expression.Body);

            Assert.Equal("(([Foo] LIKE {0}) AND ([Bar] LIKE {1}) )", converter.Sql);
            Assert.Equal(new object[] {"Hello", 123}, converter.Parameters);
        }

        [Fact]
        public void OrTest()
        {
            Expression<Func<GracefulTestModel, bool>> expression =
                e => e.Foo == "Hello" || e.Bar == 123;

            var converter = new LikeConverter();
            converter.Visit(expression.Body);

            Assert.Equal("(([Foo] LIKE {0}) OR ([Bar] LIKE {1}) )", converter.Sql);
            Assert.Equal(new object[] {"Hello", 123}, converter.Parameters);
        }

        [Fact]
        public void LocalVarTest()
        {
            var localVar = "Hello";

            Expression<Func<GracefulTestModel, bool>> expression =
                e => e.Foo == localVar;

            var converter = new LikeConverter();
            converter.Visit(expression.Body);

            Assert.Equal("([Foo] LIKE {0})", converter.Sql);
            Assert.Equal(new object[] {"Hello"}, converter.Parameters);
        }

        [Fact]
        public void MethodVarObjectTest()
        {
            MethodVarObjectTestPrvate(new Models.CustomContext { Foo = "Bar" });
        }

        private void MethodVarObjectTestPrvate(Models.CustomContext entity)
        {
            Expression<Func<Models.CustomContext, bool>> expression =
                e => e.Foo == entity.Foo;

            var converter = new LikeConverter();
            converter.Visit(expression.Body);

            Assert.Equal("([Foo] LIKE {0})", converter.Sql);
            Assert.Equal(new object[] { "Bar" }, converter.Parameters);
        }

        [Theory]
        [InlineData(123)]
        public void MethodVarTest(int value)
        {
            Expression<Func<GracefulTestModel, bool>> expression =
                e => e.Bar == value;

            var converter = new LikeConverter();
            converter.Visit(expression.Body);

            Assert.Equal("([Bar] LIKE {0})", converter.Sql);
            Assert.Equal(new object[] {123}, converter.Parameters);
        }
    }
}
