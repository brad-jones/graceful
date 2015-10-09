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

namespace Graceful.Tests
{
    using Xunit;
    using System;
    using System.Data;
    using Graceful.Query;
    using Graceful.Extensions;
    using System.Data.SqlClient;
    using System.Collections;
    using System.Collections.Generic;

    [Collection("ContextSensitive")]
    public class QueryHelperTests : IDisposable
    {
        private Context ctx;
        private Helper helper;

        public QueryHelperTests()
        {
            this.ctx = TestHelpers.DbConnect();
            this.helper = new Helper(this.ctx);
            this.helper.Execute("CREATE TABLE Foo (Bar nvarchar(max));");
            this.helper.Execute("INSERT INTO Foo (Bar) VALUES('Hello');");
        }

        public void Dispose()
        {
            TestHelpers.DbDisconnect(this.ctx);
        }

        [Fact]
        public void BuildCmdTest()
        {
            var cmd = this.helper.BuildCmd
            (
                "CREATE TABLE @tableName (Bar nvarchar(max));",
                new Dictionary<string, object>
                {
                    { "@tableName", new SqlTable(this.ctx, "Baz") }
                }
            );
            cmd.ExecuteNonQuery();
            cmd.Dispose();

            Assert.True(this.helper.TableExists("Baz"));
        }

        [Fact]
        public void ReadTest()
        {
            using (var reader = this.helper.Read("SELECT * FROM Foo"))
            {
                Assert.True(reader.HasRows);

                while (reader.Read())
                {
                    Assert.Equal("Hello", reader.GetString(0));
                }
            }
        }

        [Fact]
        public void ReadToDtTest()
        {
            using (var dt = this.helper.ReadToDt("SELECT * FROM Foo"))
            {
                foreach (DataRow row in dt.Rows)
                {
                    foreach (DataColumn column in dt.Columns)
                    {
                        Assert.Equal("Bar", column.ColumnName);
                        Assert.Equal("Hello", row[column]);
                    }
                }
            }
        }

        [Fact]
        public void ReadToScalarTest()
        {
            Assert.Equal("Hello", helper.ReadToScalar("SELECT TOP 1 Bar FROM Foo"));
        }

        [Fact]
        public void ExecuteTest()
        {
            this.helper.Execute("CREATE TABLE Qux (Bar nvarchar(max));");
            Assert.True(this.helper.TableExists("Qux"));
        }

        [Fact]
        public void GetRowsTest()
        {
            var rows = this.helper.GetRows("SELECT * FROM Foo");
            Assert.Equal(1, rows.Count);
            Assert.Equal("Hello", rows[0]["Bar"]);
        }

        [Fact]
        public void GetRowTest()
        {
            var row = this.helper.GetRow("SELECT * FROM Foo");
            Assert.Equal("Hello", row["Bar"]);
        }

        [Fact]
        public void TableExistsTest()
        {
            Assert.True(this.helper.TableExists("Foo"));
            Assert.False(this.helper.TableExists("FooBar"));
        }

        [Fact]
        public void TableEmptyTest()
        {
            this.helper.Execute("CREATE TABLE EmptyTable (Bar nvarchar(max));");
            Assert.True(this.helper.TableEmpty("EmptyTable"));
        }

        [Fact]
        public void ColumnExistsTest()
        {
            Assert.True(this.helper.ColumnExists("Foo", "Bar"));
            Assert.False(this.helper.ColumnExists("Foo", "Xyz"));
        }

        [Fact]
        public void ColumnDataTypeTest()
        {
            Assert.Equal(SqlDbType.NVarChar, this.helper.ColumnDataType("Foo", "Bar"));
        }
    }
}
