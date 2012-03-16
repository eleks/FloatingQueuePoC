using System;
using NUnit.Framework;

namespace FloatingQueue.Server.Tests
{
    [TestFixture]
    public class ServerTests : TestBase
    {
        [Test]
        public void InitForbidsNullArgumentsTest()
        {
            Assert.Throws<ArgumentNullException>(() => Core.Server.Init(null));
        }

        [Test]
        public void FireTransactionCommitedChangesCounterTest()
        {
            var counter = Core.Server.TransactionCounter;
            Core.Server.FireTransactionCommited();

            Assert.AreNotEqual(counter, Core.Server.TransactionCounter);
        }
    }
}
