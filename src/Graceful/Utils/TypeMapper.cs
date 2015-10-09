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

namespace Graceful.Utils
{
    using System;
    using System.Linq;
    using System.Data;
    using System.Reflection;
    using System.ComponentModel;
    using System.Collections.Generic;

    public static class TypeMapper
    {
        /**
         * Given a CLR Type, this will return the matching SqlDbType.
         *
         * ```cs
         *  var sqlDbType = TypeMapper.GetDBType(typeof(string));
         *  Console.WriteLine("The next line will say True, I promise :)");
         *  Console.WriteLine(sqlDbType == SqlDbType.NVarChar);
         * ```
         */
        public static SqlDbType GetDBType(Type clrType)
        {
            // Convert Nullables to their underlying type. For the purpose of
            // determining the correct SqlDbType we don't care about null.
            if (clrType.IsGenericType)
            {
                if (clrType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    clrType = Nullable.GetUnderlyingType(clrType);
                }
            }

            // Enums get special treatment, we simply store their integer value.
            if (clrType.IsEnum) return SqlDbType.Int;

            switch (clrType.FullName)
            {
                case "System.Int64":
                    return SqlDbType.BigInt;

                case "System.Int32":
                    return SqlDbType.Int;

                case "System.Int16":
                    return SqlDbType.SmallInt;

                case "System.Decimal":
                    return SqlDbType.Decimal;

                case "System.Double":
                    return SqlDbType.Float;

                case "System.Single":
                    return SqlDbType.Real;

                case "System.Byte":
                    return SqlDbType.TinyInt;

                case "System.Byte[]":
                    return SqlDbType.VarBinary;

                case "System.Boolean":
                    return SqlDbType.Bit;

                case "System.Char":
                case "System.Char[]":
                    return SqlDbType.Char;

                case "System.String":
                    return SqlDbType.NVarChar;

                case "System.DateTime":

                    // TODO: Detect server version, if less than 2008
                    // return just SqlDbType.DateTime but for now lets assume
                    // we are using a more recent version of SQL Server.
                    return SqlDbType.DateTime2;

                case "System.DateTimeOffset":

                    // TODO: Again this only valid for SQL 2008 and above.
                    return SqlDbType.DateTimeOffset;

                case "System.TimeSpan":

                    // TODO: Again this only valid for SQL 2008 and above.
                    return SqlDbType.Time;

                case "System.Guid":
                    return SqlDbType.UniqueIdentifier;

                case "System.Data.DataTable":
                    return SqlDbType.Structured;

                default:
                    throw new ArgumentOutOfRangeException
                    (
                        "clrType => " + clrType.FullName
                    );
            }
        }

        /**
         * Given a CLR Value, this will return the matching SqlDbType.
         *
         * ```cs
         *  var sqlDbType = TypeMapper.GetDBType("hello");
         *  Console.WriteLine("The next line will say True, I promise :)");
         *  Console.WriteLine(sqlDbType == SqlDbType.NVarChar);
         * ```
         */
        public static SqlDbType GetDBType(object clrValue)
        {
            return GetDBType(clrValue.GetType());
        }

        /**
         * Given a PropertyInfo, this will return the property's SqlDbType.
         *
         * ```cs
         *  class Foo { public string Bar { get; set; } }
         *  var sqlDbType = TypeMapper.GetDBType(typeof(Foo).GetProperty("Bar"));
         *  Console.WriteLine("The next line will say True, I promise :)");
         *  Console.WriteLine(sqlDbType == SqlDbType.NVarChar);
         * ```
         */
        public static SqlDbType GetDBType(PropertyInfo property)
        {
            return GetDBType(property.PropertyType);
        }

        /**
         * Given a SqlDbType, this will return the equivalent CLR Type.
         *
         * ```cs
         *  var clrType = TypeMapper.GetClrType(SqlDbType.NVarChar);
         *  Console.WriteLine("The next line will say True, I promise :)");
         *  Console.WriteLine(clrType == System.String);
         * ```
         */
        public static Type GetClrType(SqlDbType sqlType)
        {
            switch (sqlType)
            {
                case SqlDbType.BigInt:
                    return typeof(Int64);

                case SqlDbType.Int:
                    return typeof(Int32);

                case SqlDbType.SmallInt:
                    return typeof(Int16);

                case SqlDbType.Decimal:
                case SqlDbType.Money:
                case SqlDbType.SmallMoney:
                    return typeof(Decimal);

                case SqlDbType.Float:
                    return typeof(Double);

                case SqlDbType.Real:
                    return typeof(Single);

                case SqlDbType.TinyInt:
                    return typeof(Byte);

                case SqlDbType.Binary:
                case SqlDbType.Image:
                case SqlDbType.Timestamp:
                case SqlDbType.VarBinary:
                    return typeof(Byte[]);

                case SqlDbType.Bit:
                    return typeof(Boolean);

                case SqlDbType.Char:
                case SqlDbType.NChar:
                case SqlDbType.NText:
                case SqlDbType.NVarChar:
                case SqlDbType.Text:
                case SqlDbType.VarChar:
                case SqlDbType.Xml:
                    return typeof(String);

                case SqlDbType.DateTime:
                case SqlDbType.SmallDateTime:
                case SqlDbType.Date:
                case SqlDbType.DateTime2:
                    return typeof(DateTime);

                case SqlDbType.DateTimeOffset:
                    return typeof(DateTimeOffset);

                case SqlDbType.Time:
                    return typeof(TimeSpan);

                case SqlDbType.UniqueIdentifier:
                    return typeof(Guid);

                case SqlDbType.Variant:
                case SqlDbType.Udt:
                    return typeof(Object);

                case SqlDbType.Structured:
                    return typeof(DataTable);

                default:
                    throw new ArgumentOutOfRangeException
                    (
                        "sqlType => " + sqlType.ToString()
                    );
            }
        }

        /**
         * Given the string name of an SqlDbType, we return the Enum value.
         *
         * ```cs
         *  var sqlDbType = TypeMapper.GetSqlDbTypeFromString("nvarchar");
         *  Console.WriteLine("The next line will say True, I promise :)");
         *  Console.WriteLine(sqlDbType == SqlDbType.NVarChar);
         * ```
         */
        public static SqlDbType GetSqlDbTypeFromString(string sqlType)
        {
            return Enum.GetValues(typeof(SqlDbType)).Cast<SqlDbType>().ToList()
            .Single(type => type.ToString().ToLower() == sqlType.ToLower());
        }

        /**
         * Caches the list created by IsClrType.
         */
        private static List<Type> ClrTypes;

        /**
         * Given a Type, we will attempt to work out if the type is a built in
         * simple primative type, such as String, Decimal, Boolean, etc.
         *
         * ```cs
         *  if (TypeMapper.IsClrType(typeof(string)))
         *  {
         *  	Console.WriteLine("Yes, string is a built in Clr Type.");
         *  }
         *
         * 	if (!TypeMapper.IsClrType(typeof(Foo)))
         * 	{
         * 		Console.WriteLine("No, Foo is a user defined class.");
         * 	}
         * ```
         *
         * > NOTE: Neither String nor Decimal are primitives so using:
         * > type.IsPrimitive, does not work in all cases.
         */
        public static bool IsClrType(Type type)
        {
            if (ClrTypes == null)
            {
                var types = new List<Type>
                {
                    typeof(Enum),
                    typeof(String),
                    typeof(Char),
                    typeof(Guid),
                    typeof(Boolean),
                    typeof(Byte),
                    typeof(Int16),
                    typeof(Int32),
                    typeof(Int64),
                    typeof(Single),
                    typeof(Double),
                    typeof(Decimal),
                    typeof(SByte),
                    typeof(UInt16),
                    typeof(UInt32),
                    typeof(UInt64),
                    typeof(DateTime),
                    typeof(DateTimeOffset),
                    typeof(TimeSpan)
                };

                var nullTypes = types.Where(t => t.IsValueType)
                .Select(t => typeof(Nullable<>).MakeGenericType(t));

                ClrTypes = types.Concat(nullTypes).ToList();
            }

            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    type = Nullable.GetUnderlyingType(type);
                }
            }

            if (type.IsArray)
            {
                type = type.GetElementType();
            }

            return ClrTypes.Any(x => x.IsAssignableFrom(type));
        }

        /**
         * Given a Value, we will attempt to work out if the type is a built in
         * simple primative type, such as String, Decimal, Boolean...
         *
         * ```cs
         *  if (TypeMapper.IsClrType("hello"))
         *  {
         *  	Console.WriteLine("Yes, string is a built in Clr Type.");
         *  }
         *
         * 	if (!TypeMapper.IsClrType(new Foo()))
         * 	{
         * 		Console.WriteLine("No, Foo is a user defined class.");
         * 	}
         * ```
         *
         * > NOTE: Neither String nor Decimal are primitives so using:
         * > type.IsPrimitive, does not work in all cases.
         */
        public static bool IsClrType(object value)
        {
            return IsClrType(value.GetType());
        }

        /**
         * Given a PropertyInfo, we will attempt to work out if the type is a
         * built in simple primative type, such as String, Decimal, Boolean...
         *
         * ```cs
         *  class Foo : Model<Foo>
         *  {
         *  	public string Bar { get; set; }
         *  	public FuBar Baz { get; set; }
         *  }
         *
         *  if (TypeMapper.IsClrType(typeof(Foo).GetProperty("Bar")))
         *  {
         *  	Console.WriteLine("Yes, Bar is a built in Clr Type.");
         *  }
         *
         *  if (!TypeMapper.IsClrType(typeof(Foo).GetProperty("Baz")))
         *  {
         *  	Console.WriteLine("No, Baz is a user defined class.");
         *  }
         * ```
         *
         * > NOTE: Neither String nor Decimal are primitives so using:
         * > type.IsPrimitive, does not work in all cases.
         */
        public static bool IsClrType(PropertyInfo property)
        {
            return IsClrType(property.PropertyType);
        }

        /**
         * Given a type, we will tell you if it is a nullable.
         *
         * ```cs
         * 	int? Id;
         *  if (TypeMapper.IsNullable(typeof(Id))
         *  {
         *  	Console.WriteLine("Yep, its a nullable int.");
         *  }
         * ```
         */
        public static bool IsNullable(Type type)
        {
            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    return true;
                }
            }

            return false;
        }

        /**
         * Given a value, we will tell you if it is a nullable.
         *
         * ```cs
         * 	int? Id = 0;
         *  if (TypeMapper.IsNullable(Id)
         *  {
         *  	Console.WriteLine("Yep, its a nullable int.");
         *  }
         * ```
         */
        public static bool IsNullable<T>(T value)
        {
            if (value == null) return true;
            return IsNullable(typeof(T));
        }

        /**
         * Given a PropertyInfo instance, we will tell you if the
         * property type is nullable.
         *
         * ```cs
         *  class Foo
         *  {
         *  	public int? Bar { get; set; }
         *  }
         *
         *  if (TypeMapper.IsNullable(typeof(Foo).GetProperty("Bar"))
         *  {
         *  	Console.WriteLine("Yep, its a nullable int.");
         *  }
         * ```
         */
        public static bool IsNullable(PropertyInfo property)
        {
            return IsNullable(property.PropertyType);
        }

        /**
         * Given a Type, we will tell you if it is a Graceful Model.
         *
         * ```cs
         * 	class Foo {}
         * 	class Bar : Graceful.Model<Bar> {}
         *
         * 	TypeMapper.IsEntity(typeof(Foo)); // false
         *  TypeMapper.IsEntity(typeof(Bar)); // true
         * ```
         */
        public static bool IsEntity(Type type)
        {
            return type.IsSubclassOf(typeof(Model));
        }

        /**
         * Tells you if the value is an instance of a Graceful Model.
         *
         * ```cs
         * 	class Foo {}
         * 	class Bar : Graceful.Model<Bar> {}
         *
         * 	TypeMapper.IsEntity(new Foo()); // false
         *  TypeMapper.IsEntity(new Bar()); // true
         * ```
         */
        public static bool IsEntity(object value)
        {
            return IsEntity(value.GetType());
        }

        /**
         * Tells you if a property might contain an instance of a Graceful Model.
         *
         * ```cs
         * 	class Qux :  Graceful.Model<Qux> {}
         *
         * 	class Foo : Graceful.Model<Foo>
         * 	{
         * 		public string Bar { get; set; }
         * 		public Qux Baz { get; set; }
         * 	}
         *
         * 	TypeMapper.IsEntity(typeof(Foo).GetProperty("Bar")); // false
         *  TypeMapper.IsEntity(typeof(Foo).GetProperty("Baz")); // true
         * ```
         */
        public static bool IsEntity(PropertyInfo property)
        {
            return IsEntity(property.PropertyType);
        }

        /**
         * Given a type, we will tell you if it is a List or not.
         *
         * ```cs
         *  var names = new List<string>{ "Bob", "Fred" };
         *  if (TypeMapper.IsList(names.GetType())
         *  {
         *  	Console.WriteLine("Yep, we have a list.");
         *  }
         * ```
         */
        public static bool IsList(Type type)
        {
            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    return true;
                }

                if (type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    return true;
                }

                if (type.GetGenericTypeDefinition() == typeof(BindingList<>))
                {
                    return true;
                }
            }

            return false;
        }

        /**
         * Given a value, we will tell you if it is a List or not.
         *
         * ```cs
         *  var names = new List<string>{ "Bob", "Fred" };
         *  if (TypeMapper.IsList(names)
         *  {
         *  	Console.WriteLine("Yep, we have a list.");
         *  }
         * ```
         */
        public static bool IsList(object value)
        {
            return IsList(value.GetType());
        }

        /**
         * Given a PropertyInfo, we will tell you if it is a List or not.
         *
         * ```cs
         *  class Foo : Model<Foo>
         *  {
         *  	public string Bar { get; set; }
         *  	public List<string> Baz { get; set; }
         *  }
         *
         *  if (!TypeMapper.IsList(typeof(Foo).GetProperty("Bar"))
         *  {
         *  	Console.WriteLine("Nope, this is not a List.");
         *  }
         *
         *  if (TypeMapper.IsList(typeof(Foo).GetProperty("Baz"))
         *  {
         *  	Console.WriteLine("Yep, we have a list.");
         *  }
         * ```
         */
        public static bool IsList(PropertyInfo property)
        {
            return IsList(property.PropertyType);
        }

        /**
         * Given a Type, we check to see if it is a List of Graceful Entities.
         *
         * ```
         * 	var names = new List<string>{ "Bob", "Fred" };
         * 	if (!TypeMapper.IsListOfEntities(names.GetType()))
         * 	{
         * 		Console.WriteLine("Names is NOT a list of Graceful Entities.");
         * 	}
         *
         * 	var users = new List<User>
         * 	{
         * 		new User { Name = "Bob" },
         * 		new User { Name = "Fred" }
         * 	};
         * 	if (TypeMapper.IsListOfEntities(users.GetType()))
         * 	{
         * 		Console.WriteLine("But users is a list of Graceful Entities.");
         * 	}
         * ```
         */
        public static bool IsListOfEntities(Type type)
        {
            if (IsList(type))
            {
                if (type.GenericTypeArguments.Count() == 1)
                {
                    var innerType = type.GenericTypeArguments[0];

                    if (innerType.IsSubclassOf(typeof(Model)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /**
         * Given a Value, we check to see if it is a List of Graceful Entities.
         *
         * ```
         * 	var names = new List<string>{ "Bob", "Fred" };
         * 	if (!TypeMapper.IsListOfEntities(names))
         * 	{
         * 		Console.WriteLine("Names is NOT a list of Graceful Entities.");
         * 	}
         *
         * 	var users = new List<User>
         * 	{
         * 		new User { Name = "Bob" },
         * 		new User { Name = "Fred" }
         * 	};
         * 	if (TypeMapper.IsListOfEntities(users))
         * 	{
         * 		Console.WriteLine("But users is a list of Graceful Entities.");
         * 	}
         * ```
         */
        public static bool IsListOfEntities(object value)
        {
            return IsListOfEntities(value.GetType());
        }

        /**
         * Given a PropertyInfo, we check to see if it is
         * a List of Graceful Entities.
         *
         * ```cs
         *  class Foo : Model<Foo>
         *  {
         *  	public List<string> Bars { get; set; }
         *  	public List<Baz> Bazs { get; set; }
         *  }
         *
         *  if (!TypeMapper.IsListOfEntities(typeof(Foo).GetProperty("Bars"))
         *  {
         *  	Console.WriteLine("Not a List of Graceful Entities.");
         *  }
         *
         *  if (TypeMapper.IsListOfEntities(typeof(Foo).GetProperty("Bazs"))
         *  {
         *  	Console.WriteLine("Yep, we have a list of Graceful Entities.");
         *  }
         * ```
         */
        public static bool IsListOfEntities(PropertyInfo property)
        {
            return IsListOfEntities(property.PropertyType);
        }

        /**
         * Given a Type to check, and a specfic type of model
         * we will check to see if it is a List<modelType>.
         *
         * ```
         * 	var apples = new List<Apple>
         * 	{
         * 		new Apple { Type = "Pink Lady" },
         * 		new Apple { Type = "Granny Smith" }
         * 	};
         *
         *  var bananas = new List<Banana>
         * 	{
         * 		new Banana { Type = "Manzano" },
         * 		new Banana { Type = "Burro" }
         * 	};
         *
         * 	if (TypeMapper.IsListOfEntities(apples.GetType(), typeof(Apple)))
         * 	{
         * 		Console.WriteLine("Yep apples is a list of apples.");
         * 	}
         *
         * 	if (!TypeMapper.IsListOfEntities(apples.GetType(), typeof(Banana)))
         * 	{
         * 		Console.WriteLine("No apples is not a list of bananas.");
         * 	}
         * ```
         */
        public static bool IsListOfEntities(Type type, Type modelType)
        {
            if (IsListOfEntities(type))
            {
                if (type.GenericTypeArguments[0] == modelType)
                {
                    return true;
                }
            }

            return false;
        }

        /**
         * Given a Value to check, and a specfic type of model
         * we will check to see if it is a List<modelType>.
         *
         * ```
         * 	var apples = new List<Apple>
         * 	{
         * 		new Apple { Type = "Pink Lady" },
         * 		new Apple { Type = "Granny Smith" }
         * 	};
         *
         *  var bananas = new List<Banana>
         * 	{
         * 		new Banana { Type = "Manzano" },
         * 		new Banana { Type = "Burro" }
         * 	};
         *
         * 	if (TypeMapper.IsListOfEntities(apples, typeof(Apple)))
         * 	{
         * 		Console.WriteLine("Yep apples is a list of apples.");
         * 	}
         *
         * 	if (!TypeMapper.IsListOfEntities(apples, typeof(Banana)))
         * 	{
         * 		Console.WriteLine("No apples is not a list of bananas.");
         * 	}
         * ```
         */
        public static bool IsListOfEntities(object value, Type modelType)
        {
            return IsListOfEntities(value.GetType(), modelType);
        }

        /**
         * Given a PropertyInfo to check, and a specfic type of model
         * we will check to see if it is a List<modelType>.
         *
         * ```
         *  class Foo : Model<Foo>
         *  {
         *  	public List<Apple> Apples { get; set; }
         *  	public List<Banana> Bananas { get; set; }
         *  }
         *
         *  var applesProp = typeof(Foo).GetProperty("Apples");
         *
         * 	if (TypeMapper.IsListOfEntities(applesProp, typeof(Apple)))
         * 	{
         * 		Console.WriteLine("Yep apples is a list of apples.");
         * 	}
         *
         * 	if (!TypeMapper.IsListOfEntities(applesProp, typeof(Banana)))
         * 	{
         * 		Console.WriteLine("No apples is not a list of bananas.");
         * 	}
         * ```
         */
        public static bool IsListOfEntities(PropertyInfo property, Type modelType)
        {
            return IsListOfEntities(property.PropertyType, modelType);
        }
    }
}
