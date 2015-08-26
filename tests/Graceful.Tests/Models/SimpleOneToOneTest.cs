namespace Graceful.Tests.Models
{
    using System.Collections.Generic;

    [Connection(@"BOGUS CS - DONT WANT TO BE INCLUDED IN GLOBAL CTX!")]
    public class SimpleOneToOneTestModel1 : Model<SimpleOneToOneTestModel1>
    {
        public SimpleOneToOneTestModel2 Foo { get; set; }
    }

    [Connection(@"BOGUS CS - DONT WANT TO BE INCLUDED IN GLOBAL CTX!")]
    public class SimpleOneToOneTestModel2 : Model<SimpleOneToOneTestModel2>
    {
        public SimpleOneToOneTestModel1 Bar { get; set; }
    }
}
