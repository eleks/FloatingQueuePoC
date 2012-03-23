using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Autofac;
using FloatingQueue.Server.Core;
using FloatingQueue.Server.EventsLogic;
using FloatingQueue.Server.Exceptions;
using FloatingQueue.Server.Replication;
using FloatingQueue.Server.Services;
using FloatingQueue.Server.Services.Proxy;
using Moq;
using NUnit.Framework;

namespace FloatingQueue.Tests.Server
{
    [TestFixture]
    public class NodeInitializerTests : TestBase
    {
        private Mock<IServerConfiguration> m_ServerConfigurationMock;
        private Mock<INodeCollection> m_NodesMock;
        private Mock<INodeConfiguration> m_MasterNodeMock;
        private Mock<IInternalQueueServiceProxy> m_MasterProxyMock;
        private Mock<IAggregateRepository> m_AggregateRepositoryMock;

        public override void Setup()
        {
            m_ServerConfigurationMock = new Mock<IServerConfiguration>();
            m_NodesMock = new Mock<INodeCollection>();
            m_MasterNodeMock = new Mock<INodeConfiguration>();
            m_MasterProxyMock = new Mock<IInternalQueueServiceProxy>();
            m_AggregateRepositoryMock = new Mock<IAggregateRepository>();
            base.Setup();
        }

        protected override void RegisterMocks(Autofac.ContainerBuilder containerBuilder)
        {
            base.RegisterMocks(containerBuilder);
            m_MasterNodeMock.SetupGet(m => m.Proxy).Returns(m_MasterProxyMock.Object);
            m_NodesMock.SetupGet(m => m.Master).Returns(m_MasterNodeMock.Object);
            m_ServerConfigurationMock.SetupGet(m => m.Nodes).Returns(m_NodesMock.Object);
            containerBuilder.RegisterInstance(m_ServerConfigurationMock.Object).As<IServerConfiguration>();
            containerBuilder.RegisterInstance(m_AggregateRepositoryMock.Object).As<IAggregateRepository>();
        }
            
        [Test]
        public void StartSynchronizationTest()
        {
            m_MasterProxyMock.Setup(m => m.Open()).Verifiable();
            m_MasterProxyMock.Setup(m => m.Close());
            m_MasterProxyMock.Setup(m => m.RequestSynchronization(It.IsAny<ExtendedNodeInfo>(), It.IsAny<Dictionary<string, int>>())).Verifiable();
            var initializer = new NodeInitializer();
            initializer.StartSynchronization();

            m_MasterProxyMock.Verify();
        }

        [Test, ExpectedException(typeof(BadConfigurationException))]
        public void EnsureConfigurationValidMasterCountTest()
        {
            var nodes = Mock.Of<INodeCollection>(m => m.All == new ReadOnlyCollection<INodeConfiguration>(new[]
                                                                                                                   {
                                                                                                                       GetNodeMock(true, 1),
                                                                                                                       GetNodeMock(true, 2),
                                                                                                                   }));
            var initializer = new NodeInitializer();
            initializer.EnsureNodesConfigurationIsValid(nodes);
        }

        [Test, ExpectedException(typeof(BadConfigurationException))]
        public void EnsureConfigurationValidDistinctCountTest()
        {
            var nodes = Mock.Of<INodeCollection>(m => m.All == new ReadOnlyCollection<INodeConfiguration>(new[]
                                                                                                                   {
                                                                                                                       GetNodeMock(true, 1),
                                                                                                                       GetNodeMock(false, 1),
                                                                                                                   }));
            var initializer = new NodeInitializer();
            initializer.EnsureNodesConfigurationIsValid(nodes);
        }

        [Test]
        public void EnsureConfigurationValidOkTest()
        {
            var nodes = Mock.Of<INodeCollection>(m => m.All == new ReadOnlyCollection<INodeConfiguration>(new[]
                                                                                                                   {
                                                                                                                       GetNodeMock(true, 1),
                                                                                                                       GetNodeMock(false, 2),
                                                                                                                   }));
            var initializer = new NodeInitializer();
            initializer.EnsureNodesConfigurationIsValid(nodes);
        }


        private INodeConfiguration GetNodeMock(bool isMaster, byte serverId)
        {
            return Mock.Of<INodeConfiguration>(m => m.IsMaster == isMaster && m.ServerId == serverId);
        }
    }
}
