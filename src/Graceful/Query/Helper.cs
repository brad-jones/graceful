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

namespace Graceful.Query
{
    using System;
    using System.Linq;
    using System.Data;
    using Graceful.Utils;
    using Graceful.Extensions;
    using System.Data.SqlClient;
    using System.Collections.Generic;

    public class Helper
    {
        /**
         * The instance of the Context that we will be using.
         */
        protected readonly Context Db;

        /**
         * You must inject an instance of the Context so we
         * can connect to the database and execute queries.
         */
        public Helper(Context Db)
        {
            this.Db = Db;
        }

        /**
         * Builds a new SqlCommand ready to be executed.
         *
         * ```cs
         * 	var ctx = new Context();
         * 	var helper = new Query.Helper(ctx);
         * 	var cmd = helper.BuildCmd
         * 	(
         * 		"SELECT * FROM people WHERE surname = @surname AND age > @age",
         * 		new Dictionary<string, object>
         * 		{
         * 			{ "@surname", "Jones" },
         * 			{ "@age", 27 }
         * 		}
         * 	);
         * ```
         *
         * > NOTE: You should dispose of the command once you have executed it.
         */
        public SqlCommand BuildCmd(string query, Dictionary<string, object> parameters = null)
        {
            query = this.InlineSqlIds(query, parameters);

            var cmd = new SqlCommand(query, this.Db.Connection);

            if (parameters != null)
            {
                foreach(var parameter in parameters)
                {
                    if (parameter.Value.GetType() == typeof(DBNull))
                    {
                        cmd.Parameters.AddWithValue
                        (
                            parameter.Key,
                            parameter.Value
                        );
                    }
                    else
                    {
                        cmd.Parameters.Add
                        (
                            parameterName: parameter.Key,
                            sqlDbType: TypeMapper.GetDBType(parameter.Value)
                        ).Value = parameter.Value;
                    }
                }
            }

            return cmd;
        }

        /**
         * Returns a new SqlDataReader for your query.
         *
         * ```cs
         * 	var ctx = new Context();
         * 	var helper = new Query.Helper(ctx);
         * 	using (var reader = helper.Read("SELECT * FROM Foo"))
         * 	{
         * 		...read the reader...
         * 	}
         * ```
         *
         * > NOTE: Assuming you wrap the Query call in a "using" we will
         * > automatically close and dispose of both the SqlCommand and the
         * > SqlConnection used in the making of the reader.
         */
        public SqlDataReader Read(string query, Dictionary<string, object> parameters = null)
        {
            using (var cmd = this.BuildCmd(query, parameters))
            {
                return this.GetReader(cmd);
            }
        }

        /**
         * Returns a loaded DataTable with your Query Results.
         *
         * ```cs
         * 	var ctx = new Context();
         * 	var helper = new Query.Helper(ctx);
         * 	using (var dt = helper.ReadToDt("SELECT * FROM Bar"))
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
        public DataTable ReadToDt(string query, Dictionary<string, object> parameters = null)
        {
            var dt = new DataTable();

            using (var reader = this.Read(query, parameters))
            {
                dt.Load(reader);
            }

            return dt;
        }

        /**
         * Returns the scalar result of your query.
         *
         * ```cs
         * 	var ctx = new Context();
         * 	var helper = new Query.Helper(ctx);
         * 	var id = helper.ReadToScalar("SELECT id FROM foo WHERE x=y");
         * ```
         *
         * > NOTE: The SqlCommand & SqlConnection are disposed of for you.
         */
        public object ReadToScalar(string query, Dictionary<string, object> parameters = null)
        {
            object result;

            using (var cmd = this.BuildCmd(query, parameters))
            {
                result = this.GetScalar(cmd);
            }

            return result;
        }

        /**
         * Executes your query which acts on the db but does not query it.
         * Eg: Inserting, Updating, Deleting, etc
         *
         * ```cs
         * 	var ctx = new Context();
         * 	var helper = new Query.Helper(ctx);
         * 	var rowsEffected = helper.Execute("DELETE FROM foo WHERE x=y");
         * ```
         *
         * > NOTE: The SqlCommand & SqlConnection are disposed of for you.
         */
        public int Execute(string query, Dictionary<string, object> parameters = null)
        {
            int result;

            using (var cmd = this.BuildCmd(query, parameters))
            {
                result = this.Execute(cmd);
            }

            return result;
        }

        /**
         * Returns a list, where each row is represented by a Dictionary.
         *
         * ```cs
         * 	var ctx = new Context();
         * 	var helper = new Query.Helper(ctx);
         * 	helper.GetRows("SELECT * FROM People").ForEach(row =>
         * 	{
         * 		row.Keys.ToList().ForEach(column =>
         * 		{
         * 			var value = row[column];
         * 		});
         * 	});
         * ```
         */
        public List<Dictionary<string, object>> GetRows(string query, Dictionary<string, object> parameters = null)
        {
            List<Dictionary<string, object>> list;

            using (DataTable dt = this.ReadToDt(query, parameters))
            {
                list = this.GetRows(dt);
            }

            return list;
        }

        /**
         * Returns the first row of a result set represented by a Dictionary.
         *
         * ```cs
         * 	var ctx = new Context();
         * 	var helper = new Query.Helper(ctx);
         * 	var person = helper.GetRow("SELECT * FROM People WHERE Id = 10");
         * ```
         */
        public Dictionary<string, object> GetRow(string query, Dictionary<string, object> parameters = null)
        {
            return this.GetRows(query, parameters).First();
        }

        /**
         * Check to see if the table exists in the database.
         *
         * ```cs
         * 	var ctx = new Context();
         * 	var helper = new Query.Helper(ctx);
         * 	if (helper.TableExists("FooBar"))
         * 	{
         * 		// The Table FooBar exists
         * 	}
         * 	else
         * 	{
         * 		// The Table FooBar does not exist
         * 	}
         * ```
         */
        public bool TableExists(string tableName)
        {
            return (int)this.ReadToScalar
            (
                "SELECT COUNT(*)\n" +
                "FROM @dbName.[INFORMATION_SCHEMA].[TABLES]\n" +
                "WHERE [TABLE_NAME] = @value",
                new Dictionary<string, object>
                {
                    {"@dbName", new SqlId(this.Db.DatabaseName)},
                    {"@value", tableName}
                }
            ) > 0 ? true : false;
        }

        /**
         * Check to see if the table is empty or not.
         *
         * ```cs
         * 	var ctx = new Context();
         * 	var helper = new Query.Helper(ctx);
         * 	if (helper.TableEmpty("FooBar"))
         * 	{
         * 		// The table FooBar is Empty, contains no records.
         * 	}
         * 	else
         * 	{
         * 		// The table FooBar is not Empty, does contains some records.
         * 	}
         * ```
         */
        public bool TableEmpty(string tableName)
        {
            return (int)this.ReadToScalar
            (
                "SELECT COUNT(*) FROM @value",
                new Dictionary<string, object>
                {
                    {"@value", new SqlId(tableName)}
                }
            ) == 0 ? true : false;
        }

        /**
         * Check to see if a column exists in a given table.
         *
         * ```cs
         * 	var ctx = new Context();
         * 	var helper = new Query.Helper(ctx);
         * 	if (helper.ColumnExists("FooBar", "Baz"))
         * 	{
         * 		// The table FooBar contains the column Baz.
         * 	}
         * 	else
         * 	{
         * 		// The table FooBar does not contain the column Baz.
         * 	}
         * ```
         */
        public bool ColumnExists(string tableName, string colName)
        {
            return (int)this.ReadToScalar
            (
                "SELECT COUNT(*)\n" +
                "FROM @dbName.[INFORMATION_SCHEMA].[COLUMNS]\n" +
                "WHERE [TABLE_NAME] = @tableName " +
                "AND [COLUMN_NAME] = @colName",
                new Dictionary<string, object>
                {
                    {"@dbName", new SqlId(this.Db.DatabaseName)},
                    {"@tableName", tableName},
                    {"@colName", colName}
                }
            ) > 0 ? true : false;
        }

        /**
         * Returns the current SqlDbType of the given column.
         *
         * ```cs
         * 	var ctx = new Context();
         * 	var helper = new Query.Helper(ctx);
         * 	var type = helper.ColumnDataType("FooBar", "Baz");
         *
         * 	// ie: SqlDbType.NVarChar
         * ```
         */
        public SqlDbType ColumnDataType(string tableName, string colName)
        {
            var dataType = (string)this.ReadToScalar
            (
                "SELECT data_type \n" +
                "FROM information_schema.columns \n" +
                "WHERE table_catalog = @dbName " +
                "AND table_name = @tableName " +
                "AND column_name = @colName",
                new Dictionary<string, object>
                {
                    {"@dbName", this.Db.DatabaseName},
                    {"@tableName", tableName},
                    {"@colName", colName}
                }
            );

            return TypeMapper.GetSqlDbTypeFromString(dataType);
        }

        /**
         * Inlines SqlId Parameters.
         *
         * > NOTE: See the Graceful.Query.SqlId class docs for more info.
         */
        protected string InlineSqlIds(string query, Dictionary<string, object> parameters)
        {
            // If the query has no parameters then we don't have anything to do.
            if (parameters == null || parameters.Count == 0) return query;

            // Grab a list of parameters that are SqlIds.
            parameters.Where
            (
                p =>
                    p.Value.GetType() == typeof(SqlId) ||
                    p.Value.GetType().IsSubclassOf(typeof(SqlId))
            )
            .ToList().ForEach(parameter =>
            {
                // And inline the SqlId Value into the query string,
                // as an SqlId can not be sent as a SqlParameter.
                query = query.Replace
                (
                    parameter.Key,
                    ((SqlId)parameter.Value).Value
                );

                // Remove the parameter from the original parameters dict.
                parameters.Remove(parameter.Key);
            });

            return query;
        }

        /**
         * Given a SqlCommand and a way to execute that command,
         * we finally run the query. Used internally by GetReader, GetScarlar
         * and Execute methods.
         *
         * ```cs
         * 	var cmd = this.BuildCmd("SELECT * FROM Foo");
         * 	this.RunQuery
         * 	(
         * 		cmd,
         * 		query => query.ExecuteReader / ExecuteScalar / ExecuteNonQuery
         * 		affected =>
         * 		{
         * 			// given the result from the above execute method
         * 			// you then need to return the number of affected rows.
         * 		},
         * 		dispose: true // do you want the connection disposed afterwards
         * 	);
         * ```
         */
        protected object RunQuery(SqlCommand cmd, Func<SqlCommand, object> query, Func<object, int> affected, bool dispose = true)
        {
            object results;

            try
            {
                results = query.Invoke(cmd);

                if (this.Db.LogWriter != null)
                {
                    this.Db.LogWriter.WriteLine
                    (
                        cmd.ToTraceString
                        (
                            affected.Invoke(results)
                        )
                    );
                }
            }
            catch
            {
                if (this.Db.LogWriter != null)
                {
                    this.Db.LogWriter.WriteLine("-- !!!NEXT QUERY ERRORED!!!");
                    this.Db.LogWriter.WriteLine(cmd.ToTraceString());
                }

                throw;
            }
            finally
            {
                if (dispose) cmd.Connection.Dispose();
            }

            return results;
        }

        /**
         * Given a SqlCommand, we will return a SqlReader.
         */
        protected SqlDataReader GetReader(SqlCommand cmd)
        {
            return (SqlDataReader)this.RunQuery
            (
                cmd,
                query => query.ExecuteReader(CommandBehavior.CloseConnection),
                affected => ((SqlDataReader)affected).RecordsAffected,
                dispose: false
            );
        }

        /**
         * Given a SqlCommand, we will run an ExecuteScalar Operation.
         */
        protected object GetScalar(SqlCommand cmd)
        {
            return this.RunQuery
            (
                cmd,
                query =>
                {
                    var result = query.ExecuteScalar();
                    if (result is DBNull) return null;
                    return result;
                },
                affected => 0
            );
        }

        /**
         * Given a SqlCommand, we will run an ExecuteNonQuery Operation.
         */
        protected int Execute(SqlCommand cmd)
        {
            return (int)this.RunQuery
            (
                cmd,
                query => query.ExecuteNonQuery(),
                affected => (int)affected
            );
        }

        /**
         * Given a DataTable, we will return a List of Dictionary<string, object>s.
         */
        protected List<Dictionary<string, object>> GetRows(DataTable dt)
        {
            var list = new List<Dictionary<string, object>>();

            foreach (DataRow row in dt.Rows)
            {
                var result = new Dictionary<string, object>();

                foreach (DataColumn column in dt.Columns)
                {
                    if (!(row[column] is DBNull))
                    {
                        result.Add(column.ColumnName, row[column]);
                    }
                }

                list.Add(result);
            }

            return list;
        }
    }
}
