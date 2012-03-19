using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using FloatingQueue.Server.Core;
using FloatingQueue.Server.EventsLogic;
using FloatingQueue.Server.Exceptions;
using FloatingQueue.Server.Services.Implementation;
using Moq;
using NUnit.Framework;

namespace FloatingQueue.Server.Tests
{
    public abstract class QueueServicesTestsBase : TestBase
    {
        private readonly Mock<IAggregateRepository> m_AggregateRepositoryMock = new Mock<IAggregateRepository>();
        private readonly Mock<IEventAggregate> m_EventAggregateMock = new Mock<IEventAggregate>();
        private readonly Mock<IServerConfiguration> m_ServerConfiguration = new Mock<IServerConfiguration>();

        protected override void RegisterMocks(Autofac.ContainerBuilder containerBuilder)
        {
            base.RegisterMocks(containerBuilder);
            containerBuilder.RegisterInstance(m_EventAggregateMock.Object).As<IEventAggregate>();
            containerBuilder.RegisterInstance(m_AggregateRepositoryMock.Object).As<IAggregateRepository>();
            containerBuilder.RegisterInstance(m_ServerConfiguration.Object).As<IServerConfiguration>();
        }

        protected abstract QueueServiceBase GetService();

        [Test, ExpectedException(typeof(BusinessLogicException))]
        public void GetAllNext_WhenSyncing_Test()
        {
            m_ServerConfiguration.SetupGet(m => m.IsSynced).Returns(false);
            var service = GetService();

            service.GetAllNext("a", -1);
        }

        [Test, Combinatorial]
        public void GetAllNextTest([Values(true, false)]bool aggregateExists)
        {
            m_ServerConfiguration.SetupGet(m => m.IsSynced).Returns(true);
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
            m_ServerConfiguration.SetupGet(m => m.IsSynced).Returns(false);
            var service = GetService();

            object next;
            service.TryGetNext("a", -1, out next);
        }

        [Test, Combinatorial]
        public void TryGetNextTest([Values(true, false)]bool aggregateExists)
        {
            m_ServerConfiguration.SetupGet(m => m.IsSynced).Returns(true);
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
    }
}
