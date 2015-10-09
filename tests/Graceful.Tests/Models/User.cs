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
