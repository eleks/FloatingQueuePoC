using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using FloatingQueue.Server.EventsLogic;
using Moq;
using NUnit.Framework;

namespace FloatingQueue.Tests.Server
{
    [TestFixture]
    public class AggreagateRepositoryTests : TestBase
    {
        private readonly Mock<IEventAggregate> m_EventAggregate = new Mock<IEventAggregate>();
        private readonly Mock<IAggregateRepository> m_AggregateRepositoryMock = new Mock<IAggregateRepository>();

        protected override void RegisterMocks(Autofac.ContainerBuilder containerBuilder)
        {
            base.RegisterMocks(containerBuilder);
            containerBuilder.RegisterInstance(m_EventAggregate.Object).As<IEventAggregate>();
            containerBuilder.RegisterInstance(m_AggregateRepositoryMock.Object).As<IAggregateRepository>();
        }

        [Test]
        public void CreateAndGetSuccessfullTest()
        {
            var repository = new AggregateRepository();
            var id = "testId";
            var aggregate = repository.CreateAggregate(id);

            IEventAggregate retrieved;
            Assert.IsTrue(repository.TryGetEventAggregate(id, out retrieved));
            Assert.AreEqual(aggregate, retrieved);
        }

        [Test]
        public void CreateAndGetUnsuccessfullTest()
        {
            var repository = new AggregateRepository();

            IEventAggregate retrieved;
            Assert.IsFalse(repository.TryGetEventAggregate("testId", out retrieved));
        }

        [Test]
        public void DuplicateCreateTest()
        {
            var repository = new AggregateRepository();
            var id = "testId";
            repository.CreateAggregate(id);
            repository.CreateAggregate(id);
        }

        [Test]
        public void AggregateRepositoryInstanceTest()
        {
            Assert.AreEqual(m_AggregateRepositoryMock.Object, AggregateRepository.Instance);
        }

        [Test]
        public void GetAllIdsTest([Range(0, 2)]int count)
        {
            var a = new AggregateRepository();
            for (int i = 0; i < count; i++)
            {
                a.CreateAggregate(i.ToString());
            }
            var ids = a.GetAllIds();
            Assert.AreEqual(count, ids.Count);
            CollectionAssert.AreEquivalent(Enumerable.Range(0, count).Select(i => i.ToString()), ids);
        }

        [Test]
        public void GetLastVersionsTest()
        {
            m_EventAggregate.SetupGet(m => m.LastVersion).Returns(123);
            var a = new AggregateRepository();
            var aggregate = a.CreateAggregate("1");

            var result = a.GetLastVersions();
            CollectionAssert.AreEquivalent(new[] { new KeyValuePair<string, int>("1", 123)}, result);
        }
    }
}
