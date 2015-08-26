namespace Graceful
{
    using System;
    using Inflector;
    using System.Linq;
    using Graceful.Query;
    using System.Reflection;
    using System.Collections.Generic;
    using Relation = Graceful.Utils.RelationshipDiscoverer.Relation;

    public class Model
    {
        /**
         * Cache the results of GetAllModels.
         */
        private static List<Type> _AllModels;

        /**
         * Returns a list of all defined models in the current app domain.
         *
         * ```
         * 	var models = Model.GetAllModels();
         * ```
         */
        public static List<Type> GetAllModels()
        {
            if (_AllModels != null) return _AllModels;

            _AllModels = new List<Type>();

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
         * ```
         * 	Model.GetModel("Aceme.Models.Person");
         * ```
         *
         * Or you may provide just the class name:
         * ```
         *  Model.GetModel("Person");
         * ```
         *
         * Or you may provide the plurized version:
         * ```
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
         *
         * > NOTE: If you hadn't caught on DModel is short for Dynamic Model.
         */
        public static DModel Dynamic(Type modelType)
        {
            return new DModel(modelType);
        }

        /**
         * Return a new DModel instance from the given model name.
         *
         * ```cs
         *  Model.Dynamic("Foo").SqlTableName;
         * ```
         *
         * > NOTE: If you hadn't caught on DModel is short for Dynamic Model.
         */
        public static DModel Dynamic(string modelName)
        {
            return new DModel(GetModel(modelName));
        }

        /**
         * Return a new DModel instance from the given entity.
         *
         * ```cs
         *  Model.Dynamic(entity).SqlTableName;
         * ```
         *
         * > NOTE: If you hadn't caught on DModel is short for Dynamic Model.
         */
        public static DModel Dynamic(object entity)
        {
            return new DModel(entity);
        }

        /**
         * Return a new DModel instance from the given generic type parameter.
         *
         * ```cs
         *  Model.Dynamic<Foo>().SqlTableName;
         * ```
         *
         * > NOTE: If you hadn't caught on DModel is short for Dynamic Model.
         */
        public static DModel<TModel> Dynamic<TModel>(object entity = null)
        {
            return new DModel<TModel>(entity);
        }
    }
}
