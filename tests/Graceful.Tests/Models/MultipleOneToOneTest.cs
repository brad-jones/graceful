namespace Graceful.Tests.Models
{
    using System.Collections.Generic;

    [Connection(@"BOGUS CS - DONT WANT TO BE INCLUDED IN GLOBAL CTX!")]
    public class MultipleOneToOneTestModel1 : Model<MultipleOneToOneTestModel1>
    {
        public MultipleOneToOneTestModel2 FooMultipleOneToOneTestModel2 { get; set; }
        public MultipleOneToOneTestModel2 BarMultipleOneToOneTestModel2 { get; set; }
    }

    [Connection(@"BOGUS CS - DONT WANT TO BE INCLUDED IN GLOBAL CTX!")]
    public class MultipleOneToOneTestModel2 : Model<MultipleOneToOneTestModel2>
    {
        public MultipleOneToOneTestModel1 FooMultipleOneToOneTestModel1 { get; set; }
        public MultipleOneToOneTestModel1 BarMultipleOneToOneTestModel1 { get; set; }
    }
}
