using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FloatingQueue.Server.Replication;
using NUnit.Framework;

namespace FloatingQueue.Tests.Server
{
    [TestFixture]
    public class SyncTests : TestBase
    {
        [Test]
        public void AreEqualToVersionsCountTest()
        {
            var dic1 = new Dictionary<string, int>() {{"a", 1}};
            var dic2 = new Dictionary<string, int>();

            Assert.IsFalse(dic1.AreEqualToVersions(dic2));
        }

        [Test]
        public void AreEqualToVersionsKeysTest()
        {
            var dic1 = new Dictionary<string, int>() { { "a", 1 } };
            var dic2 = new Dictionary<string, int>() { { "b", 1 } };

            Assert.IsFalse(dic1.AreEqualToVersions(dic2));
        }

        [Test]
        public void AreEqualToVersionsValuesTest()
        {
            var dic1 = new Dictionary<string, int>() { { "a", 1 } };
            var dic2 = new Dictionary<string, int>() { { "a", 2 } };

            Assert.IsFalse(dic1.AreEqualToVersions(dic2));
        }

        [Test]
        public void AreEqualToVersionsTest()
        {
            var dic1 = new Dictionary<string, int>() { { "a", 1 } };
            var dic2 = new Dictionary<string, int>() { { "a", 1 } };

            Assert.IsTrue(dic1.AreEqualToVersions(dic2));
        }
    }
}
