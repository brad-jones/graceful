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

    [Collection("ContextSensitive")]
    public class DbTests : IDisposable
    {
        private Context ctx;

        public DbTests()
        {
            this.ctx = TestHelpers.DbConnect();
            new Graceful.Utils.Migrator(this.ctx);

            User.UpdateOrCreate(e => e.FirstName == "Brad", new User
            {
                FirstName = "Brad",
                LastName = "Jones",
                HomeAddress = new Address
                {
                    StreetNo = 20,
                    StreetName = "Foo Street",
                    City = "Bar Land"
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

            User.UpdateOrCreate(e => e.FirstName == "Foo", new User
            {
                FirstName = "Foo",
                LastName = "Qux",
                HomeAddress = new Address
                {
                    StreetNo = 200,
                    StreetName = "Fake St",
                    City = "Virtual Land"
                },
                WorkAddress = new Address
                {
                    StreetNo = 23,
                    StreetName = "A Road",
                    City = "SomeWhere"
                },
                OldCars = new List<Car>
                {
                    new Car { Model = "Holden" }
                },
                NewCars = new List<Car>
                {
                    new Car { Model = "Toyota" }
                },
                Groups = new List<Group>
                {
                    Group.Find(2)
                }
            });
        }
        
        public void Dispose()
        {
            TestHelpers.DbDisconnect(this.ctx);
        }

        [Fact]
        public void ModifiedPropsTest()
        {
            // Grab a User
            var user  = User.Find(1);

            // It should have no modified properties.
            Assert.Equal(0, user.ModifiedProps.Count);

            // Lets make a change
            user.FirstName = "Foobar";
            Assert.Equal(1, user.ModifiedProps.Count);

            // Lets make a change to a list
            user.Groups.RemoveAt(1);
            Assert.Equal(1, user.Groups.Count);
            Assert.Equal(2, user.ModifiedProps.Count);

            // Ensure that the Original Property Bag contains the expected data.
            Assert.Equal("Brad", user.OriginalPropertyBag["FirstName"]);
            Assert.Equal("Admins", user.Groups[0].OriginalPropertyBag["Name"]);
            Assert.Equal(2, ((dynamic)user.OriginalPropertyBag["Groups"]).Count);
        }

        [Fact]
        public void FindTest()
        {
            var user = Models.User.Find(1);
            Assert.Equal(1, user.Id);
            Assert.Equal("Brad", user.FirstName);
            Assert.Equal("Jones", user.LastName);
        }

        [Fact]
        public void UpdateTest()
        {
            var user = Models.User.Find(1);

            Assert.Equal("Brad", user.FirstName);

            user.FirstName = "Foo";
            user.Save();

            Assert.Equal("Foo", Models.User.Find(1).FirstName);
        }

        [Fact]
        public void MassUpdateTest()
        {
            // Update all users to have the same first and last name
            Models.User.UpdateAll(m => m.FirstName == "Bar" && m.LastName == "Baz");

            var user1 = Models.User.Find(1);
            Assert.Equal("Bar", user1.FirstName);
            Assert.Equal("Baz", user1.LastName);

            var user2 = Models.User.Find(2);
            Assert.Equal("Bar", user2.FirstName);
            Assert.Equal("Baz", user2.LastName);

            // Update only one user
            Models.User.Where(m => m.Id == 1).UpdateAll(m => m.FirstName == "Brad" && m.LastName == "Jones");

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
            Assert.Equal("Brad", Models.User.Linq.Where("e.Id = 1").ToArray()[0].FirstName);
            Assert.Equal("Brad", Models.User.Linq.Where("Id = {0}", 1).ToArray()[0].FirstName);
            Assert.Equal("Brad", Models.User.Linq.Where(m => m.Id == 1).ToArray()[0].FirstName);
            Assert.Equal("Brad", Models.User.Linq.Where(m => m.Id == 10 || m.FirstName == "Brad" && m.LastName == "Jones").ToArray()[0].FirstName);
        }

        [Fact]
        public void LinqLikeTest()
        {
            Assert.Equal("Brad", Models.User.Linq.Like(e => e.FirstName == "%ra%").ToArray()[0].FirstName);
            Assert.Equal("Brad", Models.User.Linq.Like(e => e.FirstName != "%o%").ToArray()[0].FirstName);
        }

        [Fact]
        public void AddRemoveMtoMTest()
        {
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

        [Fact]
        public void CreateFromJsonTest()
        {
            Models.Car.Create("{\"Model\":\"Holden\", \"OldUser\":{\"Id\":1, \"FirstName\":\"Fred\"}}");
            Assert.Equal("Fred", User.Find(1).FirstName);
        }
    }
}
