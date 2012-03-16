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
    public class MasterElectionsTests : TestBase
    {
        private const byte ServerId = 123;
        private const byte MasterId = 1;
        private readonly Mock<IConnectionManager> m_ConnectionManagerMock = new Mock<IConnectionManager>();
        private readonly Mock<IServerConfiguration> m_ServerConfigurationMock = new Mock<IServerConfiguration>();
        private readonly Mock<INodeCollection> m_NodesCollectionMock = new Mock<INodeCollection>();
        private readonly Mock<INodeConfiguration> m_MasterConfigurationMock = new Mock<INodeConfiguration>();

        protected override void RegisterMocks(Autofac.ContainerBuilder containerBuilder)
        {
            base.RegisterMocks(containerBuilder);
            containerBuilder.RegisterInstance(m_ConnectionManagerMock.Object).As<IConnectionManager>();

            m_ServerConfigurationMock.SetupGet(m => m.Nodes).Returns(m_NodesCollectionMock.Object);
            containerBuilder.RegisterInstance(m_ServerConfigurationMock.Object).As<IServerConfiguration>();

            m_NodesCollectionMock.SetupGet(m => m.Master).Returns(m_MasterConfigurationMock.Object);
            containerBuilder.RegisterInstance(m_NodesCollectionMock.Object).As<INodeCollection>();

            m_MasterConfigurationMock.SetupGet(m => m.ServerId).Returns(MasterId);
            containerBuilder.RegisterInstance(m_MasterConfigurationMock.Object).As<INodeConfiguration>();
        }

        [Test, ExpectedException(typeof(ApplicationException))]
        public void LostConnectionWithItselfTest()
        {
            var elections = new MasterElections();
            elections.Init();

            m_ServerConfigurationMock.SetupGet(m => m.ServerId).Returns(ServerId);
            m_ConnectionManagerMock.Raise(m => m.OnConnectionLoss += null, ServerId);
        }

        [Test]
        public void LostConnectionWithSlaveTest()
        {
            byte lostServerId = 121;

            var elections = new MasterElections();
            elections.Init();

            m_ServerConfigurationMock.SetupGet(m => m.ServerId).Returns(ServerId);

            m_NodesCollectionMock.Setup(m => m.RemoveDeadNode(lostServerId)).Verifiable();

            m_ConnectionManagerMock.Raise(m => m.OnConnectionLoss += null, lostServerId);

            m_NodesCollectionMock.Verify(m => m.RemoveDeadNode(lostServerId));
        }

        [Test]
        public void LostConnectionWithMasterTest()
        {
            byte lostServerId = MasterId;

            var elections = new MasterElections();
            elections.Init();

            m_ServerConfigurationMock.SetupGet(m => m.ServerId).Returns(ServerId);

            var sibling1 = new Mock<INodeConfiguration>();
            sibling1.SetupGet(m => m.ServerId).Returns(2);
            sibling1.Setup(m => m.DeclareAsNewMaster()).Verifiable();

            var sibling2 = new Mock<INodeConfiguration>();
            sibling2.SetupGet(m => m.ServerId).Returns(3);

            m_NodesCollectionMock.SetupGet(m => m.All).Returns(new[] { sibling1.Object, sibling2.Object }.ToList().AsReadOnly());

            m_NodesCollectionMock.Setup(m => m.RemoveDeadNode(lostServerId)).Verifiable();

            m_ConnectionManagerMock.Raise(m => m.OnConnectionLoss += null, lostServerId);

            m_NodesCollectionMock.Verify(m => m.RemoveDeadNode(lostServerId));
            sibling1.Verify(m => m.DeclareAsNewMaster());
        }
    }
}
