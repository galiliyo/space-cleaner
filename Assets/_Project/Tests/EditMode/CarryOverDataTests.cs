using NUnit.Framework;
using SpaceCleaner.Boss;

namespace SpaceCleaner.Tests
{
    public class CarryOverDataTests
    {
        [SetUp]
        public void SetUp()
        {
            // Ensure clean state before each test
            CarryOverData.GetAndClear();
        }

        [Test]
        public void Record_StoresEntry()
        {
            CarryOverData.Record("Buzz", 25);
            var entries = CarryOverData.GetAndClear();
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("Buzz", entries[0].name);
            Assert.AreEqual(25, entries[0].ammo);
        }

        [Test]
        public void GetAndClear_ClearsAfterRetrieval()
        {
            CarryOverData.Record("Buzz", 25);
            CarryOverData.GetAndClear(); // first call retrieves
            var entries = CarryOverData.GetAndClear(); // second call should be empty
            Assert.AreEqual(0, entries.Count);
        }

        [Test]
        public void MultipleRecords_Accumulate()
        {
            CarryOverData.Record("Buzz", 25);
            CarryOverData.Record("Rex", 40);
            var entries = CarryOverData.GetAndClear();
            Assert.AreEqual(2, entries.Count);
        }

        [Test]
        public void GetAndClear_OnEmpty_ReturnsEmptyList()
        {
            var entries = CarryOverData.GetAndClear();
            Assert.IsNotNull(entries);
            Assert.AreEqual(0, entries.Count);
        }
    }
}
