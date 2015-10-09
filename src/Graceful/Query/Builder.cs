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

namespace Graceful.Query
{
    using System;
    using System.Text;
    using System.Linq;
    using System.Data;
    using Newtonsoft.Json;
    using Graceful.Extensions;
    using Newtonsoft.Json.Linq;
    using System.Data.SqlClient;
    using System.Collections.Generic;
    using System.Security.Cryptography;

    public class Builder : Helper
    {
        /**
         * Pass the context on to the Base Helper Class.
         */
        public Builder(Context Db) : base(Db) {}

        /**
         * This is where we store and build the actual SQL text.
         */
        protected StringBuilder Buffer = new StringBuilder();

        /**
         * We store the values for any Sql Parameters here.
         */
        protected List<object> BufferValues = new List<object>();

        /**
         * Keep track of the current clause that is being used.
         * This allows us to call the same method twice and simply
         * add on to the clause in a sensible fashion.
         */
        protected string CurrentClause;

        /**
         * The counter part to the CurrentClause. When we append to the same
         * clause, how do we seperate the values, with a comma or somethingelse.
         */
        protected string CurrentSeperator;

        /**
         * Public access to the current buffer.
         */
        public string Sql
        {
            get { return this.Buffer.ToString(); }
        }

        /**
         * If the buffer has nothing in it, we will return true.
         */
        public bool IsEmpty
        {
            get
            {
                if (this.Buffer.Length == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /**
         * Public access to the current list of parameter values.
         */
        public Dictionary<string, object> Parameters
        {
            get
            {
                var sqlparams = new Dictionary<string, object>();

                this.BufferValues.ForEach
                (
                    (key, value) => sqlparams["@p" + key + "p"] = value
                );

                return sqlparams;
            }
        }

        /**
         * Returns are hash of the current _"built"_ query, helpful for caching.
         */
        public string Hash
        {
            get
            {
                // Lets compile the query into a string that we can then hash.
                var queryString = new StringBuilder(this.Sql);
                queryString.AppendLine();

                this.BufferValues.ForEach((key, value) =>
                {
                    queryString.Append("@p" + key + "p => ");

                    if (Utils.TypeMapper.IsClrType(value))
                    {
                        queryString.Append(value.ToString());
                    }
                    else
                    {
                        queryString.Append
                        (
                            // NOTE: We are not expecting any recursive
                            // entities or the like here. Maybe an SqlId
                            // SqlTable or SqlColumn class.
                            JObject.FromObject
                            (
                                value,
                                new JsonSerializer
                                {
                                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                                }
                            ).ToString()
                        );
                    }

                    queryString.AppendLine();
                });

                // We now have a string that represents
                // the query that might be performed.
                // Let create a hash of this string.
                var hash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(queryString.ToString()));
                var sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }

        /**
         * Fluent version of BuildCmd.
         *
         * ```cs
         * 	var ctx = new Context();
         * 	var qb = new QueryBuilder(ctx);
         * 	var cmd = qb
         * 		.SELECT("*").FROM("people")
         * 		.WHERE("surname = {0}", Jones)
         * 		.WHERE("age > {0}", 27)
         * 	 	.Cmd;
         * ```
         */
        public SqlCommand Cmd
        {
            get
            {
                return this.BuildCmd(this.Sql, this.Parameters);
            }
        }

        /**
         * Fluent version of Read.
         *
         * ```cs
         * 	var ctx = new Context();
         * 	var qb = new QueryBuilder(ctx);
         * 	using (var reader = qb.SELECT("*").FROM("Foo").Reader)
         * 	{
         * 		...read the reader...
         * 	}
         * ```
         *
         * > NOTE: Assuming you wrap the Query call in a "using" we will
         * > automatically close and dispose of both the SqlCommand and the
         * > SqlConnection used in the making of the reader.
         */
        public SqlDataReader Reader
        {
            get
            {
                using (var cmd = this.Cmd)
                {
                    return this.GetReader(cmd);
                }
            }
        }

        /**
         * Fluent version of ReadToDt.
         *
         * ```cs
         * 	var ctx = new Context();
         * 	var qb = new QueryBuilder(ctx);
         * 	using (var dt = qb.SELECT("*").FROM("Bar").DataTable)
         * 	{
         * 		foreach (DataRow row in dt.Rows)
         * 		{
         * 			foreach (DataColumn column in dt.Columns)
         * 			{
         * 				var ColumnName = column.ColumnName;
         * 				var ColumnData = row[column];
         * 			}
         * 		}
         * 	}
         * ```
         */
        public DataTable DataTable
        {
            get
            {
                var dt = new DataTable();

                using (var reader = this.Reader)
                {
                    dt.Load(reader);
                }

                return dt;
            }
        }

        /**
         * Fluent version ReadToScalar.
         *
         * ```cs
         * 	var ctx = new Context();
         * 	var qb = new QueryBuilder(ctx);
         * 	var id = qb.SELECT("id").FROM("foo").WHERE("x=y").Scalar;
         * ```
         *
         * > NOTE: The SqlCommand & SqlConnection are disposed of for you.
         */
        public object Scalar
        {
            get
            {
                object result;

                using (var cmd = this.Cmd)
                {
                    result = this.GetScalar(cmd);
                }

                return result;
            }
        }

        /**
         * Fluent version of Execute.
         *
         * ```cs
         * 	var ctx = new Context();
         * 	var qb = new QueryBuilder(ctx);
         * 	var rowsEffected = qb.DELETE_FROM("foo").WHERE("x=y").Execute();
         * ```
         *
         * > NOTE: It didn't make sense to have this one as a Property.
         */
        public int Execute()
        {
            int result;

            using (var cmd = this.Cmd)
            {
                result = this.Execute(cmd);
            }

            return result;
        }

        /**
         * Fluent version of GetRows.
         *
         * ```cs
         * 	var ctx = new Context();
         * 	var qb = new QueryBuilder(ctx);
         * 	qb.SELECT("*").FROM("People").Rows.ForEach(row =>
         * 	{
         * 		row.Keys.ToList().ForEach(column =>
         * 		{
         * 			var value = row[column];
         * 		});
         * 	});
         * ```
         */
        public List<Dictionary<string, object>> Rows
        {
            get
            {
                List<Dictionary<string, object>> list;

                using (DataTable dt = this.DataTable)
                {
                    list = this.GetRows(dt);
                }

                return list;
            }
        }

        /**
         * Fluent version of GetRow.
         *
         * ```cs
         * 	var ctx = new Context();
         * 	var qb = new QueryBuilder(ctx);
         * 	var person = qb.SELECT("*").FROM("People").WHERE("Id = 10").Row;
         * ```
         */
        public Dictionary<string, object> Row
        {
            get
            {
                return this.Rows.First();
            }
        }

        /**
         * The corner stone of this Query Builder, this will append the
         * provided format string and arguments to the current SQL clause
         * in the buffer string builder.
         *
         * > NOTE: We keep this method internal, to extend the query builder
         * > you may use this or even override it to provide further advanced
         * > functionality.
         */
        protected virtual Builder AppendClause(string clause, string seperator, string format, params object[] args)
        {
            var appendClause = false;

            if (clause == null) clause = "";

            if (clause.ToUpper() == this.CurrentClause)
            {
                // We are appending to whatever the current
                // clause is, so append a seperator.
                if (this.CurrentSeperator != null)
                {
                    // Only append the seperator if we have a previous section to the clause.
                    if (!this.Buffer.ToString().EndsWith(this.CurrentClause + " "))
                    {
                        this.Buffer.Append(this.CurrentSeperator);
                        this.Buffer.Append(" ");
                    }
                }
                else
                {
                    // A null seperator simply means we append the same
                    // clause again. For example multiple JOIN clauses.
                    appendClause = true;
                }
            }
            else
            {
                // Save the new clause details.
                this.CurrentClause = clause.ToUpper();
                this.CurrentSeperator = seperator;
                appendClause = true;
            }

            // Yes we can have an empty clause, for example COLS
            if (appendClause && this.CurrentClause != "")
            {
                // Ensure the new clause starts on a new line.
                if (this.Buffer.Length > 0) this.Buffer.AppendLine();

                // Append the new clause.
                this.Buffer.Append(this.CurrentClause);
                this.Buffer.Append(" ");
            }

            // Bail out early, as we have nothing further to do.
            if (string.IsNullOrWhiteSpace(format)) return this;

            // Do we have any string format placeholders?
            if (args.Length > 0)
            {
                // Loop through our placeholders
                var placeholders = new object[args.Length];
                for (int i = 0; i < placeholders.Length; i++)
                {
                    // Grab the actual placeholder value.
                    var value = args[i];

                    // If the placeholder it's self is an array we need to treat
                    // each item of the array as an individual placeholder.
                    if (value.GetType().IsArray && value.GetType() != typeof(byte[]))
                    {
                        var strings = new List<string>();

                        foreach (var subValue in (dynamic)value)
                        {
                            var count = this.BufferValues.Count;
                            this.BufferValues.Add(subValue);
                            strings.Add("@p" + count + "p");
                        }

                        placeholders[i] = string.Join(",", strings.ToArray());
                    }

                    // If the placeholder is in fact another Query Builder
                    // we will inject it's buffer into ours and copy across
                    // all it's parameter values.
                    else if (value.GetType().IsAssignableFrom(typeof(Builder)))
                    {
                        var x = 0;
                        var subBuilder = ((Builder)value);
                        var subBuilderSql = subBuilder.Sql;
                        subBuilderSql = subBuilderSql.Replace("@p", "@tobereplaced");
                        foreach (var parameter in subBuilder.Parameters)
                        {
                            subBuilderSql = subBuilderSql.Replace
                            (
                                parameter.Key.Replace("@p", "@tobereplaced"),
                                "@p" + this.BufferValues.Count + "p"
                            );

                            this.BufferValues.Add(parameter.Value);

                            x++;
                        }

                        placeholders[i] = subBuilderSql;
                    }

                    // Otherwise it's just a normal everday placeholder.
                    else
                    {
                        placeholders[i] = "@p" + this.BufferValues.Count + "p";
                        this.BufferValues.Add(value);
                    }
                }

                this.Buffer.Append(string.Format(format, placeholders));
            }
            else
            {
                // No, so we can simply append the format string as is.
                this.Buffer.Append(format);
            }

            // Add a space after the format string.
            this.Buffer.Append(" ");

            // Return ourselves for simplified method chaining.
            return this;
        }

        /**
         * Appends to the current clause.
         *
         * You can construct your queries like this:
         * ```cs
         * 	Qb
         * 	.SELECT("Id")
         * 	.SELECT("Name")
         * 	.FROM("Users")
         * 	.WHERE("Name LIKE {0}", "%rad")
         * 	.WHERE("Age > {0}", 20)
         * ```
         *
         * Or you can do something like this:
         * ```cs
         * 	Qb
         * 	.SELECT("Id")
         * 	._("Name")
         * 	.FROM("Users")
         * 	.WHERE("Name LIKE {0}", "%rad")
         * 	._("Age > {0}", 20)
         * ```
         */
        public Builder _(string format, params object[] args)
        {
            return this.AppendClause
            (
                this.CurrentClause,
                this.CurrentSeperator,
                format,
                args
            );
        }

        /**
         * Only appends to the current clause if the condition is true.
         *
         * ```cs
         * 	void DynamicSql(int? categoryId, int? supplierId)
         * 	{
         *  	var query = Qb
         *   	.SELECT("ID, Name")
         *    	.FROM("Products")
         *     	.WHERE()
         *      ._IF(categoryId.HasValue, "CategoryID = {0}", categoryId)
         *      ._IF(supplierId.HasValue, "SupplierID = {0}", supplierId)
         *      .ORDER_BY("Name DESC");
         *  }
         * ```
         */
        public Builder _IF(bool condition, string format, params object[] args)
        {
            if (condition) this._(format, args);

            return this;
        }

        public Builder WITH(string format = null, params object[] args)
        {
            return this.AppendClause("WITH", ",", format, args);
        }

        public Builder WITH(SqlId alias, Builder subQuery)
        {
            return this.WITH("{0} AS ({1})", alias, subQuery);
        }

        public Builder WITH(string alias, Builder subQuery)
        {
            return this.WITH(new SqlId(alias), subQuery);
        }

        public Builder SELECT(string format = null, params object[] args)
        {
            return this.AppendClause("SELECT", ",", format, args);
        }

        public Builder FROM(string format = null, params object[] args)
        {
            return this.AppendClause("FROM", ",", format, args);
        }

        public Builder FROM(SqlId alias, Builder subQuery)
        {
            return this.FROM("({0}) {1}", subQuery, alias);
        }

        public Builder FROM(string alias, Builder subQuery)
        {
            if (alias.Contains('{') && alias.Contains('}'))
            {
                return this.FROM(alias, new object[] { subQuery });
            }

            return this.FROM(new SqlId(alias), subQuery);
        }

        public Builder FROM(Builder subQuery)
        {
            return this.FROM(new SqlId(Guid.NewGuid().ToString()), subQuery);
        }

        public Builder FROM(string table)
        {
            return this.FROM("{0}", new SqlTable(this.Db, table));
        }

        public Builder FROM(SqlTable table)
        {
            return this.FROM("{0}", table);
        }

        public Builder JOIN(string format, params object[] args)
        {
            return this.AppendClause("JOIN", null, format, args);
        }

        public Builder LEFT_JOIN(string format, params object[] args)
        {
            return this.AppendClause("LEFT JOIN", null, format, args);
        }

        public Builder RIGHT_JOIN(string format, params object[] args)
        {
            return this.AppendClause("RIGHT JOIN", null, format, args);
        }

        public Builder INNER_JOIN(string format, params object[] args)
        {
            return this.AppendClause("INNER JOIN", null, format, args);
        }

        public Builder WHERE(string format = null, params object[] args)
        {
            if (args == null)
            {
                return this.WHERE(format, DBNull.Value);
            }

            return this.AppendClause("WHERE", "AND", format, args);
        }

        public Builder WHERE(string key, string operation, object value)
        {
            return this.WHERE("{0} {1} {2}", new SqlId(key), operation, value);
        }

        public Builder WHERE(string key, object value)
        {
            if (key.Contains('{') && key.Contains('}'))
            {
                return this.WHERE(key, new object[] { value });
            }

            return this.WHERE("{0} = {1}", new SqlId(key), value);
        }

        public Builder HAVING(string format = null, params object[] args)
        {
            return this.AppendClause("HAVING", "AND", format, args);
        }

        public Builder GROUP_BY(string format = null, params object[] args)
        {
            return this.AppendClause("GROUP BY", ",", format, args);
        }

        public Builder GROUP_BY(string column)
        {
            return this.GROUP_BY("{0}", new SqlId(column));
        }

        public Builder GROUP_BY(SqlColumn column)
        {
            return this.GROUP_BY("{0}", column);
        }

        public Builder ORDER_BY(string format = null, params object[] args)
        {
            return this.AppendClause("ORDER BY", ",", format, args);
        }

        public Builder ORDER_BY(string column, string direction = null)
        {
            if (column.Contains('{') && column.Contains('}'))
            {
                return this.ORDER_BY(column, new object[] { direction });
            }

            if (column == "1")
            {
                if (direction != null)
                {
                    return this.ORDER_BY("1", new object[] { direction });
                }

                return this.ORDER_BY("1", new object[] { });
            }
            else
            {
                if (direction != null)
                {
                    return this.ORDER_BY("{0} {1}", new SqlId(column), direction);
                }

                return this.ORDER_BY("{0}", new SqlId(column));
            }
        }

        public Builder ORDER_BY(SqlColumn column, string direction = null)
        {
            if (direction != null)
            {
                return this.ORDER_BY("{0} {1}", column, direction);
            }

            return this.ORDER_BY("{0}", column);
        }

        public Builder OFFSET(int noOfRecordsToSkip)
        {
            if (!this.Buffer.ToString().Contains("ORDER BY"))
            {
                this.ORDER_BY("1");
            }

            return this.AppendClause
            (
                null, null, "OFFSET {0} ROWS", noOfRecordsToSkip
            );
        }

        public Builder LIMIT(int maxRecords)
        {
            if (!this.Buffer.ToString().Contains("OFFSET"))
            {
                this.OFFSET(0);
            }

            return this.AppendClause
            (
                null, null, "FETCH NEXT {0} ROWS ONLY", maxRecords
            );
        }

        public Builder INSERT_INTO(string format, params object[] args)
        {
            return this.AppendClause("INSERT INTO", null, format, args);
        }

        public Builder INSERT_INTO(string table)
        {
            return this.INSERT_INTO("{0}", new SqlTable(this.Db, table));
        }

        public Builder INSERT_INTO(SqlTable table)
        {
            return this.INSERT_INTO("{0}", table);
        }

        /**
         * Appends a list of columns to an INSERT INTO clause.
         *
         * ```cs
         * 	Qb.INSERT_INTO("Foo").COLS("name", "age");
         * ```
         */
        public Builder COLS(params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                throw new ArgumentException("args cannot be empty", "args");
            }

            return this.AppendClause(null, null, "({0})", new object[]
            {
                args.Select(arg => new SqlId(arg as string)).ToArray()
            });
        }

        /**
         * Appends a list of values to an INSERT INTO clause.
         *
         * ```cs
         * 	Qb.INSERT_INTO("Foo").VALUES("Bar", "Baz");
         * ```
         */
        public Builder VALUES(params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                throw new ArgumentException("args cannot be empty", "args");
            }

            return this.AppendClause("VALUES", null, "({0})", new object[]
            {
                args
            });
        }

        public Builder DELETE_FROM(string format, params object[] args)
        {
            return this.AppendClause("DELETE FROM", null, format, args);
        }

        public Builder DELETE_FROM(string table)
        {
            return this.DELETE_FROM("{0}", new SqlTable(this.Db, table));
        }

        public Builder DELETE_FROM(SqlTable table)
        {
            return this.DELETE_FROM("{0}", table);
        }

        public Builder DELETE_FROM(SqlId id)
        {
            return this.DELETE_FROM("{0}", id);
        }

        public Builder DELETE_FROM(Builder subQuery)
        {
            var withAlias = new SqlId("subQuery");

            return this.WITH(withAlias, subQuery).DELETE_FROM(withAlias);
        }

        public Builder UPDATE(string format, params object[] args)
        {
            return this.AppendClause("UPDATE", null, format, args);
        }

        public Builder UPDATE(string table)
        {
            return this.UPDATE("{0}", new SqlTable(this.Db, table));
        }

        public Builder UPDATE(SqlTable table)
        {
            return this.UPDATE("{0}", table);
        }

        public Builder UPDATE(SqlId id)
        {
            return this.UPDATE("{0}", id);
        }

        public Builder UPDATE(Builder subQuery)
        {
            var withAlias = new SqlId("subQuery");

            return this.WITH(withAlias, subQuery).UPDATE(withAlias);
        }

        public Builder SET(string format = null, params object[] args)
        {
            if (args == null)
            {
                return this.SET(format, DBNull.Value);
            }

            return this.AppendClause("SET", ",", format, args);
        }

        public Builder SET(string column, object value)
        {
            if (column.Contains('{') && column.Contains('}'))
            {
                return this.SET(column, new object[] { value });
            }

            return this.SET(new SqlId(column), value);
        }

        public Builder SET(SqlColumn column, object value)
        {
            return this.SET("{0} = {1}", column, value);
        }

        public Builder SET(SqlId id, object value)
        {
            return this.SET("{0} = {1}", id, value);
        }

        /**
         * Appends multiple SET clause's using the provided Dictionary.
         *
         * ```cs
         * 	Qb.UPDATE("Foo").SET
         * 	(
         * 		new Dictionary<string, object>
         * 		{
         * 			{"name", "Brad"},
         * 			{"age", 27}
         * 		}
         * 	);
         * ```
         */
        public Builder SET(Dictionary<string, object> dict)
        {
            foreach (var item in dict)
            {
                this.SET(item.Key, item.Value);
            }

            return this;
        }
    }
}
