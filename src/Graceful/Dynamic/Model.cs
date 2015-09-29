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
    using System.Linq;
    using Graceful.Query;
    using System.Reflection;
    using Newtonsoft.Json.Schema;
    using System.Linq.Expressions;
    using System.Collections.Generic;
    using BaseModel = Graceful.Model;

    public class Model
    {
        public Type ModelType { get; protected set; }

        public dynamic Instance { get; protected set; }

        public Model(Type modelType)
        {
            this.ModelType = modelType;
        }

        public Model(object entity = null)
        {
            if (entity != null)
            {
                this.ModelType = entity.GetType();
                this.Instance = (dynamic)entity;
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

        public T InvokeStatic<T>(string methodName, params object[] args)
        {
            return (T)this.InvokeStatic(methodName, args);
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

        public T GetStatic<T>(string propName)
        {
            return (T)this.GetStatic(propName);
        }

        public Context Db
        {
            get
            {
                return this.GetStatic<Context>("Db");
            }
        }

        public string SqlTableName
        {
            get
            {
                return this.GetStatic<string>("SqlTableName");
            }
        }

        public List<PropertyInfo> MappedProps
        {
            get
            {
                return this.GetStatic<List<PropertyInfo>>("MappedProps");
            }
        }

        public List<PropertyInfo> MappedPropsExceptId
        {
            get
            {
                return this.GetStatic<List<PropertyInfo>>("MappedPropsExceptId");
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
                return this.GetStatic<JSchema>("JsonSchema");
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

        public dynamic FromJson(string json)
        {
            return this.InvokeStatic("FromJson", json);
        }

        public dynamic FromJsonArray(string json)
        {
            return this.InvokeStatic("FromJsonArray", json);
        }

        public override string ToString()
        {
            return this.Instance.ToString();
        }

        public T Get<T>(string propName, bool loadFromDiscovered = true, bool LoadFromDb = true)
        {
            return this.Instance.Get<T>(propName, loadFromDiscovered, LoadFromDb);
        }

        public void Set<T>(T value, string propName, bool triggerChangeEvent = true)
        {
            this.Instance.Set<T>(value, propName, triggerChangeEvent);
        }

        public dynamic Hydrate(Dictionary<string, object> record, bool fromUser = false)
        {
            return this.InvokeStatic("Hydrate", record, fromUser);
        }

        public dynamic Hydrate(List<Dictionary<string, object>> records, bool fromUser = false)
        {
            return this.InvokeStatic("Hydrate", records, fromUser);
        }

        public dynamic FilterTrashed(bool withTrashed = false)
        {
            return this.InvokeStatic("FilterTrashed", withTrashed);
        }

        public dynamic Find(int key, bool withTrashed = false)
        {
            return this.InvokeStatic("Find", key, withTrashed);
        }

        public dynamic Find(object entity, bool withTrashed = false)
        {
            return this.InvokeStatic("Find", entity, withTrashed);
        }

        public bool Exists(int key, bool withTrashed = false)
        {
            return this.InvokeStatic<bool>("Exists", key, withTrashed);
        }

        public bool Exists(object entity, bool withTrashed = false)
        {
            return this.InvokeStatic<bool>("Exists", entity, withTrashed);
        }

        public bool All(string predicate, bool withTrashed = false)
        {
            return this.InvokeStatic<bool>("All", predicate, withTrashed);
        }

        public bool Any(bool withTrashed = false)
        {
            return this.InvokeStatic<bool>("Any", withTrashed);
        }

        public bool Any(string predicate, bool withTrashed = false)
        {
            return this.InvokeStatic<bool>("Any", predicate, withTrashed);
        }

        public int Count(bool withTrashed = false)
        {
            return this.InvokeStatic<int>("Count", withTrashed);
        }

        public int Count(string predicate, bool withTrashed = false)
        {
            return this.InvokeStatic<int>("Count", predicate, withTrashed);
        }

        public dynamic First(bool withTrashed = false)
        {
            return this.InvokeStatic("First", withTrashed);
        }

        public dynamic First(string predicate, bool withTrashed = false)
        {
            return this.InvokeStatic("First", predicate, withTrashed);
        }

        public dynamic FirstOrDefault(bool withTrashed = false)
        {
            return this.InvokeStatic("FirstOrDefault", withTrashed);
        }

        public dynamic FirstOrDefault(string predicate, bool withTrashed = false)
        {
            return this.InvokeStatic("FirstOrDefault", predicate, withTrashed);
        }

        public dynamic Single(bool withTrashed = false)
        {
            return this.InvokeStatic("Single", withTrashed);
        }

        public dynamic Single(string predicate, bool withTrashed = false)
        {
            return this.InvokeStatic("Single", predicate, withTrashed);
        }

        public dynamic SingleOrDefault(bool withTrashed = false)
        {
            return this.InvokeStatic("SingleOrDefault", withTrashed);
        }

        public dynamic SingleOrDefault(string predicate, bool withTrashed = false)
        {
            return this.InvokeStatic("SingleOrDefault", predicate, withTrashed);
        }

        public dynamic Where(string predicate, bool withTrashed = false)
        {
            return this.InvokeStatic("Where", predicate, withTrashed);
        }

        public dynamic Like(string predicate, bool withTrashed = false)
        {
            return this.InvokeStatic("Like", predicate, withTrashed);
        }

        public dynamic OrderBy(string predicate, OrderDirection direction = OrderDirection.ASC, bool withTrashed = false)
        {
            return this.InvokeStatic("OrderBy", predicate, direction, withTrashed);
        }

        public dynamic Skip(int count, bool withTrashed = false)
        {
            return this.InvokeStatic("Skip", count, withTrashed);
        }

        public dynamic Take(int count, bool withTrashed = false)
        {
            return this.InvokeStatic("Take", count, withTrashed);
        }

        public dynamic ToArray(bool withTrashed = false)
        {
            return this.InvokeStatic("ToArray", withTrashed);
        }

        public dynamic ToList(bool withTrashed = false)
        {
            return this.InvokeStatic("ToList", withTrashed);
        }

        public dynamic Create(object entity)
        {
            return this.InvokeStatic("Create", entity);
        }

        public dynamic Create(Dictionary<string, object> record)
        {
            return this.InvokeStatic("Create", record);
        }

        public dynamic Create(string json)
        {
            return this.InvokeStatic("Create", json);
        }

        public dynamic CreateMany(string json)
        {
            return this.InvokeStatic("CreateMany", json);
        }

        public dynamic SingleOrCreate(object entity)
        {
            return this.InvokeStatic("SingleOrCreate", entity);
        }

        public dynamic SingleOrCreate(Dictionary<string, object> record)
        {
            return this.InvokeStatic("SingleOrCreate", record);
        }

        public dynamic SingleOrCreate(string json)
        {
            return this.InvokeStatic("SingleOrCreate", json);
        }

        public dynamic SingleOrNew(object entity)
        {
            return this.InvokeStatic("SingleOrNew", entity);
        }

        public dynamic SingleOrNew(Dictionary<string, object> record)
        {
            return this.InvokeStatic("SingleOrNew", record);
        }

        public dynamic SingleOrNew(string json)
        {
            return this.InvokeStatic("SingleOrNew", json);
        }

        public dynamic FirstOrCreate(object entity)
        {
            return this.InvokeStatic("FirstOrCreate", entity);
        }

        public dynamic FirstOrCreate(Dictionary<string, object> record)
        {
            return this.InvokeStatic("FirstOrCreate", record);
        }

        public dynamic FirstOrCreate(string json)
        {
            return this.InvokeStatic("FirstOrCreate", json);
        }

        public dynamic FirstOrNew(object entity)
        {
            return this.InvokeStatic("FirstOrNew", entity);
        }

        public dynamic FirstOrNew(Dictionary<string, object> record)
        {
            return this.InvokeStatic("FirstOrNew", record);
        }

        public dynamic FirstOrNew(string json)
        {
            return this.InvokeStatic("FirstOrNew", json);
        }

        public void Update(Dictionary<string, object> record)
        {
            this.InvokeStatic("Update", record);
        }

        public void Update(string json)
        {
            this.InvokeStatic("Update", json);
        }

        public void UpdateMany(string json)
        {
            this.InvokeStatic("UpdateMany", json);
        }

        public void Update(string assignments, bool withTrashed = false)
        {
            this.InvokeStatic("Update", assignments, withTrashed);
        }

        public dynamic UpdateOrCreate(object find, object update)
        {
            return this.InvokeStatic("UpdateOrCreate", find, update);
        }

        public dynamic UpdateOrCreate(string find, object update)
        {
            return this.InvokeStatic("UpdateOrCreate", find, update);
        }

        public void Destroy(bool hardDelete = false)
        {
            this.InvokeStatic("Destroy", hardDelete);
        }

        public void Destroy(int key, bool hardDelete = false)
        {
            this.InvokeStatic("Destroy", key, hardDelete);
        }

        public void Destroy(bool hardDelete = false, params int[] keys)
        {
            this.InvokeStatic("Destroy", hardDelete, keys);
        }

        public void Delete(bool hardDelete = false)
        {
            this.Instance.Delete(hardDelete);
        }

        public dynamic Restore()
        {
            return this.Instance.Restore();
        }

        public dynamic Save()
        {
            return this.Instance.Save();
        }
    }
}
