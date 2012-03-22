using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FloatingQueue.Server.Exceptions;

namespace FloatingQueue.Server.EventsLogic
{
    public interface ITransaction : IDisposable
    {
        void Commit();
        void Rollback();
    }

    public interface IEventAggregate
    {
        void Push(int version, object e);
        void PushMany(int version, IEnumerable<object> events);
        bool TryGetNext(int version, out object next);
        IEnumerable<object> GetAllNext(int version);
        IEnumerable<object> GetRange(int version, int count);
        int LastVersion { get; }

        ITransaction BeginTransaction();
    }

    public class EventAggregate : IEventAggregate
    {
        private readonly List<object> m_InternalStorage = new List<object>();
        private readonly object m_SyncRoot = new object();
        private int m_UncommitedChangesCount;

        public void Push(int version, object e)
        {
            lock (m_SyncRoot)
            {
                if (version != -1 && version != m_InternalStorage.Count)
                {
                    throw new OptimisticLockException();
                }
                m_InternalStorage.Add(e);
                m_UncommitedChangesCount++;
            }
        }

        public void PushMany(int version, IEnumerable<object> events)
        {
            lock (m_SyncRoot)
            {
                if (version != -1 && version != m_InternalStorage.Count)
                {
                    throw new OptimisticLockException();
                }
                int prevCount = m_InternalStorage.Count;
                m_InternalStorage.AddRange(events);
                m_UncommitedChangesCount += m_InternalStorage.Count - prevCount;
            }
        }

        public bool TryGetNext(int version, out object next)
        {
            lock (m_SyncRoot)
            {
                if (version < m_InternalStorage.Count)
                {
                    next = m_InternalStorage[version];
                    return true;
                }
                else
                {
                    next = null;
                    return false;
                }
            }
        }

        public IEnumerable<object> GetAllNext(int version)
        {
            lock (m_SyncRoot)
            {
                return m_InternalStorage.Skip(version + 1).ToList();
            }
        }

        public IEnumerable<object> GetRange(int version, int count)
        {
            lock (m_SyncRoot)
            {
                return m_InternalStorage.GetRange(version, count);
            }
        }

        public int LastVersion
        {
            get
            {
                lock (m_SyncRoot)
                {
                    return m_InternalStorage.Count;
                }
            }
        }

        public bool HasUncommitedChanges
        {
            get { return m_UncommitedChangesCount > 0; }
        }

        public ITransaction BeginTransaction()
        {
            Monitor.Enter(m_SyncRoot);
            return new Transaction(this);
        }

        private void CommitTransaction()
        {
            // todo: flush the data into file system here
            if (HasUncommitedChanges)
            {
                m_UncommitedChangesCount = 0;
                Core.Server.FireTransactionCommited(); // todo: use pub/sub here
            }
            Monitor.Exit(m_SyncRoot);
        }

        private void RollbackTransaction()
        {
            if (HasUncommitedChanges)
            {
                for (int i = 0; i < m_UncommitedChangesCount; i++)
                {
                    m_InternalStorage.RemoveAt(m_InternalStorage.Count - 1);
                }
                m_UncommitedChangesCount = 0;
            }
            Monitor.Exit(m_SyncRoot);
        }

        private class Transaction : ITransaction
        {
            private readonly EventAggregate m_Aggregate;
            private bool m_Commited;
            private bool m_RolledBack;

            public Transaction(EventAggregate aggregate)
            {
                m_Aggregate = aggregate;
            }

            private bool Finalized
            {
                get { return m_Commited || m_RolledBack; }
            }

            public void Commit()
            {
                if (Finalized)
                    throw new InvalidOperationException("Transaction finalized");
                m_Aggregate.CommitTransaction();
                m_Commited = true;
            }

            public void Rollback()
            {
                if (Finalized)
                    throw new InvalidOperationException("Transaction finalized");
                m_Aggregate.RollbackTransaction();
                m_RolledBack = true;
            }

            public void Dispose()
            {
                if (!Finalized)
                {
                    Rollback();
                }
            }

            ~Transaction()
            {
                Dispose();
            }
        }
    }
}
