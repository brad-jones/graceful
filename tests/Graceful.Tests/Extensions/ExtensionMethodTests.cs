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
    using System.Text;
    using System.Data.SqlClient;
    using System.Collections.Generic;
    using Graceful.Extensions;

    public class ExtensionMethodTests
    {
        [Fact]
        public void EnumerableForEachTest()
        {
            IList<string> fooList = new List<string>{ "abc", "xyz" };

            fooList.ForEach(value =>
            {
                Assert.IsType<string>(value);
            });
        }

        [Fact]
        public void ForEachBreakTest()
        {
            var fooList = new List<string>{ "abc", "xyz" };
            int counter = 0;

            fooList.ForEach(value =>
            {
                counter++;

                if (value == "abc")
                {
                    // same as saying: break.
                    return false;
                }

                // same as saying: continue.
                // only downside is that you must explcitly say continue.
                return true;
            });

            Assert.Equal(1, counter);
        }

        [Fact]
        public void ForEachWithIndexTest()
        {
            var fooList = new List<string>{ "abc", "xyz" };

            fooList.ForEach((key, value) =>
            {
                Assert.Equal(fooList.IndexOf(value), key);
            });
        }

        [Fact]
        public void ForEachWithIndexBreakTest()
        {
            var fooList = new List<string>{ "abc", "xyz" };
            int counter = 0;

            fooList.ForEach((key, value) =>
            {
                counter++;

                if (value == "abc")
                {
                    return false;
                }

                return true;
            });

            Assert.Equal(1, counter);
        }

        [Fact]
        public void ToTraceStringTest()
        {
            var cmd = new SqlCommand("SELECT * FROM Foo");

            var expected = new StringBuilder();
            expected.AppendLine("================================================================================");
            expected.AppendLine("SELECT * FROM Foo");
            expected.AppendLine("-- [0] records affected.");
            expected.AppendLine();

            Assert.Equal
            (
                expected.ToString(),
                cmd.ToTraceString()
            );
        }

        [Fact]
        public void ToTraceStringWithParamsTest()
        {
            var cmd = new SqlCommand("SELECT * FROM Foo WHERE Id = @p0");
            cmd.Parameters.AddWithValue("@p0", 1);

            var expected = new StringBuilder();
            expected.AppendLine("================================================================================");
            expected.AppendLine("SELECT * FROM Foo WHERE Id = @p0");
            expected.AppendLine("-- @p0: Input Int (Size = 0) [1]");
            expected.AppendLine("-- [0] records affected.");
            expected.AppendLine();

            Assert.Equal
            (
                expected.ToString(),
                cmd.ToTraceString()
            );
        }
    }
}
