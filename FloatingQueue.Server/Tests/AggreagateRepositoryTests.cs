using System;
using Autofac;
using FloatingQueue.Server.EventsLogic;
using Moq;
using NUnit.Framework;

namespace FloatingQueue.Server.Tests
{
    [TestFixture]
    public class AggreagateRepositoryTests : TestBase
    {
        protected override void RegisterMocks(Autofac.ContainerBuilder containerBuilder)
        {
            base.RegisterMocks(containerBuilder);
            containerBuilder.RegisterInstance(new Mock<IEventAggregate>().Object).As<IEventAggregate>();
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

        [Test, ExpectedException(typeof(ArgumentException))]
        public void DuplicateCreateTest()
        {
            var repository = new AggregateRepository();
            var id = "testId";
            repository.CreateAggregate(id);
            repository.CreateAggregate(id);
        }
    }
}
