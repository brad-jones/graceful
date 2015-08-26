graceful - A Laravel (Eloquent) inspired ORM for .NET
================================================================================
Like most of my projects this one has a story.
If your interested in this story see the end of this readme.

Installation
--------------------------------------------------------------------------------
Assuming a DNX451 App:

```
dnu install graceful
```

> See: https://www.nuget.org/packages/graceful

Connecting to a Database
--------------------------------------------------------------------------------

```cs
Graceful.Context.Connect("my connection string");
```

### Seeding
TODO

### Migrating
TODO

Defining Models
--------------------------------------------------------------------------------
First we need a model.

```cs
namespace Acme
{
    public class Person : Graceful.Model<Person>
    {

    }
}
```

Pretty straight forward right? Lets see what a person looks like at the moment.

```cs
Console.WriteLine(new Acme.Person().ToJson());
```

And we would get something like:

```json
{
    "Id": 0,
    "CreatedAt": "2015-08-26T02:47:05.2154641",
    "ModifiedAt": "2015-08-26T02:47:05.2154641",
    "DeletedAt": null
}
```

> All entities have these 4 basic properties.

Now lets add some more useful properties to the person.

```cs
namespace Acme
{
    public class Person : Graceful.Model<Person>
    {
        public string Name { get { return Get<string>(); } set { Set(value); } }
        public int Age { get { return Get<int>(); } set { Set(value); } }
    }
}
```

Note what is happening with our property getters and setters.
All models implement INotifyPropertyChange using the method outlined here:
http://timoch.com/blog/2013/08/annoyed-with-inotifypropertychange/

What about relationships? Here is a One to One example.

```cs
namespace Acme
{
    public class Person : Graceful.Model<Person>
    {
        public string Name { get { return Get<string>(); } set { Set(value); } }
        public int Age { get { return Get<int>(); } set { Set(value); } }

        public Address Address { get { return Get<Address>(); } set { Set(value); } }
    }

    public class Address : Graceful.Model<Address>
    {
        public int StreetNo { get { return Get<int>(); } set { Set(value); } }
        public string StreetName { get { return Get<string>(); } set { Set(value); } }
        public string City { get { return Get<string>(); } set { Set(value); } }
    }
}
```

Notice how the Address does not contain a navigational property back to the
Person. This is what I call a "Lazy" relationship. Here is an example that
defines both sides of the One to One relationship.

```cs
namespace Acme
{
    public class Person : Graceful.Model<Person>
    {
        public string Name { get { return Get<string>(); } set { Set(value); } }
        public int Age { get { return Get<int>(); } set { Set(value); } }

        public Address MyAddress { get { return Get<Address>(); } set { Set(value); } }
    }

    public class Address : Graceful.Model<Address>
    {
        public int StreetNo { get { return Get<int>(); } set { Set(value); } }
        public string StreetName { get { return Get<string>(); } set { Set(value); } }
        public string City { get { return Get<string>(); } set { Set(value); } }

        public User MyUser { get { return Get<User>(); } set { Set(value); } }
    }
}
```

Here is a Lazy Many to One relationship.

```cs
namespace Acme
{
    public class Person : Graceful.Model<Person>
    {
        public string Name { get { return Get<string>(); } set { Set(value); } }
        public int Age { get { return Get<int>(); } set { Set(value); } }

        public IList<Car> MyCars { get { return Get<IList<Car>>(); } set { Set(value); } }
    }

    public class Car : Graceful.Model<Car>
    {
        public string Model { get { return Get<string>(); } set { Set(value); } }
    }
}
```

Here is the same relationship with both sides defined.

```cs
namespace Acme
{
    public class Person : Graceful.Model<Person>
    {
        public string Name { get { return Get<string>(); } set { Set(value); } }
        public int Age { get { return Get<int>(); } set { Set(value); } }

        public IList<Car> MyCars { get { return Get<IList<Car>>(); } set { Set(value); } }
    }

    public class Car : Graceful.Model<Car>
    {
        public string Model { get { return Get<string>(); } set { Set(value); } }

        public User Owner { get { return Get<User>(); } set { Set(value); } }
    }
}
```

Now for a Many to Many relationship, classic memmbership example.

```cs
namespace Acme
{
    public class Person : Graceful.Model<Person>
    {
        public string Name { get { return Get<string>(); } set { Set(value); } }
        public int Age { get { return Get<int>(); } set { Set(value); } }

        public IList<Club> Clubs { get { return Get<IList<Club>>(); } set { Set(value); } }
    }

    public class Club : Graceful.Model<Club>
    {
        public IList<Person> Members { get { return Get<IList<Person>>(); } set { Set(value); } }
    }
}
```

Finally we may define the same relationship to the same type many times.
So long as the properties on each side of the relationship follow a convention.

The property names must contain a shared unique identifier
as well as the model's type name: ie: Person. The type name
may come before or after the identifier.

```cs
namespace Acme
{
    public class Person : Graceful.Model<Person>
    {
        public string Name { get { return Get<string>(); } set { Set(value); } }
        public int Age { get { return Get<int>(); } set { Set(value); } }

        public Address HomeAddress { get { return Get<Address>(); } set { Set(value); } }
        public Address WorkAddress { get { return Get<Address>(); } set { Set(value); } }

        public IList<Car> OldCars { get { return Get<IList<Car>>(); } set { Set(value); } }
        public IList<Car> NewCars { get { return Get<IList<Car>>(); } set { Set(value); } }

        public IList<Club> CarClubs { get { return Get<IList<Club>>(); } set { Set(value); } }
        public IList<Club> ComputerClubs { get { return Get<IList<Club>>(); } set { Set(value); } }
    }

    public class Address : Graceful.Model<Address>
    {
        public int StreetNo { get { return Get<int>(); } set { Set(value); } }
        public string StreetName { get { return Get<string>(); } set { Set(value); } }
        public string City { get { return Get<string>(); } set { Set(value); } }

        public Person HomePerson { get { return Get<Person>(); } set { Set(value); } }
        public Person WorkPerson { get { return Get<Person>(); } set { Set(value); } }
    }

    public class Car : Graceful.Model<Car>
    {
        public string Model { get { return Get<string>(); } set { Set(value); } }

        public Person OldPerson { get { return Get<Person>(); } set { Set(value); } }
        public Person NewPerson { get { return Get<Person>(); } set { Set(value); } }
    }

    public class Club : Graceful.Model<Club>
    {
        public IList<Person> CarPersons { get { return Get<IList<Person>>(); } set { Set(value); } }
        public IList<Person> ComputerPersons { get { return Get<IList<Person>>(); } set { Set(value); } }
    }
}
```

Lets look at "HomeAddress" and "HomePerson".
The property names both contain the word "Home",
internally this is what is known as the "LinkIdentifier".

Next notice the use of plurals, for example OldCars and OldPerson.
This is just as important as the LinkIdentifier.

Inserting, Updating, & Deleting Entities
--------------------------------------------------------------------------------
All entities when first created, will have an Id of 0.
Entities with an Id of 0 will __"NEVER"__ exist in the database.
This is how the ORM detects if it needs to _"Insert"_ or _"Update"_ the entity.

```cs
var brad = new Person
{
    Name = "Brad Jones",
    Age = 27
    Address = new Address
    {
        StreetNo = 123,
        StreetName = "Fake St",
        City = "Virtual Land"
    }
};

Assert.Equal(0, brad.Id);
brad.Save();
Assert.Equal(1, brad.Id);
```

> All relationships are saved recursively.

Basically inserting and updating are handled for you, you just need to call
Save() after changing some properties.

To Delete, we need an entity from the database.

```cs
var brad = Person.Find(1);
brad.Delete();
```

This will only soft delete (sets a DeletedAt timestamp), to really delete.

```cs
var brad = Person.Find(1);
brad.Delete(hardDelete: true);
```

### Mass Updates and Deletes
TODO

Querying for Entities
--------------------------------------------------------------------------------

### Query Helper
TODO

### Query Builder
TODO

Dynamic Operations
--------------------------------------------------------------------------------
TODO

Events
--------------------------------------------------------------------------------
TODO

Oh my documenting this is going to be just as involved as building it...

Why Yet Another ORM?
--------------------------------------------------------------------------------
I come from a PHP background for the most part. When I started coding up a .NET
project I started with Entity Framework as my ORM of choice.

This worked fine for a while but then, I found my self wanting to do more and
more complex things and before long I was hitting my head against imaginary
brick walls.

Things that frustrated me:

* Entity Set Caching, when I ask for a value, I half expect the query to made
  there and then on the spot, thus retrieving the very latest data from the
  database.

* The number of layers between my entity and the underlying SQL. Here is an
  example that retrieves the name of the database table being used for a
  particular entity.

  ```cs
  public static string GetTableName()
  {
      var metadata = ((IObjectContextAdapter)Db).ObjectContext.MetadataWorkspace;

      // Get the part of the model that contains info about the actual CLR types
      var objectItemCollection = ((ObjectItemCollection)metadata.GetItemCollection(DataSpace.OSpace));

      // Get the entity type from the model that maps to the CLR type
      var entityType = metadata
              .GetItems<EntityType>(DataSpace.OSpace)
              .Single(e => objectItemCollection.GetClrType(e) == typeof(TModel));

      // Get the entity set that uses this entity type
      var entitySet = metadata
          .GetItems<EntityContainer>(DataSpace.CSpace)
          .Single()
          .EntitySets
          .Single(s => s.ElementType.Name == entityType.Name);

      // Find the mapping between conceptual and storage model for this entity set
      var mapping = metadata.GetItems<EntityContainerMapping>(DataSpace.CSSpace)
              .Single()
              .EntitySetMappings
              .Single(s => s.EntitySet == entitySet);

      // Find the storage entity set (table) that the entity is mapped
      var table = mapping
          .EntityTypeMappings.Single()
          .Fragments.Single()
          .StoreEntitySet;

      // Return the table name from the storage entity set
      return (string)table.MetadataProperties["Table"].Value ?? table.Name;
  }
  ```

  > Yes all that code is needed just to get the name of the table.
  > Considering it's basically just the name of the class,
  > it seems overkill to me. I got that code from: http://goo.gl/5MMDCj
  > I still don't really understand it fully.

* Having to create a _"new"_ entity to find an existing entity,
  I understand why but logically this seemed back to front.

* Working with proxies of my classes, instead of my actual POCO's.

* Batch operations, basically can't do them with stock EF.

I could go on but I won't, don't get me wrong EF still has some pretty awesome
stuff going for it. And now that I have built my own ORM I can see perhaps why
certain things are the way they are... building an ORM is hard and complex!!!

> Feels like saying: DON'T TRY THIS AT HOME KIDS :)

Next I moved over to DbExtensions, I felt more comfortable here, there are less
layers between the entity and the SQL. However again there were things I found
frustrating.

The first version of this project was built on top of DbExtensions. But over
time I kept replacing DbExtension code with my own, until I really wasn't using
anything but the query builder.

So I started from scratch, used ideas from Entity Framework, DbExtensions,
Laravel, Linq, etc. I now have something that works for me and my needs.

--------------------------------------------------------------------------------
Developed by Brad Jones - brad@bjc.id.au
