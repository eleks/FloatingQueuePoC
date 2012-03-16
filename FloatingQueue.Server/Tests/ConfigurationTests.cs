using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using FloatingQueue.Server.Core;
using FloatingQueue.Server.Replication;
using Moq;
using NUnit.Framework;

namespace FloatingQueue.Server.Tests
{
    [TestFixture]
    public class ConfigurationTests : TestBase
    {
        private Mock<IServerConfiguration> m_ServerConfigurationMock = new Mock<IServerConfiguration>();

        protected override void RegisterMocks(Autofac.ContainerBuilder containerBuilder)
        {
            base.RegisterMocks(containerBuilder);
            containerBuilder.RegisterInstance(m_ServerConfigurationMock.Object).As<IServerConfiguration>();
        }
            
        [Test, Combinatorial]
        public void ServerConfigurationTest([Values(false, true)]bool isMaster, [Values(false, true)]bool isSynced, [Values(false, true)]bool isReadonly, [Values("a", "b")]string address)
        {
            var nodeMock = new Mock<INodeConfiguration>();
            nodeMock.SetupGet(m => m.InternalAddress).Returns(address);
            nodeMock.SetupGet(m => m.IsMaster).Returns(isMaster);
            nodeMock.SetupGet(m => m.IsSynced).Returns(isSynced);
            nodeMock.SetupGet(m => m.IsReadonly).Returns(isReadonly);

            var collectionMock = new Mock<INodeCollection>();
            collectionMock.SetupGet(m => m.Self).Returns(nodeMock.Object);

            var serverConfiguration = new ServerConfiguration { Nodes = collectionMock.Object };

            Assert.AreEqual(isMaster, serverConfiguration.IsMaster);
            Assert.AreEqual(isSynced, serverConfiguration.IsSynced);
            Assert.AreEqual(isReadonly, serverConfiguration.IsReadonly);
            Assert.AreEqual(address, serverConfiguration.InternalAddress);
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void NodeConfiguration_DeclareAsNewMaster_NonSync_Test()
        {
            var nodeConfiguration = new NodeConfiguration();
            nodeConfiguration.DeclareAsNewMaster();
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void NodeConfiguration_DeclareAsNewMaster_AlreadyMaster_Test()
        {
            var nodeConfiguration = new NodeConfiguration();
            nodeConfiguration.DeclareAsSyncedNode();
            nodeConfiguration.DeclareAsNewMaster();
            nodeConfiguration.DeclareAsNewMaster();
        }

        [Test]
        public void NodeConfiguration_DeclareAsNewMaster_Success_Test()
        {
            var nodeConfiguration = new NodeConfiguration();
            nodeConfiguration.DeclareAsSyncedNode();
            nodeConfiguration.DeclareAsNewMaster();
            Assert.IsTrue(nodeConfiguration.IsMaster);
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void NodeConfiguration_DeclareAsSynced_AlreadySynced_Test()
        {
            var nodeConfiguration = new NodeConfiguration();
            nodeConfiguration.DeclareAsSyncedNode();
            nodeConfiguration.DeclareAsSyncedNode();
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void NodeConfiguration_DeclareAsSynced_AlreadyMaster_Test()
        {
            var nodeConfiguration = new NodeConfiguration();
            nodeConfiguration.DeclareAsSyncedNode();
            nodeConfiguration.DeclareAsNewMaster();
            nodeConfiguration.IsSynced = false;
            nodeConfiguration.DeclareAsSyncedNode();
        }

        [Test]
        public void NodeConfiguration_DeclareAsSynced_Success_Test()
        {
            var nodeConfiguration = new NodeConfiguration();
            nodeConfiguration.DeclareAsSyncedNode();
            Assert.IsTrue(nodeConfiguration.IsSynced);
        }

        [Test]
        public void ServerConfigurationInstanceTest()
        {
            Assert.AreEqual(m_ServerConfigurationMock.Object, Core.Server.Configuration);
        }
    }
}
