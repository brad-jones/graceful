namespace Graceful.Tests.Models
{
    using System.Collections.Generic;

    [Connection(@"BOGUS CS - DONT WANT TO BE INCLUDED IN GLOBAL CTX!")]
    public class SimpleManyToOneTestModel1 : Model<SimpleManyToOneTestModel1>
    {
        public List<SimpleManyToOneTestModel2> Foos { get; set; }
    }

    [Connection(@"BOGUS CS - DONT WANT TO BE INCLUDED IN GLOBAL CTX!")]
    public class SimpleManyToOneTestModel2 : Model<SimpleManyToOneTestModel2>
    {
        public SimpleManyToOneTestModel1 Bar { get; set; }
    }
}
