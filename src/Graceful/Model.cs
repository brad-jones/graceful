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
    using Inflector;
    using System.Text;
    using System.Linq;
    using Graceful.Query;
    using Graceful.Utils;
    using Newtonsoft.Json;
    using System.Reflection;
    using Graceful.Extensions;
    using Newtonsoft.Json.Linq;
    using System.ComponentModel;
    using Newtonsoft.Json.Schema;
    using Graceful.Utils.Visitors;
    using System.Linq.Expressions;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.ComponentModel.DataAnnotations;
    using ExpressionBuilder = Graceful.Dynamic.ExpressionBuilder;
    using RelationType = Graceful.Utils.RelationshipDiscoverer.Relation.RelationType;

    public class Model
    {
        /**
         * Cache the results of GetAllModels.
         */
        private static HashSet<Type> _AllModels;

        /**
         * Returns a list of all defined models in the current app domain.
         *
         * ```cs
         * 	var models = Model.GetAllModels();
         * ```
         */
        public static HashSet<Type> GetAllModels()
        {
            if (_AllModels != null) return _AllModels;

            _AllModels = new HashSet<Type>();

            AppDomain.CurrentDomain.GetAssemblies().ToList().ForEach(assembly =>
            {
                assembly.GetTypes()
                .Where(type => type.IsSubclassOf(typeof(Model)))
                .Where(type => type.IsPublic)
                .Where(type => !type.ContainsGenericParameters)
                .ToList().ForEach(type => _AllModels.Add(type));
            });

            return _AllModels;
        }

        /**
         * Returns a list of all the model names in the current app domain.
         */
        public static List<string> GetAllModelNames()
        {
            var modelNames = new List<string>();

            GetAllModels().ForEach(model =>
            {
                var typeParts = model.ToString().Split('.');
                modelNames.Add(typeParts[typeParts.Length - 1]);
            });

            return modelNames;
        }

        /**
         * Given a model name, we will return the model type.
         *
         * The name can be a fully qualified type name:
         *
         * ```cs
         * 	Model.GetModel("Aceme.Models.Person");
         * ```
         *
         * Or you may provide just the class name:
         *
         * ```cs
         *  Model.GetModel("Person");
         * ```
         *
         * Or you may provide the plurized version:
         *
         * ```cs
         *  Model.GetModel("Persons");
         * ```
         *
         * > NOTE: This is case-insensitive.
         */
        public static Type GetModel(string modelName)
        {
            modelName = modelName.ToLower();

            return GetAllModels().Single(model =>
            {
                var modelNameToCheck = model.ToString().ToLower();

                // Do we have a complete full namespace match
                if (modelNameToCheck == modelName)
                {
                    return true;
                }
                else
                {
                    // Check for a class name match
                    var typeParts = modelNameToCheck.Split('.');
                    var className = typeParts[typeParts.Length - 1];
                    if (className == modelName)
                    {
                        return true;
                    }

                    // We will also check for the pluralized version
                    else if (className.Pluralize() == modelName)
                    {
                        return true;
                    }
                }

                return false;
            });
        }

        /**
         * Return a new DModel instance from the given Type.
         *
         * ```cs
         *  Model.Dynamic(typeof(Foo)).SqlTableName;
         * ```
         */
        public static Dynamic.Model Dynamic(Type modelType)
        {
            return new Dynamic.Model(modelType);
        }

        /**
         * Return a new DModel instance from the given model name.
         *
         * ```cs
         *  Model.Dynamic("Foo").SqlTableName;
         * ```
         */
        public static Dynamic.Model Dynamic(string modelName)
        {
            return new Dynamic.Model(GetModel(modelName));
        }

        /**
         * Return a new DModel instance from the given entity.
         *
         * ```cs
         *  Model.Dynamic(entity).SqlTableName;
         * ```
         */
        public static Dynamic.Model Dynamic(object entity)
        {
            return new Dynamic.Model(entity);
        }

        /**
         * Return a new DModel instance from the given generic type parameter.
         *
         * ```cs
         *  Model.Dynamic<Foo>().SqlTableName;
         * ```
         */
        public static Dynamic.Model Dynamic<TModel>()
        {
            return new Dynamic.Model(typeof(TModel));
        }
    }

    public class Model<TModel> : Model, INotifyPropertyChanged, IModel<TModel> where TModel : Model<TModel>, new()
    {
        /**
         * The Context will automatically inject it's self here upon creation.
         * Until this has happened your Model will be all but useless.
         *
         * > NOTE: Obviously you may inject your own Custom Context here.
         */
        public static Context Db { get; set; }

        /**
         * Automatically works out the name of the table
         * based on the name of the model class.
         *
         * > NOTE: We only take into account the final class name.
         * > The namespace does not effect the table name at all.
         *
         * You may override this in your model class like so:
         *
         * ```cs
         * 	[SqlTableName("CustomFoo")]
         * 	public class Foo
         * 	{
         * 		...
         * 	}
         * ```
         */
        public static string SqlTableName
        {
            get
            {
                if (_SqlTableName == null)
                {
                    try
                    {
                        // First lets see if the model has it's own table name.
                        _SqlTableName = typeof(TModel)
                        .GetCustomAttribute<SqlTableNameAttribute>(false)
                        .Value;
                    }
                    catch (NullReferenceException)
                    {
                        // Calculate the table name based on the class name.
                        var typeString = typeof(TModel).ToString();
                        var typeParts = typeString.Split('.');
                        _SqlTableName = typeParts[typeParts.Length - 1];
                        _SqlTableName = _SqlTableName.Pluralize();
                    }
                }

                return _SqlTableName;
            }
        }

        private static string _SqlTableName;

        /**
         * A list of properties that are mapped through to the SQL Table.
         */
        public static List<PropertyInfo> MappedProps
        {
            get
            {
                if (_MappedProps == null)
                {
                    // Grab the public instance properties
                    _MappedProps = typeof(TModel).GetProperties(BindingFlags.Instance | BindingFlags.Public).ToList();

                    // We only want properties with public setters
                    _MappedProps = _MappedProps.Where(prop => prop.GetSetMethod() != null).ToList();

                    // Ignore any properties that have the NotMappedAttribute.
                    _MappedProps = _MappedProps.Where(prop => prop.GetCustomAttribute<NotMappedAttribute>(false) == null).ToList();

                    // Because the Id Property is inherited from the Model,
                    // it will be one of the last properties in the list. This
                    // is not ideal and the Id field needs to be first, so it is
                    // the first column in the db table.
                    var idx = _MappedProps.FindIndex(p => p.Name == "Id");
                    var item = _MappedProps[idx];
                    _MappedProps.RemoveAt(idx);
                    _MappedProps.Insert(0, item);
                }

                // Return a new list, and leave the cached copy as is.
                return _MappedProps.ToList();
            }
        }

        private static List<PropertyInfo> _MappedProps;

        /**
         * When inserting and updating we do not care for the Id property.
         * This is because it AUTO INCREMENTS and should never be written to.
         */
        public static List<PropertyInfo> MappedPropsExceptId
        {
            get
            {
                // Grab the classes properties.
                var props = MappedProps;

                // Forget the Id as this is AUTO INCREMENT
                props.RemoveAt(props.FindIndex(p => p.Name == "Id"));

                return props;
            }
        }

        /**
         * Where we store the actual data for the entity.
         * This is used in conjuction with our Get and Set methods.
         *
         * http://timoch.com/blog/2013/08/annoyed-with-inotifypropertychange/
         */
        [JsonIgnore]
        public Dictionary<string, object> PropertyBag { get; protected set; }

        /**
         * When a property is first set, we store a shallow clone of the value.
         * Used in the _"Save"_ method to determin what relationships should be
         * removed.
         *
         * > NOTE: Combine this with a Before and AfterSave event,
         * > makes for simple change detection.
         */
        [JsonIgnore]
        public Dictionary<string, object> OriginalPropertyBag
        {
            get
            {
                if (this._OriginalPropertyBag == null)
                {
                    // Here we create _"THE"_ original property bag.
                    // Think about it the original values of all properties are
                    // their defaults. Lists are initialised so we don't have to
                    // check for null, we can just loop over an empty list.

                    this._OriginalPropertyBag = new Dictionary<string,object>();

                    MappedProps.ForEach(prop =>
                    {
                        if (TypeMapper.IsList(prop.PropertyType))
                        {
                            this._OriginalPropertyBag[prop.Name] =
                            Activator.CreateInstance
                            (
                                typeof(List<>).MakeGenericType
                                (
                                    prop.PropertyType.GenericTypeArguments[0]
                                )
                            );
                        }
                        else if (prop.PropertyType.IsValueType)
                        {
                            this._OriginalPropertyBag[prop.Name] = Activator
                            .CreateInstance(prop.PropertyType);
                        }
                        else
                        {
                            this._OriginalPropertyBag[prop.Name] = null;
                        }
                    });
                }

                return this._OriginalPropertyBag;
            }
        }
        private Dictionary<string, object> _OriginalPropertyBag;

        /**
         * When an entity is first hydrated from the database, we will save the
         * actual result returned from the db here so that we have access to
         * foreign keys.
         */
        [JsonIgnore]
        public Dictionary<string, object> DbRecord { get; protected set; }

        /**
         * This will contain a list of properties that have indeed been
         * modified since being first hydrated from the database.
         * Used in the _"Save"_ method to only update what needs updating.
         */
        [JsonIgnore]
        public List<PropertyInfo> ModifiedProps
        {
            get
            {
                return this._ModifiedProps;
            }
        }

        private List<PropertyInfo> _ModifiedProps = new List<PropertyInfo>();

        /**
         * Provides a shortcut to a new Query.Linq<TModel>.
         */
        public static Linq<TModel> Linq
        {
            get
            {
                return new Linq<TModel>(SqlTableName, Db);
            }
        }

        /**
         * A list of all entities that we already know about. We will do our
         * best to always load directly for this list if we can. This is NOT a
         * GLOBAL cache but a LOCAL cache unique to the current graph.
         *
         * ```cs
         * 	var foo1 = Foo.Find(1);
         * 	var foo2 = Foo.Find(1);
         * 	// foo1.DiscoveredEntities != foo2.DiscoveredEntities
         * ```
         *
         * If however you requested a list of Foo's like this:
         *
         * ```cs
         * 	var foos = Foo.Where(f => f.Bar == "baz").ToList();
         * 	// foos[1].DiscoveredEntities == foos[2].DiscoveredEntities
         * ```
         */
        [NotMapped]
        [JsonIgnore]
        public List<object> DiscoveredEntities
        {
            get
            {
                if (this._DiscoveredEntities == null)
                {
                    // Add ourselves to the discovered list.
                    this._DiscoveredEntities = new List<object>{ this };
                }

                return this._DiscoveredEntities;
            }

            set
            {
                this._DiscoveredEntities = value;
            }
        }

        private List<object> _DiscoveredEntities;

        /**
         * This is used to reduce the number of database calls required to load
         * a full object graph. This is NOT a GLOBAL cache but a LOCAL cache
         * unique the current graph.
         *
         * ```cs
         * 	var foo1 = Foo.Find(1);
         * 	var foo2 = Foo.Find(1);
         * 	// foo1.CachedQueries != foo2.CachedQueries
         * ```
         *
         * If however you requested a list of Foo's like this:
         *
         * ```cs
         * 	var foos = Foo.Where(f => f.Bar == "baz").ToList();
         * 	// foos[1].CachedQueries == foos[2].CachedQueries
         * ```
         *
         * This may seem crazy considering we already have a discovered entities
         * list, I promise its not. Loading from discovered entities can only
         * be done in certian circumstances. Consider the following thought
         * experiment:
         *
         * 		- We load a Customer Entity.
         *
         * 		- A Customer has 2 lists of Products.
         * 		  The first list is their purchased products.
         * 		  The second list is all products that they returned.
         *
         * 		- Lets say we load the returned products list.
         * 		  While we have some of the purchased products list
         * 		  we don't have all of it.
         *
         * 		- Thus we have to ask the database.
         *
         * 		- Cached quriers really becomes helpful in circular reference
         * 		  situations. Consider those Products have a relationship of all
         * 		  Customers that bought that product.
         *
         * 		- Eventually we end up loading the Customer we started with.
         */
        [NotMapped]
        [JsonIgnore]
        public Dictionary<string, List<Dictionary<string, object>>> CachedQueries
        {
            get
            {
                if (this._CachedQueries == null)
                {
                    this._CachedQueries = new Dictionary<string, List<Dictionary<string, object>>>();
                }

                return this._CachedQueries;
            }

            set
            {
                this._CachedQueries = value;
            }
        }

        private Dictionary<string, List<Dictionary<string, object>>> _CachedQueries;

        /**
         * Returns a JSON Schema Document for the Model.
         *
         * ```cs
         * 	var schema = Models.Foo.JsonSchema;
         * ```
         *
         * > TODO: Either work out why the default JSchemaGenerator does not
         * > honour our "Required" attributes or lets just build the schema
         * > ourselves. And remove the fudge we have done below.
         */
        public static JSchema JsonSchema
        {
            get
            {
                if (_JsonSchema == null)
                {
                    var schemaJsonString = new JSchemaGenerator().Generate(typeof(TModel)).ToString();
                    var schemaJson = JObject.Parse(schemaJsonString);
                    new MakeJsonSchemaFollowRequiredAttributes(schemaJson);
                    _JsonSchema = JSchema.Parse(schemaJson.ToString());
                }

                return _JsonSchema;
            }
        }

        private static JSchema _JsonSchema;

        private class MakeJsonSchemaFollowRequiredAttributes : JsonVisitor
        {
            private List<JToken> toRemove = new List<JToken>();

            public MakeJsonSchemaFollowRequiredAttributes(JObject schema)
            {
                this.Visit(schema);

                foreach (var token in toRemove)
                {
                    token.Remove();
                }
            }

            protected override JToken VisitProperty(JProperty property)
            {
                if (property.Name == "required")
                {
                    foreach (var key in (JArray)property.Value)
                    {
                        toRemove.Add(key);
                    }
                }

                return base.VisitProperty(property);
            }
        }

        /**
         * All entities have a Primary Key Id property.
         *
         * > NOTE: An Id < 1 means the entity does not exist in the database.
         */
        [Key]
        [Required]
        [JsonProperty(Order = -2)]
        public int Id
        {
            get { return Get<int>(); }
            set { Set(value); }
        }

        /**
         * All entities automatically get a created timestamp set.
         *
         * > NOTE: This is set once, when the entity is first inserted.
         */
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt
        {
            get { return Get<DateTime>(); }
            set { Set(value); }
        }

        /**
         * All entities automatically get a modified timestamp set.
         *
         * > NOTE: This is set every time an entity is "Saved".
         */
        [DataType(DataType.DateTime)]
        public DateTime ModifiedAt
        {
            get { return Get<DateTime>(); }
            set { Set(value); }
        }

        /**
         * All entities automatically get a deleted timestamp. ie: Soft Deletes.
         *
         * > NOTE: If an entity has any value other than "NULL" in the DeletedAt
         * > column, it will be filtered out of all queries, unless of course
         * > "withTrashed" is set to true.
         */
        [DataType(DataType.DateTime)]
        public DateTime? DeletedAt
        {
            get { return Get<DateTime?>(); }
            set { Set(value); }
        }

        /**
         * Event handler signature that is fired before performing an action.
         *
         * > NOTE: All before events, allow you to stop the action from
         * > happening by returning false. Once an event handler returns false
         * > it will also prevent further handlers from running.
         */
        public delegate bool EntityBeforeEventHandler(object entity);

        /**
         * Event handler signature that is fired after performing an action.
         */
        public delegate void EntityAfterEventHandler(object entity);

        /**
         * Fired just before an entity is about to be saved.
         */
        public event EntityBeforeEventHandler BeforeSave;
        protected virtual bool OnBeforeSave() { return true; }
        public bool FireBeforeSave()
        {
            if (!this.OnBeforeSave()) return false;

            // Force the execution of our validation method.
            if (!this.Validate()) return false;

            var handler = this.BeforeSave;
            if (handler != null)
            {
                foreach (EntityBeforeEventHandler h in handler.GetInvocationList())
                {
                    if (!h(this))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /**
         * Fired after an entity has been saved.
         */
        public event EntityAfterEventHandler AfterSave;
        protected virtual void OnAfterSave() { }
        public void FireAfterSave()
        {
            this.OnAfterSave();

            EntityAfterEventHandler handler = this.AfterSave;
            if (handler != null)
            {
                handler(this);
            }
        }

        /**
         * Fired just before an entity is about to be inserted.
         */
        public event EntityBeforeEventHandler BeforeInsert;
        protected virtual bool OnBeforeInsert() { return true; }
        public bool FireBeforeInsert()
        {
            if (!this.OnBeforeInsert()) return false;

            var handler = this.BeforeInsert;
            if (handler != null)
            {
                foreach (EntityBeforeEventHandler h in handler.GetInvocationList())
                {
                    if (!h(this))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /**
         * Fired after an entity has been inserted.
         */
        public event EntityAfterEventHandler AfterInsert;
        protected virtual void OnAfterInsert() { }
        public void FireAfterInsert()
        {
            this.OnAfterInsert();

            EntityAfterEventHandler handler = this.AfterInsert;
            if (handler != null)
            {
                handler(this);
            }
        }

        /**
         * Fired just before an entity is about to be updated.
         */
        public event EntityBeforeEventHandler BeforeUpdate;
        protected virtual bool OnBeforeUpdate() { return true; }
        public bool FireBeforeUpdate()
        {
            if (!this.OnBeforeUpdate()) return false;

            var handler = this.BeforeUpdate;
            if (handler != null)
            {
                foreach (EntityBeforeEventHandler h in handler.GetInvocationList())
                {
                    if (!h(this))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /**
         * Fired after an entity has been updated.
         */
        public event EntityAfterEventHandler AfterUpdate;
        protected virtual void OnAfterUpdate() { }
        public void FireAfterUpdate()
        {
            this.OnAfterUpdate();

            EntityAfterEventHandler handler = this.AfterUpdate;
            if (handler != null)
            {
                handler(this);
            }
        }

        /**
         * Fired just before an entity is about to be deleted.
         */
        public event EntityBeforeDeleteEventHandler BeforeDelete;
        public delegate bool EntityBeforeDeleteEventHandler(object entity, bool hardDelete);
        protected virtual bool OnBeforeDelete(bool hardDelete) { return true; }
        public bool FireBeforeDelete(bool hardDelete)
        {
            if (!this.OnBeforeDelete(hardDelete)) return false;

            var handler = this.BeforeDelete;
            if (handler != null)
            {
                foreach (EntityBeforeDeleteEventHandler h in handler.GetInvocationList())
                {
                    if (!h(this, hardDelete))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /**
         * Fired after an entity has been deleted.
         */
        public event EntityAfterDeleteEventHandler AfterDelete;
        public delegate void EntityAfterDeleteEventHandler(object entity, bool hardDelete);
        protected virtual void OnAfterDelete(bool hardDelete) { }
        public void FireAfterDelete(bool hardDelete)
        {
            this.OnAfterDelete(hardDelete);

            EntityAfterDeleteEventHandler handler = this.AfterDelete;
            if (handler != null)
            {
                handler(this, hardDelete);
            }
        }

        /**
         * Fired just before an entity is about to be restored.
         */
        public event EntityBeforeEventHandler BeforeRestore;
        protected virtual bool OnBeforeRestore() { return true; }
        public bool FireBeforeRestore()
        {
            if (!this.OnBeforeRestore()) return false;

            var handler = this.BeforeRestore;
            if (handler != null)
            {
                foreach (EntityBeforeEventHandler h in handler.GetInvocationList())
                {
                    if (!h(this))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /**
         * Fired after an entity has been restored.
         */
        public event EntityAfterEventHandler AfterRestore;
        protected virtual void OnAfterRestore() { }
        public void FireAfterRestore()
        {
            this.OnAfterRestore();

            EntityAfterEventHandler handler = this.AfterRestore;
            if (handler != null)
            {
                handler(this);
            }
        }

        /**
         * Fired when ever a _"Mapped"_ property changes on the entity.
         *
         * > NOTE: Lists are automatically wrapped in BindingLists and setup
         * > to fire this event whenever an entity is added or removed from
         * > the list.
         */
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(PropertyInfo prop) {}
        public void FirePropertyChanged(PropertyInfo prop)
        {
            // Run some of our own code first.
            this.UpdateModified(prop);
            this.SaveDiscoveredEntities(prop);

            // Run the OnPropertyChanged method. This allows models to override
            // the method and not have to worry about calling the base method.
            this.OnPropertyChanged(prop);

            // Now fire off any other attached handlers
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(prop.Name));
            }
        }

        /**
         * Serialises the entity to JSON.
         *
         * ```cs
         * 	var json = Models.Foo.Find(1).ToJson();
         * ```
         */
        public string ToJson()
        {
            return JObject.FromObject(this, Db.JsonSerializer).ToString();
        }

        /**
         * DeSerialises the JSON to an entity.
         *
         * ```cs
         * 	var entity = Models.Foo.FromJson("{...}");
         * ```
         *
         * Even though the entity may well have a Id greater than 0 and for all
         * other intents and purposes look as though it has been freshly
         * Hydrated from the database.
         *
         * It has NOT been hydrated and will be treated as though you manually
         * "newed" up the entity instance yourself. This will ensure all
         * validation checks are performed when and if the entity is saved.
         *
         * > NOTE: The JSON is ALWAYS validated against the json
         * > schema before we attempt to deserialize the json.
         */
        public static TModel FromJson(string json)
        {
            ValidateJson(json);

            return JsonConvert.DeserializeObject<TModel>
            (
                json,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                }
            );
        }

        /**
         * Given a JSON Array, we return a List of Deserialized Entities.
         *
         * ```cs
         * 	var entities = Models.Foo.FromJson("[...]");
         * ```
         */
        public static List<TModel> FromJsonArray(string json)
        {
            var entities = new List<TModel>();

            foreach (var token in JArray.Parse(json))
            {
                entities.Add(FromJson(token.ToString()));
            }

            return entities;
        }

        /**
         * Given a json string representing an entity of type TModel
         * we will validate the json string against the generated Json Schema.
         *
         * > NOTE: If validation fails we throw a JsonValidationException.
         */
        private static void ValidateJson(string json)
        {
            // First parse the json into a JObject
            var input = JObject.Parse(json);

            // Next we need to remove all $id and $ref properties.
            // The Json Schema validation does not understand the meaning
            // of these special properties.
            new RemoveRefsFromJson(input);

            // Attempt to validate the json
            IList<ValidationError> errors;
            var filteredErrors = new List<ValidationError>();
            var result = input.IsValid(JsonSchema, out errors);
            if (!result)
            {
                // Sometimes the validation will fail because a nested object
                // does not contain a circular refrenced property. To satisfy
                // the validator these properties need to exist but have a null
                // value. In any case if detect such an error we do not add it
                // to filteredErrors lists.
                foreach (var error in errors)
                {
                    if (error.ErrorType == ErrorType.Required)
                    {
                        var requiredList = (List<string>)error.Value;
                        if (requiredList.Count == 1)
                        {
                            if (error.Path.StartsWith(requiredList[0]))
                            {
                                continue;
                            }
                        }
                    }

                    filteredErrors.Add(error);
                }
            }

            // Finally if the filteredErrors list does
            // contain errors then throw an exception.
            if (filteredErrors.Count > 0)
            {
                throw new JsonValidationException(filteredErrors);
            }
        }

        private class RemoveRefsFromJson : JsonVisitor
        {
            private List<JToken> toRemove = new List<JToken>();

            private List<JToken> toSetToNull = new List<JToken>();

            public RemoveRefsFromJson(JToken root)
            {
                this.Visit(root);

                foreach (var token in toRemove)
                {
                    token.Remove();
                }

                foreach (var token in toSetToNull)
                {
                    if (token.Parent.Parent is JProperty)
                    {
                        var prop = token.Parent.Parent as JProperty;
                        prop.Value = null;
                    }

                    if (token.Parent.Parent is JArray)
                    {
                        var array = token.Parent.Parent as JArray;
                        array.Remove(token.Parent);
                    }
                }
            }

            protected override JToken VisitProperty(JProperty property)
            {
                if (property.Name == "$id")
                {
                    toRemove.Add(property);
                }

                if (property.Name == "$ref")
                {
                    toSetToNull.Add(property);
                }

                return base.VisitProperty(property);
            }
        }

        /**
         * Determines whether the input object is equal to the current entity.
         *
         * > NOTE: If either entity has an Id of 0 then a reference equality
         * > check is done. If both entities have Id's greater than 0 then
         * > a value equality check is done on the Id's.
         */
        public override bool Equals(object input)
        {
            var entity = input as TModel;

            if (entity == null) return false;

            if (this.Id == 0 || entity.Id == 0)
            {
                return base.Equals(input);
            }

            return this.Id == entity.Id;
        }

        /**
         * Returns the entities hash code.
         *
         * > NOTE: If the entity has an Id of 0, then the hash code of the
         * > base object is returned. If greater than 0 then the Id's hash code
         * > is returned, this matches the same logic as our Equals method.
         */
        public override int GetHashCode()
        {
            if (this.Id == 0)
            {
                return base.GetHashCode();
            }

            return this.Id.GetHashCode();
        }

        /**
         * Entity Property Getter.
         *
         * All _"mapped"_ properties need to implement this as their Getter.
         *
         * ```cs
         * 	class Foo : Model<Foo>
         * 	{
         * 		public string Bar { get { return Get<string>(); } set... }
         * 	}
         * ```
         *
         * > TODO: Investigate IL Weaving... or possibly just a super simple
         * > pre compilation script (grunt/gulp task) to automatically add
         * > the needed method calls.
         */
        public virtual T Get<T>([CallerMemberName] string propName = "", bool loadFromDiscovered = true, bool loadFromDb = true)
        {
            // If the property bag hasn't been created yet then obviously we
            // won't find anything in it. Even if someone asks for a related
            // entity, we must either have an Id or the entity / entities will
            // have been "Set" and thus the the PropertyBag will exist.
            if (this.PropertyBag == null) return default(T);

            // Lets attempt to get the value from the PropertyBag Dict.
            object value = null;
            if (this.PropertyBag.TryGetValue(propName, out value))
            {
                return value == null ? default(T) : (T)value;
            }

            // Bail out if we have been told not to load anything
            // from our discovered list or from the database.
            if (!loadFromDiscovered || !loadFromDb)
            {
                return default(T);
            }

            // Lets see if the property maps to a discovered relationship.
            RelationshipDiscoverer.Relation relation;
            try
            {
                relation = Db.Relationships.Discovered.Single
                (
                    r => r.LocalProperty == MappedProps.Single
                    (
                        p => p.Name == propName
                    )
                );
            }
            catch
            {
                // Bail out we could not find any matching relationship.
                // The value the caller asked for really does not exist.
                return default(T);
            }

            // Before we go requesting data from the database,
            // lets see if the entity or entities already exist
            // in our DiscoveredEntities list.
            if (loadFromDiscovered)
            {
                // The entity or entities we are looking
                // for will always be of the ForeignType.
                var discovered = this.DiscoveredEntities
                .Where(e => e.GetType() == relation.ForeignType)
                .ToList();

                // To continue we need to have at least one entity in the list.
                if (discovered.Count > 0)
                {
                    switch (relation.Type)
                    {
                        case RelationType.MtoM:
                        {
                            // We can only load from our discovered entities if
                            // we ourselves do not exist in the database yet.
                            // This is because it is not guaranteed that we will
                            // have discovered "ALL" our related entities.
                            if (this.Id == 0)
                            {
                                var dicoveredEntitiesUntyped = discovered
                                .Where(e =>
                                {
                                    var foreignEntity = (IModel<Model>)e;

                                    var foreignValue = foreignEntity.Get<IEnumerable<object>>
                                    (
                                        relation.ForeignProperty.Name,
                                        loadFromDiscovered: false,
                                        loadFromDb: false
                                    );

                                    if (foreignValue == null) return false;

                                    return foreignValue.Contains(this);
                                })
                                .ToList();

                                if (dicoveredEntitiesUntyped.Count > 0)
                                {
                                    dynamic dicoveredEntities = Activator.CreateInstance
                                    (
                                        typeof(List<>).MakeGenericType
                                        (
                                            relation.ForeignType
                                        )
                                    );

                                    foreach (var discoveredEntity in dicoveredEntitiesUntyped)
                                    {
                                        dicoveredEntities.Add((dynamic)discoveredEntity);
                                    }

                                    this.Set(dicoveredEntities, propName, false);

                                    return this.Get<T>
                                    (
                                        propName,
                                        loadFromDiscovered: false,
                                        loadFromDb: false
                                    );
                                }
                            }
                        }
                        break;

                        case RelationType.MtoO:
                        {
                            if (this.Id == 0)
                            {
                                var dicoveredEntitiesUntyped = discovered.Where(e =>
                                {
                                    var foreignEntity = (IModel<Model>)e;

                                    var foreignValue = foreignEntity.Get<object>
                                    (
                                        relation.ForeignProperty.Name,
                                        loadFromDiscovered: false,
                                        loadFromDb: false
                                    );

                                    if (foreignValue == null) return false;

                                    return foreignValue == this;
                                })
                                .ToList();

                                if (dicoveredEntitiesUntyped.Count > 0)
                                {
                                    dynamic dicoveredEntities = Activator.CreateInstance
                                    (
                                        typeof(List<>).MakeGenericType
                                        (
                                            relation.ForeignType
                                        )
                                    );

                                    foreach (var discoveredEntity in dicoveredEntitiesUntyped)
                                    {
                                        dicoveredEntities.Add((dynamic)discoveredEntity);
                                    }

                                    this.Set(dicoveredEntities, propName, false);

                                    return this.Get<T>
                                    (
                                        propName,
                                        loadFromDiscovered: false,
                                        loadFromDb: false
                                    );
                                }
                            }
                        }
                        break;

                        case RelationType.OtoM:
                        {
                            var entities = discovered.Where(e =>
                            {
                                var foreignEntity = (IModel<Model>)e;

                                var foreignValue = foreignEntity.Get<IEnumerable<object>>
                                (
                                    relation.ForeignProperty.Name,
                                    loadFromDiscovered: false,
                                    loadFromDb: false
                                );

                                // Because we are looking for a single entity we
                                // can use the foreign key to be sure we have
                                // loaded the correct discovered entity.
                                if (foreignValue == null)
                                {
                                    if (this.DbRecord != null)
                                    {
                                        if (this.DbRecord.ContainsKey(relation.ForeignKeyColumnName))
                                        {
                                            if ((int)this.DbRecord[relation.ForeignKeyColumnName] == foreignEntity.Id)
                                            {
                                                return true;
                                            }
                                        }
                                    }

                                    return false;
                                }

                                return foreignValue.Contains(this);
                            });

                            if (entities.Count() > 0)
                            {
                                object entity = null;

                                if (entities.Count() == 1)
                                {
                                    entity = entities.First();
                                }
                                else
                                {
                                    // In some cases it is possible that we end
                                    // up with 2 versions of our entity that
                                    // have been discovered. A version that does
                                    // not exist in the Db and a version that
                                    // does, we will always take the version
                                    // that came directly from the db.
                                    entity = entities.SingleOrDefault
                                    (
                                        e => ((IModel<Model>)e).Id > 0
                                    );
                                }

                                if (entity != null)
                                {
                                    this.Set(entity, propName, false);

                                    return this.Get<T>
                                    (
                                        propName,
                                        loadFromDiscovered: false,
                                        loadFromDb: false
                                    );
                                }
                            }
                        }
                        break;

                        case RelationType.OtoO:
                        {
                            var entity = (T)discovered.SingleOrDefault(e =>
                            {
                                var foreignEntity = (IModel<Model>)e;

                                var foreignValue = foreignEntity.Get<object>
                                (
                                    relation.ForeignProperty.Name,
                                    loadFromDiscovered: false,
                                    loadFromDb: false
                                );

                                // Because we are looking for a single entity we
                                // can use the foreign key to be sure we have
                                // loaded the correct discovered entity.
                                if (foreignValue == null)
                                {
                                    if (foreignEntity.DbRecord != null && foreignEntity.DbRecord.ContainsKey(relation.ForeignKeyColumnName))
                                    {
                                        if ((int)foreignEntity.DbRecord[relation.ForeignKeyColumnName] == this.Id)
                                        {
                                            return true;
                                        }
                                    }
                                    else if (this.DbRecord != null && this.DbRecord.ContainsKey(relation.ForeignKeyColumnName))
                                    {
                                        if ((int)this.DbRecord[relation.ForeignKeyColumnName] == foreignEntity.Id)
                                        {
                                            return true;
                                        }
                                    }

                                    return false;
                                }

                                return this == foreignValue;
                            });

                            if (entity != null)
                            {
                                this.Set(entity, propName, false);

                                return this.Get<T>
                                (
                                    propName,
                                    loadFromDiscovered: false,
                                    loadFromDb: false
                                );
                            }
                        }
                        break;
                    }
                }
            }

            // If we get to here and we have not managed to load the requested
            // entity or entities from our discovered list, then the last place
            // to look is obviously the database. However if we ourselves do not
            // have an Id then we can not possibly have any related entities.
            if (!this.PropertyBag.ContainsKey("Id") || !this.Hydrated)
            {
                return default(T);
            }

            switch (relation.Type)
            {
                case RelationType.MtoM:
                {
                    // Swap the column names around depending on which
                    // side of the relationship we are looking up.
                    var col1 = new SqlId(relation.PivotTableFirstColumnName);
                    if (!relation.PivotTableFirstColumnName.Contains(relation.LocalTableNameSingular))
                    {
                        col1 = new SqlId(relation.PivotTableSecondColumnName);
                    }

                    var col2 = new SqlId(relation.PivotTableSecondColumnName);
                    if (!relation.PivotTableSecondColumnName.Contains(relation.ForeignTableNameSingular))
                    {
                        col2 = new SqlId(relation.PivotTableFirstColumnName);
                    }

                    var localTable = new SqlTable(Db, relation.LocalTableName);
                    var foreignTable = new SqlTable(Db, relation.ForeignTableName);
                    var pivotTable = new SqlTable(Db, relation.PivotTableName);

                    // Grab our relationship from the database.
                    var query = Db.Qb
                    .SELECT("{0}.*", foreignTable)
                    .FROM(foreignTable)
                    .INNER_JOIN("{0} ON {0}.{1} = {2}.[Id]", pivotTable, col2, foreignTable)
                    .INNER_JOIN("{2} ON {0}.{1} = {2}.[Id]", pivotTable, col1, localTable)
                    .WHERE("{0}.[Id] = {1} AND {0}.[DeletedAt] IS NULL", localTable, this.Id);

                    List<Dictionary<string, object>> records;
                    var queryHash = query.Hash;

                    if (this.CachedQueries.ContainsKey(queryHash))
                    {
                        records = this.CachedQueries[queryHash];
                    }
                    else
                    {
                        records = query.Rows;
                        this.CachedQueries[queryHash] = records;
                    }

                    if (records.Count > 0)
                    {
                        // Hydrate the records into entities.
                        var entities = Model.Dynamic(relation.ForeignType).Hydrate(records);

                        dynamic tmp = Activator.CreateInstance
                        (
                            typeof(List<>).MakeGenericType
                            (
                                relation.ForeignType
                            )
                        );

                        foreach (var entity in entities)
                        {
                            if (this.DiscoveredEntities.Contains(entity))
                            {
                                tmp.Add((dynamic)this.DiscoveredEntities[this.DiscoveredEntities.IndexOf(entity)]);
                            }
                            else
                            {
                                entity.CachedQueries = this.CachedQueries;
                                tmp.Add(entity);
                            }
                        }

                        // Save the new list of entities into the object graph
                        this.Set(tmp, propName, false);

                        return this.Get<T>
                        (
                            propName,
                            loadFromDiscovered: false,
                            loadFromDb: false
                        );
                    }
                }
                break;

                case RelationType.MtoO:
                {
                    // Grab the entities
                    var query = Db.Qb
                    .SELECT("*")
                    .FROM(relation.ForeignTableName)
                    .WHERE(relation.ForeignKeyColumnName, this.Id)
                    .WHERE("[DeletedAt] IS NULL");

                    List<Dictionary<string, object>> records;
                    var queryHash = query.Hash;

                    if (this.CachedQueries.ContainsKey(queryHash))
                    {
                        records = this.CachedQueries[queryHash];
                    }
                    else
                    {
                        records = query.Rows;
                        this.CachedQueries[queryHash] = records;
                    }

                    if (records.Count > 0)
                    {
                        // Hydrate the records into entities.
                        var entities = Model.Dynamic(relation.ForeignType).Hydrate(records);

                        dynamic tmp = Activator.CreateInstance
                        (
                            typeof(List<>).MakeGenericType
                            (
                                relation.ForeignType
                            )
                        );

                        foreach (var entity in entities)
                        {
                            if (this.DiscoveredEntities.Contains(entity))
                            {
                                tmp.Add((dynamic)this.DiscoveredEntities[this.DiscoveredEntities.IndexOf(entity)]);
                            }
                            else
                            {
                                entity.CachedQueries = this.CachedQueries;
                                tmp.Add(entity);
                            }
                        }

                        // Save the new list of entities into the object graph
                        this.Set(tmp, propName, false);

                        return this.Get<T>
                        (
                            propName,
                            loadFromDiscovered: false,
                            loadFromDb: false
                        );
                    }
                }
                break;

                case RelationType.OtoM:
                {
                    if (this.DbRecord.ContainsKey(relation.ForeignKeyColumnName))
                    {
                        var ForeignId = this.DbRecord[relation.ForeignKeyColumnName];

                        if (ForeignId != null)
                        {
                            var query = Db.Qb
                            .SELECT("*")
                            .FROM(relation.ForeignTableName)
                            .WHERE("Id", (int)ForeignId)
                            .WHERE("[DeletedAt] IS NULL");

                            List<Dictionary<string, object>> records;
                            var queryHash = query.Hash;

                            if (this.CachedQueries.ContainsKey(queryHash))
                            {
                                records = this.CachedQueries[queryHash];
                            }
                            else
                            {
                                records = query.Rows;
                                this.CachedQueries[queryHash] = records;
                            }

                            if (records.Count == 1)
                            {
                                // Hydrate the records into entities.
                                var entity = Model.Dynamic(relation.ForeignType).Hydrate(records[0]);

                                if (this.DiscoveredEntities.Contains(entity))
                                {
                                    entity = this.DiscoveredEntities[this.DiscoveredEntities.IndexOf(entity)];
                                }
                                else
                                {
                                    entity.CachedQueries = this.CachedQueries;
                                }

                                // Save the new entity into the object graph
                                this.Set(entity, propName, false);

                                return this.Get<T>
                                (
                                    propName,
                                    loadFromDiscovered: false,
                                    loadFromDb: false
                                );
                            }
                        }
                    }
                }
                break;

                case RelationType.OtoO:
                {
                    if (relation.ForeignKeyTableName == relation.LocalTableName)
                    {
                        if (this.DbRecord.ContainsKey(relation.ForeignKeyColumnName))
                        {
                            var ForeignId = this.DbRecord[relation.ForeignKeyColumnName];

                            if (ForeignId != null)
                            {
                                var query = Db.Qb
                                .SELECT("*")
                                .FROM(relation.ForeignTableName)
                                .WHERE("Id", (int)ForeignId)
                                .WHERE("[DeletedAt] IS NULL");

                                List<Dictionary<string, object>> records;
                                var queryHash = query.Hash;

                                if (this.CachedQueries.ContainsKey(queryHash))
                                {
                                    records = this.CachedQueries[queryHash];
                                }
                                else
                                {
                                    records = query.Rows;
                                    this.CachedQueries[queryHash] = records;
                                }

                                if (records.Count == 1)
                                {
                                    // Hydrate the records into entities.
                                    var entity = Model.Dynamic(relation.ForeignType).Hydrate(records[0]);

                                    if (this.DiscoveredEntities.Contains(entity))
                                    {
                                        entity = this.DiscoveredEntities[this.DiscoveredEntities.IndexOf(entity)];
                                    }
                                    else
                                    {
                                        entity.CachedQueries = this.CachedQueries;
                                    }

                                    // Save the new entity into the object graph
                                    this.Set(entity, propName, false);

                                    return this.Get<T>
                                    (
                                        propName,
                                        loadFromDiscovered: false,
                                        loadFromDb: false
                                    );
                                }
                            }
                        }
                    }
                    else
                    {
                        // Grab the entities
                        var query = Db.Qb
                        .SELECT("*")
                        .FROM(relation.ForeignTableName)
                        .WHERE(relation.ForeignKeyColumnName, this.Id)
                        .WHERE("[DeletedAt] IS NULL");

                        List<Dictionary<string, object>> records;
                        var queryHash = query.Hash;

                        if (this.CachedQueries.ContainsKey(queryHash))
                        {
                            records = this.CachedQueries[queryHash];
                        }
                        else
                        {
                            records = query.Rows;
                            this.CachedQueries[queryHash] = records;
                        }

                        if (records.Count == 1)
                        {
                            // Hydrate the records into entities.
                            var entity = Model.Dynamic(relation.ForeignType).Hydrate(records[0]);

                            if (this.DiscoveredEntities.Contains(entity))
                            {
                                entity = this.DiscoveredEntities[this.DiscoveredEntities.IndexOf(entity)];
                            }
                            else
                            {
                                entity.CachedQueries = this.CachedQueries;
                            }

                            // Save the new entity into the object graph
                            this.Set(entity, propName, false);

                            return this.Get<T>
                            (
                                propName,
                                loadFromDiscovered: false,
                                loadFromDb: false
                            );
                        }
                    }
                }
                break;
            }

            // If we get to hear, we have checked the property bag for a value,
            // the discovered entities list and the database and found nothing
            // so lets set the value to null and move on.
            if (TypeMapper.IsList(typeof(T)))
            {
                dynamic tmp = Activator.CreateInstance
                (
                    typeof(List<>).MakeGenericType
                    (
                        typeof(T).GenericTypeArguments[0]
                    )
                );

                this.Set(tmp, propName, false);
                return tmp;
            }
            else
            {
                this.Set(default(T), propName, false);
                return default(T);
            }
        }

        /**
         * Entity Property Setter.
         *
         * All _"mapped"_ properties need to implement this as their Setter.
         *
         * ```cs
         * 	class Foo : Model<Foo>
         * 	{
         * 		public string Bar { get... set { Set(value); } }
         * 	}
         * ```
         *
         * > TODO: Investigate IL Weaving... or possibly just a super simple
         * > pre compilation script (grunt/gulp task) to automatically add
         * > the needed method calls.
         */
        public virtual void Set<T>(T value, [CallerMemberName] string propName = "", bool triggerChangeEvent = true)
        {
            // Grab the property
            var prop = MappedProps.Single(p => p.Name == propName);

            // Create the property bag dict if it doesn't exist yet.
            if (this.PropertyBag == null)
            {
                this.PropertyBag = new Dictionary<string, object>();
            }

            // If the value is an entity or list of entities
            // we will save it to our discovered list.
            if (value != null && !TypeMapper.IsClrType(value))
            {
                this.SaveDiscoveredEntities(prop, value);
            }

            // If the property does not already have
            // a value, set it's original value.
            if (this.Hydrated && this.Get<object>(propName, loadFromDiscovered: false, loadFromDb: false) == null)
            {
                if (value != null && TypeMapper.IsListOfEntities(value))
                {
                    var clone = (value as IEnumerable<object>)
                    .Cast<IModel<Model>>().ToList();

                    this.OriginalPropertyBag[propName] = clone;
                }
                else
                {
                    this.OriginalPropertyBag[propName] = value;
                }
            }

            // Wrap any normal Lists in a BindingList so that we can track when
            // new entities are added so that we may save those entities to our
            // discovered list.
            dynamic propertyBagValue;
            if (value != null && TypeMapper.IsList(value))
            {
                dynamic bindingList = Activator.CreateInstance
                (
                    typeof(BindingList<>).MakeGenericType
                    (
                        value.GetType().GenericTypeArguments[0]
                    ),
                    new object[] { value }
                );

                bindingList.ListChanged += new ListChangedEventHandler
                (
                    (sender, e) =>
                    {
                        //if (!triggerChangeEvent) return;

                        switch (e.ListChangedType)
                        {
                            case ListChangedType.ItemAdded:
                            case ListChangedType.ItemDeleted:
                            {
                                this.FirePropertyChanged(prop);
                            }
                            break;
                        }
                    }
                );

                propertyBagValue = bindingList;
            }
            else
            {
                propertyBagValue = value;
            }

            // Save the new value
            this.PropertyBag[propName] = propertyBagValue;

            // Trigger the change event
            if (triggerChangeEvent) this.FirePropertyChanged(prop);
        }

        /**
         * Sometimes we need to know if an entity has been "newed" up by us
         * and hydrated with valid data from the database. or if it has been
         * created else where with un-validated data.
         */
        [JsonIgnore]
        public bool Hydrated { get; protected set; }

        /**
         * Given an Dictionary<string, object> we assign the data to a new TModel and return it.
         *
         * ```cs
         * 	var entity = Models.Foo.Hydrate
         * 	(
         * 		Context.GlobalCtx.Qb
         *		.SELECT("*")
         *		.FROM("FoosTable")
         *		.WHERE("Bar = {0}", "XYZ")
         *		.WHERE("Baz != {0}", "ABC")
         *		.WHERE("SomeReallyComlexSQLQuery")
         *		.Row
         * 	);
         * ```
         */
        public static TModel Hydrate(Dictionary<string, object> record, bool fromUser = false)
        {
            // Make sure we have some data to actually hydrate the entity with.
            if (record.Count == 0)
            {
                throw new NullReferenceException("Empty record!");
            }

            // Create the new entity
            var entity = new TModel();

            if (!fromUser)
            {
                entity.Hydrated = true;

                // Save the record to the entity.
                // We do this so that we may quickly look up any
                // foreign keys when and if the time comes.
                entity.DbRecord = record;
            }

            // Hydrate all primative types.
            foreach(var col in record)
            {
                if (col.Value == null) continue;

                if (typeof(TModel).GetProperty(col.Key) != null)
                {
                    entity.Set(col.Value, col.Key, triggerChangeEvent: fromUser);
                }
            }

            // NOTE: Relationships are loaded as they are requested.

            return entity;
        }

        /**
         * Given a list of Dictionary<string, object>s we assign the data
         * to a new list of TModels and return it.
         *
         * ```cs
         * 	var entities = Models.Foo.Hydrate
         * 	(
         * 		Context.GlobalCtx.Qb
         *		.SELECT("*")
         *		.FROM("FoosTable")
         *		.WHERE("Bar = {0}", "XYZ")
         *		.WHERE("Baz != {0}", "ABC")
         *		.WHERE("SomeReallyComlexSQLQuery")
         *		.Rows
         * 	);
         * ```
         */
        public static List<TModel> Hydrate(List<Dictionary<string, object>> records, bool fromUser = false)
        {
            var entities = new List<TModel>();

            records.ForEach(record => entities.Add(Hydrate(record, fromUser)));

            return entities;
        }

        /**
         * When ever a property is changed this will run and ensure we have
         * saved all new entities to our dicovered list so we may load them
         * easily in the future.
         */
        protected void SaveDiscoveredEntities(PropertyInfo prop, object value = null)
        {
            // Ignore all primative types
            if (TypeMapper.IsClrType(prop.PropertyType)) return;

            // Grab the value of the local property
            if (value == null)
            {
                value = this.Get<object>
                (
                    prop.Name,
                    loadFromDiscovered: false,
                    loadFromDb: false
                );
            }

            // Bail out if the value is null, we have nothing to do.
            if (value == null) return;

            // Save any loaded entities into our discovered entities list.
            if (TypeMapper.IsListOfEntities(value))
            {
                foreach (var entity in (value as IEnumerable<object>))
                {
                    if (!this.DiscoveredEntities.Contains(entity))
                    {
                        this.DiscoveredEntities.Add(entity);
                        ((IModel<Model>)entity).DiscoveredEntities = this.DiscoveredEntities;
                    }
                }
            }
            else
            {
                if (!this.DiscoveredEntities.Contains(value))
                {
                    this.DiscoveredEntities.Add(value);
                    ((IModel<Model>)value).DiscoveredEntities = this.DiscoveredEntities;
                }
            }
        }

        /**
         * This just keeps a list of all the mapped properties that have
         * changed since hydration.
         */
        protected void UpdateModified(PropertyInfo changedProp)
        {
            if (!this.ModifiedProps.Contains(changedProp))
            {
                this.ModifiedProps.Add(changedProp);
            }
        }

        /**
         * Filters out Soft Deleted Entities
         *
         * All "READ" methods will use this to filter out any soft deleted
         * entities. Each of the methods will have a "withTrashed" parameter.
         */
        public static Linq<TModel> FilterTrashed(bool withTrashed = false)
        {
            if (withTrashed)
            {
                return Linq;
            }
            else
            {
                return Linq.Where(e => e.DeletedAt == null);
            }
        }

        /**
         * Find an entity by it's primary key.
         *
         * ```cs
         * 	var entity = Models.Foo.Find(123);
         * ```
         *
         * > NOTE: Returns null if nothing, throws exception if more than one.
         */
        public static TModel Find(int key, bool withTrashed = false)
        {
            // Entities with an Id of 0 will NEVER exist in the database.
            if (key == 0) return null;

            return FilterTrashed(withTrashed).SingleOrDefault(e => e.Id == key);
        }

        /**
         * Find a similar entity.
         *
         * ```cs
         * 	var entity = Models.Foo.Find(new Foo { Bar = "abc" });
         * ```
         *
         * > NOTE: Returns null if nothing, throws exception if more than one.
         */
        public static TModel Find(TModel entity, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).SingleOrDefault
            (
                ExpressionBuilder.BuildEqualityExpression(entity)
            );
        }

        /**
         * Checks to see if an entity exists by it's primary key.
         *
         * ```cs
         * 	if (Models.Foo.Exists(123))
         * 	{
         *
         * 	}
         * ```
         */
        public static bool Exists(int key, bool withTrashed = false)
        {
            // Entities with an Id of 0 will NEVER exist in the database.
            if (key == 0) return false;

            try
            {
                FilterTrashed(withTrashed).Single(e => e.Id == key);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /**
         * Checks to see if a similar entity already exists.
         *
         * ```cs
         * 	if (Models.Foo.Exists(new Foo { Bar = "abc" }))
         * 	{
         *
         * 	}
         * ```
         */
        public static bool Exists(TModel entity, bool withTrashed = false)
        {
            try
            {
                FilterTrashed(withTrashed).Single
                (
                    ExpressionBuilder.BuildEqualityExpression(entity)
                );
            }
            catch
            {
                return false;
            }

            return true;
        }

        /**
         * Do all entities in the set pass the expression?
         *
         * ```cs
         * 	if (Models.Foo.All(e => e.Bar == "abc"))
         * 	{
         * 		// All Foo's have their Bar property set to abc
         * 	}
         * 	else
         * 	{
         * 		// Not all Foo's have their Bar property set to abc
         * 	}
         * ```
         */
        public static bool All(Expression<Func<TModel, bool>> predicate, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).All(predicate);
        }

        /**
         * Do all entities in the set pass the dynamic string expression?
         *
         * ```cs
         * 	if (Models.Foo.All("e => e.Bar == \"abc\""))
         * 	{
         * 		// All Foo's have their Bar property set to abc
         * 	}
         * 	else
         * 	{
         * 		// Not all Foo's have their Bar property set to abc
         * 	}
         * ```
         */
        public static bool All(string predicate, bool withTrashed = false)
        {
            return All(ExpressionBuilder.BuildPredicateExpression<TModel>(predicate), withTrashed);
        }

        /**
         * Are there any entities in the set at all?
         *
         * ```cs
         * 	if (Models.Foo.Where(e => e.Bar == "abc").Any())
         * 	{
         * 		// The set contains at least one Foo.
         * 	}
         * 	else
         * 	{
         * 		// No Foo's were found.
         * 	}
         * ```
         */
        public static bool Any(bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).Any();
        }

        /**
         * Do any entities in the set pass the expression?
         *
         * ```cs
         * 	if (Models.Foo.Any(e => e.Bar == "abc"))
         * 	{
         * 		// At least one Foo has it's Bar property set to abc
         * 	}
         * 	else
         * 	{
         * 		// No Foo's have their Bar property set to abc
         * 	}
         * ```
         */
        public static bool Any(Expression<Func<TModel, bool>> predicate, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).Any(predicate);
        }

        /**
         * Do any entities in the set pass the dynamic string expression?
         *
         * ```cs
         * 	if (Models.Foo.Any("e => e.Bar == \"abc\""))
         * 	{
         * 		// At least one Foo has it's Bar property set to abc
         * 	}
         * 	else
         * 	{
         * 		// No Foo's have their Bar property set to abc
         * 	}
         * ```
         */
        public static bool Any(string predicate, bool withTrashed = false)
        {
            return Any(ExpressionBuilder.BuildPredicateExpression<TModel>(predicate), withTrashed);
        }

        /**
         * How many entities are there in the set?
         *
         * ```cs
         * 	var numberOfEntities = Models.Foo.Count();
         * ```
         */
        public static int Count(bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).Count();
        }

        /**
         * Number of entities that match the expression?
         *
         * ```cs
         * 	var numberOfEntities = Models.Foo.Count(e => e.Bar == "abc");
         * ```
         */
        public static int Count(Expression<Func<TModel, bool>> predicate, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).Count(predicate);
        }

        /**
         * Number of entities that match the dynamic string expression?
         *
         * ```cs
         * 	var numberOfEntities = Models.Foo.Count("e => e.Bar == \"abc\"");
         * ```
         */
        public static int Count(string predicate, bool withTrashed = false)
        {
            return Count(ExpressionBuilder.BuildPredicateExpression<TModel>(predicate), withTrashed);
        }

        /**
         * Returns the first entity of the set.
         *
         * ```cs
         * 	var entity = Models.Foo.First();
         * ```
         */
        public static TModel First(bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).First();
        }

        /**
         * Returns the first entity of the set that matches the expression.
         *
         * ```cs
         * 	var entity = Models.Foo.First(e => e.Bar == "abc");
         * ```
         */
        public static TModel First(Expression<Func<TModel, bool>> predicate, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).First(predicate);
        }

        /**
         * Returns the first entity of the set that matches the
         * dynamic string expression.
         *
         * ```cs
         * 	var entity = Models.Foo.First("e => e.Bar == \"abc\"");
         * ```
         */
        public static TModel First(string predicate, bool withTrashed = false)
        {
            return First(ExpressionBuilder.BuildPredicateExpression<TModel>(predicate), withTrashed);
        }

        /**
         * Returns the first element of the set,
         * or a default value if the set contains no elements.
         *
         * ```cs
         * 	var entity = Models.Foo.FirstOrDefault();
         * ```
         */
        public static TModel FirstOrDefault(bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).FirstOrDefault();
        }

        /**
         * Returns the first element of the set that matches the expression,
         * or a default value if the set contains no elements.
         *
         * ```cs
         * 	var entity = Models.Foo.FirstOrDefault(e => e.Bar == "abc");
         * ```
         */
        public static TModel FirstOrDefault(Expression<Func<TModel, bool>> predicate, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).FirstOrDefault(predicate);
        }

        /**
         * Returns the first element of the set that matches the dynamic string
         * expression, or a default value if the set contains no elements.
         *
         * ```cs
         * 	var entity = Models.Foo.FirstOrDefault("e => e.Bar == \"abc\"");
         * ```
         */
        public static TModel FirstOrDefault(string predicate, bool withTrashed = false)
        {
            return FirstOrDefault(ExpressionBuilder.BuildPredicateExpression<TModel>(predicate), withTrashed);
        }

        /**
         * Returns the only entity of the set. If the set contains no entities
         * or more than one entity then an exception will be thrown.
         *
         * ```cs
         * 	var entity = Models.Foo.Single();
         * ```
         */
        public static TModel Single(bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).Single();
        }

        /**
         * Returns the only entity of the set that matches the expression.
         * If the set contains no entities or more than one entity then an
         * exception will be thrown.
         *
         * ```cs
         * 	var entity = Models.Foo.Single(e => e.Bar == "abc");
         * ```
         */
        public static TModel Single(Expression<Func<TModel, bool>> predicate, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).Single(predicate);
        }

        /**
         * Returns the only entity of the set that matches the dynamic string
         * expression. If the set contains no entities or more than one entity
         * then an exception will be thrown.
         *
         * ```cs
         * 	var entity = Models.Foo.Single("e => e.Bar == \"abc\"");
         * ```
         */
        public static TModel Single(string predicate, bool withTrashed = false)
        {
            return Single(ExpressionBuilder.BuildPredicateExpression<TModel>(predicate), withTrashed);
        }

        /**
         * Returns the only element of the set, or a default value if the set
         * is empty; this method throws an exception if there is more than one
         * element in the set.
         *
         * ```cs
         * 	var entity = Models.Foo.SingleOrDefault();
         * ```
         */
        public static TModel SingleOrDefault(bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).SingleOrDefault();
        }

        /**
         * Returns the only element of the set that matches the expression, or a
         * default value if the set is empty; this method throws an exception
         * if there is more than one element in the set.
         *
         * ```cs
         * 	var entity = Models.Foo.SingleOrDefault(e => e.Bar == "abc");
         * ```
         */
        public static TModel SingleOrDefault(Expression<Func<TModel, bool>> predicate, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).SingleOrDefault(predicate);
        }

        /**
         * Returns the only element of the set that matches the dynamic string
         * expression, or a default value if the set is empty; this method
         * throws an exception if there is more than one element in the set.
         *
         * ```cs
         * 	var entity = Models.Foo.SingleOrDefault("e => e.Bar == \"abc\"");
         * ```
         */
        public static TModel SingleOrDefault(string predicate, bool withTrashed = false)
        {
            return SingleOrDefault(ExpressionBuilder.BuildPredicateExpression<TModel>(predicate), withTrashed);
        }

        /**
         * Filters the set based on the expression.
         * This does not return results but a new filtered Linq<TModel>.
         *
         * ```cs
         * 	var filteredEntities = Models.Foo.Where(e => e.Bar == "abc");
         * ```
         */
        public static Linq<TModel> Where(Expression<Func<TModel, bool>> predicate, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).Where(predicate);
        }

        /**
         * Filters the set based on the dyanmic string expression.
         * This does not return results but a new filtered Linq<TModel>.
         *
         * ```cs
         * 	var filteredEntities = Models.Foo.Where("e => e.Bar == \"abc\"");
         * ```
         */
        public static Linq<TModel> Where(string predicate, bool withTrashed = false)
        {
            return Where(ExpressionBuilder.BuildPredicateExpression<TModel>(predicate), withTrashed);
        }

        /**
         * Filters the set based on the expression, using sql LIKE clauses.
         * This does not return results but a new filtered Linq<TModel>.
         *
         * ```
         * 	var filteredEntities = Models.Foo.Like(e => e.Bar == "%abc%");
         * ```
         *
         * > NOTE: You may use ```!=``` for a NOT LIKE query.
         */
        public static Linq<TModel> Like(Expression<Func<TModel, bool>> predicate, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).Like(predicate);
        }

        /**
         * Filters the set based on the dyanmic string expression, using sql
         * LIKE clauses. This does not return results but a new filtered
         * Linq<TModel>.
         *
         * ```cs
         * 	var filteredEntities = Models.Foo.Like("e => e.Bar == \"%abc%\"");
         * ```
         *
         * > NOTE: You may use ```!=``` for a NOT LIKE query.
         */
        public static Linq<TModel> Like(string predicate, bool withTrashed = false)
        {
            return Like(ExpressionBuilder.BuildPredicateExpression<TModel>(predicate), withTrashed);
        }

        /**
         * Orders the set based on the expression.
         *
         * ```cs
         * 	var orderedEntities = Models.Foo
         * 	.OrderBy(e => e.Bar, OrderDirection.DESC)
         * 	.OrderBy(e => e.Baz, OrderDirection.ASC);
         * ```
         *
         * > NOTE: ASC is the default direction, if not supplied.
         */
        public static Linq<TModel> OrderBy(Expression<Func<TModel, object>> predicate, OrderDirection direction = OrderDirection.ASC, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).OrderBy(predicate, direction);
        }

        /**
         * Orders the set based on the dynamic string expression.
         *
         * ```cs
         * 	var orderedEntities = Models.Foo
         * 	.OrderBy("e => e.Bar", OrderDirection.DESC)
         * 	.OrderBy("e => e.Baz", OrderDirection.ASC);
         * ```
         *
         * > NOTE: ASC is the default direction, if not supplied.
         */
        public static Linq<TModel> OrderBy(string predicate, OrderDirection direction = OrderDirection.ASC, bool withTrashed = false)
        {
            return OrderBy(ExpressionBuilder.BuildPropertySelectExpression<TModel>(predicate), direction, withTrashed);
        }

        /**
         * Bypasses a specified number of entities in the set
         * and then returns the remaining entities.
         *
         * ```cs
         * 	var first10EntitiesIgnored = Models.Foo.Skip(10);
         * ```
         */
        public static Linq<TModel> Skip(int count, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).Skip(count);
        }

        /**
         * Returns a specified number of contiguous
         * entities from the start of the set.
         *
         * ```cs
         * 	var IHave10Entities = Models.Foo.Take(10);
         * ```
         */
        public static Linq<TModel> Take(int count, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).Take(count);
        }

        /**
         * Returns an array of entities.
         *
         * ```cs
         * 	foreach (var entity in Models.Foo.ToArray())
         * 	{
         *
         * 	}
         * ```
         */
        public static TModel[] ToArray(bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).ToArray();
        }

        /**
         * Returns a List of entities.
         *
         * ```cs
         * 	Models.Foo.ToList().ForEach(entity =>
         *  {
         *
         *  });
         * ```
         */
        public static List<TModel> ToList(bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).ToList();
        }

        /**
         * Given an instance we will add it to the db then return it.
         *
         * ```cs
         * 	var entity = Model.Foo.Create(new Foo { Bar = "abc" });
         * ```
         *
         * Which is exactly the same as:
         *
         * ```cs
         * 	var entity = new Foo { Bar = "abc" }.Save();
         * ```
         */
        public static TModel Create(TModel entity)
        {
            // If we are creating a new entity this implies the data is coming
            // from an untrusted source and thus has not really been hydrated
            // with already validated data from the database. Setting this to
            // false will ensure all validation checks are performed upon save.
            entity.Hydrated = false;

            return entity.Save();
        }

        /**
         * Given a Dict we will create a new instance and save it.
         *
         * ```cs
         * 	var entity = Model.Foo.Create
         * 	(
         * 		new Dictionary<string, object>
         * 		{
         * 			{"Bar", "abc"},
         * 		 	{"Qux", 123}
         * 	   	}
         * 	);
         * ```
         *
         * > NOTE: You can not supply relationships, please use JSON.
         *
         * > TODO: Give entities Foreign Key Column properties.
         */
        public static TModel Create(Dictionary<string, object> record)
        {
            return Create(Hydrate(record));
        }

        /**
         * Given a json object we will create a new instance and save it.
         *
         * ```cs
         * 	var entity = Model.Foo.Create(@"{'Bar':'abc','Qux':123}");
         * ```
         */
        public static TModel Create(string json)
        {
            return Create(FromJson(json));
        }

        /**
         * Given a list of entities will save them all for you.
         *
         * ```cs
         * 	var entities = Foo.Create
         * 	(
         * 		new List<Foo>
         * 	 	{
         * 		 	new Foo { Bar = "..." },
         * 		  	new Foo { Bar = "..." },
         * 		   	new Foo { Bar = "..." }
         * 	     }
         * 	);
         * ```
         */
        public static List<TModel> CreateMany(List<TModel> entities)
        {
            entities.ForEach(entity => entity.Save());

            return entities;
        }

        /**
         * Given a List of Dicts we will create a new list of entities.
         *
         * ```cs
         * 	var entities = Model.Foo.CreateMany
         * 	(
         * 		new List<Dictionary<string, object>>
         * 		{
         * 			new Dictionary<string, object>
         * 		 	{
         * 			 	{"Bar", "abc"},
         * 		 	 	{"Qux", 123}
         * 	   	 	},
         * 	   	 	new Dictionary<string, object>
         * 		 	{
         * 			 	{"Bar", "xyz"},
         * 		 	 	{"Qux", 987}
         * 	   	 	},
         * 		}
         * 	);
         * ```
         *
         * > NOTE: You can not supply relationships, please use JSON.
         *
         * > TODO: Give entities Foreign Key Column properties.
         */
        public static List<TModel> CreateMany(List<Dictionary<string, object>> records)
        {
            var entities = new List<TModel>();

            records.ForEach(record =>
            {
                entities.Add(Create(Hydrate(record)));
            });

            return entities;
        }

        /**
         * Given a json array we will return a list of saved entities.
         *
         * ```cs
         * 	var entities = Model.Foo.Create(@"[{...},{...},{...}]");
         * ```
         */
        public static List<TModel> CreateMany(string json)
        {
            var entities = FromJsonArray(json);

            foreach (var entity in entities)
            {
                Create(entity);
            }

            return entities;
        }

        /**
         * Given an instance we will first check to see if a similar entity
         * exists in the database. If it does we will return that instance.
         * If not we will create a new instance for you.
         *
         * ```cs
         * 	var entity = Models.Foo.SingleOrCreate(new Foo { Bar = "abc" });
         * ```
         */
        public static TModel SingleOrCreate(TModel entity)
        {
            var existing = SingleOrDefault
            (
                ExpressionBuilder.BuildEqualityExpression(entity)
            );

            if (existing == null)
            {
                return Create(entity);
            }
            else
            {
                return existing;
            }
        }

        /**
         * Given a Dict we will first check to see if a similar entity exists
         * in the database. If it does we will return that instance. If not we
         * will create a new instance for you.
         *
         * ```cs
         * 	var entity = Models.Foo.SingleOrCreate
         * 	(
         * 		new Dictionary<string, object>
         * 		{
         * 			{"Bar", "abc"},
         * 			{"Baz", 123}
         * 		}
         * 	);
         * ```
         */
        public static TModel SingleOrCreate(Dictionary<string, object> record)
        {
            return SingleOrCreate(Hydrate(record));
        }

        /**
         * Given a json string we will first check to see if a similar entity
         * exists in the database. If it does we will return that instance.
         * If not we will create a new instance for you.
         *
         * ```cs
         * 	var entity = Models.Foo.SingleOrCreate("{...JSON...}");
         * ```
         */
        public static TModel SingleOrCreate(string json)
        {
            return SingleOrCreate(FromJson(json));
        }

        /**
         * Similar to SingleOrCreate except we do not add the new instance
         * to the database. We simply return the model you passed in.
         * It is then on you to call Save() on that model if you wish to
         * add it to the db.
         *
         * ```cs
         * 	var user = Model.User.SingleOrNew(new User { Name = "Fred"; });
         * 	user.BirthYear = 2001;
         * 	user.Save();
         * ```
         *
         * > NOTE: This can be handy when merging object graphs manually.
         */
        public static TModel SingleOrNew(TModel entity)
        {
            var existing = SingleOrDefault
            (
                ExpressionBuilder.BuildEqualityExpression(entity)
            );

            if (existing == null)
            {
                entity.Hydrated = false;
                return entity;
            }
            else
            {
                return existing;
            }
        }

        /**
         * Similar to SingleOrCreate except we do not add the new instance
         * to the database. We simply return the model you passed in.
         * It is then on you to call Save() on that model if you wish to
         * add it to the db.
         *
         * ```cs
         * 	var user = Model.User.SingleOrNew
         * 	(
         * 		new Dictionary<string, object>
         * 		{
         * 			{"Name", "Fred"}
         * 		}
         * 	);
         * 	user.BirthYear = 2001;
         * 	user.Save();
         * ```
         *
         * > NOTE: This can be handy when merging object graphs manually.
         */
        public static TModel SingleOrNew(Dictionary<string, object> record)
        {
            return SingleOrNew(Hydrate(record));
        }

        /**
         * Similar to SingleOrCreate except we do not add the new instance
         * to the database. We simply return the model you passed in.
         * It is then on you to call Save() on that model if you wish to
         * add it to the db.
         *
         * ```cs
         * 	var user = Model.User.SingleOrNew("{...JSON...}");
         * 	user.BirthYear = 2001;
         * 	user.Save();
         * ```
         *
         * > NOTE: This can be handy when merging object graphs manually.
         */
        public static TModel SingleOrNew(string json)
        {
            return SingleOrNew(FromJson(json));
        }

        /**
         * Given an instance we will first check to see if there are any similar
         * entities that exist in the database. If there are 1 or more entities
         * that are similar to the provided we will return the first in the set.
         * If there are no similar entities we will save the provided entity
         * and return it.
         *
         * ```cs
         * 	var entity = Models.Foo.FirstOrCreate(new Foo { Bar = "abc" });
         * ```
         */
        public static TModel FirstOrCreate(TModel entity)
        {
            var existing = FirstOrDefault
            (
                ExpressionBuilder.BuildEqualityExpression(entity)
            );

            if (existing == null)
            {
                return Create(entity);
            }
            else
            {
                return existing;
            }
        }

        /**
         * Given a Dict we will first check to see if there are any similar
         * entities that exist in the database. If there are 1 or more entities
         * that are similar to the provided we will return the first in the set.
         * If there are no similar entities we will save the provided entity
         * and return it.
         *
         * ```cs
         * 	var entity = Models.Foo.FirstOrCreate
         * 	(
         * 		new Dictionary<string, object>
         * 		{
         * 			{"Bar", "abc"}
         * 		}
         * 	);
         * ```
         */
        public static TModel FirstOrCreate(Dictionary<string, object> record)
        {
            return FirstOrCreate(Hydrate(record));
        }

        /**
         * Given some json we will first check to see if there are any similar
         * entities that exist in the database. If there are 1 or more entities
         * that are similar to the provided we will return the first in the set.
         * If there are no similar entities we will save the provided entity
         * and return it.
         *
         * ```cs
         * 	var entity = Models.Foo.FirstOrCreate("{...JSON...}");
         * ```
         */
        public static TModel FirstOrCreate(string json)
        {
            return FirstOrCreate(FromJson(json));
        }

        /**
         * Similar to FirstOrCreate except we do not add the new instance
         * to the database. We simply return the model you passed in.
         * It is then on you to call Save() on that model if you wish to
         * add it to the db.
         *
         * ```cs
         * 	var user = Model.User.FirstOrNew(new User { Name = "Fred"; });
         * 	user.BirthYear = 2001;
         * 	user.Save();
         * ```
         */
        public static TModel FirstOrNew(TModel entity)
        {
            var existing = FirstOrDefault
            (
                ExpressionBuilder.BuildEqualityExpression(entity)
            );

            if (existing == null)
            {
                entity.Hydrated = false;
                return entity;
            }
            else
            {
                return existing;
            }
        }

        /**
         * Similar to FirstOrCreate except we do not add the new instance
         * to the database. We simply return the model you passed in.
         * It is then on you to call Save() on that model if you wish to
         * add it to the db.
         *
         * ```cs
         * 	var user = Model.User.FirstOrNew
         * 	(
         * 		new Dictionary<string, object>
         * 		{
         * 			{"Bar", "abc"}
         * 		}
         * 	);
         * 	user.BirthYear = 2001;
         * 	user.Save();
         * ```
         */
        public static TModel FirstOrNew(Dictionary<string, object> record)
        {
            return FirstOrNew(Hydrate(record));
        }

        /**
         * Similar to FirstOrCreate except we do not add the new instance
         * to the database. We simply return the model you passed in.
         * It is then on you to call Save() on that model if you wish to
         * add it to the db.
         *
         * ```cs
         * 	var user = Model.User.FirstOrNew("{...JSON...}");
         * 	user.BirthYear = 2001;
         * 	user.Save();
         * ```
         */
        public static TModel FirstOrNew(string json)
        {
            return FirstOrNew(FromJson(json));
        }

        /**
         * Given a Dict we will find an entity from the database with the same
         * Id and then merge dict into the entity, saving the result.
         *
         * ```cs
         * 	var entity = Model.Foo.Update
         * 	(
         * 		new Dictionary<string, object>
         * 		{
         * 			{"Id", 123},
         * 			{"Bar", "abc"},
         * 		 	{"Qux", 123}
         * 	   	}
         * 	);
         * ```
         *
         * > NOTE: You can not supply relationships, please use JSON.
         *
         * > TODO: Give entities Foreign Key Column properties.
         */
        public static TModel Update(Dictionary<string, object> record)
        {
            var update = Hydrate(record, fromUser: true);
            var existing = Single(e => e.Id == update.Id, withTrashed: true);
            return MergeEntities(update, existing).Save();
        }

        /**
         * Given a json object we will find an entity from the database with
         * the same Id and then merge json into the entity, saving the result.
         *
         * ```cs
         * 	var entity = Model.Foo.Update
         * 	(
         * 		"{ Id: 123, Bar: abc, Qux: 123 }"
         * 	);
         * ```
         */
        public static TModel Update(string json)
        {
            var update = FromJson(json);
            var existing = Single(e => e.Id == update.Id, withTrashed: true);
            return MergeEntities(update, existing).Save();
        }

        /**
         * Given a List of entities we will loop through each entity, find an
         * entity from the database with the same Id and then merge the provided
         * entity into the existing entity, saving the result.
         *
         * ```cs
         * 	Model.Foo.UpdateMany
         * 	(
         * 		new List<Foo>
         * 		{
         * 			new Foo { Id = 1, Bar = "abc" },
         * 			new Foo { Id = 2, Bar = "123" }
         * 		}
         * 	);
         * ```
         */
        public static void UpdateMany(List<TModel> entities)
        {
            entities.ForEach(update =>
            {
                MergeEntities(update, Single(e => e.Id == update.Id, withTrashed: true)).Save();
            });
        }

        /**
         * Given a json array we will loop through each entity, find an entity
         * from the database with the same Id and then merge json into the
         * entity, saving the result.
         *
         * ```cs
         * 	Model.Foo.UpdateMany
         * 	(
         * 		"[{Id:123,Bar:abc,Qux:123},{Id:456,Bar:xyz,Qux:789}]"
         * 	);
         * ```
         */
        public static void UpdateMany(string json)
        {
            UpdateMany(FromJsonArray(json));
        }

        /**
         * Updates enMasse without first loading the entites into memory.
         *
         * ```cs
         * 	Models.Foo.UpdateAll(e => e.Bar == "abc" && e.Baz == 123);
         * 	Models.Foo.Where(e => e.Id == 78).UpdateAll(e => e.Qux == "qwerty");
         * ```
         *
         * __The expression is NOT a "WHERE" predicate:__
         * The expression you provide this Update method follows the same
         * structure as a predicate but we parse it slightly diffrently.
         * Consider each "&&" or "||" as a comma and each "==" simply as a "="
         * operator.
         *
         * __Relationships:__
         * You can only update primative values, you can not update
         * relationships using this method.
         *
         * > NOTE: Keep in mind this will not trigger any of the entity events.
         */
        public static void UpdateAll(Expression<Func<TModel, bool>> assignments, bool withTrashed = false)
        {
            FilterTrashed(withTrashed).UpdateAll(assignments);
        }

        /**
         * Updates enMasse without first loading the entites into memory.
         *
         * ```cs
         * 	Models.Foo.UpdateAll("e.Bar == \"abc\" && e.Baz == 123");
         * 	Models.Foo.Where(e => e.Id == 78).UpdateAll("e.Qux == \"qwerty\"");
         * ```
         *
         * __The expression is NOT a "WHERE" predicate:__
         * The expression you provide this Update method follows the same
         * structure as a predicate but we parse it slightly diffrently.
         * Consider each "&&" or "||" as a comma and each "==" simply as a "="
         * operator.
         *
         * __Relationships:__
         * You can only update primative values, you can not update
         * relationships using this method.
         *
         * > NOTE: Keep in mind this will not trigger any of the entity events.
         */
        public static void UpdateAll(string assignments, bool withTrashed = false)
        {
            UpdateAll(ExpressionBuilder.BuildPredicateExpression<TModel>(assignments), withTrashed);
        }

        /**
         * Check to see if a similar model exists, using properties.
         * If so lets update it with some new values.
         * If not lets create a brand new record.
         *
         * ```cs
         * 	var user = Models.User.UpdateOrCreate
         * 	(
         * 		new User { Name = "Brad" },
         * 		new User { Email = "brad@kdis.com.au" }
         * 	);
         * ```
         */
        public static TModel UpdateOrCreate(TModel find, TModel update)
        {
            var existing = SingleOrDefault
            (
                ExpressionBuilder.BuildEqualityExpression(find)
            );

            if (existing == null)
            {
                return Create(update);
            }
            else
            {
                return MergeEntities(update, existing).Save();
            }
        }

        /**
         * Check to see if model exists, using expression.
         * If so lets update it with some new values.
         * If not lets create a brand new record.
         *
         * ```cs
         * 	var user = Models.User.UpdateOrCreate
         * 	(
         * 		e => e.Id == 1,
         * 		new User { Email = "brad@kdis.com.au" }
         * 	);
         * ```
         */
        public static TModel UpdateOrCreate(Expression<Func<TModel, bool>> find, TModel update)
        {
            var existing = SingleOrDefault(find);

            if (existing == null)
            {
                return Create(update);
            }
            else
            {
                return MergeEntities(update, existing).Save();
            }
        }

        /**
         * This overload is super useful for seeding or other mass creation.
         *
         * ```cs
         * 	Models.User.UpdateOrCreate
         * 	(
         * 		"Id",
         * 		new User { Id = 1, Name = "Brad" },
         * 		new User { Id = 2, Name = "Fred" },
         * 		new User { Id = 3, Name = "Bob" }
         * 	);
         * ```
         *
         * > NOTE: You should NOT use Id unless you specfically set the Id for
         * > each entity in the seed. If you do not set the Id yourself the
         * > database server will, which means the User whos name is Brad may
         * > not always have the Id of "1".
         */
        public static void UpdateOrCreate(string find, params TModel[] entities)
        {
            entities.ToList().ForEach(entity =>
            {
                var property = MappedProps.Single(p => p.Name == find);
                var propertyValue = property.GetValue(entity);
                var propertyType = propertyValue.GetType();

                var expressionString = new StringBuilder();
                expressionString.Append("e.");
                expressionString.Append(property.Name);
                expressionString.Append(" == ");

                switch (propertyType.FullName)
                {
                    case "System.Int64":
                    case "System.Int32":
                    case "System.Int16":
                    case "System.Decimal":
                    case "System.Double":
                    case "System.Single":
                    case "System.Boolean":
                        expressionString.Append(propertyValue);
                    break;

                    case "System.Char":
                        expressionString.Append("'");
                        expressionString.Append(propertyValue);
                        expressionString.Append("'");
                    break;

                    case "System.String":
                        expressionString.Append('"');
                        expressionString.Append(propertyValue);
                        expressionString.Append('"');
                    break;
                }

                UpdateOrCreate
                (
                    ExpressionBuilder.BuildPredicateExpression<TModel>
                    (
                        expressionString.ToString()
                    ),
                    entity
                );
            });
        }

        /**
         * Deletes enMasse without first loading the entites into memory.
         *
         * ```cs
         * 	// soft deletes all Foo's in the table!
         * 	Models.Foo.DeleteAll();
         *
         * 	// soft deletes all Foo's that have Bar set to Baz
         * 	Models.Foo.Where(e => e.Bar == "Baz").DeleteAll();
         *
         * // hard deletes a Foo with the Id of 56
         * Models.Foo.Where(e => e.Id == 56).DeleteAll(hardDelete: true);
         * ```
         *
         * > NOTE: Keep in mind this will not trigger any of the entity events.
         */
        public static void DeleteAll(bool hardDelete = false)
        {
            Linq.DeleteAll(hardDelete);
        }

        /**
         * Delete the entity for the given primary key id.
         *
         * ```cs
         * 	Models.Foo.DeleteAll(12);
         * ```
         *
         * Which is the same as:
         *
         * ```cs
         * 	Models.Foo.Find(12).Delete();
         * ```
         *
         * Except that Destroy only performs a single SQL Query.
         * Use Delete when you already have an entity loaded.
         * Otherwise use Destroy if you know the Id in advance.
         *
         * > NOTE: Keep in mind this will not trigger any of the entity events.
         */
        public static void DeleteAll(int key, bool hardDelete = false)
        {
            if (hardDelete)
            {
                // Thats it the entity is really being Deleted Now.
                Db.Qb
                .DELETE_FROM(SqlTableName)
                .WHERE("Id", key)
                .Execute();
            }
            else
            {
                // If the entity has already been soft deleted,
                // we will just be updating the DeletedAt timestamp.
                Db.Qb
                .UPDATE(SqlTableName)
                .SET("ModifiedAt", DateTime.UtcNow)
                .SET("DeletedAt", DateTime.UtcNow)
                .WHERE("Id", key)
                .Execute();
            }
        }

        /**
         * Delete all entities for the given primary key ids.
         *
         * ```cs
         * 	Models.User.DeleteAll(43,57,102);
         * ```
         *
         * > NOTE: Keep in mind this will not trigger any of the entity events.
         */
        public static void DeleteAll(bool hardDelete = false, params int[] keys)
        {
            if (hardDelete)
            {
                Db.Qb
                .DELETE_FROM(SqlTableName)
                .WHERE("Id IN ({0})", keys)
                .Execute();
            }
            else
            {
                Db.Qb
                .UPDATE(SqlTableName)
                .SET("ModifiedAt", DateTime.UtcNow)
                .SET("DeletedAt", DateTime.UtcNow)
                .WHERE("Id IN ({0})", keys)
                .Execute();
            }
        }

        /**
         * Deletes an entity from the database.
         *
         * ```cs
         * 	Model.Foo.Find(1).Delete();
         * ```
         *
         * > NOTE: By default we only "Soft" delete.
         */
        public void Delete(bool hardDelete = false)
        {
            if (this.Id == 0)
            {
                throw new DeleteNonExistentEntityException(this);
            }

            if (!this.FireBeforeDelete(hardDelete)) return;

            if (hardDelete)
            {
                // Execute the DELETE Query
                // Thats it the record is gone now... bye bye
                Db.Qb.DELETE_FROM(SqlTableName)
                .WHERE("Id", this.Id)
                .Execute();
            }
            else
            {
                // Set the DeleteAt Timestamp
                this.DeletedAt = DateTime.UtcNow;

                // Update the model
                this.Save();
            }

            this.FireAfterDelete(hardDelete);
        }

        /**
         * Restores a soft deleted record.
         *
         * ```cs
         * 	var user = Models.User.Find(1, true).Restore();
         * ```
         *
         * > NOTE: If you need to restore in bulk, just perform a
         * > batch Update and set the DeletedAt property to null.
         */
        public TModel Restore()
        {
            if (!this.FireBeforeRestore()) return null;

            this.DeletedAt = null;
            this.Save();

            this.FireAfterRestore();

            return (TModel) this;
        }

        /**
         * Run just before an entity is about to be saved.
         * You may override this in your models like so:
         *
         * ```cs
         * 	class Foo : Model<Foo>
         * 	{
         * 		protected override bool Validate()
         * 		{
         * 			// "this" will be the entity you are validating.
         *
         * 			// you may call "base.Validate()" to run the default
         * 			// validation in addition to your code, or you may
         * 			// completely replace the default validation.
         * 		}
         * 	}
         * ```
         */
        protected virtual bool Validate()
        {
            var errors = new List<KeyValuePair<PropertyInfo, string>>();

            // Id check
            if (this.Id < 0)
            {
                errors.Add
                (
                    new KeyValuePair<PropertyInfo, string>
                    (
                        MappedProps.Single(p => p.Name == "Id"),
                        "Entities must have an Id greater than or equal to 0."
                    )
                );
            }

            // If we have a valid Id and the only other property that has
            // been modified is the ModifiedAt timestamp then we can skip
            // the validation.
            if (this.Id > 0 && this.ModifiedProps.Count == 2 && this.ModifiedProps[0].Name == "Id" && this.ModifiedProps[1].Name == "ModifiedAt")
            {
                return true;
            }

            MappedPropsExceptId.ForEach(prop =>
            {
                // Required Check
                var required = prop.GetCustomAttribute<RequiredAttribute>();
                if (required != null)
                {
                    if (this.Get<object>(prop.Name, loadFromDiscovered: false, loadFromDb: false) == null)
                    {
                        if (this.Hydrated)
                        {
                            if (this.ModifiedProps.Contains(prop) && !TypeMapper.IsNullable(prop))
                            {
                                errors.Add
                                (
                                    new KeyValuePair<PropertyInfo, string>
                                    (
                                        prop,
                                        "This property is required and does not allow NULL values."
                                    )
                                );
                            }
                        }
                        else
                        {
                            if (!TypeMapper.IsNullable(prop))
                            {
                                errors.Add
                                (
                                    new KeyValuePair<PropertyInfo, string>
                                    (
                                        prop,
                                        "This property is required and does not allow NULL values."
                                    )
                                );
                            }
                            else
                            {
                                errors.Add
                                (
                                    new KeyValuePair<PropertyInfo, string>
                                    (
                                        prop,
                                        "This property is required."
                                    )
                                );
                            }
                        }
                    }
                }

                // Min Length Check
                var minLength = prop.GetCustomAttribute<MinLengthAttribute>();
                if (minLength != null)
                {
                    var value = this.Get<object>(prop.Name, loadFromDiscovered: false, loadFromDb: false);
                    if (value != null)
                    {
                        bool result; int LengthGiven;

                        if (TypeMapper.IsList(value))
                        {
                            LengthGiven = ((dynamic)value).Count;
                            result = ((dynamic)value).Count >= minLength.Length ? true : false;
                        }
                        else
                        {
                            LengthGiven = ((dynamic)value).Length;
                            result = minLength.IsValid(value);
                        }

                        if (!result)
                        {
                            errors.Add
                            (
                                new KeyValuePair<PropertyInfo, string>
                                (
                                    prop,
                                    string.Format
                                    (
                                        "Does not meet minimum length requirement. MinLength: {0} LengthGiven: {1}",
                                        minLength.Length,
                                        LengthGiven
                                    )
                                )
                            );
                        }
                    }
                }

                // Max Length Check
                var maxLength = prop.GetCustomAttribute<MaxLengthAttribute>();
                if (maxLength != null)
                {
                    var value = this.Get<object>(prop.Name, loadFromDiscovered: false, loadFromDb: false);
                    if (value != null)
                    {
                        bool result; int LengthGiven;

                        if (TypeMapper.IsList(value))
                        {
                            LengthGiven = ((dynamic)value).Count;
                            result = ((dynamic)value).Count <= maxLength.Length ? true : false;
                        }
                        else
                        {
                            LengthGiven = ((dynamic)value).Length;
                            result = maxLength.IsValid(value);
                        }

                        if (!result)
                        {
                            errors.Add
                            (
                                new KeyValuePair<PropertyInfo, string>
                                (
                                    prop,
                                    string.Format
                                    (
                                        "Does not meet maximum length requirement. MaxLength: {0} LengthGiven: {1}",
                                        maxLength.Length,
                                        LengthGiven
                                    )
                                )
                            );
                        }
                    }
                }

                // Range Check
                var range = prop.GetCustomAttribute<RangeAttribute>();
                if (range != null)
                {
                    var value = this.Get<object>(prop.Name, loadFromDiscovered: false, loadFromDb: false);
                    if (value != null)
                    {
                        if (!range.IsValid(value))
                        {
                            errors.Add
                            (
                                new KeyValuePair<PropertyInfo, string>
                                (
                                    prop,
                                    string.Format
                                    (
                                        "Number is not between {0} and {1}.",
                                        range.Minimum,
                                        range.Maximum
                                    )
                                )
                            );
                        }
                    }
                }

                // String Length Check
                var stringLength = prop.GetCustomAttribute<StringLengthAttribute>();
                if (stringLength != null)
                {
                    var value = this.Get<object>(prop.Name, loadFromDiscovered: false, loadFromDb: false);
                    if (value != null)
                    {
                        if (!stringLength.IsValid(value))
                        {
                            errors.Add
                            (
                                new KeyValuePair<PropertyInfo, string>
                                (
                                    prop,
                                    string.Format
                                    (
                                        "String length is not between {0} and {1}.",
                                        stringLength.MinimumLength,
                                        stringLength.MaximumLength
                                    )
                                )
                            );
                        }
                    }
                }

                // Email Check
                var email = prop.GetCustomAttribute<EmailAddressAttribute>();
                if (email != null)
                {
                    var value = this.Get<object>(prop.Name, loadFromDiscovered: false, loadFromDb: false);
                    if (value != null)
                    {
                        if (!email.IsValid(value))
                        {
                            errors.Add
                            (
                                new KeyValuePair<PropertyInfo, string>
                                (
                                    prop,
                                    "Invalid email address supplied."
                                )
                            );
                        }
                    }
                }

                // Regular Expression Check
                var regex = prop.GetCustomAttribute<RegularExpressionAttribute>();
                if (regex != null)
                {
                    var value = this.Get<object>(prop.Name, loadFromDiscovered: false, loadFromDb: false);
                    if (value != null)
                    {
                        if (!regex.IsValid(value))
                        {
                            errors.Add
                            (
                                new KeyValuePair<PropertyInfo, string>
                                (
                                    prop,
                                    string.Format
                                    (
                                        "Value does not pass regular expression {0}",
                                        regex.Pattern
                                    )
                                )
                            );
                        }
                    }
                }
            });

            if (errors.Count > 0)
            {
                throw new ValidationException(errors);
            }

            return true;
        }

        /**
         * Saves an entity to the database.
         *
         * This method will look at the "Id" property, if 0 then the instance
         * must be a brand new record. And we perform an "INSERT" operation.
         * If the "Id" is greater than 0 then we perform an "UPDATE" operation.
         *
         * ```cs
         * 	var brad = new Models.User();
         * 	brad.FirstName = "Bradley";
         * 	brad.LastName = "Jones";
         * 	brad.Save();
         * ```
         *
         * > NOTE: We are recursive and will save all related entities.
         */
        public TModel Save(List<object> SavedEntities = null)
        {
            // Create the dealt with list, if it hasn't been created yet.
            // Relationships have 2 sides and once we have dealt with one side
            // of the relationship we do not need to do anything when we come
            // across the foreign side of the relationship otherwise we will
            // go a little loopy :)
            if (SavedEntities == null) SavedEntities = new List<object>();
            if (SavedEntities.Contains(this)) return (TModel)this;

            // Grab our class properties, that represent columns in the table.
            var props = MappedPropsExceptId;

            if (this.Id == 0)
            {
                // We are INSERTING
                // So Update both the created and modified times
                this.CreatedAt = DateTime.UtcNow;
                this.ModifiedAt = DateTime.UtcNow;
                if (!this.FireBeforeInsert()) return null;
            }
            else
            {
                // We are UPDATING
                // So Update the modified time only.
                this.ModifiedAt = DateTime.UtcNow;
                if (!this.FireBeforeUpdate()) return null;
            }

            if (!this.FireBeforeSave()) return null;

            // Remove any properties that have not changed since hydration.
            if (this.Id > 0 && this.ModifiedProps.Count > 0)
            {
                props.RemoveAll(prop => !this.ModifiedProps.Contains(prop));
            }

            // Filter out any properties that are relationships
            // with foreign keys stored in a foreign table.
            var insertableProps = props.Where(p =>
            {
                // All primative types are always insertable
                if (TypeMapper.IsClrType(p.PropertyType)) return true;

                // Grab the relation discriptor for the current property.
                var relation = Db.Relationships.Discovered.Single
                (
                    r => r.LocalProperty == p
                );

                // Take action based on the relationship type.
                switch (relation.Type)
                {
                    case RelationType.MtoM:

                        // the foreign key exists in a pivot table.
                        return false;

                    case RelationType.MtoO:

                        // the foreign key exists in the foreign table.
                        return false;

                    case RelationType.OtoM:

                        // the foreign key exists in this table.
                        return true;

                    case RelationType.OtoO:

                        // the foreign key may exist in this table
                        // or in the foreign table, we need to find out.
                        return relation.ForeignKeyTableName == SqlTableName;
                }

                // If we get to here something odd happend
                throw new Exception("Invalid Property!");

            }).ToList();

            // Only insert or update if we have something to insert or update.
            if (insertableProps.Count > 1)
            {
                // Create an array of column names
                var cols = insertableProps.Select(p =>
                {
                    if (TypeMapper.IsClrType(p.PropertyType))
                    {
                        return p.Name;
                    }
                    else
                    {
                        return Db.Relationships.Discovered
                        .Single(r => r.LocalProperty == p)
                        .ForeignKeyColumnName;
                    }
                }).ToArray();

                // Create an array of values
                var values = insertableProps.Select(p =>
                {
                    var value = p.GetValue(this);

                    if (value == null) return DBNull.Value;

                    if (TypeMapper.IsClrType(value))
                    {
                        return value;
                    }
                    else
                    {
                        if (SavedEntities.Contains(value))
                        {
                            if (((dynamic)value).DbRecord == null)
                            {
                                return (int)((dynamic)value).PropertyBag["Id"];
                            }
                            else
                            {
                                return (int)((dynamic)value).DbRecord["Id"];
                            }
                        }
                        else
                        {
                            return ((dynamic)value).Save(SavedEntities).Id;
                        }
                    }
                }).ToArray();

                // The new DbRecord.
                // In an insert it will contain all values, except Id.
                // In an update it will only contain those that changed.
                var record = cols.Zip(values, (k, v) => new { k, v })
                .ToDictionary(x => x.k, x =>
                {
                    if (x.v.GetType() == typeof(DBNull))
                    {
                        return null;
                    }
                    else
                    {
                        return x.v;
                    }
                });

                if (this.Id == 0)
                {
                    // Execute the INSERT Query
                    Db.Qb.INSERT_INTO(SqlTableName)
                    .COLS(cols).VALUES(values).Execute();

                    // Set the DbRecord
                    this.DbRecord = record;

                    // Grab the inserted id and update our model
                    //
                    // NOTE: SCOPE_IDENTITY does not work because we need to make
                    // the query with the same connection instance or possibly even
                    // with in the same SqlCommand.
                    this.DbRecord["Id"] = Convert.ToInt32
                    (
                        Db.Qb.SELECT("IDENT_CURRENT({0})", SqlTableName).Scalar
                    );

                    this.FireAfterInsert();
                }
                else
                {
                    // Execute the UPDATE query
                    Db.Qb.UPDATE(SqlTableName)
                    .SET
                    (
                        cols.Zip(values, (k, v) => new { k, v })
                        .ToDictionary(x => x.k, x => x.v)
                    )
                    .WHERE("Id", this.Id)
                    .Execute();

                    // Update the existing db record
                    if (this.DbRecord == null)
                    {
                        this.DbRecord = record;
                        this.DbRecord["Id"] = this.Id;
                    }
                    else
                    {
                        record.ToList().ForEach(item =>
                        {
                            this.DbRecord[item.Key] = item.Value;
                        });
                    }

                    this.FireAfterUpdate();
                }
            }

            SavedEntities.Add(this);

            // Save the relationships that require
            // the "Id" of the entity we just saved.
            props.Where(p => !insertableProps.Contains(p)).ToList()
            .ForEach(p =>
            {
                // Grab the relation
                var relation = Db.Relationships.Discovered.Single
                (
                    r => r.LocalProperty == p
                );

                // Grab the value of the property
                var value = p.GetValue(this);

                if (value == null) return;

                // Take action based on the relationship type.
                switch (relation.Type)
                {
                    case RelationType.MtoM:
                    {
                        var currentEntities = (value as IEnumerable<object>).Cast<IModel<Model>>().ToList();
                        var originalEntities = (this.OriginalPropertyBag[relation.LocalProperty.Name] as IEnumerable<object>).Cast<IModel<Model>>().ToList();

                        currentEntities.ForEach(e =>
                        {
                            if (!SavedEntities.Contains(e))
                            {
                                // Save the foreign side of the relationship
                                e.Save(SavedEntities);

                                // Only insert the pivot table entry if relationship is new.
                                if (!originalEntities.Contains(e))
                                {
                                    int firstId = (int)this.DbRecord["Id"];
                                    if (!relation.PivotTableFirstColumnName.Contains(relation.LocalTableNameSingular))
                                    {
                                        firstId = e.Id;
                                    }

                                    int secondId = e.Id;
                                    if (!relation.PivotTableSecondColumnName.Contains(relation.ForeignTableNameSingular))
                                    {
                                        secondId = (int)this.DbRecord["Id"];
                                    }

                                    Db.Qb.INSERT_INTO(relation.PivotTableName)
                                    .COLS
                                    (
                                        relation.PivotTableFirstColumnName,
                                        relation.PivotTableSecondColumnName
                                    )
                                    .VALUES(firstId, secondId)
                                    .Execute();
                                }
                            }
                        });

                        // Remove any relationships from the
                        // pivot table that got removed.
                        string col1 = relation.PivotTableFirstColumnName;
                        if (!relation.PivotTableFirstColumnName.Contains(relation.LocalTableNameSingular))
                        {
                            col1 = relation.PivotTableSecondColumnName;
                        }

                        string col2 = relation.PivotTableSecondColumnName;
                        if (!relation.PivotTableSecondColumnName.Contains(relation.ForeignTableNameSingular))
                        {
                            col2 = relation.PivotTableFirstColumnName;
                        }

                        originalEntities.ForEach(originalEntity =>
                        {
                            if (!currentEntities.Contains(originalEntity))
                            {
                                Db.Qb.DELETE_FROM(relation.PivotTableName)
                                .WHERE(col1, (int)this.DbRecord["Id"])
                                .WHERE(col2, originalEntity.Id)
                                .Execute();
                            }
                        });
                    }
                    break;

                    case RelationType.MtoO:
                    {
                        var currentEntities = (value as IEnumerable<object>)
                        .Cast<IModel<Model>>().ToList();

                        currentEntities.ForEach(e =>
                        {
                            if (relation.ForeignProperty != null)
                            {
                                e.Set(this, relation.ForeignProperty.Name);
                            }

                            e.Save(SavedEntities);
                        });

                        (this.OriginalPropertyBag[relation.LocalProperty.Name] as IEnumerable<object>)
                        .Cast<IModel<Model>>().ToList().ForEach(originalEntity =>
                        {
                            if (!currentEntities.Contains(originalEntity))
                            {
                                Db.Qb.UPDATE(relation.ForeignKeyTableName)
                                .SET(relation.ForeignKeyColumnName, null)
                                .WHERE("Id", originalEntity.Id).Execute();
                            }
                        });
                    }
                    break;

                    case RelationType.OtoO:

                        ((IModel<Model>)value).Set(this, relation.ForeignProperty.Name);
                        ((IModel<Model>)value).Save(SavedEntities);

                    break;

                    case RelationType.OtoM:

                        throw new Exception("OtoM - don't think we should ever get here....");
                }
            });

            if (this.DbRecord != null) this.Id = (int)this.DbRecord["Id"];

            // We have saved everything, so lets reset this list.
            this.ModifiedProps.Clear();

            this._OriginalPropertyBag = null;

            this.OriginalPropertyBag.Keys.ToList().ForEach(key =>
            {
                var value = this.Get<object>
                (
                    key, loadFromDiscovered: true, loadFromDb: false
                );

                if (value != null)
                {
                    if (TypeMapper.IsListOfEntities(value))
                    {
                        var clone = (value as IEnumerable<object>)
                        .Cast<IModel<Model>>().ToList();

                        this.OriginalPropertyBag[key] = clone;
                    }
                    else
                    {
                        this.OriginalPropertyBag[key] = value;
                    }
                }
            });

            this.FireAfterSave();

            return (TModel)this;
        }

        /**
         * Given 2 entities, an updated and an existing we merge them together.
         *
         * This method is smart enough to recurse into the object graph,
         * assuming an updated entity has a valid Id set, this method will
         * lookup the corresponding entity from the database and continue the
         * merge process.
         *
         * ```cs
         * 	var intial = new Foo
         * 	{
         * 		Bar = "abc",
         * 		Baz = new Baz
         * 		{
         * 			Qux = 123,
         * 			FooBar = "acme"
         * 		},
         * 		FuBar = "xyz"
         * 	}.Save();
         *
         * 	var updated = new Foo
         * 	{
         * 		Bar = "cba",
         * 		Baz = new Baz
         * 		{
         * 			Id = 1,
         * 			Qux = 456
         * 		}
         * 	};
         *
         * 	var merged = Foo.MergeEntities(updated, Foo.Find(1));
         * ```
         *
         * The merged result might look something like:
         * ```json
         * 	{
         * 		"Id": 1,
         * 		"Bar": "cba",
         * 		"Baz":
         * 		{
         * 			"Id": 1,
         * 			"Qux": 456,
         * 			"FooBar": "acme"
         * 		},
         * 		"FuBar": "xyz"
         * 	}
         * ```
         *
         * However consider the following example:
         * ```cs
         * 	var intial = new Foo
         * 	{
         * 		Bar = "abc",
         * 		Baz = new Baz
         * 		{
         * 			Qux = 123,
         * 			FooBar = "acme"
         * 		},
         * 		FuBar = "xyz"
         * 	}.Save();
         *
         * 	var updated = new Foo
         * 	{
         * 		Bar = "cba",
         * 		Baz = new Baz
         * 		{
         * 			Qux = 456
         * 		}
         * 	};
         *
         * 	var merged = Foo.MergeEntities(updated, Foo.Find(1));
         * ```
         *
         * The merged result now looks something like:
         * ```json
         * 	{
         * 		"Id": 1,
         * 		"Bar": "cba",
         * 		"Baz":
         * 		{
         * 			"Id": 0,
         * 			"Qux": 456,
         * 			"FooBar": null
         * 		},
         * 		"FuBar": "xyz"
         * 	}
         * ```
         *
         * Notice how the instance of Baz got replaced.
         * This is because we omitted the Id property on the Baz instance.
         *
         * Similar logic is used when dealing with lists of entities,
         * except that we _"Add"_ to the list instead of replace.
         *
         * > NOTE: Remember that at this point the merged result is only in
         * > memory, if you are happy with it you would then need to Save it.
         */
        public static TModel MergeEntities(TModel updated, TModel existing, List<object> Updated = null, List<object> Merged = null)
        {
            // If the existing entity is null we have nothing to merge.
            if (existing == null) return updated;

            // Setup our lists to keep track of what we have merged.
            if (Updated == null) Updated = new List<object>();
            if (Merged == null) Merged = new List<object>();

            // Check to see if we have already merged this updated entity.
            // We must enforce a reference check, we can not just use Contains.
            // Consider that an entity has 2 lists of the same entity type.
            // It is possible that there might be 2 seperate updated entity
            // instances, one for each list.
            if (Updated.Any(previous => ReferenceEquals(previous, updated)))
            {
                // Stop the recursion.
                return null;
            }
            else
            {
                Updated.Add(updated);
            }

            // If we have already come across this existing entity,
            // we need to merge the new updates into it.
            if (Merged.Contains(existing))
            {
                existing = (TModel)Merged[Merged.IndexOf(existing)];
            }
            else
            {
                Merged.Add(existing);
            }

            // We only need to merge the properties of the
            // updated entity that have actually changed.
            updated.ModifiedProps.ForEach(p =>
            {
                // Never copy the CreatedAt, ModifiedAt timestamps.
                if (p.Name == "CreatedAt" || p.Name == "ModifiedAt") return;

                // Copy the property value, if it is a simple primative.
                if (TypeMapper.IsClrType(p.PropertyType))
                {
                    p.SetValue(existing, p.GetValue(updated));
                }

                // Deal with singular entities, ie: OtoX relationships.
                else if (TypeMapper.IsEntity(p.PropertyType))
                {
                    var dModel = Dynamic(p.PropertyType);
                    var updatedEntity = p.GetValue(updated);
                    var updateEntityCasted = (IModel<Model>)updatedEntity;
                    var existingEntity = p.GetValue(existing);

                    if (updateEntityCasted.Id > 0)
                    {
                        // If the updated entity has come from the db and has
                        // not been modified. ie: is exactly the same as the
                        // existingEntity. Then we having nothing to do, we will
                        // skip to the next property.
                        if (updateEntityCasted.Hydrated && updateEntityCasted.ModifiedProps.Count == 0)
                        {
                            return;
                        }

                        // Merge the entity but only if it has an Id.
                        // Without a valid Id we have no way to be certian
                        // we are merging the same entities.
                        var mergedEntity = dModel.InvokeStatic
                        (
                            "MergeEntities",
                            updatedEntity,
                            existingEntity,
                            Updated,
                            Merged
                        );

                        if (mergedEntity != null)
                        {
                            p.SetValue(existing, mergedEntity);
                        }
                    }
                    else
                    {
                        // If the entity does not have an Id we take that to
                        // mean we are replacing the value in the database.
                        p.SetValue(existing, updatedEntity);
                    }
                }

                // Deal with lists of entities, ie: MtoX relationships.
                else if (TypeMapper.IsListOfEntities(p.PropertyType))
                {
                    var dModel = Dynamic(p.PropertyType.GenericTypeArguments[0]);
                    var updatedEntities = p.GetValue(updated) as IEnumerable<object>;
                    var existingEntities = p.GetValue(existing) as IEnumerable<object>;
                    var mergedEntities = new List<object>();

                    // This is basically the same as the singular case above,
                    // we are just adding the results a list instead.
                    foreach (var updatedEntity in updatedEntities)
                    {
                        var updateEntityCasted = (IModel<Model>)updatedEntity;
                        var updatedEntityId = updateEntityCasted.Id;

                        if (updatedEntityId > 0)
                        {
                            // If the updated entity has come from the db and
                            // has not been modified. ie: is exactly the same as
                            // the existingEntity. Then we having nothing to do,
                            // we will skip to the next property.
                            if (updateEntityCasted.Hydrated && updateEntityCasted.ModifiedProps.Count == 0)
                            {
                                return;
                            }

                            var mergedEntity = dModel.InvokeStatic
                            (
                                "MergeEntities",
                                updatedEntity,
                                existingEntities.SingleOrDefault
                                (
                                    e => ((IModel<Model>)e).Id == updatedEntityId
                                ),
                                Updated,
                                Merged
                            );

                            if (mergedEntity != null)
                            {
                                mergedEntities.Add(mergedEntity);
                            }
                        }
                        else
                        {
                            if (updatedEntity != null)
                            {
                                mergedEntities.Add((dynamic)updatedEntity);
                            }
                        }
                    }

                    // Now we need to re-add any entities that didn't get merged
                    foreach (var existingEntity in existingEntities)
                    {
                        var existingEntityId = ((IModel<Model>)existingEntity).Id;

                        if (existingEntityId > 0)
                        {
                            if (!mergedEntities.Any(me => ((IModel<Model>)me).Id == existingEntityId))
                            {
                                mergedEntities.Add((dynamic)existingEntity);
                            }
                        }
                        else
                        {
                            mergedEntities.Add((dynamic)existingEntity);
                        }
                    }

                    dynamic mergedEntitiesList = Activator.CreateInstance
                    (
                        typeof(List<>).MakeGenericType
                        (
                            p.PropertyType.GenericTypeArguments[0]
                        )
                    );

                    foreach (var me in mergedEntities.Distinct())
                    {
                        mergedEntitiesList.Add((dynamic)me);
                    }

                    p.SetValue(existing, mergedEntitiesList);
                }
            });

            return existing;
        }

        /**
         * Merges the entity based on some other value other than Id.
         *
         * ```cs
         * 	var foo = new Foo();
         *
         * 	foo.Bars.Add
         * 	(
         * 		Bar.MergeOrNew
         * 		(
         * 			e => e.Baz == "abc",
         * 			new Bar
         * 			{
         * 				Baz = "abc",
         * 				Qux = 123
         * 			}
         * 		)
         * 	);
         *
         * 	foo.Save();
         * ```
         *
         * > NOTE: You might also consider SingleOrNew or even FirstOrNew
         * > in such a situation.
         */
        public static TModel MergeOrNew(Expression<Func<TModel, bool>> find, TModel toMerge)
        {
            var existing = SingleOrDefault(find);

            if (existing == null)
            {
                return toMerge;
            }
            else
            {
                return MergeEntities(toMerge, existing);
            }
        }
    }

    /**
     * Covariant Generic Interface
     *
     * This allows us to cast, instead of using Reflection.
     * Which obviously is much, much faster.
     *
     * @see: http://stackoverflow.com/questions/16795750
     */
    public interface IModel<out TModel> where TModel : Model
    {
        event PropertyChangedEventHandler PropertyChanged;
        Dictionary<string, object> PropertyBag { get; }
        Dictionary<string, object> OriginalPropertyBag { get; }
        List<PropertyInfo> ModifiedProps { get; }
        Dictionary<string, object> DbRecord { get; }
        List<object> DiscoveredEntities { get; set; }
        bool Hydrated { get; }
        Dictionary<string, List<Dictionary<string, object>>> CachedQueries { get; set; }
        T Get<T>([CallerMemberName] string propName = "", bool loadFromDiscovered = true, bool loadFromDb = true);
        void Set<T>(T value, [CallerMemberName] string propName = "", bool triggerChangeEvent = true);
        string ToJson();
        string ToString();
        int Id { get; set; }
        DateTime CreatedAt { get; set; }
        DateTime ModifiedAt { get; set; }
        DateTime? DeletedAt { get; set; }
        void Delete(bool hardDelete = false);
        TModel Restore();
        TModel Save(List<object> SavedEntities = null);
    }
}
