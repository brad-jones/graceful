namespace Graceful.Tests.Models
{
    using System.Collections.Generic;

    [Connection(@"BOGUS CS - DONT WANT TO BE INCLUDED IN GLOBAL CTX!")]
    public class LazyOneToOneTestModel1 : Model<LazyOneToOneTestModel1>
    {
        public LazyOneToOneTestModel2 Foo { get; set; }
        public LazyOneToOneTestModel3 Bar { get; set; }
        public LazyOneToOneTestModel3 Baz { get; set; }
    }

    [Connection(@"BOGUS CS - DONT WANT TO BE INCLUDED IN GLOBAL CTX!")]
    public class LazyOneToOneTestModel2 : Model<LazyOneToOneTestModel2>
    {

    }

    [Connection(@"BOGUS CS - DONT WANT TO BE INCLUDED IN GLOBAL CTX!")]
    public class LazyOneToOneTestModel3 : Model<LazyOneToOneTestModel3>
    {

    }
}
