namespace Graceful
{
    using System;
    using System.Linq;
    using Graceful.Query;
    using System.Reflection;
    using System.ComponentModel;
    using Newtonsoft.Json.Schema;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /**
     * Covariant Generic Interface for GenericModel
     *
     * This allows us to cast, instead of using Reflection.
     * Which obviously is much, much faster.
     *
     * @see: http://stackoverflow.com/questions/16795750
     *
     * ```
     * 	someMethod(object value)
     * 	{
     * 		// Assume that we know the value passed to this method
     * 		// will be a Model<TModel>, lets say our goal is to
     * 		// Save the entity.
     *
     * 		// This won't work, results in a compiler error:
     * 		// the generic type "Model<T>" requires 1 type arguments
     * 		((Model<>)value).Save();
     *
     * 		// This will work work though.
     * 		((IModel<Model>)value).Save();
     *
     * 		// Sometimes we come across lists of entities but we
     * 		// again may not know their type at compile time.
     * 		// Keep in mind you are creating a "new" list here.
     * 		// see: http://stackoverflow.com/questions/8933434
     * 		(value as IEnumerable<object>).Cast<IModel<Model>>()
     * 		.ToList().ForEach(entity =>
     * 		{
     * 			entity.Save();
     * 		});
     * 	}
     *
     * ```
     *
     * > NOTE: This obviously only works on instance methods / properties.
     * > Some static properties have instance aliases, such as MySqlTableName.
     * > For all other static members you will still need to use reflection.
     * > The Model has many helper methods for these cases.
     */
    public interface IModel<out TModel> where TModel : Model
    {
        Context MyDb { get; }
        event PropertyChangedEventHandler PropertyChanged;
        Dictionary<string, object> PropertyBag { get; }
        Dictionary<string, object> OriginalPropertyBag { get; }
        List<PropertyInfo> ModifiedProps { get; }
        T Get<T>([CallerMemberName] string propName = "", bool loadRelations = true, bool triggerChangeEvent = true);
        void Set<T>(T value, [CallerMemberName] string propName = "", bool triggerChangeEvent = true);
        string ToJson();
        string ToString();
        int Id { get; set; }
        DateTime CreatedAt { get; set; }
        DateTime ModifiedAt { get; set; }
        DateTime? DeletedAt { get; set; }
        string MySqlTableName { get; }
        List<PropertyInfo> MyMappedProps { get; }
        JSchema MyJsonSchema { get; }
        void Delete(bool hardDelete = false);
        TModel Restore();
        TModel Save(List<PropertyInfo> DealtWithRelationships = null);
    }
}
