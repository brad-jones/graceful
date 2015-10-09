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
    using System.Data.SqlClient;

    public static class TestHelpers
    {
        public static Context DbConnect()
        {
            var dbName = "a" + Guid.NewGuid().ToString().Replace("-", "");
            var cs = @"Server=localhost\SQLEXPRESS;Database="+dbName+";Trusted_Connection=True;";
            return new Graceful.Context(cs);
        }

        public static void DbDisconnect(Context ctx)
        {
            //SqlConnection.ClearAllPools();

            using (var con = new SqlConnection(@"Server=localhost\SQLEXPRESS;Database=master;Trusted_Connection=True;"))
            {
                con.Open();

                if (new SqlCommand("SELECT database_id FROM sys.databases WHERE Name = '"+ctx.DatabaseName+"'", con).ExecuteScalar() != null)
                {
                    new SqlCommand("ALTER DATABASE "+ctx.DatabaseName+" SET SINGLE_USER WITH ROLLBACK IMMEDIATE", con).ExecuteNonQuery();
                    new SqlCommand("DROP DATABASE "+ctx.DatabaseName, con).ExecuteNonQuery();
                }
            }
        }
    }
}
