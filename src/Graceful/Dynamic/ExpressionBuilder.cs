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
        public static Expression<Func<T, bool>> BuildPredicateExpression<T>(string expression)
        {
            return (Expression<Func<T, bool>>)DynamicExpression.ParseLambda
            (
                new[] { Expression.Parameter(typeof(T), "e") },
                typeof(bool),
                expression
            );
        }

        public static Expression<Func<T, object>> BuildAssignmentExpression<T>(string expression)
        {
            return (Expression<Func<T, object>>)DynamicExpression.ParseLambda
            (
                new[] { Expression.Parameter(typeof(T), "e") },
                typeof(object),
                expression
            );
        }

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
