using System;
using System.Linq;
using System.Threading;
using Autofac;
using FloatingQueue.Common;
using FloatingQueue.Server.Core;
using FloatingQueue.Server.Replication;
using FloatingQueue.Server.Services.Proxy;
using Moq;
using NUnit.Framework;

namespace FloatingQueue.Tests.Server
{
    [TestFixture]
    public class ConnectionManagerTests : TestBase
    {
        private readonly Mock<INodeConfiguration> m_SiblingMock = new Mock<INodeConfiguration>();
        private readonly Mock<IInternalQueueServiceProxy> m_SiblingProxyMock = new Mock<IInternalQueueServiceProxy>();
        private readonly Mock<INodeCollection> m_SiblingsMock = new Mock<INodeCollection>();
        private readonly Mock<IServerConfiguration> m_ServerConfigurationMock = new Mock<IServerConfiguration>();

        protected override void RegisterMocks(ContainerBuilder containerBuilder)
        {
            base.RegisterMocks(containerBuilder);
            m_SiblingMock.SetupGet(m => m.Proxy).Returns(m_SiblingProxyMock.Object);
            m_SiblingsMock.SetupGet(m => m.Siblings).Returns(new[] { m_SiblingMock.Object }.ToList().AsReadOnly());
            m_ServerConfigurationMock.SetupGet(m => m.PingTimeout).Returns(10);
            m_ServerConfigurationMock.SetupGet(m => m.Nodes).Returns(m_SiblingsMock.Object);
            containerBuilder.RegisterInstance(m_ServerConfigurationMock.Object).As<IServerConfiguration>();
        }

        [Test]
        public void OpenOutcommingConnectionsTest()
        {
            m_SiblingProxyMock.Setup(m => m.Open()).Verifiable();

            var connectionManager = new ConnectionManager();
            connectionManager.OpenOutcomingConnections();

            m_SiblingProxyMock.Verify();
        }

        [Test]
        public void CloseOutcommingConnectionsTest()
        {
            m_SiblingProxyMock.Setup(m => m.Close()).Verifiable();

            var connectionManager = new ConnectionManager();
            connectionManager.CloseOutcomingConnections();

            m_SiblingProxyMock.Verify();
        }

        [Test]
        public void MonitoringTest()
        {
            m_SiblingProxyMock.Setup(m => m.Ping()).Verifiable();
            var connectionManager = new ConnectionManager();
            connectionManager.OpenOutcomingConnections();
            Thread.Sleep(100);
            m_SiblingProxyMock.Verify();
        }

        [Test]
        public void MonitoringInterruptedTest()
        {
            m_SiblingProxyMock.Setup(m => m.Ping()).Verifiable();
            var connectionManager = new ConnectionManager();
            connectionManager.OpenOutcomingConnections();
            Thread.Sleep(100);
            m_SiblingProxyMock.Verify();
            
            connectionManager.CloseOutcomingConnections();
            m_SiblingProxyMock.Setup(m => m.Ping()).Throws(new AssertionException("Ping after connection closed"));
            Thread.Sleep(100);
        }

        [Test]
        public void LostConnectionTest()
        {
            m_SiblingProxyMock.Setup(m => m.Ping()).Throws<ConnectionErrorException>().Verifiable();
            m_SiblingMock.SetupGet(m => m.ServerId).Returns(123);
            var connectionManager = new ConnectionManager();

            var lostId = 0;
            connectionManager.OnConnectionLoss += (id) => { lostId = id; };

            connectionManager.OpenOutcomingConnections();
            Thread.Sleep(100);
            Assert.AreEqual(lostId, 123);
        }


        [Test]
        public void TryReplicate_Success_Test()
        {
            m_SiblingProxyMock.Setup(m => m.Open()).Verifiable("Server hasn't opened connections before replication");
            m_SiblingProxyMock.Setup(m => m.Push("a", 123, "b")).Verifiable("Server hasn't pushed data to siblings");
            
            var connectionManager = new ConnectionManager();
            connectionManager.OpenOutcomingConnections();
            Assert.IsTrue(connectionManager.TryReplicate("a", 123, "b"));
            
            m_SiblingProxyMock.Verify();
        }

        [Test]
        public void TryReplicate_ConnectionLost_Test()
        {
            //Assert.Fail("Change CommunicationException by some more generic");

            m_SiblingProxyMock.Setup(m => m.Open()).Verifiable("Server hasn't opened connections before replication");
            m_SiblingProxyMock.Setup(m => m.Push("a", 123, "b")).Throws(new ConnectionErrorException()); // it's the only place in assembly that requires ServiceModel lib

            var connectionManager = new ConnectionManager();
            var connectionLostFired = false;
            connectionManager.OnConnectionLoss += (id) => { connectionLostFired = true; };
            connectionManager.OpenOutcomingConnections();
            Assert.IsFalse(connectionManager.TryReplicate("a", 123, "b"));

            m_SiblingProxyMock.Verify();
            Assert.IsTrue(connectionLostFired);
        }

        [Test]
        public void TryReplicate_GeneralError_Test()
        {
            m_SiblingProxyMock.Setup(m => m.Open()).Verifiable("Server hasn't opened connections before replication");
            m_SiblingProxyMock.Setup(m => m.Push("a", 123, "b")).Throws(new Exception());

            var connectionManager = new ConnectionManager();
            var connectionLostFired = false;
            connectionManager.OnConnectionLoss += (id) => { connectionLostFired = true; };
            connectionManager.OpenOutcomingConnections();
            Assert.IsFalse(connectionManager.TryReplicate("a", 123, "b"));

            m_SiblingProxyMock.Verify();
            Assert.IsFalse(connectionLostFired);
        }
    }
}
