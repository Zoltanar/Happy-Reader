using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Happy_Apps_Core_Tests
{
    [TestClass]
    public class DatabaseTests
    {
        [TestMethod]
        public void Connection()
        {
            var testDatabase = new VisualNovelDatabase(StaticHelpers.DatabaseFile, true);            

        }
    }
}
