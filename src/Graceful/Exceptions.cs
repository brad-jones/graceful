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

namespace Graceful
{
    using System;
    using System.Reflection;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Schema;
    using System.Linq.Expressions;
    using System.Collections.Generic;

    /**
     * Will be thrown when an entity does not pass validation.
     *
     * ```cs
     * 	var foo = new Foo { Bar = "Baz" };
     * 	try
     * 	{
     * 		foo.Save();
     * 	}
     * 	catch (ValidationException e)
     * 	{
     * 		e.Errors.ForEach(error =>
     * 		{
     * 			PropertyInfo propThatFailedValidation = error.Key;
     * 			string reasonWhyValidationFailed = error.Value;
     * 		});
     * 	}
     * ```
     */
    public class ValidationException : Exception
    {
        public List<KeyValuePair<PropertyInfo, string>> Errors { get; protected set; }

        public ValidationException(List<KeyValuePair<PropertyInfo, string>> errors)
        : base("Entity did not pass validation, see Error List for more info...")
        {
            this.Errors = errors;
        }
    }

    /**
     * Will be thrown when a json string does not pass json schema validation.
     *
     * ```cs
     * 	try
     * 	{
     * 		var foo = Foo.FromJson("{ ... json ... }");
     * 	}
     * 	catch (JsonValidationException e)
     * 	{
     * 		e.Errors.ForEach(error =>
     * 		{
     *
     * 		});
     * 	}
     * ```
     */
    public class JsonValidationException : Exception
    {
        public List<ValidationError> Errors { get; protected set; }

        public JsonValidationException(List<ValidationError> errors)
        : base("Json did not pass json schema validation, see Error List for more info...")
        {
            this.Errors = errors;
        }
    }

    /**
     * Will be thrown when an entity that has an Id of 0 is asked to be deleted.
     *
     * ```cs
     * 	var foo = new Foo();
     *
     * 	try
     * 	{
     * 		foo.Delete();
     * 	}
     * 	catch (DeleteNonExistentEntityException e)
     * 	{
     * 		e.NonExistentEntity == foo;
     * 	}
     * ```
     */
    public class DeleteNonExistentEntityException : Exception
    {
        public object NonExistentEntity { get; protected set; }

        public DeleteNonExistentEntityException(object entity)
        : base("Can't delete an entity that does not exist in the db yet!")
        {
            this.NonExistentEntity = entity;
        }
    }

    /**
     * Will be thrown when the RelationshipDiscoverer fails.
     *
     * ```cs
     * 	try
     * 	{
     * 		new RelationshipDiscoverer(new List<Type>
     * 		{
     * 			typeof(Foo),
     * 			typeof(Bar),
     * 			typeof(Baz)
     * 		});
     * 	}
     * 	catch (UnknownRelationshipException e)
     * 	{
     * 		// Basically what we are saying is that Graceful has no idea how to
     * 		// serialize this property. The TypeMapper did not pick it up as a
     * 		// simple primative type and the RelationshipDiscoverer could not
     * 		// find a matching property in another type.
     * 		e.UnknownProperty;
     * 	}
     * ```
     */
    public class UnknownRelationshipException : Exception
    {
        public PropertyInfo UnknownProperty { get; protected set; }

        public UnknownRelationshipException(PropertyInfo prop)
        : base("The relationship discoverer failed to discover the relation associated with this property: " + JObject.FromObject(prop).ToString())
        {
            this.UnknownProperty = prop;
        }
    }

    /**
     * Will be thrown when an Expression Visitor, visits an unknown operator.
     *
     * ```cs
     * 	var converter = new PredicateConverter();
     * 	Expression<Func<TModel, bool>> expression = e => e.Id + 1;
     *
     * 	try
     * 	{
     * 		converter.Visit(expression.Body);
     * 	}
     * 	catch (UnknownOperatorException e)
     * 	{
     * 		// This will be "ExpressionType.Add".
     * 		e.UnknownOperator;
     * 	}
     * ```
     */
    public class UnknownOperatorException : Exception
    {
        public ExpressionType UnknownOperator { get; protected set; }

        public UnknownOperatorException(ExpressionType unknownOperator)
        : base("We don't know what to do with the Operator: " + unknownOperator.ToString())
        {
            this.UnknownOperator = unknownOperator;
        }
    }

    /**
     * Will be thrown when an Expression Visitor, gives up basically.
     *
     * Without using the expensive DynamicInvoke it is actually rather complex
     * to extract the actual values referenced by an expression. In the case
     * the Expression Visitor can not extract such a value it will throw this.
     *
     * Instead of using something like this:
     * ```cs
     * 	var value = Some.Other.Complex.Object.That.We.Cant.Decompose.Value;
     * 	var foos = Foo.Where(e => e.Bar > value).ToList();
     * ```
     *
     * You will be able to do get the same end result, all be it without the
     * type safety provided by the expression:
     * ```cs
     * 	var value = Some.Other.Complex.Object.That.We.Cant.Decompose.Value;
     * 	var foos = Foo.Where("Bar > {0}", value).ToList();
     * ```
     */
    public class ExpressionTooComplexException : Exception
    {
        public ExpressionTooComplexException()
        : base("This expression is too complex to decompose and convert into SQL. Consider using the equivalent string.format method.")
        {}
    }
}
