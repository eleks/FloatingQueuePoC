using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using FloatingQueue.ServiceProxy.GeneratedClient;

namespace FloatingQueue.ServiceProxy
{
    public class ManualQueueProxy : QueueServiceProxy, IEquatable<ManualQueueProxy>
    {
        private readonly Binding m_Binding;
        private readonly EndpointAddress m_EndpointAddress;
        private readonly string m_OriginalAddressStr; // EndpointAddress modifies original address
        private readonly object m_SyncRoot = new object();

        public ManualQueueProxy(string address)
        {
            m_OriginalAddressStr = address;
            m_EndpointAddress = new EndpointAddress(address);
            m_Binding = new NetTcpBinding();
            CreateClient();
            //todo: think about using WCF's tools to detect failures
            //var a = Client as ICommunicationObject;
            //a.Faulted += (sender, args) => { var b = 5; };
        }

        protected override QueueServiceClient CreateClientCore()
        {
            return new QueueServiceClient(m_Binding, m_EndpointAddress);
        }

        public override void Push(string aggregateId, int version, object e)
        {
            lock (m_SyncRoot)
            {
                Client.Push(aggregateId, version, e);
            }
        }

        public override bool TryGetNext(string aggregateId, int version, out object next)
        {
            lock (m_SyncRoot)
            {
                return Client.TryGetNext(out next, aggregateId, version);
            }
        }

        public override IEnumerable<object> GetAllNext(string aggregateId, int version)
        {
            lock (m_SyncRoot)
            {
                return Client.GetAllNext(aggregateId, version);
            }
        }

        public PingResult Ping()
        {
            lock (m_SyncRoot)
            {
                // todo create enumeration for fault reasons
                try
                {
                    return Client.Ping();
                }
                catch (CommunicationException)
                {
                    return new PingResult() { ResultCode = 1 };
                }
                catch (TimeoutException)
                {
                    return new PingResult() { ResultCode = 2 };
                }
            }
        }

        public void Open()
        {
            lock (m_SyncRoot)
            {
                Client.Open();
            }
        }

        public void Close()
        {
            lock (m_SyncRoot)
            {
                DoClose();
            }
        }

        public string Address
        {
            get { return m_OriginalAddressStr; }
        }

        public bool Equals(ManualQueueProxy other)
        {
            return this.m_EndpointAddress == other.m_EndpointAddress;
        }
    }
}