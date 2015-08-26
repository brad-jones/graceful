namespace Graceful.Query
{
    using System;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Data;
    using System.Data.Common;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Inflector;
    using Graceful.Query;
    using Graceful.Utils;
    using Graceful.ExpressionVisitors;

    public class Linq<TModel> where TModel:Model<TModel>, new()
    {
        /**
         * The Graceful Database Context.
         */
        protected readonly Context Ctx;

        /**
         * The table name of this TModel.
         */
        protected readonly string TableName;

        /**
         * A Graceful Query Builder instance that
         * is the source of data for the set.
         */
        protected Query.Builder DefiningQuery
        {
            get
            {
                if (this._DefiningQuery == null || this._DefiningQuery.IsEmpty)
                {
                    this._DefiningQuery = this.Ctx.Qb
                    .SELECT("*").FROM(this.TableName);
                }

                return this._DefiningQuery;
            }

            set
            {
                this._DefiningQuery = value;
            }
        }

        protected Query.Builder _DefiningQuery;

        /**
         * Returns the actual SQL query text for the current query.
         *
         * ```cs
         * 	var linq = new Linq<Foo>("Foo", Context.GlobalCtx);
         * 	var sql = linq.Where(m => m.Id == 1).Sql;
         * 	// sql = SELECT * FROM Foo WHERE Id = 1
         * ```
         */
        public string Sql
        {
            get
            {
                return this.DefiningQuery.Sql;
            }
        }

        /**
         * Returns the SQL Parameters associated with the current DefiningQuery.
         *
         * ```cs
         * 	var linq = new Linq<Foo>("Foo", Context.GlobalCtx);
         * 	var params = linq.Where(m => m.Id == 1).Parameters;
         * 	// params = new SqlParams { { "@p0", 1 } };
         * ```
         */
        public Query.SqlParams Parameters
        {
            get
            {
                return this.DefiningQuery.Parameters;
            }
        }

        /**
         * LinqModel Constructor
         *
         * Unlike the DbExtensions SqlSet class we are mutable.
         * The Where, OrderBy, Skip, Take, etc just replace the
         * underlying Set object.
         *
         * We will also dispose of the the Datbase Connection for you
         * the second any meaningful data has been returned from the class.
         */
        public Linq(string tableName, Context Db, Query.Builder Qb = null)
        {
            this.Ctx = Db; this.TableName = tableName;

            if (Qb != null)
            {
                this.DefiningQuery = Qb;
            }
        }

        /**
         * Returns a string representation of the current query.
         */
        public override string ToString()
        {
            return this.DefiningQuery.ToString();
        }

        /**
         * Do all entities in the set pass the predicate?
         *
         * ```
         * 	if (Models.Foo.All("Bar = {0}", "abc"))
         * 	{
         * 		// All Foo's have their Bar property set to abc
         * 	}
         * 	else
         * 	{
         * 		// Not all Foo's have their Bar property set to abc
         * 	}
         * ```
         */
        public bool All(string predicate, params object[] parameters)
        {
            return !this.Any(String.Concat("NOT (",predicate,")"), parameters);
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
        public bool All(Expression<Func<TModel, bool>> predicate)
        {
            var converter = new PredicateConverter();
            converter.Visit(predicate.Body);
            return this.All(converter.Sql, converter.Parameters);
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
        public bool Any()
        {
            return (int)this.Ctx.Qb.SELECT
            (
                "(CASE WHEN EXISTS ({0}) THEN 1 ELSE 0 END)",
                this.DefiningQuery
            ).Scalar > 0 ? true : false;
        }

        /**
         * Do any entities in the set pass the predicate?
         *
         * ```
         * 	if (Models.Foo.Any("Bar = {0}", "abc"))
         * 	{
         * 		// At least one Foo has it's Bar property set to abc
         * 	}
         * 	else
         * 	{
         * 		// No Foo's have their Bar property set to abc
         * 	}
         * ```
         */
        public bool Any(string predicate, params object[] parameters)
        {
            return this.Where(predicate, parameters).Any();
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
        public bool Any(Expression<Func<TModel, bool>> predicate)
        {
            var converter = new PredicateConverter();
            converter.Visit(predicate.Body);
            return this.Any(converter.Sql, converter.Parameters);
        }

        /**
         * How many entities are there in the set?
         *
         * ```
         * 	var numberOfEntities = Models.Foo.Count();
         * ```
         */
        public int Count()
        {
            return (int)this.Ctx.Qb
            .SELECT("COUNT(*)")
            .FROM("({0}) count", this.DefiningQuery)
            .Scalar;
        }

        /**
         * How many entities are there in the set that match the predicate?
         *
         * ```
         * 	var numberOfEntities = Models.Foo.Count("Bar = {0}", "abc");
         * ```
         */
        public int Count(string predicate, params object[] parameters)
        {
            return this.Where(predicate, parameters).Count();
        }

        /**
         * How many entities are there in the set that match the predicate?
         *
         * ```
         * 	var numberOfEntities = Models.Foo.Count(m => m.Bar == "abc");
         * ```
         */
        public int Count(Expression<Func<TModel, bool>> predicate)
        {
            var converter = new PredicateConverter();
            converter.Visit(predicate.Body);
            return this.Count(converter.Sql, converter.Parameters);
        }

        /**
         * Returns the first entity of the set.
         *
         * ```
         * 	var entity = Models.Foo.First();
         * ```
         */
        public TModel First()
        {
            return Model.Dynamic<TModel>().Hydrate(this.Take(1).DefiningQuery.Row);
        }

        /**
         * Returns the first entity of the set that matches the predicate.
         *
         * ```
         * 	var entity = Models.Foo.First("Bar = {0}", "abc");
         * ```
         */
        public TModel First(string predicate, params object[] parameters)
        {
            return this.Where(predicate, parameters).First();
        }

        /**
         * Returns the first entity of the set that matches the predicate.
         *
         * ```
         * 	var entity = Models.Foo.First(m => m.Bar == "abc");
         * ```
         */
        public TModel First(Expression<Func<TModel, bool>> predicate)
        {
            var converter = new PredicateConverter();
            converter.Visit(predicate.Body);
            return this.First(converter.Sql, converter.Parameters);
        }

        /**
         * Returns the first element of the set,
         * or a default value if the set contains no elements.
         *
         * ```
         * 	var entity = Models.Foo.FirstOrDefault();
         * ```
         */
        public TModel FirstOrDefault()
        {
            var record = this.Take(1).DefiningQuery.Rows.FirstOrDefault();

            if (record == null)
            {
                return default(TModel);
            }
            else
            {
                return Model.Dynamic<TModel>().Hydrate(record);
            }
        }

        /**
         * Returns the first element of the set that matches the predicate,
         * or a default value if the set contains no elements.
         *
         * ```
         * 	var entity = Models.Foo.FirstOrDefault("Bar = {0}", "abc");
         * ```
         */
        public TModel FirstOrDefault(string predicate, params object[] parameters)
        {
            return this.Where(predicate, parameters).FirstOrDefault();
        }

        /**
         * Returns the first element of the set that matches the predicate,
         * or a default value if the set contains no elements.
         *
         * ```
         * 	var entity = Models.Foo.FirstOrDefault(m => m.Bar == "abc");
         * ```
         */
        public TModel FirstOrDefault(Expression<Func<TModel, bool>> predicate)
        {
            var converter = new PredicateConverter();
            converter.Visit(predicate.Body);
            return this.FirstOrDefault(converter.Sql, converter.Parameters);
        }

        /**
         * Returns the only entity of the set. If the set contains no entities
         * or more than one entity then an exception will be thrown.
         *
         * ```
         * 	var entity = Models.Foo.Single();
         * ```
         */
        public TModel Single()
        {
            return Model.Dynamic<TModel>().Hydrate(this.DefiningQuery.Rows.Single());
        }

        /**
         * Returns the only entity of the set that matches the predicate.
         * If the set contains no entities or more than one entity then an
         * exception will be thrown.
         *
         * ```
         * 	var entity = Models.Foo.Single("Bar = {0}", "abc");
         * ```
         */
        public TModel Single(string predicate, params object[] parameters)
        {
            return this.Where(predicate, parameters).Single();
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
        public TModel Single(Expression<Func<TModel, bool>> predicate)
        {
            var converter = new PredicateConverter();
            converter.Visit(predicate.Body);
            return this.Single(converter.Sql, converter.Parameters);
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
        public TModel SingleOrDefault()
        {
            var record = this.DefiningQuery.Rows.SingleOrDefault();

            if (record == null)
            {
                return default(TModel);
            }
            else
            {
                return Model.Dynamic<TModel>().Hydrate(record);
            }
        }

        /**
         * Returns the only element of the set that matches the predicate, or a
         * default value if the set is empty; this method throws an exception
         * if there is more than one element in the set.
         *
         * ```
         * 	var entity = Models.Foo.SingleOrDefault("Bar = {0}", "abc");
         * ```
         */
        public TModel SingleOrDefault(string predicate, params object[] parameters)
        {
            return this.Where(predicate, parameters).SingleOrDefault();
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
        public TModel SingleOrDefault(Expression<Func<TModel, bool>> predicate)
        {
            var converter = new PredicateConverter();
            converter.Visit(predicate.Body);
            return this.SingleOrDefault(converter.Sql, converter.Parameters);
        }

        /**
         * Filters the set based on the predicate.
         * This does not return results but a new filtered Linq<TModel>.
         *
         * ```
         * 	var filteredEntities = Models.Foo.Where("Bar = {0}", "abc");
         * ```
         */
        public Linq<TModel> Where(string predicate, params object[] parameters)
        {
            return new Linq<TModel>
            (
                this.TableName,
                this.Ctx,
                this.DefiningQuery.WHERE(predicate, parameters)
            );
        }

        /**
         * Filters the set based on the predicate.
         * This does not return results but a new filtered Linq<TModel>.
         *
         * ```
         * 	var filteredEntities = Models.Foo.Where(m => m.Bar == "abc");
         * ```
         */
        public Linq<TModel> Where(Expression<Func<TModel, bool>> predicate)
        {
            var converter = new PredicateConverter();
            converter.Visit(predicate.Body);
            return this.Where(converter.Sql, converter.Parameters);
        }

        /**
         * Filters the set based on the predicate, using sql LIKE clauses.
         * This does not return results but a new filtered Linq<TModel>.
         *
         * ```
         * 	var filteredEntities = Models.Foo.Like(m => m.Bar == "%abc%");
         * ```
         */
        public Linq<TModel> Like(Expression<Func<TModel, bool>> predicate)
        {
            var converter = new LikeConverter();
            converter.Visit(predicate.Body);
            return this.Where(converter.Sql, converter.Parameters);
        }

        /**
         * Orders the set based on the provided column list.
         *
         * ```
         * 	var orderedEntities = Models.Foo.OrderBy("Bar DESC, Baz ASC");
         * ```
         *
         * Or even something like this:
         *
         * ```
         * 	var orderedEntities = Models.Foo.OrderBy
         * 	(
         * 		"Bar {0}, Baz {1}",
         * 		"DESC",
         * 		"ASC"
         * 	);
         * ```
         *
         * > NOTE: ASC is the default direction, if not supplied.
         */
        public Linq<TModel> OrderBy(string columnList, params object[] parameters)
        {
            return new Linq<TModel>
            (
                this.TableName,
                this.Ctx,
                this.DefiningQuery.ORDER_BY(columnList, parameters)
            );
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
        public Linq<TModel> OrderBy(Expression<Func<TModel, object>> predicate, OrderDirection direction = OrderDirection.ASC)
        {
            MemberExpression member;

            if (predicate.Body.GetType() == typeof(UnaryExpression))
            {
                var unary = (UnaryExpression)predicate.Body;
                member = (MemberExpression)unary.Operand;
            }
            else
            {
                member = (MemberExpression)predicate.Body;
            }

            return this.OrderBy
            (
                member.Member.Name + " " + direction.ToString()
            );
        }

        /**
         * Bypasses a specified number of entities in the set
         * and then returns the remaining entities.
         *
         * ```
         * 	var first10EntitiesIgnored = Models.Foo.Skip(10);
         * ```
         */
        public Linq<TModel> Skip(int count)
        {
            return new Linq<TModel>
            (
                this.TableName,
                this.Ctx,
                this.DefiningQuery.OFFSET(count)
            );
        }

        /**
         * Returns a specified number of contiguous
         * entities from the start of the set.
         *
         * ```
         * 	var IHave10Entities = Models.Foo.Take(10);
         * ```
         */
        public Linq<TModel> Take(int count)
        {
            return new Linq<TModel>
            (
                this.TableName,
                this.Ctx,
                this.DefiningQuery.LIMIT(count)
            );
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
        public TModel[] ToArray()
        {
            return this.ToList().ToArray();
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
        public List<TModel> ToList()
        {
            return Model.Dynamic<TModel>().Hydrate(this.DefiningQuery.Rows);
        }

        /**
         * Returns a json serialisation of the entities.
         *
         * ```
         * 	var json = Models.Foo.ToJson();
         * ```
         */
        public string ToJson()
        {
            return JArray.FromObject(this.ToList(), this.Ctx.JsonSerializer).ToString();
        }

        /**
         * Updates enMasse without first loading the entites into memory.
         *
         * ```
         * 	Models.Foo.Update("Bar = {0}, Baz = {1}", "abc", 123);
         * ```
         *
         * > NOTE: You can only update the primative columns.
         * > You can not bulk update a relationship by supplying a new entity.
         * > You may however update the relationship foreign key, assuming you
         * > know the name of the foreign key column.
         */
        public void Update(string setClause, params object[] parameters)
        {
            this.Ctx.Qb
            .UPDATE(this.DefiningQuery)
            .SET(setClause, parameters)
            .SET("ModifiedAt", DateTime.UtcNow)
            .Execute();
        }

        /**
         * Updates enMasse without first loading the entites into memory.
         *
         * ```
         * 	Models.Foo.Update(m => m.Bar == "abc" && m.Baz == 123);
         * ```
         *
         * > NOTE: This is NOT a "WHERE" predicate. The expression you provide
         * > this Update method follows the same structure as a predicate but
         * > we parse it slightly diffrently. Consider each "&&" or "||" as a
         * > comma. And each "==" simply as a "=" operator.
         */
        public void Update(Expression<Func<TModel, object>> assignments)
        {
            var converter = new AssignmentsConverter();
            converter.Visit(assignments.Body);
            this.Update(converter.Sql, converter.Parameters);
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
        public void Destroy(bool hardDelete = false)
        {
            if (hardDelete)
            {
                this.Ctx.Qb.DELETE_FROM(this.DefiningQuery).Execute();
            }
            else
            {
                this.Ctx.Qb
                .UPDATE(this.DefiningQuery)
                .SET("ModifiedAt", DateTime.UtcNow)
                .SET("DeletedAt", DateTime.UtcNow)
                .Execute();
            }
        }
    }

    // Used by one of our OrderBy Overloads
    public enum OrderDirection { ASC, DESC }
}
