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
    using System.Collections.Generic;
    using Newtonsoft.Json.Schema;

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
}
