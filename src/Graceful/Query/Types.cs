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
    using System.Text;
    using System.Linq;
    using System.Data.SqlClient;

    /**
     * Used by the OrderBy methods in Query.Linq
     */
    public enum OrderDirection
    {
        ASC,
        DESC
    }

    /**
     * An SqlId is a identifier used in an Sql Query, such as a table name
     * or column name. This enables us to use "variable" or "parametized"
     * identifiers with the Query Helper and Builder.
     *
     * ```cs
     * 	var ctx = new Context("cs");
     * 	var helper = new Query.Helper(ctx);
     * 	helper.BuildCmd
     * 	(
     * 		"SELECT @col1 FROM @tableName WHERE @col2 = @value",
     * 		new Dictionary<string, object>
     * 		{
     * 			{"@col1", new SqlId("FirstName")},
     * 			{"@tableName", new SqlId("Users")},
     * 			{"@col2", new SqlId("Id")},
     * 			{"@value", 123}
     * 		}
     * 	);
     * ```
     *
     * A Many to Many Query Builder Example:
     * ```cs
     * 	var ctx = new Context("cs");
     * 	var builder = new Query.Builder(ctx);
     *
     * 	var id = 123; // NOTICE how the id is passed as a normal value.
     * 	var col1 = new SqlId(relation.PivotTableFirstColumnName);
     * 	var col2 = new SqlId(relation.PivotTableSecondColumnName);
     * 	var localTable = new SqlId(relation.LocalTableName);
     * 	var foreignTable = new SqlId(relation.ForeignTableName);
     * 	var pivotTable = new SqlId(relation.PivotTableName);
     *
     * 	var records = builder
     * 	.SELECT("{0}.*", foreignTable)
     * 	.FROM(foreignTable)
     * 	.INNER_JOIN("{0} ON {0}.{1} = {2}.Id", pivotTable, col2, foreignTable)
     * 	.INNER_JOIN("{2} ON {0}.{1} = {2}.Id", pivotTable, col1, localTable)
     * 	.WHERE("{0}.Id = {1}", localTable, id)
     * 	.Rows;
     * ```
     *
     * The Query Helper will detect you have supplied some parameters that are
     * actually Sql Indentifiers and not value parameters, it will inline the
     * SqlId's so that query will execute as you expect.
     *
     * > NOTE: All SqlId's are escaped using the QuoteIdentifier method.
     */
    public class SqlId
    {
        public string Value { get; protected set; }

        public SqlId(string value)
        {
            var sb = new StringBuilder();

            var cmdBuilder = new SqlCommandBuilder();

            value.Split('.').ToList().ForEach(segment =>
            {
                sb.Append(cmdBuilder.QuoteIdentifier(segment));
                sb.Append('.');
            });

            if (sb[sb.Length - 1] == '.')
            {
                sb.Remove(sb.Length - 1, 1);
            }

            this.Value = sb.ToString();
        }
    }

    /**
     * Extends on the idea of an SqlId, and provides a fully qualified escaped
     * table name. The cavert though is we need a copy of the Context so we can
     * work out the current database / schema.
     *
     * ```cs
     * 	var ctx = new Context("Database=Graceful;");
     * 	var table = new SqlTable(ctx, "Foo");
     * 	Console.WriteLine(table.Value);
     * 	// outputs something like: [Graceful].[dbo].[Foo]
     * ```
     */
    public class SqlTable : SqlId
    {
        public SqlTable(Context db, string table) : base(Qualified(db, table)){}

        /**
         * Given a table / view name, this will return the fully qualified name.
         *
         * > TODO: Don't make assumption schema is "dbo".
         */
        protected static string Qualified(Context db, string table)
        {
            string qualifiedName;

            if (table.Contains("INFORMATION_SCHEMA.") || table.Contains("sys."))
            {
                qualifiedName = db.DatabaseName + "." + table;
            }
            else
            {
                qualifiedName = db.DatabaseName + ".dbo." + table;
            }

            return qualifiedName;
        }
    }

    /**
     * Pretty obvious, this extends on SqlTable to provide a fully qualified
     * and escaped column name. We need the three parts, Db Context, Table Name
     * & Column Name.
     *
     * ```cs
     * 	var ctx = new Context("Database=Graceful;");
     * 	var column = new SqlColumn(ctx, "Foo", "Bar");
     * 	Console.WriteLine(column.Value);
     * 	// outputs something like: [Graceful].[dbo].[Foo].[Bar]
     * ```
     */
    public class SqlColumn : SqlTable
    {
        public SqlColumn(Context db, string table, string column)
        : base(db, table + "." + column){}
    }
}
