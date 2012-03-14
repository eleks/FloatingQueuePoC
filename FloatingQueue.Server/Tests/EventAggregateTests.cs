using System.Linq;
using FloatingQueue.Server.EventsLogic;
using FloatingQueue.Server.Exceptions;
using NUnit.Framework;

namespace FloatingQueue.Server.Tests
{
    [TestFixture]
    public class EventAggregateTests : TestBase
    {
        [Test]
        public void TryGetNextSuccessfullTest()
        {
            var aggregate = new EventAggregate();
            aggregate.Push(0, "test");

            object result;
            Assert.IsTrue(aggregate.TryGetNext(0, out result));
            Assert.AreEqual("test", result);
        }

        [Test]
        public void TryGetNextUnsuccessfullTest()
        {
            var aggregate = new EventAggregate();

            object result;
            Assert.IsFalse (aggregate.TryGetNext(0, out result));
        }

        [Test, ExpectedException(typeof(OptimisticLockException))]
        public void OptimisticLockTest()
        {
            var aggregate = new EventAggregate();
            aggregate.Push(0, "test1");
            aggregate.Push(0, "test2");
        }

        [TestCase(0, -1)]
        [TestCase(1, -1)]
        [TestCase(1, 0)]
        [TestCase(10, -1)]
        [TestCase(10, 5)]
        [TestCase(10, 9)]
        public void GetAllNextSuccessfullTest(int count, int startingFrom)
        {
            var aggregate = new EventAggregate();

            for (int i = 0; i < count; i++)
            {
                aggregate.Push(i, "test" + i);
            }

            var result = aggregate.GetAllNext(startingFrom);
            Assert.AreEqual(count - startingFrom - 1, result.Count());
        }
    }
}
