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
    using Newtonsoft.Json.Linq;
    using System.ComponentModel;
    using Newtonsoft.Json.Schema;
    using System.Linq.Expressions;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.ComponentModel.DataAnnotations;
    using DynamicExpression = System.Linq.Dynamic.DynamicExpression;

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
         * Instance alias of the static Db Property.
         *
         * > NOTE: In dynamic cases where we cast to an IModel<Model> we are
         * > able to access the static property without using reflection.
         */
        [JsonIgnoreAttribute]
        public Context MyDb { get { return Db; } }

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
         * 	[SqlTableNameAttribute("CustomFoo")]
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
         * Instance property for the sql table name.
         *
         * > NOTE: In dynamic cases where we cast to an IModel<Model> we are
         * > able to access the static property without using reflection.
         */
        [JsonIgnoreAttribute]
        public string MySqlTableName
        {
            get
            {
                return SqlTableName;
            }
        }

        /**
         * Returns a list of properties that have public Setters.
         * These are the properties of the TModel that represent
         * a database table column.
         */
        public static List<PropertyInfo> MappedProps
        {
            get
            {
                if (_MappedProps == null)
                {
                    // Grab the public instance properties
                    _MappedProps = typeof(TModel).GetProperties
                    (
                        BindingFlags.Instance |
                        BindingFlags.Public
                    ).ToList();

                    // We only want properties with public setters
                    _MappedProps = _MappedProps.Where
                    (
                        prop => prop.GetSetMethod() != null
                    ).ToList();

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
         * Instance property for the mapped props.
         *
         * > NOTE: In dynamic cases where we cast to an IModel<Model> we are
         * > able to access the static property without using reflection.
         */
        [JsonIgnoreAttribute]
        public List<PropertyInfo> MyMappedProps
        {
            get
            {
                return MappedProps;
            }
        }

        /**
         * When inserting and updating we do not care for the Id property.
         * This is because it AUTO INCREMENTS and should never be written to.
         */
        protected static List<PropertyInfo> MappedPropsExceptId
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
        [JsonIgnoreAttribute]
        public Dictionary<string, object> PropertyBag { get; protected set; }

        /**
         * When a property is first set, we store a shallow clone of the value.
         * Used in conjuction with the ModifiedProps property to determin
         * which values need to be updated.
         */
        [JsonIgnoreAttribute]
        public Dictionary<string, object> OriginalPropertyBag { get; protected set; }

        /**
         * This will contain a list of properties that have indeed been
         * modified since being first hydrated from the database.
         */
        [JsonIgnoreAttribute]
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
         * Returns a JSON Schema Document for the Model.
         *
         * ```cs
         * 	var schema = Models.Foo.JsonSchema;
         * ```
         */
        public static JSchema JsonSchema
        {
            get
            {
                if (_JsonSchema == null)
                {
                    _JsonSchema = new JSchemaGenerator()
                    .Generate(typeof(TModel));
                }

                return _JsonSchema;
            }
        }

        private static JSchema _JsonSchema;

        /**
         * Instance property for the json schema.
         *
         * > NOTE: In dynamic cases where we cast to an IModel<Model> we are
         * > able to access the static property without using reflection.
         */
        [JsonIgnoreAttribute]
        public JSchema MyJsonSchema
        {
            get
            {
                return JsonSchema;
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
            this.HydrateRelationships(prop);

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
         * ```
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
         * ```
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

        public class JsonValidationException : Exception
        {
            public List<ValidationError> Errors { get; protected set; }

            public JsonValidationException(List<ValidationError> errors)
            : base("Json did not pass json schema validation, see Error List for more info...")
            {
                this.Errors = errors;
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
         * Returns a string representation of the current entity.
         */
        public override string ToString()
        {
            return this.ToJson();
        }

        /**
         * Simple Entity Property Getter, does not load relationships.
         *
         * > NOTE: Used internally by the Validator method.
         */
        private object Get(string propName)
        {
            object value = null;

            if (this.PropertyBag != null)
            {
                this.PropertyBag.TryGetValue(propName, out value);
            }

            return value;
        }

        /**
         * Entity Property Getter.
         *
         * All properties of an entity that map to another entity,
         * ie: A Relationship, need to implement this method as their Getter.
         * But we reccommend all properties regardless of type use this.
         *
         * ```
         * 	class Foo : Model<Foo>
         * 	{
         * 		public string Bar { get { return Get<string>(); } set... }
         * 	}
         * ```
         */
        public virtual T Get<T>([CallerMemberName] string propName = "", bool loadRelations = true, bool triggerChangeEvent = true)
        {
            // If the property bag hasn't been created yet then obviously
            // we won't find anything in it. Even if someone asks for a
            // relationship, the entity must have an Id, which means the
            // PropertyBag will be set.
            if (this.PropertyBag == null) return default(T);

            // Lets attempt to get the value from the PropertyBag Dict.
            object value = null;
            if (this.PropertyBag.TryGetValue(propName, out value))
            {
                return value == null ? default(T) : (T)value;
            }

            // Bail out if we have been told not load relationships or the
            // entity does not yet have an Id set. Obviously to lookup a
            // relationship we need to know our Id.
            if (!loadRelations || !this.PropertyBag.ContainsKey("Id"))
            {
                return default(T);
            }

            // Okay so we have got this far but have not managed to find
            // anything in the PropertyBag. Lets see if the property
            // maps to a discovered relationship.
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

            switch (relation.Type)
            {
                case RelationshipDiscoverer.Relation.RelationType.MtoM:
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
                    var records = Db.Qb
                    .SELECT("{0}.*", foreignTable)
                    .FROM(foreignTable)
                    .INNER_JOIN("{0} ON {0}.{1} = {2}.[Id]", pivotTable, col2, foreignTable)
                    .INNER_JOIN("{2} ON {0}.{1} = {2}.[Id]", pivotTable, col1, localTable)
                    .WHERE("{0}.[Id] = {1}", localTable, this.Id)
                    .Rows;

                    // Hydrate the records into entities.
                    var entities = Model.Dynamic(relation.ForeignType).Hydrate(records);

                    // Save the new list of entities into the object graph
                    this.Set(entities, propName, triggerChangeEvent);
                }
                break;

                case RelationshipDiscoverer.Relation.RelationType.MtoO:
                {
                    // Grab the entities
                    var entities = Model.Dynamic(relation.ForeignType).Linq
                    .Where(relation.ForeignKeyColumnName + " = {0}", this.Id)
                    .ToList();

                    // Save the new list of entities into the object graph
                    this.Set(entities, propName, triggerChangeEvent);
                }
                break;

                case RelationshipDiscoverer.Relation.RelationType.OtoM:
                {
                    // TODO: This value could actually be cached, when we
                    // first hydrate the entity, for now though we request
                    // it from the database.
                    var ForeignId = Db.Qb
                    .SELECT(relation.ForeignKeyColumnName)
                    .FROM(relation.LocalTableName)
                    .WHERE("Id = {0}", this.Id)
                    .Scalar;

                    if (ForeignId != null)
                    {
                        // Grab the actual entity
                        var entity = Model.Dynamic(relation.ForeignType).Find
                        (
                            (int)ForeignId, withTrashed: true
                        );

                        if (entity != null)
                        {
                            // Save the new entity into the object graph
                            this.Set(entity, propName, triggerChangeEvent);
                        }
                    }
                }
                break;

                case RelationshipDiscoverer.Relation.RelationType.OtoO:
                {
                    if (relation.ForeignKeyTableName == relation.LocalTableName)
                    {
                        // TODO: As above this could be cached.
                        var ForeignId = Db.Qb
                        .SELECT(relation.ForeignKeyColumnName)
                        .FROM(relation.LocalTableName)
                        .WHERE("Id = {0}", this.Id)
                        .Scalar;

                        if (ForeignId != null)
                        {
                            var entity = Model.Dynamic(relation.ForeignType).Find
                            (
                                (int)ForeignId, withTrashed: true
                            );

                            if (entity != null)
                            {
                                // Save the new entity into the object graph
                                this.Set(entity, propName, triggerChangeEvent);
                            }
                        }
                    }
                    else
                    {
                        // Grab the entity
                        var entity = Model.Dynamic(relation.ForeignType).Linq
                        .SingleOrDefault(relation.ForeignKeyColumnName + " = {0}", this.Id);

                        if (entity != null)
                        {
                            // Save the new entity into the object graph
                            this.Set(entity, propName, triggerChangeEvent);
                        }
                    }
                }
                break;
            }

            return this.Get<T>(propName, loadRelations: false);
        }

        /**
         * Entity Property Setter.
         *
         * All properties of an entity that map to another entity,
         * ie: A Relationship, need to implement this method as their Setter.
         * But we reccommend all properties regardless of type use this.
         *
         * ```
         * 	class Foo : Model<Foo>
         * 	{
         * 		public string Bar { get... set { Set(value); } }
         * 	}
         * ```
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

            // If the property does not already have
            // a value, set it's original value.
            if (this.Get<T>(propName, loadRelations: false) == null)
            {
                if (this.OriginalPropertyBag == null)
                {
                    this.OriginalPropertyBag = new Dictionary<string, object>();
                }

                if (TypeMapper.IsListOfEntities(value))
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
            // new entities are added so that we may hydrate the foreign relationships.
            dynamic propertyBagValue;
            if (value != null && TypeMapper.IsList(value) && value.GetType().GetGenericTypeDefinition() == typeof(List<>))
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
                        if (!triggerChangeEvent) return;

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
         * created else where withun checked data.
         */
        [JsonIgnore]
        public bool Hydrated { get; protected set; }

        /**
         * Given an SqlResult we assign the data to a new TModel and return it.
         *
         * ```
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
        public static TModel Hydrate(SqlResult record)
        {
            // Make sure we have some data to actually hydrate the entity with.
            if (record.Count == 0)
            {
                throw new NullReferenceException("Empty SqlResult!");
            }

            // Create the new entity
            var entity = new TModel();

            // Hydrate all primative types.
            foreach(var col in record)
            {
                if (col.Value == null) continue;

                if (typeof(TModel).GetProperty(col.Key) != null)
                {
                    entity.Set(col.Value, col.Key, triggerChangeEvent: false);
                }
            }

            entity.Hydrated = true;

            // NOTE: Relationships are loaded as they are requested.

            return entity;
        }

        /**
         * Given a list of SqlResults we assign the data
         * to a new list of TModels and return it.
         *
         * ```
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
        public static List<TModel> Hydrate(List<SqlResult> records)
        {
            var entities = new List<TModel>();

            records.ForEach(record => entities.Add(Hydrate(record)));

            return entities;
        }

        /**
         * Updates the foreign side of the relationship when a property is set.
         * Most relationships will have 2 navigational properties on each side
         * of the relationship, consider the following example:
         *
         * ```
         * 	class Foo : Model<Foo>
         * 	{
         * 		public Bar ABar
         * 		{
         * 			get { return Get<Bar>(); }
         * 			set { Set(value); }
         * 		}
         * 	}
         *
         *  class Bar : Model<Bar>
         * 	{
         * 		public Foo AFoo
         * 		{
         * 			get { return Get<Foo>(); }
         * 			set { Set(value); }
         * 		}
         * 	}
         *
         * 	var foo = new Foo
         * 	{
         * 		Bar = new Bar
         * 		{
         * 			Baz = "abc"
         * 		}
         * 	};
         * ```
         *
         * Foo knows about Bar but Bar knows nothing about Bar.
         * This method will ensure the AFoo property on the Bar entity
         * is filled with the Foo entity, as you would expect it to be.
         */
        protected void HydrateRelationships(PropertyInfo prop)
        {
            // Cast the sender to a TModel Entity
            var entity = (TModel)this;

            // Grab the local property that was just updated
            var localProperty = prop;

            // Ignore all primative types
            if (TypeMapper.IsClrType(localProperty.PropertyType)) return;

            // Grab the relation discriptor for the current property.
            var relation = Db.Relationships.Discovered.Single
            (
                r => r.LocalProperty == localProperty
            );

            // Bail out if the foreign side of the relationship is not defined.
            if (relation.ForeignProperty == null) return;

            // Grab the value of the local property
            var localValue = localProperty.GetValue(entity);

            // Take action based on the relationship type.
            switch (relation.Type)
            {
                case RelationshipDiscoverer.Relation.RelationType.MtoM:
                {
                    // Loop through the list of foreign entities.
                    (localValue as IEnumerable<object>)
                    .Cast<IModel<Model>>()
                    .ToList().ForEach(foreignEntity =>
                    {
                        // Grab the value of the property
                        // that points back to ourselves.
                        var foreignValue = foreignEntity.Get<IList<TModel>>
                        (
                            relation.ForeignProperty.Name,
                            loadRelations: true,
                            triggerChangeEvent: false
                        );

                        // If the value is null, ie: Nothing in the database.
                        // Create a new list, add ourselves to it.
                        if (foreignValue == null)
                        {
                            foreignValue = new List<TModel> { entity };

                            foreignEntity.Set
                            (
                                foreignValue,
                                relation.ForeignProperty.Name,
                                triggerChangeEvent: false
                            );
                        }
                        else
                        {
                            // Okay so the list already exists, ensure the list
                            // does not contain ourself, can't have duplicates.
                            // This is checking for the same reference.
                            if (!foreignValue.Contains(entity))
                            {
                                // If we don't have an Id yet, it means we
                                // havn't been added to the database yet.
                                // Which means we can safely add ourselves.
                                if (entity.Id == 0)
                                {
                                    foreignValue.Add(entity);
                                }
                                else
                                {
                                    // We exist in the database, so it's very
                                    // likely that we will exist in this list,
                                    // lets see if we do.
                                    var existing = foreignValue.SingleOrDefault
                                    (
                                        e => e.Id == entity.Id
                                    );

                                    if (existing == null)
                                    {
                                        // Nope we didn't, no matter we
                                        // will add ourselves.
                                        foreignValue.Add(entity);
                                    }
                                    else
                                    {
                                        // Swap out the fresh clone of
                                        // ourself with ourself.
                                        var idx = foreignValue.IndexOf(existing);
                                        foreignValue.RemoveAt(idx);
                                        foreignValue.Insert(idx, entity);
                                    }
                                }
                            }
                        }

                        // Okay so we have loaded the foreign side of relation.
                        // But we know that the otherside of the relation we
                        // just setup, is well this relation, so to stop this
                        // madness from continuing we will set it now.
                        foreach (var e in foreignValue)
                        {
                            e.Set
                            (
                                localValue,
                                relation.LocalProperty.Name,
                                triggerChangeEvent: false
                            );
                        }
                    });
                }
                break;

                case RelationshipDiscoverer.Relation.RelationType.MtoO:
                {
                    // Loop through the foreign entities and
                    // set ourselves on the foreign property.
                    (localValue as IEnumerable<object>)
                    .Cast<IModel<Model>>()
                    .ToList().ForEach(foreignEntity =>
                    {
                        foreignEntity.Set
                        (
                            entity,
                            relation.ForeignProperty.Name,
                            triggerChangeEvent: false
                        );
                    });
                }
                break;

                case RelationshipDiscoverer.Relation.RelationType.OtoM:
                {
                    // Basically the same as the MtoM but we just do it once.

                    var foreignEntity = (IModel<Model>)localValue;

                    if (foreignEntity == null) return;

                    var foreignValue = foreignEntity.Get<IList<TModel>>
                    (
                        relation.ForeignProperty.Name,
                        loadRelations: true,
                        triggerChangeEvent: false
                    );

                    if (foreignValue == null)
                    {
                        foreignEntity.Set
                        (
                            new List<TModel> { entity },
                            relation.ForeignProperty.Name,
                            triggerChangeEvent: false
                        );
                    }
                    else
                    {
                        if (!foreignValue.Contains(entity))
                        {
                            if (entity.Id == 0)
                            {
                                foreignValue.Add(entity);
                            }
                            else
                            {
                                var existing = foreignValue.SingleOrDefault
                                (
                                    e => e.Id == entity.Id
                                );

                                if (existing == null)
                                {
                                    foreignValue.Add(entity);
                                }
                                else
                                {
                                    var idx = foreignValue.IndexOf(existing);
                                    foreignValue.RemoveAt(idx);
                                    foreignValue.Insert(idx, entity);
                                }
                            }
                        }
                    }
                }
                break;

                case RelationshipDiscoverer.Relation.RelationType.OtoO:
                {
                    // Super easy, just set ourselves to the foreign property
                    ((IModel<Model>)localValue).Set
                    (
                        entity,
                        relation.ForeignProperty.Name,
                        triggerChangeEvent: false
                    );
                }
                break;
            }
        }

        /**
         * This just keeps a list of all the mapped properties that have
         * changed since hydration.
         */
        protected void UpdateModified(PropertyInfo changedProp)
        {
            var entity = (TModel)this;

            if (!entity.ModifiedProps.Contains(changedProp))
            {
                entity.ModifiedProps.Add(changedProp);
            }
        }

        /**
         * Filters out Soft Deleted Entities
         *
         * All "READ" methods will use this to filter out any soft deleted
         * entities. Each of the methods will have a "withTrashed" parameter.
         */
        protected static Linq<TModel> FilterTrashed(bool withTrashed = false)
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
         * ```
         * 	var entity = Models.Foo.Find(123);
         * ```
         *
         * > NOTE: Returns null if nothing, throws exception if more than one.
         */
        public static TModel Find(int key, bool withTrashed = false)
        {
            // Entities with an Id of 0 will NEVER exist in the database.
            if (key == 0) return null;

            return FilterTrashed(withTrashed).SingleOrDefault(m => m.Id == key);
        }

        /**
         * Find a similar entity.
         *
         * ```
         * 	var entity = Models.Foo.Find(new Foo { Bar = "abc" });
         * ```
         *
         * > NOTE: Returns null if nothing, throws exception if more than one.
         */
        public static TModel Find(TModel entity, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).SingleOrDefault
            (
                BuildEqualityExpression(entity)
            );
        }

        /**
         * Checks to see if an entity exists by it's primary key.
         *
         * ```
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
                FilterTrashed(withTrashed).Single(m => m.Id == key);
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
         * ```
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
                    BuildEqualityExpression(entity)
                );
            }
            catch
            {
                return false;
            }

            return true;
        }

        /**
         * Do all entities in the set pass the predicate?
         *
         * ```
         * 	if (Models.Foo.All(m => m.Bar == "abc"))
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
         * Are there any entities in the set at all?
         *
         * ```
         * 	if (Models.Foo.Where(m => m.Bar == "abc").Any())
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
         * Do any entities in the set pass the predicate?
         *
         * ```
         * 	if (Models.Foo.Any(m => m.Bar == "abc"))
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
         * How many entities are there in the set?
         *
         * ```
         * 	var numberOfEntities = Models.Foo.Count();
         * ```
         */
        public static int Count(bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).Count();
        }

        /**
         * How many entities are there in the set that match the predicate?
         *
         * ```
         * 	var numberOfEntities = Models.Foo.Count(m => m.Bar == "abc");
         * ```
         */
        public static int Count(Expression<Func<TModel, bool>> predicate, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).Count(predicate);
        }

        /**
         * Returns the first entity of the set.
         *
         * ```
         * 	var entity = Models.Foo.First();
         * ```
         */
        public static TModel First(bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).First();
        }

        /**
         * Returns the first entity of the set that matches the predicate.
         *
         * ```
         * 	var entity = Models.Foo.First(m => m.Bar == "abc");
         * ```
         */
        public static TModel First(Expression<Func<TModel, bool>> predicate, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).First(predicate);
        }

        /**
         * Returns the first element of the set,
         * or a default value if the set contains no elements.
         *
         * ```
         * 	var entity = Models.Foo.FirstOrDefault();
         * ```
         */
        public static TModel FirstOrDefault(bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).FirstOrDefault();
        }

        /**
         * Returns the first element of the set that matches the predicate,
         * or a default value if the set contains no elements.
         *
         * ```
         * 	var entity = Models.Foo.FirstOrDefault(m => m.Bar == "abc");
         * ```
         */
        public static TModel FirstOrDefault(Expression<Func<TModel, bool>> predicate, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).FirstOrDefault(predicate);
        }

        /**
         * Returns the only entity of the set. If the set contains no entities
         * or more than one entity then an exception will be thrown.
         *
         * ```
         * 	var entity = Models.Foo.Single();
         * ```
         */
        public static TModel Single(bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).Single();
        }

        /**
         * Returns the only entity of the set that matches the predicate.
         * If the set contains no entities or more than one entity then an
         * exception will be thrown.
         *
         * ```
         * 	var entity = Models.Foo.Single(m => m.Bar == "abc");
         * ```
         */
        public static TModel Single(Expression<Func<TModel, bool>> predicate, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).Single(predicate);
        }

        /**
         * Returns the only element of the set, or a default value if the set
         * is empty; this method throws an exception if there is more than one
         * element in the set.
         *
         * ```
         * 	var entity = Models.Foo.SingleOrDefault();
         * ```
         */
        public static TModel SingleOrDefault(bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).SingleOrDefault();
        }

        /**
         * Returns the only element of the set that matches the predicate, or a
         * default value if the set is empty; this method throws an exception
         * if there is more than one element in the set.
         *
         * ```
         * 	var entity = Models.Foo.SingleOrDefault(m => m.Bar == "abc");
         * ```
         */
        public static TModel SingleOrDefault(Expression<Func<TModel, bool>> predicate, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).SingleOrDefault(predicate);
        }

        /**
         * Filters the set based on the predicate.
         * This does not return results but a new filtered Linq<TModel>.
         *
         * ```
         * 	var filteredEntities = Models.Foo.Where(m => m.Bar == "abc");
         * ```
         */
        public static Linq<TModel> Where(Expression<Func<TModel, bool>> predicate, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).Where(predicate);
        }

        /**
         * Filters the set based on the predicate, using sql LIKE clauses.
         * This does not return results but a new filtered Linq<TModel>.
         *
         * ```
         * 	var filteredEntities = Models.Foo.Like(m => m.Bar == "%abc%");
         * ```
         */
        public static Linq<TModel> Like(Expression<Func<TModel, bool>> predicate, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).Like(predicate);
        }

        /**
         * Orders the set based on the returned property.
         *
         * ```
         * 	var orderedEntities = Models.Foo
         * 	.OrderBy(m => m.Bar, OrderDirection.DESC)
         * 	.OrderBy(m => m.Baz, OrderDirection.ASC);
         * ```
         *
         * > NOTE: ASC is the default direction, if not supplied.
         */
        public static Linq<TModel> OrderBy(Expression<Func<TModel, object>> predicate, OrderDirection direction = OrderDirection.ASC, bool withTrashed = false)
        {
            return FilterTrashed(withTrashed).OrderBy(predicate, direction);
        }

        /**
         * Bypasses a specified number of entities in the set
         * and then returns the remaining entities.
         *
         * ```
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
         * ```
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
         * ```
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
         * ```
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
         * ```
         * 	var entity = Model.Foo.Create(new Foo { Bar = "abc" });
         * ```
         *
         * Which is exactly the same as:
         *
         * ```
         * 	var entity = new Foo { Bar = "abc" }.Save();
         * ```
         */
        public static TModel Create(TModel entity)
        {
            return entity.Save();
        }

        /**
         * Given an instance we will first check to see if a similar entity
         * exists in the database. If it does we will return that instance.
         * If not we will create a new instance for you.
         *
         * ```
         * 	var entity = Models.Foo.SingleOrCreate(new Foo { Bar = "abc" });
         * ```
         */
        public static TModel SingleOrCreate(TModel model)
        {
            try
            {
                return Single(BuildEqualityExpression(model));
            }
            catch
            {
                return Create(model);
            }
        }

        /**
         * Similar to SingleOrCreate except we do not add the new instance
         * to the database. We simply return the model you passed in.
         * It is then on you to call Save() on that model if you wish to
         * add it to the db.
         *
         * ```
         * 	var user = Model.User.SingleOrNew(new User { Name = "Fred"; });
         * 	user.BirthYear = 2001;
         * 	user.Save();
         * ```
         */
        public static TModel SingleOrNew(TModel model)
        {
            try
            {
                return Single(BuildEqualityExpression(model));
            }
            catch
            {
                return model;
            }
        }

        /**
         * Given an instance we will first check to see if there are any similar
         * entities that exist in the database. If there are 1 or more entities
         * that are similar to the provided we will return the first in the set.
         * If there are no similar entities we will save the provided entity
         * and return it.
         *
         * ```
         * 	var entity = Models.Foo.FirstOrCreate(new Foo { Bar = "abc" });
         * ```
         */
        public static TModel FirstOrCreate(TModel model)
        {
            try
            {
                return First(BuildEqualityExpression(model));
            }
            catch
            {
                return Create(model);
            }
        }

        /**
         * Similar to FirstOrCreate except we do not add the new instance
         * to the database. We simply return the model you passed in.
         * It is then on you to call Save() on that model if you wish to
         * add it to the db.
         *
         * ```
         * 	var user = Model.User.FirstOrNew(new User { Name = "Fred"; });
         * 	user.BirthYear = 2001;
         * 	user.Save();
         * ```
         */
        public static TModel FirstOrNew(TModel model)
        {
            try
            {
                return First(BuildEqualityExpression(model));
            }
            catch
            {
                return model;
            }
        }

        /**
         * Updates enMasse without first loading the entites into memory.
         *
         * ```
         * 	Models.Foo.Update(m => m.Bar == "abc" && m.Baz == 123);
         * 	Models.Foo.Where(m => m.Id == 78).Update(m => m.Qux == "qwerty");
         * ```
         *
         * > NOTE: This is NOT a "WHERE" predicate. The expression you provide
         * > this Update method follows the same structure as a predicate but
         * > we parse it slightly diffrently. Consider each "&&" or "||" as a
         * > comma. And each "==" simply as a "=" operator.
         */
        public static void Update(Expression<Func<TModel, object>> assignments, bool withTrashed = false)
        {
            FilterTrashed(withTrashed).Update(assignments);
        }

        /**
         * Check to see if model exists, using properties.
         * If so lets update it with some new values.
         * If not lets create a brand new record.
         *
         * @example
         * 	var user = Models.User.UpdateOrCreate
         * 	(
         * 		new User { Name = "Brad" },
         * 		new User { Email = "brad@kdis.com.au" }
         * 	);
         *
         * @param TModel find The instance to search for in the db.
         *
         * @param TModel update The instance with the updated properties.
         *
         * @returns TModel The 2 models provided will be merged and returned.
         */
        public static TModel UpdateOrCreate(TModel find, TModel update)
        {
            var record = FirstOrNew(find);

            record = MergeModels(update, record);

            record.Save();

            return record;
        }

        /**
         * Check to see if model exists, using expression.
         * If so lets update it with some new values.
         * If not lets create a brand new record.
         *
         * @example
         * 	var user = Models.User.UpdateOrCreate
         * 	(
         * 		m => m.Id == 1,
         * 		new User { Email = "brad@kdis.com.au" }
         * 	);
         *
         * @param Expression find The Linq expression to search the db.
         *
         * @param TModel update This instance will be merged with model from
         *                      the db or it will be created.
         *
         * @returns TModel
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
                return MergeModels(update, existing).Save();
            }
        }

        /**
         * This overload is super useful for seeding or other mass creation.
         *
         * ```
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
                expressionString.Append("m.");
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

                var expression = BuildExpression(expressionString.ToString());

                UpdateOrCreate(expression, entity);
            });
        }

        /**
         * Deletes enMasse without first loading the entites into memory.
         *
         * ```
         * 	// soft deletes all Foo's in the table!
         * 	Models.Foo.Destroy();
         *
         * 	// soft deletes all Foo's that have Bar set to Baz
         * 	Models.Foo.Where(m => m.Bar == "Baz").Destroy();
         *
         * // hard deletes a Foo with the Id of 56
         * Models.Foo.Where(m => m.Id == 56).Destroy(hardDelete: true);
         * ```
         */
        public static void Destroy(bool hardDelete = false)
        {
            Linq.Destroy(hardDelete);
        }

        /**
         * Destroy the entity for the given primary key id.
         *
         * ```
         * 	Models.Foo.Destroy(12);
         * ```
         *
         * Which is the same as:
         *
         * ```
         * 	Models.Foo.Find(12).Delete();
         * ```
         *
         * Except that Destroy only performs a single SQL Query.
         * Use Delete when you already have an entity loaded.
         * Otherwise use Destroy if you know the Id in advance.
         */
        public static void Destroy(int key, bool hardDelete = false)
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
         * Destroy all entities for the given primary key ids.
         *
         * ```
         * 	Models.User.Destroy(43,57,102);
         * ```
         */
        public static void Destroy(bool hardDelete = false, params int[] keys)
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
         * ```
         * 	Model.Foo.Find(1).Delete();
         * ```
         *
         * > NOTE: By default we only "Soft" delete.
         */
        public void Delete(bool hardDelete = false)
        {
            if (this.Id == 0)
            {
                throw new Exception
                (
                    "Can't remove a model that does not exist!"
                );
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
         * ```
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

            MappedPropsExceptId.ForEach(prop =>
            {
                // Required Check
                var required = prop.GetCustomAttribute<RequiredAttribute>();
                if (required != null)
                {
                    if (this.Get(prop.Name) == null)
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
                    var value = this.Get(prop.Name);
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
                    var value = this.Get(prop.Name);
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
                    var value = this.Get(prop.Name);
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
                    var value = this.Get(prop.Name);
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
                    var value = this.Get(prop.Name);
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
                    var value = this.Get(prop.Name);
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
                Console.WriteLine(JArray.FromObject(errors));
                throw new ValidationException(errors);
            }

            return true;
        }

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
         * Saves an entity to the database.
         *
         * This method will look at the "Id" property, if 0 then the instance
         * must be a brand new record. And we perform an "INSERT" operation.
         * If the "Id" is greater than 0 then we perform an "UPDATE" operation.
         *
         * ```
         * 	var brad = new Models.User();
         * 	brad.FirstName = "Bradley";
         * 	brad.LastName = "Jones";
         * 	brad.Save();
         * ```
         *
         * > NOTE: We are recursive and will save all related entities.
         */
        public TModel Save(List<PropertyInfo> DealtWithRelationships = null)
        {
            // Grab our class properties, that represent columns in the table.
            var props = MappedPropsExceptId;

            // Create the dealt with list, if it hasn't been created yet.
            // Relationships have 2 sides and once we have dealt with one side
            // of the relationship we do not need to do anything when we come
            // across the foreign side of the relationship otherwise we will
            // go a little loopy :)
            if (DealtWithRelationships == null)
            {
                DealtWithRelationships = new List<PropertyInfo>();
            }

            bool INSERTING;
            if (this.Id == 0)
            {
                // We are INSERTING
                // So Update both the created and modified times
                this.CreatedAt = DateTime.UtcNow;
                this.ModifiedAt = DateTime.UtcNow;
                INSERTING = true;
                if (!this.FireBeforeInsert()) return null;
            }
            else
            {
                // We are UPDATING
                // So Update the modified time only.
                this.ModifiedAt = DateTime.UtcNow;
                INSERTING = false;
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
                    case RelationshipDiscoverer.Relation.RelationType.MtoM:

                        // the foreign key exists in a pivot table.
                        return false;

                    case RelationshipDiscoverer.Relation.RelationType.MtoO:

                        // the foreign key exists in the foreign table.
                        return false;

                    case RelationshipDiscoverer.Relation.RelationType.OtoM:

                        // the foreign key exists in this table.
                        return true;

                    case RelationshipDiscoverer.Relation.RelationType.OtoO:

                        // the foreign key may exist in this table
                        // or in the foreign table, we need to find out.
                        return relation.ForeignKeyTableName == SqlTableName;
                }

                // If we get to here something odd happend
                throw new Exception("Invalid Property!");

            }).ToList();

            // Only insert or update if we have something to insert or update.
            if (insertableProps.Count > 0)
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
                        var entity = (IModel<Model>)value;

                        // Grab the relation discriptor for the current property.
                        var relation = Db.Relationships.Discovered.Single
                        (
                            r => r.LocalProperty == p
                        );

                        // Have we already dealt with the other side of this relationship?
                        if (!DealtWithRelationships.Any(dp => dp == relation.ForeignProperty))
                        {
                            // We have not dealt with this relationship yet.
                            // So lets make it known that we have now.
                            DealtWithRelationships.Add(p);

                            // Now recursively save the entity
                            entity.Save(DealtWithRelationships);
                        }

                        return entity.Id;
                    }
                }).ToArray();

                if (this.Id == 0)
                {
                    // Execute the INSERT Query
                    Db.Qb.INSERT_INTO(SqlTableName)
                    .COLS(cols).VALUES(values).Execute();

                    // Grab the inserted id and update our model
                    // NOTE: SCOPE_IDENTITY does not work because we need to make
                    // the query with the same connection instance or possibly even
                    // with in the same SqlCommand.
                    this.Id = Convert.ToInt32
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

                    this.FireAfterUpdate();
                }
            }

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

                // Skip if we have we already dealt with this relationship.
                if (DealtWithRelationships.Any
                (
                    dp => dp == relation.LocalProperty ||
                    dp == relation.ForeignProperty
                )) return;

                // Grab the value of the property
                var value = p.GetValue(this);

                if (value == null) return;

                // We have not dealt with this relationship yet.
                // So lets make it known that we have now.
                DealtWithRelationships.Add(p);

                // Take action based on the relationship type.
                switch (relation.Type)
                {
                    case RelationshipDiscoverer.Relation.RelationType.MtoM:
                    {

                        var currentEntities = (value as IEnumerable<object>)
                        .Cast<IModel<Model>>().ToList();

                        currentEntities.ForEach(e =>
                        {
                            bool FOREIGN_INSERTING = e.Id > 0 ? false : true;

                            // Save the foreign side of the relationship
                            e.Save(DealtWithRelationships);

                            // Only insert the new pivot table entry if one of
                            // the entities was inserted and not updated.
                            if (INSERTING || FOREIGN_INSERTING)
                            {
                                Db.Qb.INSERT_INTO(relation.PivotTableName)
                                .COLS
                                (
                                    relation.PivotTableFirstColumnName,
                                    relation.PivotTableSecondColumnName
                                )
                                .VALUES(this.Id, e.Id)
                                .Execute();
                            }
                        });

                        var originalEntities = (List<IModel<Model>>)
                        this.OriginalPropertyBag[relation.LocalProperty.Name];

                        // Remove any relationships from the
                        // pivot table that got removed.
                        originalEntities.ForEach(originalEntity =>
                        {
                            if (!currentEntities.Any(currentEntity => currentEntity.Id == originalEntity.Id))
                            {
                                Db.Qb.DELETE_FROM(relation.PivotTableName)
                                .WHERE(relation.PivotTableSecondColumnName, originalEntity.Id)
                                .Execute();
                            }
                        });
                    }
                    break;

                    case RelationshipDiscoverer.Relation.RelationType.MtoO:
                    {
                        var currentEntities = (value as IEnumerable<object>)
                        .Cast<IModel<Model>>().ToList();

                        currentEntities.ForEach(e =>
                        {
                            e.Save(DealtWithRelationships);
                        });

                        var originalEntities = (List<IModel<Model>>)
                        this.OriginalPropertyBag[relation.LocalProperty.Name];

                        originalEntities.ForEach(originalEntity =>
                        {
                            if (!currentEntities.Any(currentEntity => currentEntity.Id == originalEntity.Id))
                            {
                                Db.Qb.UPDATE(relation.ForeignKeyTableName)
                                .SET(relation.ForeignKeyColumnName, null)
                                .WHERE("Id", originalEntity.Id).Execute();
                            }
                        });
                    }
                    break;

                    case RelationshipDiscoverer.Relation.RelationType.OtoO:

                        ((IModel<Model>)value)
                        .Save(DealtWithRelationships);

                    break;

                    case RelationshipDiscoverer.Relation.RelationType.OtoM:

                        throw new Exception("OtoM - don't think we should ever get here....");
                }
            });

            this.FireAfterSave();

            return (TModel)this;
        }

        /**
         * A private helper method to merge 2 models together.
         * We use this in the UpdateOrCreate methods.
         */
        public static TModel MergeModels(TModel updated, TModel existing, List<PropertyInfo> DealtWithRelations = null)
        {
            // Setup up the dealt with relationships list.
            if (DealtWithRelations == null)
            {
                DealtWithRelations = new List<PropertyInfo>();
            }

            MappedPropsExceptId.ForEach(p =>
            {
                // If the updated entity has a null value, we will skip to the
                // next property and leave the existing property value as is.
                if (p.GetValue(updated) == null) return;

                // Do not copy the CreatedAt, ModifiedAt timestamps.
                if (p.Name == "CreatedAt" || p.Name == "ModifiedAt") return;

                // If the property type is a primative,
                // we can easily just copy it over.
                if (TypeMapper.IsClrType(p.PropertyType))
                {
                    p.SetValue(existing, p.GetValue(updated)); return;
                }

                // If the property type is a relationship, we need to recurse.
                RelationshipDiscoverer.Relation relation;

                try
                {
                    relation = Db.Relationships.Discovered
                    .Where(r => !DealtWithRelations.Contains(r.ForeignProperty))
                    .Single(r => r.LocalProperty == p);
                }
                catch
                {
                    // Skip to the next property
                    return;
                }

                DealtWithRelations.Add(relation.LocalProperty);

                switch (relation.Type)
                {
                    case RelationshipDiscoverer.Relation.RelationType.MtoM:
                    case RelationshipDiscoverer.Relation.RelationType.MtoO:
                    {
                        dynamic mergedEntities = Activator.CreateInstance
                        (
                            typeof(List<>).MakeGenericType
                            (
                                relation.ForeignType
                            )
                        );

                        var existingEntities = (dynamic)p.GetValue(existing);
                        var updatedEntities = (dynamic)p.GetValue(updated);

                        var x = 0;
                        foreach (var existingEntity in existingEntities)
                        {
                            var updatedEntity = updatedEntities[x];

                            var mergedEntity = Model.Dynamic(updatedEntity).InvokeStatic
                            (
                                "MergeModels",
                                updatedEntity,
                                existingEntity,
                                DealtWithRelations
                            );

                            mergedEntities.Add(mergedEntity);

                            x++;
                        }

                        p.SetValue(existing, mergedEntities);
                    }
                    break;

                    case RelationshipDiscoverer.Relation.RelationType.OtoM:
                    case RelationshipDiscoverer.Relation.RelationType.OtoO:
                    {
                        var existingEntity = p.GetValue(existing);
                        var updatedEntity = p.GetValue(updated);

                        var mergedEntity = Model.Dynamic(p.PropertyType).InvokeStatic
                        (
                            "MergeModels",
                            updatedEntity,
                            existingEntity,
                            DealtWithRelations
                        );

                        p.SetValue(existing, mergedEntity);
                    }
                    break;
                }
            });

            return existing;
        }

        /**
         * Private helper method to build an expression from a string.
         * This is useful insoide this generic class and in other reflection
         * situations because we don't know the type of the model.
         */
        private static Expression<Func<TModel, bool>> BuildExpression(string expression)
        {
            return (Expression<Func<TModel, bool>>) DynamicExpression.ParseLambda
            (
                new[] { Expression.Parameter(typeof(TModel), "m") },
                typeof(bool),
                expression
            );
        }

        /**
         * Private helper method to dynamically build a Linq Expression
         * that we use to compare 2 entities for equality.
         */
        private static Expression<Func<TModel, bool>> BuildEqualityExpression(TModel model)
        {
            var expressionString = new StringBuilder();

            // Grab our properties
            var props = MappedPropsExceptId;

            // Remove all properties that have null values.
            props.RemoveAll(prop => prop.GetValue(model) == null);

            // Loop through each property
            props.ForEach(prop =>
            {
                // Build our lamda expression
                expressionString.Append("m.");
                expressionString.Append(prop.Name);
                expressionString.Append(" == model.");
                expressionString.Append(prop.Name);
                expressionString.Append(" && ");
            });

            // If nothing was added to our expression bail out.
            if (expressionString.Length == 0) return null;

            // Remove the last &&
            expressionString.Remove(expressionString.Length - 4, 4);

            // Build the expression
            return (Expression<Func<TModel, bool>>)DynamicExpression.ParseLambda
            (
                new[] { Expression.Parameter(typeof(TModel), "m") },
                typeof(bool),
                expressionString.ToString(),
                new Dictionary<string, object>{ {"model", model} }
            );
        }
    }
}
