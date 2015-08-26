namespace Graceful.Tests.Models
{
    using System;
    using System.ComponentModel;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class User : Model<User>
    {
        public string FirstName { get { return Get<string>(); } set { Set(value); } }
        public string LastName { get { return Get<string>(); } set { Set(value); } }

        // 1:1 Example - I have ONE Address
        public Address HomeAddress { get { return Get<Address>(); } set { Set(value); } }
        public Address WorkAddress { get { return Get<Address>(); } set { Set(value); } }

        // Many:1 Example - I have MANY Cars
        public IList<Car> OldCars { get { return Get<IList<Car>>(); } set { Set(value); } }
        public IList<Car> NewCars { get { return Get<IList<Car>>(); } set { Set(value); } }

        // Many:Many Example - I am a member of MANY Groups,
        public IList<Group> Groups { get { return Get<IList<Group>>(); } set { Set(value); } }

        public static void Seed()
        {
            UpdateOrCreate(e => e.FirstName == "Brad", new User
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

            UpdateOrCreate(e => e.FirstName == "Foo", new User
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
    }

    public class Address : Model<Address>
    {
        // 1:1 Example - I have ONE User that lives here.
        public User HomeUser { get { return Get<User>(); } set { Set(value); } }
        public User WorkUser { get { return Get<User>(); } set { Set(value); } }

        public int StreetNo { get { return Get<int>(); } set { Set(value); } }

        public string StreetName { get { return Get<string>(); } set { Set(value); } }

        public string City { get { return Get<string>(); } set { Set(value); } }
    }

    public class Car : Model<Car>
    {
        // Many:1 Example - I have ONE Owner
        public User OldUser { get { return Get<User>(); } set { Set(value); } }
        public User NewUser { get { return Get<User>(); } set { Set(value); } }

        public string Model { get { return Get<string>(); } set { Set(value); } }
    }

    public class Group : Model<Group>
    {
        // Many:Many Example - I have MANY Users
        public IList<User> Users { get { return Get<IList<User>>(); } set { Set(value); } }

        public string Name { get { return Get<string>(); } set { Set(value); } }
    }
}
