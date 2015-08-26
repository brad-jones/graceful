namespace Graceful
{
    using System;
    using System.Linq;
    using Graceful.Query;
    using System.Reflection;
    using Newtonsoft.Json.Schema;
    using System.Collections.Generic;

    public class DModel
    {
        public Type ModelType { get; protected set; }

        public IModel<Model> Instance { get; protected set; }

        public DModel(Type modelType)
        {
            this.ModelType = modelType;
        }

        public DModel(object entity = null)
        {
            if (entity != null)
            {
                this.ModelType = entity.GetType();
                this.Instance = (IModel<Model>)entity;
            }
        }

        public dynamic InvokeStatic(string methodName, params object[] args)
        {
            var types = new Type[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                types[i] = args[i].GetType();
            }

            return this.ModelType.GetMethod
            (
                methodName,
                BindingFlags.FlattenHierarchy |
                BindingFlags.Public |
                BindingFlags.Static,
                null,
                types,
                null
            ).Invoke(null, args);
        }

        public dynamic GetStatic(string propName)
        {
            return this.ModelType.GetProperty
            (
                propName,
                BindingFlags.FlattenHierarchy |
                BindingFlags.Public |
                BindingFlags.Static
            ).GetValue(null);
        }

        public Context Db
        {
            get
            {
                if (this.Instance == null)
                {
                    return this.GetStatic("Db");
                }
                else
                {
                    return this.Instance.MyDb;
                }
            }
        }

        public string SqlTableName
        {
            get
            {
                if (this.Instance == null)
                {
                    return this.GetStatic("SqlTableName");
                }
                else
                {
                    return this.Instance.MySqlTableName;
                }
            }
        }

        public List<PropertyInfo> MappedProps
        {
            get
            {
                if (this.Instance == null)
                {
                    return this.GetStatic("MappedProps");
                }
                else
                {
                    return this.Instance.MyMappedProps;
                }
            }
        }

        public dynamic Linq
        {
            get
            {
                return this.GetStatic("Linq");
            }
        }

        public JSchema JsonSchema
        {
            get
            {
                if (this.Instance == null)
                {
                    return this.GetStatic("JsonSchema");
                }
                else
                {
                    return this.Instance.MyJsonSchema;
                }
            }
        }

        public int Id
        {
            get { return this.Instance.Id; }
            set { this.Instance.Id = value; }
        }

        public DateTime CreatedAt
        {
            get { return this.Instance.CreatedAt; }
            set { this.Instance.CreatedAt = value; }
        }

        public DateTime ModifiedAt
        {
            get { return this.Instance.ModifiedAt; }
            set { this.Instance.ModifiedAt = value; }
        }

        public DateTime? DeletedAt
        {
            get { return this.Instance.DeletedAt; }
            set { this.Instance.DeletedAt = value; }
        }

        public string ToJson()
        {
            return this.Instance.ToJson();
        }

        public IModel<Model> FromJson(string json)
        {
            return this.InvokeStatic("FromJson", json);
        }

        public override string ToString()
        {
            return this.Instance.ToString();
        }

        public T Get<T>(string propName, bool loadRelations = true, bool triggerChangeEvent = true)
        {
            return this.Instance.Get<T>(propName, loadRelations, triggerChangeEvent);
        }

        public void Set<T>(T value, string propName, bool triggerChangeEvent = true)
        {
            this.Instance.Set<T>(value, propName, triggerChangeEvent);
        }

        public IModel<Model> Hydrate(SqlResult record)
        {
            return this.InvokeStatic("Hydrate", record);
        }

        public dynamic Hydrate(List<SqlResult> records)
        {
            return this.InvokeStatic("Hydrate", records);
        }

        public IModel<Model> Find(int key, bool withTrashed = false)
        {
            return this.InvokeStatic("Find", key, withTrashed);
        }

        public dynamic ToList(bool withTrashed = false)
        {
            return this.InvokeStatic("ToList", withTrashed);
        }
    }

    public class DModel<TModel> : DModel
    {
        public DModel(object entity = null) : base(entity)
        {
            if (entity == null)
            {
                this.ModelType = typeof(TModel);
            }
        }

        public new TModel Hydrate(SqlResult record)
        {
            return this.InvokeStatic("Hydrate", record);
        }

        public new List<TModel> Hydrate(List<SqlResult> records)
        {
            return this.InvokeStatic("Hydrate", records);
        }

        public new TModel Find(int key, bool withTrashed = false)
        {
            return this.InvokeStatic("Find", key, withTrashed);
        }
    }
}
