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

namespace Graceful.Dynamic
{
    using System;
    using System.Text;
    using Graceful.Utils;
    using System.Linq.Expressions;
    using System.Collections.Generic;
    using DynamicExpression = System.Linq.Dynamic.DynamicExpression;

    public static class ExpressionBuilder
    {
        /**
         * Given a string, we create an Expression, to be used as a predicate.
         *
         * ```cs
         * 	using Graceful.Dynamic;
         *
         * 	Expression<Func<T, bool>> compliedExpression =
         * 		e => e.Id == 1;
         *
         * 	Expression<Func<T, bool>> builtExpression =
         * 		ExpressionBuilder.BuildPredicateExpression<Foo>("e.Id == 1");
         *
         * 	compliedExpression == builtExpression
         * ```
         *
         * > NOTE: You must always use the parameter of _"e"_.
         */
        public static Expression<Func<T, bool>> BuildPredicateExpression<T>(string expression)
        {
            return (Expression<Func<T, bool>>)DynamicExpression.ParseLambda
            (
                new[] { Expression.Parameter(typeof(T), "e") },
                typeof(bool),
                expression
            );
        }

        /**
         * Create an Expression, to be used for property selection.
         *
         * ```cs
         * 	using Graceful.Dynamic;
         *
         * 	Expression<Func<T, object>> compliedExpression =
         * 		e => e.CreatedAt;
         *
         * 	Expression<Func<T, object>> builtExpression =
         * 		ExpressionBuilder.BuildPropertySelectExpression<Foo>
         * 		(
         * 			"e.CreatedAt"
         * 		);
         *
         * 	// compliedExpression == builtExpression
         *
         * 	Foo.Where(e => e.Bar == "Baz").OrderBy(builtExpression).ToList();
         * ```
         *
         * > NOTE: You must always use the parameter of _"e"_.
         */
        public static Expression<Func<T, object>> BuildPropertySelectExpression<T>(string expression)
        {
            return (Expression<Func<T, object>>)DynamicExpression.ParseLambda
            (
                new[] { Expression.Parameter(typeof(T), "e") },
                typeof(object),
                expression
            );
        }

        /**
         * Create an expression that checks for equality of all properties.
         *
         * ```cs
         * 	using Graceful.Dynamic;
         *
         * 	var e2 = new Foo
         * 	{
         * 		Bar = "abc",
         * 		Baz = 123
         * 	};
         *
         * 	Expression<Func<T, bool>> compliedExpression =
         * 		e1 => e1.Bar == e2.Bar && e1.Baz == e2.Baz;
         *
         * 	Expression<Func<T, bool>> builtExpression =
         * 		ExpressionBuilder.BuildEqualityExpression<Foo>(e2);
         *
         * 	// compliedExpression == builtExpression
         * ```
         *
         * > NOTE: Some properties are excluded from the equality check,
         * > such as Id, CreatedAt, ModifiedAt & DeletedAt. Also beaware
         * > only primatives are checked, relationships are completely ignored.
         */
        public static Expression<Func<T, bool>> BuildEqualityExpression<T>(T e2) where T : Model<T>, new()
        {
            var expressionString = new StringBuilder();

            // Grab our properties
            var props = Graceful.Model.Dynamic(e2).MappedPropsExceptId;

            // Remove all properties that are not primatives.
            props.RemoveAll(prop => !TypeMapper.IsClrType(prop.PropertyType));

            // Remove all properties that have null values.
            props.RemoveAll(prop => prop.GetValue(e2) == null);

            // Remove the CreatedAt, ModifiedAt & DeletedAt timestamps
            props.RemoveAll(prop => prop.Name == "CreatedAt");
            props.RemoveAll(prop => prop.Name == "ModifiedAt");
            props.RemoveAll(prop => prop.Name == "DeletedAt");

            // Loop through each property
            props.ForEach(prop =>
            {
                // Build our lamda expression
                expressionString.Append("e1.");
                expressionString.Append(prop.Name);
                expressionString.Append(" == e2.");
                expressionString.Append(prop.Name);
                expressionString.Append(" && ");
            });

            // If nothing was added to our expression bail out.
            if (expressionString.Length == 0) return null;

            // Remove the last &&
            expressionString.Remove(expressionString.Length - 4, 4);

            // Build the expression
            return (Expression<Func<T, bool>>)DynamicExpression.ParseLambda
            (
                new[] { Expression.Parameter(typeof(T), "e1") },
                typeof(bool),
                expressionString.ToString(),
                new Dictionary<string, object>{ {"e2", e2} }
            );
        }
    }
}
