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
    using System.Collections.Generic;

    [Connection(@"BOGUS CS - DONT WANT TO BE INCLUDED IN GLOBAL CTX!")]
    public class MultipleManyToOneTestModel1 : Model<MultipleManyToOneTestModel1>
    {
        public List<MultipleManyToOneTestModel2> FooMultipleManyToOneTestModel2s { get; set; }
        public List<MultipleManyToOneTestModel2> BarMultipleManyToOneTestModel2s { get; set; }
    }

    [Connection(@"BOGUS CS - DONT WANT TO BE INCLUDED IN GLOBAL CTX!")]
    public class MultipleManyToOneTestModel2 : Model<MultipleManyToOneTestModel2>
    {
        public MultipleManyToOneTestModel1 FooMultipleManyToOneTestModel1 { get; set; }
        public MultipleManyToOneTestModel1 BarMultipleManyToOneTestModel1 { get; set; }
    }
}
