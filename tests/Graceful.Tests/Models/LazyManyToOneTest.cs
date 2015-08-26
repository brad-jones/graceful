namespace Graceful.Tests.Models
{
    using System.Collections.Generic;

    [Connection(@"BOGUS CS - DONT WANT TO BE INCLUDED IN GLOBAL CTX!")]
    public class LazyManyToOneTestModel1 : Model<LazyManyToOneTestModel1>
    {
        public List<LazyManyToOneTestModel2> Foos { get; set; }
        public List<LazyManyToOneTestModel3> Bars { get; set; }
        public List<LazyManyToOneTestModel3> Bazs { get; set; }
    }

    [Connection(@"BOGUS CS - DONT WANT TO BE INCLUDED IN GLOBAL CTX!")]
    public class LazyManyToOneTestModel2 : Model<LazyManyToOneTestModel2>
    {

    }

    [Connection(@"BOGUS CS - DONT WANT TO BE INCLUDED IN GLOBAL CTX!")]
    public class LazyManyToOneTestModel3 : Model<LazyManyToOneTestModel3>
    {

    }
}
