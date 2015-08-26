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

namespace Graceful.Tests
{
    using Xunit;
    using System;
    using System.Collections.Generic;
    using Graceful.Extensions;

    public class ExtensionMethodTests
    {
        [Fact]
        public void ForEachWithIndexTest()
        {
            var fooList = new List<string>{ "abc", "xyz" };

            fooList.ForEachWithIndex((key, value) =>
            {
                Assert.Equal(fooList.IndexOf(value), key);
            });
        }
    }
}
