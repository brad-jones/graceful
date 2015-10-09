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
