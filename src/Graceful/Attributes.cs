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
    using System.Data;

    /**
     * Models may provide a custom connection string.
     *
     * Lets say you had 2 sets of users, one set is stored in CompanyX's db
     * and another set of users is stored in CompanyY's db. This might be
     * represented like this:
     *
     * ```cs
     * 	using Graceful;
     *
     * 	public class User : Model<User>
     * 	{
     * 		public string UserName { get; set; }
     * 		public string Password { get; set; }
     * 	}
     *
     * 	[Connection("Server=CompanyX\SQLEXPRESS;Database=Users;...")]
     * 	public class UserX : User {}
     *
     * 	[Connection("Server=CompanyY\SQLEXPRESS;Database=Users;...")]
     * 	public class UserY : User {}
     * ```
     *
     * > NOTE: This is very much an alpha stage feature.
     */
    [AttributeUsage(AttributeTargets.Class)]
    public class ConnectionAttribute : Attribute
    {
        public readonly string Value;

        public ConnectionAttribute(string value)
        {
            this.Value = value;
        }
    }

    /**
     * Custom SQL Table Name
     *
     * By default the C# class name of the Model is used as the SQL table name.
     *
     * This may be overidden like so:
     * ```cs
     * 	using Graceful;
     *
     * 	[SqlTableName("Bar")]
     * 	public class Foo : Graceful.Model<Foo>
     * 	{
     *
     * 	}
     * ```
     */
    [AttributeUsage(AttributeTargets.Class)]
    public class SqlTableNameAttribute : Attribute
    {
        public readonly string Value;

        public SqlTableNameAttribute(string value)
        {
            this.Value = value;
        }
    }

    /**
     * Custom SQL Type Parameter
     *
     * Each property of a model is mapped to a column in an SQL table. The type
     * of the property is also mapped through to the column. Sometimes you may
     * wish to overide the automatically choosen SQL Type.
     *
     * Consider the following example:
     * ```cs
     * 	using Graceful;
     * 	using System.Data;
     *
     * 	public class Foo : Model<Foo>
     * 	{
     * 		public string Bar { get; set; }
     *
     * 		[SqlType(SqlDbType.VarChar)]
     * 		public string Baz { get; set; }
     * 	}
     * ```
     *
     * Results in the following table schema:
     * ```sql
     * 	CREATE TABLE Foo
     *  (
     * 		Bar NVARCHAR(MAX),
     * 		Baz VARCHAR(MAX)
     *  );
     * ```
     */
    [AttributeUsage(AttributeTargets.Property)]
    public class SqlTypeAttribute : Attribute
    {
        public readonly SqlDbType Value;

        public SqlTypeAttribute(SqlDbType value)
        {
            this.Value = value;
        }
    }

    /**
     * Custom SQL Length Parameters
     *
     * Each property of a model is mapped to a column in an SQL table. The type
     * of the property is also mapped through to the column. Some SQL Types have
     * additional _"length"_ parameters that can be supplied.
     *
     * Consider the following example:
     * ```cs
     * 	using Graceful;
     *
     * 	public class Foo : Model<Foo>
     * 	{
     * 		public string Bar { get; set; }
     *
     * 		[SqlLength("(50)")]
     * 		public string Baz { get; set; }
     * 	}
     * ```
     *
     * Results in the following table schema:
     * ```sql
     * 	CREATE TABLE Foo
     *  (
     * 		Bar NVARCHAR(MAX),
     * 		Baz NVARCHAR(50)
     *  );
     * ```
     *
     * The value you provide to this attribute is appended directly after the
     * SQL Type so you must include the brackets and any other formatting that
     * might be required, depending on the SQL Type.
     *
     * > NOTE: Some SQL Types such as _NVARCHAR_ will automatically get _(MAX)_.
     */
    [AttributeUsage(AttributeTargets.Property)]
    public class SqlLengthAttribute : Attribute
    {
        public readonly string Value;

        public SqlLengthAttribute(string value)
        {
            this.Value = value;
        }
    }

    /**
     * Creates a UNIQUE Constraint on the Property Column.
     *
     * If your model has a property that's value should only ever appear once.
     * You may use this attribute to set a UNIQUE Constraint on the SQL Column.
     *
     * Consider the following example:
     * ```cs
     * 	using Graceful;
     *
     * 	public class Foo : Model<Foo>
     * 	{
     * 		[Unique]
     * 		public string Bar { get; set; }
     * 	}
     * ```
     *
     * Effectively results in the following table schema:
     * ```sql
     * 	CREATE TABLE Foo
     * 	(
     * 		Bar NVARCHAR(MAX) UNIQUE
     * 	);
     * ```
     *
     * In typcial Microsoft fashion SQL Server does not support the standard
     * ANSI UNIQUE logic. By default SQL Server treats "NULL" as a value and
     * thus you can only have a single "NULL" value in your unique column.
     * For more info on this see: http://dba.stackexchange.com/questions/80514
     *
     * So Graceful will actually create a _"Filtered"_ unique index for you.
     * if you really do want to stick with Microsoft's definition of unique
     * you may set the struct value to true.
     *
     * ```cs
     *  using Graceful;
     *
     * 	public class Foo : Model<Foo>
     * 	{
     * 		[Unique(strict: true)]
     * 		public string Bar { get; set; }
     * 	}
     * ```
     *
     * > NOTE: This won;t actually create a traditional UNIQUE contraint.
     * > It will just create an _"UnFiltered"_ UNIQUE INDEX, behind the scenes
     * > I imagine this is all a UNIQUE contraint would do anyway.
     */
    [AttributeUsage(AttributeTargets.Property)]
    public class UniqueAttribute : Attribute
    {
        public readonly bool Strict;

        public UniqueAttribute(bool strict = false)
        {
            this.Strict = strict;
        }
    }

    /**
     * Exclude a property from being mapped to the database.
     *
     * By default all properties that have public setters will be considered
     * to be a _"MappedProperty"_. Sometimes though we may need to setup a
     * property that only has any meaning at runtime, ie: it's state is never
     * saved to disc.
     *
     * ```cs
     * 	using Graceful;
     *
     * 	public class Foo : Model<Foo>
     * 	{
     * 		public string IAmMapped { get; set; }
     *
     * 		public string IAmNOTMapped { get; protected set; }
     *
     * 		public string IAmNOTMapped2 { get; private set; }
     *
     * 		protected string IAmNOTMapped3 { get; set; }
     *
     * 		private string IAmNOTMapped4 { get; set; }
     *
     * 		[NotMapped]
     * 		public string IAmNOTMapped5 { get; set; }
     * 	}
     * ```
     */
    [AttributeUsage(AttributeTargets.Property)]
    public class NotMappedAttribute : Attribute
    {
    }

    /**
     * Explicitly define the remote property of a relationship.
     *
     * For the most part the _"RelationshipDiscoverer"_ does this
     * automatically for you and you are free to define your relationships
     * following Graceful's logical conventions without any configuration.
     *
     * However if you need or wish to explicitly tell Graceful about
     * the remote side of your relationship you may do so like this:
     * ```cs
     * 	using Graceful;
     *
     * 	public class User : Model<User>
     * 	{
     * 		[InverseProperty("UsersThatAreAMember")]
     * 		public IList<Group> GroupsIBelongTo { get; set; }
     * 	}
     *
     * 	public class Group : Model<Group>
     * 	{
     * 		[InverseProperty("GroupsIBelongTo")]
     * 		public IList<User> UsersThatAreAMember { get; set; }
     * 	}
     * ```
     *
     * > NOTE: In the example we show that both sides define their Inverse.
     * > While this is totally fine, you can omit one of the InverseProperty
     * > attributes and the _"RelationshipDiscoverer"_ will still be able to
     * > discover the relationship for you.
     */
    [AttributeUsage(AttributeTargets.Property)]
    public class InversePropertyAttribute : Attribute
    {
        public readonly string Value;

        public InversePropertyAttribute(string value)
        {
            this.Value = value;
        }
    }
}
