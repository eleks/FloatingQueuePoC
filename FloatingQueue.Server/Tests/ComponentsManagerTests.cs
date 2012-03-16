using System;
using FloatingQueue.Server.Core;
using Moq;
using NUnit.Framework;

namespace FloatingQueue.Server.Tests
{
    [TestFixture]
    public class ComponentsManagerTests : TestBase
    {
        [Test]
        public void ConstructorForbidsNullArgumentsTest()
        {
            Assert.Throws<ArgumentNullException>(() => new ComponentsManager().GetContainer(null));
        }

        [Test]
        public void ReturnContainerNotNullTest()
        {
            var manager = new ComponentsManager();
            var container = manager.GetContainer(new Mock<IServerConfiguration>().Object);

            Assert.IsNotNull(container);
        }
    }
}