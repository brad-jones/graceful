namespace Graceful.Tests.Models
{
    using System.Collections.Generic;

    [Connection(@"BOGUS CS - DONT WANT TO BE INCLUDED IN GLOBAL CTX!")]
    public class MultipleManyToManyTestModel1 : Model<MultipleManyToManyTestModel1>
    {
        public List<MultipleManyToManyTestModel2> FooMultipleManyToManyTestModel2s { get; set; }
        public List<MultipleManyToManyTestModel2> BarMultipleManyToManyTestModel2s { get; set; }
    }

    [Connection(@"BOGUS CS - DONT WANT TO BE INCLUDED IN GLOBAL CTX!")]
    public class MultipleManyToManyTestModel2 : Model<MultipleManyToManyTestModel2>
    {
        public List<MultipleManyToManyTestModel1> FooMultipleManyToManyTestModel1s { get; set; }
        public List<MultipleManyToManyTestModel1> BarMultipleManyToManyTestModel1s { get; set; }
    }
}
