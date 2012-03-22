using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using FloatingQueue.Server.Core;
using FloatingQueue.Server.Replication;
using FloatingQueue.Server.Services.Proxy;
using Moq;
using NUnit.Framework;
using ServerClass = FloatingQueue.Server.Core.Server;

namespace FloatingQueue.Tests.Server
{
    [TestFixture]
    public class NodeCollectionTests : TestBase
    {
        private readonly Mock<IInternalQueueServiceProxy> m_InternalQueueServiceProxyMock = new Mock<IInternalQueueServiceProxy>();
        private readonly Mock<IServerConfiguration> m_ServerConfigurationMock = new Mock<IServerConfiguration>();
        private const int NodesCount = 3;

        protected override void RegisterMocks(ContainerBuilder containerBuilder)
        {
            base.RegisterMocks(containerBuilder);
            containerBuilder.RegisterInstance(m_ServerConfigurationMock.Object).As<IServerConfiguration>();
        }

        [Test]
        public void ConstrructorForbidsNullArgumentTest()
        {
            Assert.Throws<ArgumentNullException>(() => new NodeCollection(null));
        }

        [Test]
        public void ConstructorForbidsEmptyNodesListTest()
        {
            Assert.Throws<ArgumentException>(() => new NodeCollection(Enumerable.Empty<INodeConfiguration>().ToList()));
        }

        [Test]
        public void ManyNodesWithSameIdForbiddenTest()
        {
            var nodes = CreateNodes(NodesCount, (i) => 1);
            Assert.Throws<ArgumentException>(() => new NodeCollection(nodes));
        }

        [Test]
        public void SyblingsTest()
        {
            m_ServerConfigurationMock.Setup((config) => config.ServerId).Returns(0);

            var nodes = CreateNodes(NodesCount);
            var nodesCollection = new NodeCollection(nodes);

            var syblings = nodesCollection.Siblings;

            Assert.IsFalse(syblings.Any(sybling => sybling.ServerId == ServerClass.Configuration.ServerId));
        }

        [Test]
        public void AllCountTest()
        {
            var nodes = CreateNodes(NodesCount);
            var nodesCollection = new NodeCollection(nodes);

            Assert.AreEqual(NodesCount, nodesCollection.All.Count());
        }

        [Test]
        public void SelfAccessInCaseBadIdTest()
        {
            m_ServerConfigurationMock.Setup((config) => config.ServerId).Returns(11);
            var nodes = CreateNodes(NodesCount);
            var nodesCollection = new NodeCollection(nodes);

            Assert.Throws<ApplicationException>(() =>
                                                    {
                                                        var self = nodesCollection.Self;
                                                    });
        }

        [Test]
        public void SelfAccessTest()
        {
            m_ServerConfigurationMock.Setup((config) => config.ServerId).Returns(1);
            var nodes = CreateNodes(NodesCount);
            var nodesCollection = new NodeCollection(nodes);

            var self = nodesCollection.Self;

            Assert.AreEqual(nodes.Single(n => n.ServerId == 1), self);
        }

        [Test]
        public void MasterThrowsExceptionInCaseOfManyMastersTest()
        {
            var nodes = CreateNodes(NodesCount, (serverId) => serverId < 2);
            var nodesCollection = new NodeCollection(nodes);

            Assert.Throws<ApplicationException>(() =>
            {
                var master = nodesCollection.Master;
            });
        }

        [Test]
        public void MasterTest()
        {
            var nodes = CreateNodes(NodesCount, (serverId) => serverId == 0);
            var nodesCollection = new NodeCollection(nodes);

            var master = nodesCollection.Master;
            Assert.AreEqual(0, master.ServerId);
        }

        [Test]
        public void AddNewNodeForbidsNullsTest()
        {
            var nodes = CreateNodes(2);
            var nodesCollection = new NodeCollection(nodes);

            Assert.Throws<ArgumentNullException>(() => nodesCollection.AddNewNode(null));
        }

        [Test]
        public void AddNewNodeForbidsSecondMasterTest()
        {
            var nodes = CreateNodes(2, i => i == 0);
            var nodesCollection = new NodeCollection(nodes);

            var secondMaster = new Mock<INodeConfiguration>();
            secondMaster.SetupGet(m => m.IsMaster).Returns(true);
            Assert.Throws<ArgumentException>(() => nodesCollection.AddNewNode(secondMaster.Object));
        }

        [Test]
        public void AddNewNodeForbidsIdDuplication()
        {
            var nodes = CreateNodes(2);
            var nodesCollection = new NodeCollection(nodes);

            Assert.Throws<ArgumentException>(() => nodesCollection.AddNewNode(new NodeConfiguration{ServerId = 1}));
        }

        [Test]
        public void CannotAddSecondMasterTest()
        {
            var nodes = CreateNodes(NodesCount);
            var nodesColection = new NodeCollection(nodes);

            var master = new NodeConfiguration() {IsMaster = true};

            Assert.Throws<ArgumentException>(() => nodesColection.AddNewNode(master));
        }

        [Test]
        public void CanAddNodesTest()
        {
            var nodes = CreateNodes(NodesCount);
            var nodesColection = new NodeCollection(nodes);

            nodesColection.AddNewNode(new NodeConfiguration() {ServerId = 19});

            Assert.AreEqual(NodesCount + 1, nodesColection.All.Count());
        }

        [Test]
        public void RemoveDeadNodeWorksWithInvalidIdsTest()
        {
            var nodes = CreateNodes(NodesCount);
            var nodesColection = new NodeCollection(nodes);

            Assert.DoesNotThrow(() => nodesColection.MarkAsDead(17));
        }

        [Test]
        public void CanMarkAsDeadNodeTest()
        {
            var nodes = CreateNodes(NodesCount);
            var nodesColection = new NodeCollection(nodes);

            nodesColection.MarkAsDead(1);

            Assert.AreEqual(NodesCount - 1, nodesColection.All.Count());
            Assert.AreEqual(1, nodesColection.DeadNodesCount);
        }

        [Test]
        public void RemoveDeadNodesTest()
        {
            var nodes = CreateNodes(NodesCount);
            var nodesColection = new NodeCollection(nodes);

            nodesColection.MarkAsDead(1);
            Assert.AreEqual(1, nodesColection.DeadNodesCount);
            nodesColection.RemoveDeadNodes();

            Assert.AreEqual(NodesCount - 1, nodesColection.All.Count());
            Assert.AreEqual(0, nodesColection.DeadNodesCount);
        }

        private List<INodeConfiguration> CreateNodes(int count, Func<byte, byte> idSelector, Func<byte, bool> masterSelector)
        {
            var nodes = new List<INodeConfiguration>();
            for (var i = 0; i < count; i++)
            {
                nodes.Add(new NodeConfiguration
                              {
                                  InternalAddress = String.Format("net.tcp://localhost:{0}", 11080 + i),
                                  Proxy = m_InternalQueueServiceProxyMock.Object,
                                  IsMaster = masterSelector((byte) i),
                                  ServerId = idSelector((byte)i)
                              });
            }
            return nodes;
        }

        private List<INodeConfiguration> CreateNodes(int count, Func<byte, byte> idSelector)
        {
            return CreateNodes(count, idSelector, (serverId) => serverId == 0);
        }

        private List<INodeConfiguration> CreateNodes(int count, Func<byte, bool> masterSelector)
        {
            return CreateNodes(count, i => i, masterSelector);
        }

        private List<INodeConfiguration> CreateNodes(int count)
        {
            return CreateNodes(count, i => i, (serverId) => serverId == 0);
        }
    }
}
