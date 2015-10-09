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
    using Graceful.Query;
    using System.Data.SqlClient;
    using System.Collections;
    using System.Collections.Generic;

    [Collection("ContextSensitive")]
    public class QueryTypeTests : IDisposable
    {
        private Context ctx;

        public QueryTypeTests()
        {
            this.ctx = TestHelpers.DbConnect();
        }

        public void Dispose()
        {
            TestHelpers.DbDisconnect(this.ctx);
        }

        [Fact]
        public void SqlIdTest()
        {
            Assert.Equal("[Foo]", new SqlId("Foo").Value);
            Assert.Equal("[Foo].[Bar]", new SqlId("Foo.Bar").Value);
        }

        [Fact]
        public void SqlTableTest()
        {
            Assert.Equal
            (
                "["+this.ctx.DatabaseName+"].[dbo].[Foo]",
                new SqlTable(this.ctx, "Foo").Value
            );
        }

        [Fact]
        public void SqlColumnTest()
        {
            Assert.Equal
            (
                "["+this.ctx.DatabaseName+"].[dbo].[Foo].[Bar]",
                new SqlColumn(this.ctx, "Foo", "Bar").Value
            );
        }
    }
}
