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
    using Graceful.Dynamic;
    using System.Linq.Expressions;

    public class ExpressionBuilderTests
    {
        [Fact]
        public void BuildPredicateExpressionTest()
        {
            Expression<Func<Models.User, bool>> compliedExpression =
                e => e.Id == 1;

            Expression<Func<Models.User, bool>> builtExpression =
                ExpressionBuilder.BuildPredicateExpression<Models.User>
                (
                    "e.Id == 1"
                );

            Assert.Equal
            (
                compliedExpression.ToString(),
                builtExpression.ToString()
            );
        }

        [Fact]
        public void BuildPropertySelectExpressionTest()
        {
            Expression<Func<Models.User, object>> compliedExpression =
                e => e.Id;

            Expression<Func<Models.User, object>> builtExpression =
                ExpressionBuilder.BuildPropertySelectExpression<Models.User>
                (
                    "e.Id"
                );

            Assert.Equal
            (
                compliedExpression.ToString(),
                builtExpression.ToString()
            );
        }

        [Fact]
        public void BuildEqualityExpressionTest()
        {
            var e2 = new Models.EqualityExpressionTest
            {
                Foo = "Bar"
            };

            Expression<Func<Models.EqualityExpressionTest, bool>> builtExpression =
                ExpressionBuilder.BuildEqualityExpression<Models.EqualityExpressionTest>(e2);

            Assert.Equal
            (
                "e1 => (e1.Foo == value(Graceful.Tests.Models.EqualityExpressionTest).Foo)",
                builtExpression.ToString()
            );
        }
    }
}
