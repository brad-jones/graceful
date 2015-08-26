namespace Graceful.Tests.Models
{
    using System.Collections.Generic;

    [Connection(@"BOGUS CS - DONT WANT TO BE INCLUDED IN GLOBAL CTX!")]
    public class SimpleManyToManyTestModel1 : Model<SimpleManyToManyTestModel1>
    {
        public List<SimpleManyToManyTestModel2> Foos { get; set; }
    }

    [Connection(@"BOGUS CS - DONT WANT TO BE INCLUDED IN GLOBAL CTX!")]
    public class SimpleManyToManyTestModel2 : Model<SimpleManyToManyTestModel2>
    {
        public List<SimpleManyToManyTestModel1> Bars { get; set; }
    }
}
