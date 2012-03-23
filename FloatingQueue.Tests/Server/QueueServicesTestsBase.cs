using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using Autofac;
using FloatingQueue.Server.Core;
using FloatingQueue.Server.EventsLogic;
using FloatingQueue.Server.Exceptions;
using FloatingQueue.Server.Replication;
using FloatingQueue.Server.Services.Implementation;
using Moq;
using NUnit.Framework;

namespace FloatingQueue.Tests.Server
{
    public abstract class QueueServicesTestsBase : TestBase
    {
        private Mock<IAggregateRepository> m_AggregateRepositoryMock;
        private Mock<IEventAggregate> m_EventAggregateMock;
        protected Mock<IServerConfiguration> m_ServerConfigurationMock;
        private Mock<IConnectionManager> m_ConnectionManagerMock;

        public override void Setup()
        {
            m_ServerConfigurationMock = new Mock<IServerConfiguration>();
            m_EventAggregateMock = new Mock<IEventAggregate>();
            m_AggregateRepositoryMock = new Mock<IAggregateRepository>();
            m_ConnectionManagerMock = new Mock<IConnectionManager>();
            base.Setup();
        }

        protected override void RegisterMocks(Autofac.ContainerBuilder containerBuilder)
        {
            base.RegisterMocks(containerBuilder);
            containerBuilder.RegisterInstance(m_EventAggregateMock.Object).As<IEventAggregate>();
            containerBuilder.RegisterInstance(m_AggregateRepositoryMock.Object).As<IAggregateRepository>();
            containerBuilder.RegisterInstance(m_ServerConfigurationMock.Object).As<IServerConfiguration>();
            containerBuilder.RegisterInstance(m_ConnectionManagerMock.Object).As<IConnectionManager>();
        }

        protected abstract QueueServiceBase GetService();

        [Test, ExpectedException(typeof(BusinessLogicException))]
        public void GetAllNext_WhenSyncing_Test()
        {
            m_ServerConfigurationMock.SetupGet(m => m.IsSynced).Returns(false);
            var service = GetService();

            service.GetAllNext("a", -1);
        }

        [Test, Combinatorial]
        public void GetAllNextTest([Values(true, false)]bool aggregateExists)
        {
            m_ServerConfigurationMock.SetupGet(m => m.IsSynced).Returns(true);
            var events = new[] { "a", "b", "c" };
            m_EventAggregateMock.Setup(m => m.GetAllNext(It.IsAny<int>())).Returns(events).Verifiable();
            var aggregate = m_EventAggregateMock.Object;
            m_AggregateRepositoryMock.Setup(m => m.TryGetEventAggregate("a", out aggregate)).Returns(aggregateExists);
            m_AggregateRepositoryMock.Setup(m => m.CreateAggregate("a")).Returns(aggregate);

            var service = GetService();

            var result = service.GetAllNext("a", -1);

            CollectionAssert.AreEqual(events, result);
            m_EventAggregateMock.Verify();
        }

        [Test, ExpectedException(typeof(BusinessLogicException))]
        public void TryGetNext_WhenSyncing_Test()
        {
            m_ServerConfigurationMock.SetupGet(m => m.IsSynced).Returns(false);
            var service = GetService();

            object next;
            service.TryGetNext("a", -1, out next);
        }

        [Test, Combinatorial]
        public void TryGetNextTest([Values(true, false)]bool aggregateExists)
        {
            m_ServerConfigurationMock.SetupGet(m => m.IsSynced).Returns(true);
            object e = "a";
            m_EventAggregateMock.Setup(m => m.TryGetNext(It.IsAny<int>(), out e)).Returns(true).Verifiable();
            var aggregate = m_EventAggregateMock.Object;
            m_AggregateRepositoryMock.Setup(m => m.TryGetEventAggregate("a", out aggregate)).Returns(aggregateExists);
            m_AggregateRepositoryMock.Setup(m => m.CreateAggregate("a")).Returns(aggregate);

            var service = GetService();

            object next;
            Assert.IsTrue(service.TryGetNext("a", -1, out next));

            Assert.AreEqual(e, next);
                    
            m_EventAggregateMock.Verify();
        }

        [Test]
        public void GetClusterMetadataTest()
        {
            var nodesMock = new Mock<INodeCollection>();
            nodesMock.SetupGet(m => m.All).Returns(new ReadOnlyCollection<INodeConfiguration>(new[]
                                                   {
                                                       MockNodeConfiguration(true, "a"),
                                                       MockNodeConfiguration(false, "b"),
                                                   }.ToList()));
            m_ServerConfigurationMock.SetupGet(m => m.Nodes).Returns(nodesMock.Object);
            var service = GetService();
            var metadata = service.GetClusterMetadata();
            Assert.AreEqual(2, metadata.Nodes.Count);
            Assert.AreEqual(true, metadata.Nodes[0].IsMaster);
            Assert.AreEqual(false, metadata.Nodes[1].IsMaster);
            Assert.AreEqual("a", metadata.Nodes[0].Address);
            Assert.AreEqual("b", metadata.Nodes[1].Address);
        }

        private INodeConfiguration MockNodeConfiguration(bool isMaster, string publicAddress)
        {
            var mock = new Mock<INodeConfiguration>();

            mock.SetupGet(m => m.IsMaster).Returns(isMaster);
            mock.SetupGet(m => m.PublicAddress).Returns(publicAddress);

            return mock.Object;
        }

        [Test]
        public virtual void PushToReadonlyTest()
        {
            m_ServerConfigurationMock.SetupGet(m => m.IsReadonly).Returns(true);
            var service = GetService();
            Assert.Throws<ReadOnlyException>(() => service.Push("a", -1, new object()));
        }

        [Test]
        public virtual void PushToNotSyncedTest()
        {
            m_ServerConfigurationMock.SetupGet(m => m.IsSynced).Returns(false);
            var service = GetService();
            Assert.Throws<InvalidOperationException>(() => service.Push("a", -1, new object()));
        }

        [Test]
        public virtual void PushTest([Values(true, false)]bool canReplicate)
        {
            m_ServerConfigurationMock.SetupGet(m => m.IsMaster).Returns(true);
            m_EventAggregateMock.Setup(m => m.BeginTransaction()).Returns(Mock.Of<ITransaction>());
            
            var aggregate = m_EventAggregateMock.Object;
            m_AggregateRepositoryMock.Setup(m => m.TryGetEventAggregate("a", out aggregate)).Returns(true);
            m_AggregateRepositoryMock.Setup(m => m.CreateAggregate("a")).Returns(aggregate);
            m_ServerConfigurationMock.SetupGet(m => m.IsSynced).Returns(true);
            m_ConnectionManagerMock.Setup(m => m.TryReplicate("a", -1, "b")).Returns(canReplicate).Verifiable();

            var service = GetService();

            if (canReplicate)
            {
                service.Push("a", -1, "b");

                m_ConnectionManagerMock.Verify();
            }
            else
            {
                Assert.Throws<ApplicationException>(() => service.Push("a", -1, "b"));
            }
        }
    }

    [TestFixture]
    public class InternalQueueServiceTests : QueueServicesTestsBase
    {
        protected override QueueServiceBase GetService()
        {
            return new InternalQueueService();
        }
    }

    [TestFixture]
    public class PublicQueueServiceTests : QueueServicesTestsBase
    {
        protected override QueueServiceBase GetService()
        {
            return new PublicQueueService();
        }

        [Test]
        public override void PushTest([Values(true, false)]bool canReplicate)
        {
            base.PushTest(canReplicate);
        }

        [Test]
        public override void PushToNotSyncedTest()
        {
            m_ServerConfigurationMock.SetupGet(m => m.IsSynced).Returns(false);
            var service = GetService();
            Assert.Throws<ApplicationException>(() => service.Push("a", -1, new object()));
        }

        [Test]
        public override void PushToReadonlyTest()
        {
            m_ServerConfigurationMock.SetupGet(m => m.IsReadonly).Returns(true);
            var service = GetService();
            Assert.Throws<ApplicationException>(() => service.Push("a", -1, new object()));
        }

        [Test]
        public void PushErrTest()
        {
            m_ServerConfigurationMock.SetupGet(m => m.IsMaster).Returns(true);
            var service = GetService();
            Assert.Throws<ApplicationException>(() => service.Push("-err", -1, new object()));
        }
    }
}
