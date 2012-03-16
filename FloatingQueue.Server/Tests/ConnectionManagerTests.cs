using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using Autofac;
using FloatingQueue.Server.Core;
using FloatingQueue.Server.Replication;
using FloatingQueue.Server.Services.Proxy;
using Moq;
using NUnit.Framework;

namespace FloatingQueue.Server.Tests
{
    [TestFixture]
    public class ConnectionManagerTests : TestBase
    {
        private readonly Mock<INodeConfiguration> m_SyncedSiblingMock = new Mock<INodeConfiguration>();
        private readonly Mock<IInternalQueueServiceProxy> m_SyncedSiblingProxyMock = new Mock<IInternalQueueServiceProxy>();
        private readonly Mock<INodeConfiguration> m_NonSyncedSiblingMock = new Mock<INodeConfiguration>();
        private readonly Mock<INodeCollection> m_SiblingsMock = new Mock<INodeCollection>();
        private readonly Mock<IServerConfiguration> m_ServerConfigurationMock = new Mock<IServerConfiguration>();

        protected override void RegisterMocks(ContainerBuilder containerBuilder)
        {
            base.RegisterMocks(containerBuilder);
            m_SyncedSiblingMock.SetupGet(m => m.Proxy).Returns(m_SyncedSiblingProxyMock.Object);
            m_SiblingsMock.SetupGet(m => m.SyncedSiblings).Returns(new[] {m_SyncedSiblingMock.Object});
            m_SiblingsMock.SetupGet(m => m.Siblings).Returns(new[] {m_SyncedSiblingMock.Object, m_NonSyncedSiblingMock.Object});
            m_ServerConfigurationMock.SetupGet(m => m.PingTimeout).Returns(10);
            m_ServerConfigurationMock.SetupGet(m => m.Nodes).Returns(m_SiblingsMock.Object);
            containerBuilder.RegisterInstance(m_ServerConfigurationMock.Object).As<IServerConfiguration>();
        }

        [Test]
        public void OpenOutcommingConnectionsTest()
        {
            m_SyncedSiblingProxyMock.Setup(m => m.Open()).Verifiable();

            var connectionManager = new ConnectionManager();
            connectionManager.OpenOutcomingConnections();

            m_SyncedSiblingProxyMock.Verify();
        }

        [Test]
        public void CloseOutcommingConnectionsTest()
        {
            m_SyncedSiblingProxyMock.Setup(m => m.Close()).Verifiable();

            var connectionManager = new ConnectionManager();
            connectionManager.CloseOutcomingConnections();

            m_SyncedSiblingProxyMock.Verify();
        }

        [Test]
        public void MonitoringTest()
        {
            m_SyncedSiblingProxyMock.Setup(m => m.Ping()).Returns(0).Verifiable();
            var connectionManager = new ConnectionManager();
            connectionManager.OpenOutcomingConnections();
            Thread.Sleep(100);
            m_SyncedSiblingProxyMock.Verify();
        }

        [Test]
        public void MonitoringInterruptedTest()
        {
            m_SyncedSiblingProxyMock.Setup(m => m.Ping()).Returns(0).Verifiable();
            var connectionManager = new ConnectionManager();
            connectionManager.OpenOutcomingConnections();
            Thread.Sleep(100);
            m_SyncedSiblingProxyMock.Verify();
            
            connectionManager.CloseOutcomingConnections();
            m_SyncedSiblingProxyMock.Setup(m => m.Ping()).Throws(new AssertionException("Ping after connection closed"));
            Thread.Sleep(100);
        }

        [Test]
        public void LostConnectionTest()
        {
            m_SyncedSiblingProxyMock.Setup(m => m.Ping()).Returns(1).Verifiable();
            m_SyncedSiblingMock.SetupGet(m => m.ServerId).Returns(123);
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
            m_SyncedSiblingProxyMock.Setup(m => m.Open()).Verifiable("Server hasn't opened connections before replication");
            m_SyncedSiblingProxyMock.Setup(m => m.Push("a", 123, "b")).Verifiable("Server hasn't pushed data to siblings");
            
            var connectionManager = new ConnectionManager();
            Assert.IsTrue(connectionManager.TryReplicate("a", 123, "b"));
            
            m_SyncedSiblingProxyMock.Verify();
        }

        [Test]
        public void TryReplicate_ConnectionLost_Test()
        {
            m_SyncedSiblingProxyMock.Setup(m => m.Open()).Verifiable("Server hasn't opened connections before replication");
            m_SyncedSiblingProxyMock.Setup(m => m.Push("a", 123, "b")).Throws(new CommunicationException());

            var connectionManager = new ConnectionManager();
            var connectionLostFired = false;
            connectionManager.OnConnectionLoss += (id) => { connectionLostFired = true; };
            Assert.IsFalse(connectionManager.TryReplicate("a", 123, "b"));

            m_SyncedSiblingProxyMock.Verify();
            Assert.IsTrue(connectionLostFired);
        }

        [Test]
        public void TryReplicate_GeneralError_Test()
        {
            m_SyncedSiblingProxyMock.Setup(m => m.Open()).Verifiable("Server hasn't opened connections before replication");
            m_SyncedSiblingProxyMock.Setup(m => m.Push("a", 123, "b")).Throws(new Exception());

            var connectionManager = new ConnectionManager();
            var connectionLostFired = false;
            connectionManager.OnConnectionLoss += (id) => { connectionLostFired = true; };
            Assert.IsFalse(connectionManager.TryReplicate("a", 123, "b"));

            m_SyncedSiblingProxyMock.Verify();
            Assert.IsFalse(connectionLostFired);
        }
    }
}
