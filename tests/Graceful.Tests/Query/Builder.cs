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
    using System.Text;
    using System.Data;
    using Graceful.Query;
    using Graceful.Extensions;
    using System.Data.SqlClient;
    using System.Collections;
    using System.Collections.Generic;

    [Collection("ContextSensitive")]
    public class QueryBuilderTests : IDisposable
    {
        private Context ctx;

        public QueryBuilderTests()
        {
            this.ctx = TestHelpers.DbConnect();
        }

        public void Dispose()
        {
            TestHelpers.DbDisconnect(this.ctx);
        }

        [Fact]
        public void SqlTest()
        {
            var builder = new Builder(this.ctx);

            builder.SELECT("*").FROM("Foo").WHERE("Bar = 'Hello'");

            var expected = new StringBuilder();
            expected.AppendLine("SELECT * ");
            expected.AppendLine("FROM @p0p ");
            expected.Append("WHERE Bar = 'Hello' ");

            Assert.Equal(expected.ToString(), builder.Sql);
        }

        [Fact]
        public void IsEmptyTest()
        {
            var builder = new Builder(this.ctx);

            Assert.True(builder.IsEmpty);
            Assert.Equal(0, builder.Sql.Length);
        }

        [Fact]
        public void ParametersTest()
        {
            var builder = new Builder(this.ctx);

            builder.SELECT("*").FROM("Foo").WHERE("Id", 1);

            Assert.Equal(3, builder.Parameters.Count);
            Assert.Equal("["+this.ctx.DatabaseName+"].[dbo].[Foo]", ((SqlTable)builder.Parameters["@p0p"]).Value);
            Assert.Equal("[Id]", ((SqlId)builder.Parameters["@p1p"]).Value);
            Assert.Equal(1, builder.Parameters["@p2p"]);
        }

        [Fact]
        public void HashTest()
        {
            var builder1 = new Builder(this.ctx);
            builder1.SELECT("*").FROM("Foo").WHERE("Id", 1);

            var builder2 = new Builder(this.ctx);
            builder2.SELECT("*").FROM("Foo").WHERE("Id", 1);

            Assert.Equal(builder1.Hash, builder2.Hash);
        }

        [Fact]
        public void _Test()
        {
            var builder = new Builder(this.ctx);

            builder
                .SELECT("Foo")
                ._("Bar")
                .FROM("Baz")
                .WHERE("Abc = '123'")
                ._("Xyz = '987'")
            ;

            var expected = new StringBuilder();
            expected.AppendLine("SELECT Foo , Bar ");
            expected.AppendLine("FROM @p0p ");
            expected.Append("WHERE Abc = '123' AND Xyz = '987' ");

            Assert.Equal(expected.ToString(), builder.Sql);
        }

        [Fact]
        public void _IFTest()
        {
            var builder = new Builder(this.ctx);

            var abc = true;
            var xyz = false;

            builder
                .SELECT("*")
                .FROM("Baz")
                .WHERE()
                ._IF(abc, "Abc = '123'")
                ._IF(xyz, "Xyz = '987'")
            ;

            var expected = new StringBuilder();
            expected.AppendLine("SELECT * ");
            expected.AppendLine("FROM @p0p ");
            expected.Append("WHERE Abc = '123' ");

            Assert.Equal(expected.ToString(), builder.Sql);
        }

        [Fact]
        public void WITHTest()
        {
            var builder = new Builder(this.ctx);

            builder.WITH("Bar AS (SELECT * FROM Foo)");

            Assert.Equal("WITH Bar AS (SELECT * FROM Foo) ", builder.Sql);

            builder.WITH("Baz",  new Builder(this.ctx).SELECT("*").FROM("Bar"));

            var expected = new StringBuilder();
            expected.AppendLine("WITH Bar AS (SELECT * FROM Foo) , @p0p AS (SELECT * ");
            expected.Append("FROM @p1p ) ");

            Assert.Equal(expected.ToString(), builder.Sql);
        }
    }
}
