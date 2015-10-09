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
