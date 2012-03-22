using System.Linq;
using FloatingQueue.Server.EventsLogic;
using FloatingQueue.Server.Exceptions;
using NUnit.Framework;
using ServerClass = FloatingQueue.Server.Core.Server;

namespace FloatingQueue.Tests.Server
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

        [Test]
        public void PushManyTest()
        {
            var aggregate = new EventAggregate();
            var events = new[] {"a", "b", "c"};
            aggregate.PushMany(-1, events);
            CollectionAssert.AreEqual(events, aggregate.GetRange(0, 3));
        }


        [Test, Combinatorial]
        public void GetRangeTest([Values(0, 1)]int version, [Values(0, 1)]int count)
        {
            var aggregate = new EventAggregate();
            var events = new[] { "a", "b", "c" };
            aggregate.PushMany(-1, events);
            CollectionAssert.AreEqual(events.ToList().GetRange(version, count), aggregate.GetRange(version, count));
        }

        [Test, ExpectedException(typeof(OptimisticLockException))]
        public void PushMany_OptimisticLock_Test()
        {
            var aggregate = new EventAggregate();
            var events = new[] { "a", "b", "c" };
            aggregate.PushMany(-1, events);
            CollectionAssert.AreEqual(events, aggregate.GetRange(0, 3));
            aggregate.PushMany(0, events);
        }

        [Test]
        public void CommitTest()
        {
            var tranCounter = ServerClass.TransactionCounter;
            var aggregate = new EventAggregate();
            Assert.AreEqual(0, aggregate.LastVersion);
            Assert.IsFalse(aggregate.HasUncommitedChanges);

            using (var tran = aggregate.BeginTransaction())
            {
                aggregate.Push(-1, new object());
                Assert.IsTrue(aggregate.HasUncommitedChanges);
                Assert.AreEqual(1, aggregate.LastVersion);

                tran.Commit();
            }

            Assert.IsFalse(aggregate.HasUncommitedChanges);
            Assert.AreEqual(tranCounter + 1, ServerClass.TransactionCounter);
            Assert.AreEqual(1, aggregate.LastVersion);
        }

        [Test]
        public void RollbackTest()
        {
            var tranCounter = ServerClass.TransactionCounter;
            var aggregate = new EventAggregate();
            Assert.AreEqual(0, aggregate.LastVersion);
            Assert.IsFalse(aggregate.HasUncommitedChanges);

            using (var tran = aggregate.BeginTransaction())
            {
                aggregate.Push(-1, new object());
                Assert.IsTrue(aggregate.HasUncommitedChanges);
                Assert.AreEqual(1, aggregate.LastVersion);

                tran.Rollback();
            }

            Assert.IsFalse(aggregate.HasUncommitedChanges);
            Assert.AreEqual(tranCounter, ServerClass.TransactionCounter);
            Assert.AreEqual(0, aggregate.LastVersion);
        }
    }
}
