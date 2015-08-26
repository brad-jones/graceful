namespace Graceful.Tests
{
    using Xunit;

    public class SqlTableNameTests
    {
        [Fact]
        public void ClassNameTest()
        {
            Assert.Equal("Users", Models.User.SqlTableName);
        }

        [Fact]
        public void CustomNameTest()
        {
            Assert.Equal("i_am_special", Models.CustomTableName.SqlTableName);
        }
    }
}
