////////////////////////////////////////////////////////////////////////////////
//            ________                                _____        __
//           /  _____/_______ _____     ____   ____ _/ ____\__ __ |  |
//          /   \  ___\_  __ \\__  \  _/ ___\_/ __ \\   __\|  |  \|  |
//          \    \_\  \|  | \/ / __ \_\  \___\  ___/ |  |  |  |  /|  |__
//           \______  /|__|   (____  / \___  >\___  >|__|  |____/ |____/
//                  \/             \/      \/     \/
// =============================================================================
//           Designed & Developed by Brad Jones <brad @="bjc.id.au" />
// =============================================================================
////////////////////////////////////////////////////////////////////////////////

namespace Graceful.Utils.Visitors
{
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    abstract public class JsonVisitor
    {
        public virtual JToken Visit(JToken token)
        {
            return VisitInternal(token);
        }

        protected virtual JToken VisitInternal(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    return VisitObject((JObject)token);

                case JTokenType.Property:
                    return VisitProperty((JProperty)token);

                case JTokenType.Array:
                    return VisitArray((JArray)token);

                case JTokenType.String:
                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.Date:
                case JTokenType.Boolean:
                case JTokenType.Null:
                    return VisitValue((JValue)token);

                default:
                    throw new InvalidOperationException();
            }
        }

        protected virtual JToken VisitObject(JObject obj)
        {
            foreach (var property in obj.Properties())
                VisitInternal(property);

            return obj;
        }

        protected virtual JToken VisitProperty(JProperty property)
        {
            VisitInternal(property.Value);

            return property;
        }

        protected virtual JToken VisitArray(JArray array)
        {
            foreach (var item in array)
                VisitInternal(item);

            return array;
        }

        protected virtual JToken VisitValue(JValue value)
        {
            return value;
        }
    }
}
