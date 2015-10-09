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

namespace Graceful.Tests
{
    using Xunit;
    using System;
    using System.Data;
    using Graceful.Utils;
    using System.Collections.Generic;

    public class TypeMapperTests
    {
        class IsClrTypeTestClass {}

        class UserTestClass : Model
        {
            public string Name { get; set; }
        }

        [Fact]
        public void GetDBTypeTest()
        {
            Assert.Equal(SqlDbType.BigInt, TypeMapper.GetDBType(typeof(Int64)));
            Assert.Equal(SqlDbType.Int, TypeMapper.GetDBType(typeof(Int32)));
            Assert.Equal(SqlDbType.SmallInt, TypeMapper.GetDBType(typeof(Int16)));
            Assert.Equal(SqlDbType.Decimal, TypeMapper.GetDBType(typeof(Decimal)));
            Assert.Equal(SqlDbType.Float, TypeMapper.GetDBType(typeof(Double)));
            Assert.Equal(SqlDbType.Real, TypeMapper.GetDBType(typeof(Single)));
            Assert.Equal(SqlDbType.TinyInt, TypeMapper.GetDBType(typeof(Byte)));
            Assert.Equal(SqlDbType.VarBinary, TypeMapper.GetDBType(typeof(Byte[])));
            Assert.Equal(SqlDbType.Bit, TypeMapper.GetDBType(typeof(Boolean)));
            Assert.Equal(SqlDbType.Char, TypeMapper.GetDBType(typeof(Char)));
            Assert.Equal(SqlDbType.Char, TypeMapper.GetDBType(typeof(Char[])));
            Assert.Equal(SqlDbType.NVarChar, TypeMapper.GetDBType(typeof(String)));
            Assert.Equal(SqlDbType.DateTime2, TypeMapper.GetDBType(typeof(DateTime)));
            Assert.Equal(SqlDbType.DateTimeOffset, TypeMapper.GetDBType(typeof(DateTimeOffset)));
            Assert.Equal(SqlDbType.Time, TypeMapper.GetDBType(typeof(TimeSpan)));
            Assert.Equal(SqlDbType.UniqueIdentifier, TypeMapper.GetDBType(typeof(Guid)));
            Assert.Equal(SqlDbType.Structured, TypeMapper.GetDBType(typeof(DataTable)));
        }

        [Fact]
        public void GetDBTypeNullableTest()
        {
            Assert.Equal(SqlDbType.BigInt, TypeMapper.GetDBType(typeof(Int64?)));
            Assert.Equal(SqlDbType.Int, TypeMapper.GetDBType(typeof(Int32?)));
            Assert.Equal(SqlDbType.SmallInt, TypeMapper.GetDBType(typeof(Int16?)));
            Assert.Equal(SqlDbType.Decimal, TypeMapper.GetDBType(typeof(Decimal?)));
            Assert.Equal(SqlDbType.Float, TypeMapper.GetDBType(typeof(Double?)));
            Assert.Equal(SqlDbType.Real, TypeMapper.GetDBType(typeof(Single?)));
            Assert.Equal(SqlDbType.TinyInt, TypeMapper.GetDBType(typeof(Byte?)));
            Assert.Equal(SqlDbType.Bit, TypeMapper.GetDBType(typeof(Boolean?)));
            Assert.Equal(SqlDbType.Char, TypeMapper.GetDBType(typeof(Char?)));
            Assert.Equal(SqlDbType.DateTime2, TypeMapper.GetDBType(typeof(DateTime?)));
            Assert.Equal(SqlDbType.DateTimeOffset, TypeMapper.GetDBType(typeof(DateTimeOffset?)));
            Assert.Equal(SqlDbType.Time, TypeMapper.GetDBType(typeof(TimeSpan?)));
            Assert.Equal(SqlDbType.UniqueIdentifier, TypeMapper.GetDBType(typeof(Guid?)));
        }

        [Fact]
        public void GetDBTypeFromValueTest()
        {
            Assert.Equal(SqlDbType.BigInt, TypeMapper.GetDBType((Int64)1));
            Assert.Equal(SqlDbType.Int, TypeMapper.GetDBType((Int32)1));
            Assert.Equal(SqlDbType.SmallInt, TypeMapper.GetDBType((Int16)1));
            Assert.Equal(SqlDbType.Decimal, TypeMapper.GetDBType((Decimal)1.2));
            Assert.Equal(SqlDbType.Float, TypeMapper.GetDBType((Double)1.2));
            Assert.Equal(SqlDbType.Real, TypeMapper.GetDBType((Single)1.2));
            Assert.Equal(SqlDbType.TinyInt, TypeMapper.GetDBType((Byte)1));
            Assert.Equal(SqlDbType.VarBinary, TypeMapper.GetDBType(new Byte[]{1,2,3}));
            Assert.Equal(SqlDbType.Bit, TypeMapper.GetDBType(true));
            Assert.Equal(SqlDbType.Char, TypeMapper.GetDBType('a'));
            Assert.Equal(SqlDbType.Char, TypeMapper.GetDBType(new Char[]{'a', 'b'}));
            Assert.Equal(SqlDbType.NVarChar, TypeMapper.GetDBType("abc"));
            Assert.Equal(SqlDbType.DateTime2, TypeMapper.GetDBType(DateTime.UtcNow));
            Assert.Equal(SqlDbType.DateTimeOffset, TypeMapper.GetDBType(DateTimeOffset.Now));
            Assert.Equal(SqlDbType.Time, TypeMapper.GetDBType(TimeSpan.FromDays(1)));
            Assert.Equal(SqlDbType.UniqueIdentifier, TypeMapper.GetDBType(Guid.NewGuid()));
            Assert.Equal(SqlDbType.Structured, TypeMapper.GetDBType(new DataTable()));
        }

        [Fact]
        public void GetClrTypeTest()
        {
            Assert.Equal(typeof(Int64), TypeMapper.GetClrType(SqlDbType.BigInt));
            Assert.Equal(typeof(Int32), TypeMapper.GetClrType(SqlDbType.Int));
            Assert.Equal(typeof(Int16), TypeMapper.GetClrType(SqlDbType.SmallInt));
            Assert.Equal(typeof(Decimal), TypeMapper.GetClrType(SqlDbType.Decimal));
            Assert.Equal(typeof(Decimal), TypeMapper.GetClrType(SqlDbType.Money));
            Assert.Equal(typeof(Decimal), TypeMapper.GetClrType(SqlDbType.SmallMoney));
            Assert.Equal(typeof(Double), TypeMapper.GetClrType(SqlDbType.Float));
            Assert.Equal(typeof(Single), TypeMapper.GetClrType(SqlDbType.Real));
            Assert.Equal(typeof(Byte), TypeMapper.GetClrType(SqlDbType.TinyInt));
            Assert.Equal(typeof(Byte[]), TypeMapper.GetClrType(SqlDbType.Binary));
            Assert.Equal(typeof(Byte[]), TypeMapper.GetClrType(SqlDbType.Image));
            Assert.Equal(typeof(Byte[]), TypeMapper.GetClrType(SqlDbType.Timestamp));
            Assert.Equal(typeof(Byte[]), TypeMapper.GetClrType(SqlDbType.VarBinary));
            Assert.Equal(typeof(Boolean), TypeMapper.GetClrType(SqlDbType.Bit));
            Assert.Equal(typeof(String), TypeMapper.GetClrType(SqlDbType.Char));
            Assert.Equal(typeof(String), TypeMapper.GetClrType(SqlDbType.NChar));
            Assert.Equal(typeof(String), TypeMapper.GetClrType(SqlDbType.NText));
            Assert.Equal(typeof(String), TypeMapper.GetClrType(SqlDbType.NVarChar));
            Assert.Equal(typeof(String), TypeMapper.GetClrType(SqlDbType.Text));
            Assert.Equal(typeof(String), TypeMapper.GetClrType(SqlDbType.VarChar));
            Assert.Equal(typeof(String), TypeMapper.GetClrType(SqlDbType.Xml));
            Assert.Equal(typeof(DateTime), TypeMapper.GetClrType(SqlDbType.DateTime));
            Assert.Equal(typeof(DateTime), TypeMapper.GetClrType(SqlDbType.SmallDateTime));
            Assert.Equal(typeof(DateTime), TypeMapper.GetClrType(SqlDbType.Date));
            Assert.Equal(typeof(DateTime), TypeMapper.GetClrType(SqlDbType.DateTime2));
            Assert.Equal(typeof(DateTimeOffset), TypeMapper.GetClrType(SqlDbType.DateTimeOffset));
            Assert.Equal(typeof(TimeSpan), TypeMapper.GetClrType(SqlDbType.Time));
            Assert.Equal(typeof(Guid), TypeMapper.GetClrType(SqlDbType.UniqueIdentifier));
            Assert.Equal(typeof(Object), TypeMapper.GetClrType(SqlDbType.Variant));
            Assert.Equal(typeof(Object), TypeMapper.GetClrType(SqlDbType.Udt));
            Assert.Equal(typeof(DataTable), TypeMapper.GetClrType(SqlDbType.Structured));
        }

        [Fact]
        public void GetSqlDbTypeFromStringTest()
        {
            Assert.Equal(SqlDbType.BigInt, TypeMapper.GetSqlDbTypeFromString("BigInt"));
            Assert.Equal(SqlDbType.Int, TypeMapper.GetSqlDbTypeFromString("Int"));
            Assert.Equal(SqlDbType.SmallInt, TypeMapper.GetSqlDbTypeFromString("SmallInt"));
            Assert.Equal(SqlDbType.Decimal, TypeMapper.GetSqlDbTypeFromString("Decimal"));
            Assert.Equal(SqlDbType.Float, TypeMapper.GetSqlDbTypeFromString("Float"));
            Assert.Equal(SqlDbType.Real, TypeMapper.GetSqlDbTypeFromString("Real"));
            Assert.Equal(SqlDbType.TinyInt, TypeMapper.GetSqlDbTypeFromString("TinyInt"));
            Assert.Equal(SqlDbType.VarBinary, TypeMapper.GetSqlDbTypeFromString("VarBinary"));
            Assert.Equal(SqlDbType.Bit, TypeMapper.GetSqlDbTypeFromString("Bit"));
            Assert.Equal(SqlDbType.Char, TypeMapper.GetSqlDbTypeFromString("Char"));
            Assert.Equal(SqlDbType.NVarChar, TypeMapper.GetSqlDbTypeFromString("NVarChar"));
            Assert.Equal(SqlDbType.DateTime2, TypeMapper.GetSqlDbTypeFromString("DateTime2"));
            Assert.Equal(SqlDbType.DateTimeOffset, TypeMapper.GetSqlDbTypeFromString("DateTimeOffset"));
            Assert.Equal(SqlDbType.Time, TypeMapper.GetSqlDbTypeFromString("Time"));
            Assert.Equal(SqlDbType.UniqueIdentifier, TypeMapper.GetSqlDbTypeFromString("UniqueIdentifier"));
            Assert.Equal(SqlDbType.Structured, TypeMapper.GetSqlDbTypeFromString("Structured"));
        }

        [Fact]
        public void IsClrTypeTest()
        {
            Assert.False(TypeMapper.IsClrType(typeof(IsClrTypeTestClass)));
            Assert.False(TypeMapper.IsClrType(new IsClrTypeTestClass()));

            Assert.True(TypeMapper.IsClrType(typeof(Enum)));
            Assert.True(TypeMapper.IsClrType(typeof(String)));
            Assert.True(TypeMapper.IsClrType(typeof(Char)));
            Assert.True(TypeMapper.IsClrType(typeof(Guid)));
            Assert.True(TypeMapper.IsClrType(typeof(Boolean)));
            Assert.True(TypeMapper.IsClrType(typeof(Byte)));
            Assert.True(TypeMapper.IsClrType(typeof(Int16)));
            Assert.True(TypeMapper.IsClrType(typeof(Int32)));
            Assert.True(TypeMapper.IsClrType(typeof(Int64)));
            Assert.True(TypeMapper.IsClrType(typeof(Single)));
            Assert.True(TypeMapper.IsClrType(typeof(Double)));
            Assert.True(TypeMapper.IsClrType(typeof(Decimal)));
            Assert.True(TypeMapper.IsClrType(typeof(SByte)));
            Assert.True(TypeMapper.IsClrType(typeof(UInt16)));
            Assert.True(TypeMapper.IsClrType(typeof(UInt32)));
            Assert.True(TypeMapper.IsClrType(typeof(UInt64)));
            Assert.True(TypeMapper.IsClrType(typeof(DateTime)));
            Assert.True(TypeMapper.IsClrType(typeof(DateTimeOffset)));
            Assert.True(TypeMapper.IsClrType(typeof(TimeSpan)));

            Assert.True(TypeMapper.IsClrType(typeof(System.Char?)));
            Assert.True(TypeMapper.IsClrType(typeof(System.Guid?)));
            Assert.True(TypeMapper.IsClrType(typeof(System.Boolean?)));
            Assert.True(TypeMapper.IsClrType(typeof(System.Byte?)));
            Assert.True(TypeMapper.IsClrType(typeof(System.Int16?)));
            Assert.True(TypeMapper.IsClrType(typeof(System.Int32?)));
            Assert.True(TypeMapper.IsClrType(typeof(System.Int64?)));
            Assert.True(TypeMapper.IsClrType(typeof(System.Single?)));
            Assert.True(TypeMapper.IsClrType(typeof(System.Double?)));
            Assert.True(TypeMapper.IsClrType(typeof(System.Decimal?)));
            Assert.True(TypeMapper.IsClrType(typeof(System.SByte?)));
            Assert.True(TypeMapper.IsClrType(typeof(System.UInt16?)));
            Assert.True(TypeMapper.IsClrType(typeof(System.UInt32?)));
            Assert.True(TypeMapper.IsClrType(typeof(System.UInt64?)));
            Assert.True(TypeMapper.IsClrType(typeof(System.DateTime?)));
            Assert.True(TypeMapper.IsClrType(typeof(System.DateTimeOffset?)));
            Assert.True(TypeMapper.IsClrType(typeof(System.TimeSpan?)));
        }

        [Fact]
        public void IsNullableTest()
        {
            Assert.True(TypeMapper.IsNullable(typeof(int?)));

            int? x = null;
            Assert.True(TypeMapper.IsNullable(x));

            int? y = 1;
            Assert.True(TypeMapper.IsNullable(y));
        }

        [Fact]
        public void IsEntityTest()
        {
            Assert.True(TypeMapper.IsEntity(typeof(Models.User)));
            Assert.False(TypeMapper.IsEntity(typeof(IsClrTypeTestClass)));
        }

        [Fact]
        public void IsListTest()
        {
            var names = new List<string>{ "Bob", "Fred" };
            Assert.True(TypeMapper.IsList(names.GetType()));
            Assert.True(TypeMapper.IsList(names));
            Assert.False(TypeMapper.IsList(typeof(string)));
        }

        [Fact]
        public void IsListOfEntities()
        {
            var names = new List<string>{ "Bob", "Fred" };
            var users = new List<UserTestClass>
            {
                new UserTestClass { Name = "Bob"},
                new UserTestClass { Name = "Fred"}
            };

            Assert.False(TypeMapper.IsListOfEntities(names.GetType()));
            Assert.False(TypeMapper.IsListOfEntities(names));

            Assert.True(TypeMapper.IsListOfEntities(users.GetType()));
            Assert.True(TypeMapper.IsListOfEntities(users));

            Assert.True(TypeMapper.IsListOfEntities(users.GetType(), typeof(UserTestClass)));
            Assert.True(TypeMapper.IsListOfEntities(users, typeof(UserTestClass)));

            Assert.False(TypeMapper.IsListOfEntities(users, typeof(IsClrTypeTestClass)));
        }
    }
}
