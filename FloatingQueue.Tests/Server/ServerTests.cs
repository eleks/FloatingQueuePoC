using System;
using NUnit.Framework;
using ServerClass = FloatingQueue.Server.Core.Server;

namespace FloatingQueue.Tests.Server
{
    [TestFixture]
    public class ServerTests : TestBase
    {
        [Test]
        public void InitForbidsNullArgumentsTest()
        {
            Assert.Throws<ArgumentNullException>(() =>  ServerClass.Init(null));
        }

        [Test]
        public void FireTransactionCommitedChangesCounterTest()
        {
            var counter = ServerClass.TransactionCounter;
            ServerClass.FireTransactionCommited();

            Assert.AreNotEqual(counter, ServerClass.TransactionCounter);
        }
    }
}
