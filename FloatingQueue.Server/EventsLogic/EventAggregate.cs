﻿using System;
using System.Collections.Generic;
using System.Linq;
using FloatingQueue.Server.Exceptions;

namespace FloatingQueue.Server.EventsLogic
{
    public interface IEventAggregate
    {
        void Push(int version, object e);
        void PushMany(int version, IEnumerable<object> events);
        bool TryGetNext(int version, out object next);
        IEnumerable<object> GetAllNext(int version);
        IEnumerable<object> GetRange(int version, int count);
        int LastVersion { get; }
        void Commit();
        void Rollback();
    }

    public class EventAggregate : IEventAggregate
    {
        private readonly List<object> m_InternalStorage = new List<object>();
        private readonly object m_SyncRoot = new object();
        private bool m_HasUncommitedChanges;

        public void Push(int version, object e)
        {
            lock (m_SyncRoot)
            {
                if (version != -1 && version != m_InternalStorage.Count)
                {
                    throw new OptimisticLockException();
                }
                m_InternalStorage.Add(e);
                m_HasUncommitedChanges = true;
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
                m_InternalStorage.AddRange(events);
                m_HasUncommitedChanges = true;
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
            get { return m_HasUncommitedChanges; }
        }

        public void Commit()
        {
            // todo: flush the data into file system here
            if (m_HasUncommitedChanges)
            {
                m_HasUncommitedChanges = false;
                Core.Server.FireTransactionCommited(); // todo: use pub/sub here
            }
        }

        public void Rollback()
        {
            if (m_HasUncommitedChanges)
            {
                m_InternalStorage.RemoveAt(m_InternalStorage.Count - 1);
                m_HasUncommitedChanges = false;
            }
        }
    }
}
