using System;
using Autofac;
using FloatingQueue.Server.Core;
using FloatingQueue.Server.Replication;
using Moq;
using NUnit.Framework;
using ServerClass = FloatingQueue.Server.Core.Server;

namespace FloatingQueue.Tests.Server
{
    [TestFixture]
    public class ConfigurationTests : TestBase
    {
        private readonly Mock<IServerConfiguration> m_ServerConfigurationMock = new Mock<IServerConfiguration>();

        protected override void RegisterMocks(Autofac.ContainerBuilder containerBuilder)
        {
            base.RegisterMocks(containerBuilder);
            containerBuilder.RegisterInstance(m_ServerConfigurationMock.Object).As<IServerConfiguration>();
        }
            
        [Test, Combinatorial]
        public void ServerConfigurationTest([Values(false, true)]bool isMaster, [Values("a", "b")]string address)
        {
            var nodeMock = new Mock<INodeConfiguration>();
            nodeMock.SetupGet(m => m.InternalAddress).Returns(address);
            nodeMock.SetupGet(m => m.IsMaster).Returns(isMaster);

            var collectionMock = new Mock<INodeCollection>();
            collectionMock.SetupGet(m => m.Self).Returns(nodeMock.Object);

            var serverConfiguration = new ServerConfiguration { Nodes = collectionMock.Object };

            Assert.AreEqual(isMaster, serverConfiguration.IsMaster);
            Assert.AreEqual(address, serverConfiguration.InternalAddress);
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void NodeConfiguration_DeclareAsNewMaster_AlreadyMaster_Test()
        {
            var nodeConfiguration = new NodeConfiguration();
            nodeConfiguration.DeclareAsNewMaster();
            nodeConfiguration.DeclareAsNewMaster();
        }

        [Test]
        public void NodeConfiguration_DeclareAsNewMaster_Success_Test()
        {
            var nodeConfiguration = new NodeConfiguration();
            nodeConfiguration.DeclareAsNewMaster();
            Assert.IsTrue(nodeConfiguration.IsMaster);
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void NodeConfiguration_DeclareAsSynced_AlreadySynced_Test()
        {
            var serverConfiguration = new ServerConfiguration();
            serverConfiguration.DeclareAsSyncedNode();
            serverConfiguration.DeclareAsSyncedNode();
        }

        //note MM: looks like this test doesn't make sense anymore
        //[Test, ExpectedException(typeof(InvalidOperationException))]
        //public void NodeConfiguration_DeclareAsSynced_AlreadyMaster_Test()
        //{
        //    var nodeConfiguration = new NodeConfiguration();
        //    nodeConfiguration.DeclareAsSyncedNode();
        //    nodeConfiguration.DeclareAsNewMaster();
        //    nodeConfiguration.IsSynced = false;
        //    nodeConfiguration.DeclareAsSyncedNode();
        //}

        [Test]
        public void NodeConfiguration_DeclareAsSynced_Success_Test()
        {
            var serverConfiguration = new ServerConfiguration();
            serverConfiguration.DeclareAsSyncedNode();
            Assert.IsTrue(serverConfiguration.IsSynced);
        }

        [Test]
        public void ServerConfigurationInstanceTest()
        {
            Assert.AreEqual(m_ServerConfigurationMock.Object, ServerClass.Configuration);
        }
    }
}
