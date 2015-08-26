namespace Graceful.Tests
{
    using Xunit;
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Graceful.Tests.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Schema;

    /**
     * These tests all require the use of a database server.
     * Each test will create a new context, run the test, then the Dispose
     * method will ensure all clean up is performed, regardless of the outcome
     * of the test.
     *
     * > NOTE: To run these tests I am assuming SqlExpress is installed.
     * > Or you could modify the connection string to suit your environment.
     */
    public class DbTests : IDisposable
    {
        private string ConnectionString = @"Server=localhost\SQLEXPRESS;Database=Graceful;Trusted_Connection=True;";

        [Fact]
        public void ConnectTest()
        {
            Context.Connect(this.ConnectionString);

            using (var con = Context.GlobalCtx.Connection)
            {
                Assert.IsType<SqlConnection>(con);
                Assert.Equal(ConnectionState.Open, con.State);
            }
        }

        [Fact]
        public void DatabaseNameTest()
        {
            Context.Connect(this.ConnectionString);

            using (var con = Context.GlobalCtx.Connection)
            {
                Assert.Equal(Context.GlobalCtx.DatabaseName, con.Database);
            }
        }

        [Fact]
        public void QueryLogTest()
        {
            // Connect, with logging enabled
            Context.Connect(this.ConnectionString, log: true);

            // We have not run any quries yet, the log should be empty.
            Assert.True(string.IsNullOrWhiteSpace(Context.GlobalCtx.Log));

            // Run the migrations, and we should get some data.
            Context.GlobalCtx.RunMigrations();
            var log1 = Context.GlobalCtx.Log;
            Assert.False(string.IsNullOrWhiteSpace(log1));

            // Run the seeds, and we should get some more data.
            Context.GlobalCtx.RunSeeds();
            var log2 = Context.GlobalCtx.Log;
            Assert.False(string.IsNullOrWhiteSpace(log2));

            // That is not the same as the first log
            Assert.NotEqual(log1, log2);
        }

        [Fact]
        public void ModelsTest()
        {
            var ctx = new Context(this.ConnectionString);

            // NOTE: Order is important!
            Assert.Equal
            (
                new List<Type>
                {
                    typeof(Models.CustomTableName),
                    typeof(Models.User),
                    typeof(Models.Address),
                    typeof(Models.Car),
                    typeof(Models.Group)
                },
                ctx.Models
            );
        }

        [Fact]
        public void MigratorTest()
        {
            // Connect to database
            var ctx = new Context(this.ConnectionString);

            // Create the Users table with a schema
            // that does not match the POCO model
            ctx.Qb.Execute("CREATE TABLE Users (Id INT NOT NULL PRIMARY KEY, Name VARCHAR(255) NOT NULL, FirstName TEXT NULL);");

            // The Name column should exist now
            Assert.True(ctx.Qb.ColumnExists("Users", "Name"));

            // Run the migrations
            Graceful.Utils.Migrator.DataLossAllowed = false;
            ctx.RunMigrations();

            // Ensure the basics exist
            Assert.True(Context.GlobalCtx.Qb.TableExists("Users"));
            Assert.True(Context.GlobalCtx.Qb.TableExists("Addresses"));
            Assert.True(Context.GlobalCtx.Qb.TableExists("Cars"));
            Assert.True(Context.GlobalCtx.Qb.TableExists("Groups"));
            Assert.True(Context.GlobalCtx.Qb.TableExists("UsersToGroups"));

            // Now lets make sure the right columns got put in the right tables
            Assert.True(Context.GlobalCtx.Qb.ColumnExists("Users", "AddressHomeId"));
            Assert.True(Context.GlobalCtx.Qb.ColumnExists("Users", "AddressWorkId"));
            Assert.True(Context.GlobalCtx.Qb.ColumnExists("Cars", "UserOldId"));
            Assert.True(Context.GlobalCtx.Qb.ColumnExists("Cars", "UserNewId"));
            Assert.True(Context.GlobalCtx.Qb.ColumnExists("UsersToGroups", "UserId"));
            Assert.True(Context.GlobalCtx.Qb.ColumnExists("UsersToGroups", "GroupId"));

            // The Name column should still exist because
            // we ran a migration with dataLosss set to false.
            Assert.True(ctx.Qb.ColumnExists("Users", "Name"));

            // Run the migrate method again but this time we will delete stuff
            Graceful.Utils.Migrator.DataLossAllowed = true;
            ctx.RunMigrations();

            // This should now be false
            Assert.False(ctx.Qb.ColumnExists("Users", "Name"));

            // Finally make sure that FirstName got updated the nvarchar
            Assert.Equal(SqlDbType.NVarChar, ctx.Qb.ColumnDataType("Users", "FirstName"));
        }

        [Fact]
        public void RunSeedsTest()
        {
            Context.Connect(this.ConnectionString, migrate: true);

            // At this point in time the table should be empty
            Assert.True(Context.GlobalCtx.Qb.TableEmpty("Users"));

            // Run the Contexts Seeds
            Context.GlobalCtx.RunSeeds();

            // Now the table should not be empty and we should have
            // 2 users, 4 addresses (2 per user), 4 cars (2 per user)
            // and 2 groups.
            Assert.False(Context.GlobalCtx.Qb.TableEmpty("Users"));
            Assert.Equal(2, Context.GlobalCtx.Qb.SELECT("COUNT(*)").FROM("Users").Scalar);
            Assert.Equal(4, Context.GlobalCtx.Qb.SELECT("COUNT(*)").FROM("Addresses").Scalar);
            Assert.Equal(4, Context.GlobalCtx.Qb.SELECT("COUNT(*)").FROM("Cars").Scalar);
            Assert.Equal(2, Context.GlobalCtx.Qb.SELECT("COUNT(*)").FROM("Groups").Scalar);

            // Run the seeds again
            Context.GlobalCtx.RunSeeds();

            // The same tests should still pass.
            Assert.False(Context.GlobalCtx.Qb.TableEmpty("Users"));
            Assert.Equal(2, Context.GlobalCtx.Qb.SELECT("COUNT(*)").FROM("Users").Scalar);
            Assert.Equal(4, Context.GlobalCtx.Qb.SELECT("COUNT(*)").FROM("Addresses").Scalar);
            Assert.Equal(4, Context.GlobalCtx.Qb.SELECT("COUNT(*)").FROM("Cars").Scalar);
            Assert.Equal(2, Context.GlobalCtx.Qb.SELECT("COUNT(*)").FROM("Groups").Scalar);

            // Now lets make some actual changes
            User.UpdateOrCreate(e => e.FirstName == "Brad", new User
            {
                FirstName = "Brad",
                LastName = "Smith",
                HomeAddress = new Address
                {
                    StreetNo = 20,
                    StreetName = "Foo Street",
                    City = "Bar Land Baz"
                },
                WorkAddress = new Address
                {
                    StreetNo = 190,
                    StreetName = "Queen Street",
                    City = "Melbourne"
                },
                OldCars = new List<Car>
                {
                    new Car { Model = "T Model Ford" }
                },
                NewCars = new List<Car>
                {
                    new Car { Model = "Zook" }
                },
                Groups = new List<Group>
                {
                    new Group { Name = "Admins" },
                    new Group { Name = "Users" }
                }
            });

            // The same tests should still pass.
            Assert.False(Context.GlobalCtx.Qb.TableEmpty("Users"));
            Assert.Equal(2, Context.GlobalCtx.Qb.SELECT("COUNT(*)").FROM("Users").Scalar);
            Assert.Equal(4, Context.GlobalCtx.Qb.SELECT("COUNT(*)").FROM("Addresses").Scalar);
            Assert.Equal(4, Context.GlobalCtx.Qb.SELECT("COUNT(*)").FROM("Cars").Scalar);
            Assert.Equal(2, Context.GlobalCtx.Qb.SELECT("COUNT(*)").FROM("Groups").Scalar);

            Assert.Equal("Smith", User.Find(1).LastName);
            Assert.Equal("Bar Land Baz", User.Find(1).HomeAddress.City);
        }

        [Fact]
        public void ModifiedPropsTest()
        {
            Context.Connect(this.ConnectionString, migrate: true, seed: true);

            // Grab a User
            var user  = User.Find(1);

            // It should have no modified properties.
            Assert.Equal(0, user.ModifiedProps.Count);

            // Lets make a change
            user.FirstName = "Foobar";
            Assert.Equal(1, user.ModifiedProps.Count);

            // Lets make a change to a list
            user.Groups[0].Name = "Some Other Group";
            Assert.Equal(2, user.ModifiedProps.Count);

            // Remove a group
            user.Groups.RemoveAt(1);
            Assert.Equal(1, user.Groups.Count);

            // Ensure that the Original Property Bag contains the expected data.
            Assert.Equal("Brad", user.OriginalPropertyBag["FirstName"]);
            Assert.Equal("Admins", user.Groups[0].OriginalPropertyBag["Name"]);
            Assert.Equal(2, ((dynamic)user.OriginalPropertyBag["Groups"]).Count);
        }

        [Fact]
        public void CustomContextTest()
        {
            var ctx = new Context(@"Server=localhost\SQLEXPRESS;Database=Graceful_FOO;Trusted_Connection=True;");

            // NOTE: Order is important!
            Assert.Equal
            (
                new List<Type>
                {
                    typeof(Models.CustomContext)
                },
                ctx.Models
            );

            ctx.RunMigrations();
            Assert.True(ctx.Qb.TableExists("CustomContexts"));

            ctx.RunSeeds();
            Assert.False(ctx.Qb.TableEmpty("CustomContexts"));
        }

        [Fact]
        public void FindTest()
        {
            Context.Connect(this.ConnectionString, migrate: true, seed: true);
            var user = Models.User.Find(1);
            Assert.Equal(1, user.Id);
            Assert.Equal("Brad", user.FirstName);
            Assert.Equal("Jones", user.LastName);
        }

        [Fact]
        public void UpdateTest()
        {
            Context.Connect(this.ConnectionString, migrate: true, seed: true);

            var user = Models.User.Find(1);

            Assert.Equal("Brad", user.FirstName);

            user.FirstName = "Foo";
            user.Save();

            Assert.Equal("Foo", Models.User.Find(1).FirstName);
        }

        [Fact]
        public void MassUpdateTest()
        {
            Context.Connect(this.ConnectionString, migrate: true, seed: true);

            // Update all users to have the same first and last name
            Models.User.Update(m => m.FirstName == "Bar" && m.LastName == "Baz");

            var user1 = Models.User.Find(1);
            Assert.Equal("Bar", user1.FirstName);
            Assert.Equal("Baz", user1.LastName);

            var user2 = Models.User.Find(2);
            Assert.Equal("Bar", user2.FirstName);
            Assert.Equal("Baz", user2.LastName);

            // Update only one user
            Models.User.Where(m => m.Id == 1).Update(m => m.FirstName == "Brad" && m.LastName == "Jones");

            user1 = Models.User.Find(1);
            Assert.Equal("Brad", user1.FirstName);
            Assert.Equal("Jones", user1.LastName);

            user2 = Models.User.Find(2);
            Assert.Equal("Bar", user2.FirstName);
            Assert.Equal("Baz", user2.LastName);
        }

        [Fact]
        public void SoftDeleteTest()
        {
            // Connect and seed
            Context.Connect(this.ConnectionString, migrate: true, seed: true);

            // Find and Soft Delete the user
            Models.User.Find(1).Delete();

            // Find the user again, we should get null
            Assert.Equal(null, Models.User.Find(1));

            // We should get a valid user now
            var trashedUser = Models.User.Find(1, withTrashed: true);
            Assert.Equal("Brad", trashedUser.FirstName);

            // Restore the user
            trashedUser.Restore();
            Assert.Equal("Brad", Models.User.Find(1).FirstName);
        }

        [Fact]
        public void LinqWhereTest()
        {
            Context.Connect(this.ConnectionString, migrate: true, seed: true);
            Assert.Equal("Brad", Models.User.Linq.Where("Id = 1").ToArray()[0].FirstName);
            Assert.Equal("Brad", Models.User.Linq.Where("Id = {0}", 1).ToArray()[0].FirstName);
            Assert.Equal("Brad", Models.User.Linq.Where(m => m.Id == 1).ToArray()[0].FirstName);
            Assert.Equal("Brad", Models.User.Linq.Where(m => m.Id == 10 || m.FirstName == "Brad" && m.LastName == "Jones").ToArray()[0].FirstName);
        }

        [Fact]
        public void LinqLikeTest()
        {
            Context.Connect(this.ConnectionString, migrate: true, seed: true);
            Assert.Equal("Brad", Models.User.Linq.Like(e => e.FirstName == "%ra%").ToArray()[0].FirstName);
            Assert.Equal("Brad", Models.User.Linq.Like(e => e.FirstName != "%o%").ToArray()[0].FirstName);
        }

        [Fact]
        public void AddRemoveMtoMTest()
        {
            Context.Connect(this.ConnectionString, migrate: true, seed: true);

            // Grab a user, remember the seeds add 2 groups, Users and Admins
            var user = User.Find(1);
            Assert.Equal(2, user.Groups.Count);

            // Lets add another
            user.Groups.Add(new Group { Name = "FooBar" });

            // This checks to make sure our new group
            // had it's foreign relationship hydrated.
            Assert.Equal("Brad", user.Groups[2].Users[0].FirstName);

            // Locally we should have 3
            Assert.Equal(3, user.Groups.Count);

            // But the Database should still only have 2
            Assert.Equal(2, User.Find(1).Groups.Count);

            // Save the user
            user.Save();

            // Now our local user entity and brand new one should have 3 groups
            Assert.Equal(3, user.Groups.Count);
            Assert.Equal(3, User.Find(1).Groups.Count);

            // Lets remove the by index, this should remove the admins group.
            user.Groups.RemoveAt(0);

            // Locally we have 2 but the db should still have 3
            Assert.Equal(2, user.Groups.Count);
            Assert.Equal(3, User.Find(1).Groups.Count);

            // Save the user again
            user.Save();

            // Now we should both report 2
            Assert.Equal(2, user.Groups.Count);
            Assert.Equal(2, User.Find(1).Groups.Count);

            // At this stage we have not hydrated any groups.
            // Lets check to make sure the first group is now Users
            // and the second FooBar
            Assert.Equal("Users", User.Find(1).Groups[0].Name);
            Assert.Equal("FooBar", User.Find(1).Groups[1].Name);
        }

        [Fact]
        public void AddRemoveMtoOTest()
        {
            Context.Connect(this.ConnectionString, migrate: true, seed: true);

            // Grab a user, remember the seeds add 1 old car and one new car.
            var user = User.Find(1);
            Assert.Equal(1, user.OldCars.Count);
            Assert.Equal(1, user.NewCars.Count);

            // Lets add another few cars
            user.OldCars.Add(new Car { Model = "A Old Car" });
            user.NewCars.Add(new Car { Model = "A New Car" });

            // This checks to make sure our new cars
            // have had their foreign relationship hydrated.
            Assert.Equal("Brad", user.OldCars[1].OldUser.FirstName);
            Assert.Equal("Brad", user.NewCars[1].NewUser.FirstName);

            // Locally we should have 2 old and new cars
            Assert.Equal(2, user.OldCars.Count);
            Assert.Equal(2, user.NewCars.Count);

            // But the Database should still only have 1 each
            Assert.Equal(1, User.Find(1).OldCars.Count);
            Assert.Equal(1, User.Find(1).NewCars.Count);

            // Save the user
            user.Save();

            // Now our local user entity and our brand
            // new users should have 4 cars total each.
            Assert.Equal(2, user.OldCars.Count);
            Assert.Equal(2, user.NewCars.Count);
            Assert.Equal(2, User.Find(1).OldCars.Count);
            Assert.Equal(2, User.Find(1).NewCars.Count);

            // Lets remove by index
            user.OldCars.RemoveAt(0);
            user.NewCars.RemoveAt(0);

            // Locally we should have 1 of each car
            // but the database should still have 2 of each type.
            Assert.Equal(1, user.OldCars.Count);
            Assert.Equal(1, user.NewCars.Count);
            Assert.Equal(2, User.Find(1).OldCars.Count);
            Assert.Equal(2, User.Find(1).NewCars.Count);

            // Save the user again
            user.Save();

            // Now we should both report the same
            Assert.Equal(1, user.OldCars.Count);
            Assert.Equal(1, user.NewCars.Count);
            Assert.Equal(1, User.Find(1).OldCars.Count);
            Assert.Equal(1, User.Find(1).NewCars.Count);

            // At this stage we have not hydrated any cars.
            // Lets check to make sure the first car is what wenow expect.
            Assert.Equal("A Old Car", User.Find(1).OldCars[0].Model);
            Assert.Equal("A New Car", User.Find(1).NewCars[0].Model);
        }

        [Fact]
        public void AddRemoveOtoMTest()
        {
            Context.Connect(this.ConnectionString, migrate: true, seed: true);

            // Grab an old car that belongs to Brad
            var car = Car.Single(e => e.Model == "T Model Ford");
            Assert.Equal("Brad", car.OldUser.FirstName);
            Assert.Equal(null, car.NewUser);

            // Update the Old User
            car.OldUser = User.Single(e => e.FirstName == "Foo");
            Assert.Equal("Foo", car.OldUser.FirstName);
            Assert.Equal("Brad", Car.Single(e => e.Model == "T Model Ford").OldUser.FirstName);
            car.Save();
            Assert.Equal("Foo", Car.Single(e => e.Model == "T Model Ford").OldUser.FirstName);

            // Lets try actaully removing a relationship
            car.OldUser = null;
            Assert.Equal(null, car.OldUser);
            car.Save();
            Assert.Equal(null, Car.Single(e => e.Model == "T Model Ford").OldUser);
        }

        [Fact]
        public void AddRemoveOtoOTest()
        {
            Context.Connect(this.ConnectionString, migrate: true, seed: true);

            // Grab a user, make sure it's address is what we expect
            var user = User.Find(1);
            Assert.Equal("Foo Street", user.HomeAddress.StreetName);

            // Change it's home address to it's work address
            user.HomeAddress = Address.Single(e => e.StreetName == "Queen Street");
            user.Save();

            // Remember we allow the same entity to be related mutliple times.
            // The "Address" has 2 one to one relationships.
            Assert.Equal("Queen Street", User.Find(1).HomeAddress.StreetName);
            Assert.Equal("Queen Street", User.Find(1).WorkAddress.StreetName);
        }

        [Fact]
        public void DynamicFindTest()
        {
            Context.Connect(this.ConnectionString, migrate: true, seed: true);

            var user = Model.Dynamic("User").Find(1);
            Assert.Equal(1, user.Id);
            Assert.Equal("Brad", ((User)user).FirstName);

            var users = Model.Dynamic("User").ToList();
            Assert.Equal(1, users[0].Id);
            Assert.Equal("Brad", users[0].FirstName);
        }

        [Fact]
        public void FromJsonTest()
        {
            Context.Connect(this.ConnectionString, migrate: true, seed: true);

            var user = User.Find(1);
            var userJsonString = user.ToJson();
            var userFromJson = User.FromJson(userJsonString);
            
            Assert.Equal(user.Id, userFromJson.Id);
            Assert.Equal(user.FirstName, userFromJson.FirstName);
            Assert.Equal(user.LastName, userFromJson.LastName);

            Assert.Equal(user.HomeAddress.Id, userFromJson.HomeAddress.Id);
            Assert.Equal(user.HomeAddress.HomeUser.Id, userFromJson.HomeAddress.HomeUser.Id);
            Assert.Equal(user.HomeAddress.WorkUser, userFromJson.HomeAddress.WorkUser);
            Assert.Equal(user.HomeAddress.StreetNo, userFromJson.HomeAddress.StreetNo);
            Assert.Equal(user.HomeAddress.StreetName, userFromJson.HomeAddress.StreetName);
            Assert.Equal(user.HomeAddress.City, userFromJson.HomeAddress.City);

            Assert.Equal(user.WorkAddress.Id, userFromJson.WorkAddress.Id);
            Assert.Equal(user.WorkAddress.HomeUser, userFromJson.WorkAddress.HomeUser);
            Assert.Equal(user.WorkAddress.WorkUser.Id, userFromJson.WorkAddress.WorkUser.Id);
            Assert.Equal(user.WorkAddress.StreetNo, userFromJson.WorkAddress.StreetNo);
            Assert.Equal(user.WorkAddress.StreetName, userFromJson.WorkAddress.StreetName);
            Assert.Equal(user.WorkAddress.City, userFromJson.WorkAddress.City);

            // etc... maybe I'll finish this one day :)
        }

        public void Dispose()
        {
            SqlConnection.ClearAllPools();

            using (var con = new SqlConnection(@"Server=localhost\SQLEXPRESS;Database=master;Trusted_Connection=True;"))
            {
                con.Open();

                if (new SqlCommand("SELECT database_id FROM sys.databases WHERE Name = 'Graceful'", con).ExecuteScalar() != null)
                {
                    new SqlCommand("ALTER DATABASE Graceful SET SINGLE_USER WITH ROLLBACK IMMEDIATE", con).ExecuteNonQuery();
                    new SqlCommand("DROP DATABASE Graceful", con).ExecuteNonQuery();
                }

                if (new SqlCommand("SELECT database_id FROM sys.databases WHERE Name = 'Graceful_FOO'", con).ExecuteScalar() != null)
                {
                    new SqlCommand("ALTER DATABASE Graceful_FOO SET SINGLE_USER WITH ROLLBACK IMMEDIATE", con).ExecuteNonQuery();
                    new SqlCommand("DROP DATABASE Graceful_FOO", con).ExecuteNonQuery();
                }
            }
        }
    }
}
