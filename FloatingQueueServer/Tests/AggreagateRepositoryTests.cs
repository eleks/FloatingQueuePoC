using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace FloatingQueueServer.Tests
{
    [TestFixture]
    public class AggreagateRepositoryTests
    {
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
