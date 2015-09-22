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

namespace Graceful.Utils.Visitors
{
    using System;
    using System.Text;
    using Graceful.Query;
    using System.Reflection;
    using System.Linq.Expressions;
    using System.Collections.Generic;

    /**
     * Given an Expression Tree, we will convert it into a SQL SET clause.
     *
     * ```
     * 	Expression<Func<TModel, bool>> expression =
     * 		m => m.FirstName == "Brad" && m.LastName == "Jones";
     *
     * 	var converter = new AssignmentsConverter();
     * 	converter.Visit(expression.Body);
     *
     * 	// converter.Sql == "FirstName = {0}, LastName = {1}"
     * 	// converter.Parameters == new object[] { "Brad", "Jones" }
     * ```
     */
    public class AssignmentsConverter : ExpressionVisitor
    {
        /**
         * The portion of the SQL query that will come after a SET clause.
         */
        public string Sql
        {
            get { return this.sql.ToString();  }
        }

        private StringBuilder sql = new StringBuilder();

        /**
         * A list of parameter values that go along with our sql query segment.
         */
        public object[] Parameters
        {
            get { return this.parameters.ToArray();  }
        }

        private List<object> parameters = new List<object>();

        /**
         * When we recurse into a MemberExpression, looking for a
         * ConstantExpression, we do not want to write anything to
         * the sql StringBuilder.
         */
        private bool blockWriting = false;

        /**
         * In some cases, we need to save the value we get from a MemberInfo
         * and save it for later use, when we are at the correct
         * MemberExpression.
         */
        private object value;

        protected override Expression VisitBinary(BinaryExpression node)
        {
            // Go and visit the left hand side of this expression
            this.Visit(node.Left);

            // Add the operator in the middle
            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    this.sql.Append("=");
                break;

                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    this.sql.Append(",");
                break;

                default:
                    throw new UnknownOperatorException(node.NodeType);
            }

            // Operator needs a space after it.
            this.sql.Append(" ");

            // Now visit the right hand side of this expression.
            this.Visit(node.Right);

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            // This will get filled with the "actual" value from our child
            // ConstantExpression if happen to have a child ConstantExpression.
            // see: http://stackoverflow.com/questions/6998523
            object value = null;

            // Recurse down to see if we can simplify...
            this.blockWriting = true;
            var expression = this.Visit(node.Expression);
            this.blockWriting = false;

            // If we've ended up with a constant, and it's a property
            // or a field, we can simplify ourselves to a constant.
            if (expression is ConstantExpression)
            {
                MemberInfo member = node.Member;
                object container = ((ConstantExpression)expression).Value;

                if (member is FieldInfo)
                {
                    value = ((FieldInfo)member).GetValue(container);

                }
                else if (member is PropertyInfo)
                {
                    value = ((PropertyInfo)member).GetValue(container, null);
                }

                // If we managed to actually get a value, lets now create a
                // ConstantExpression with the expected value and Vist it.
                if (value != null)
                {
                    if (TypeMapper.IsClrType(value))
                    {
                        this.Visit(Expression.Constant(value));
                    }
                    else
                    {
                        // So if we get to here, what has happened is that
                        // the value returned by the FieldInfo GetValue call
                        // is actually the container, so we save it for later.
                        this.value = value;
                    }
                }
            }
            else if (expression is MemberExpression)
            {
                // Now we can use the value we saved earlier to actually grab
                // the constant value that we expected. I guess this sort of
                // recursion could go on for ages and hence why the accepted
                // answer used DyanmicInvoke. Anyway we will hope that this
                // does the job for our needs.

                MemberInfo member = node.Member;
                object container = this.value;

                if (member is FieldInfo)
                {
                    value = ((FieldInfo)member).GetValue(container);
                }
                else if (member is PropertyInfo)
                {
                    value = ((PropertyInfo)member).GetValue(container, null);
                }

                this.value = null;

                if (TypeMapper.IsClrType(value))
                {
                    this.Visit(Expression.Constant(value));
                }
                else
                {
                    throw new ExpressionTooComplexException();
                }
            }

            // We only need to do this if we did not
            // have a child ConstantExpression
            if (value == null)
            {
                this.sql.Append(new SqlId(node.Member.Name).Value);
                this.sql.Append(" ");
            }

            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (!this.blockWriting)
            {
                this.sql.Append("{");
                this.sql.Append(this.parameters.Count);
                this.sql.Append("}");
                this.parameters.Add(node.Value);
            }

            return node;
        }
    }
}
