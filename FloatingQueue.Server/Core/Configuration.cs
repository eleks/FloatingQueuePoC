using System;
using System.ServiceModel;
using FloatingQueue.Common;
using FloatingQueue.Server.Replication;
using FloatingQueue.Server.Services.Proxy;

namespace FloatingQueue.Server.Core
{
    public interface IConfiguration
    {
        bool IsMaster { get; }
        bool IsSynced { get; } //note MM: currently all new nodes are not Synced, but this assumption may be false in future
        bool IsReadonly { get; }
        byte ServerId { get; }
        string InternalAddress { get; }
        string PublicAddress { get; }
    }

    public interface INodeConfiguration : IConfiguration
    {
        IInternalQueueServiceProxy Proxy { get; }
        void CreateProxy();
        void DeclareAsNewMaster();
        void DeclareAsSyncedNode();
    }

    public interface IServerConfiguration : IConfiguration
    {
        INodeCollection Nodes { get; }
        bool IsSyncing { get; set; }
        int PingTimeout { get; }
    }

    public class ServerConfiguration : IServerConfiguration
    {
        public bool IsMaster { get { return Nodes.Self.IsMaster; } }
        public bool IsSynced { get { return Nodes.Self.IsSynced; } }
        public bool IsReadonly { get { return Nodes.Self.IsReadonly; } }
        public byte ServerId { get; set; }
        public string InternalAddress { get { return Nodes.Self.InternalAddress; } }
        public string PublicAddress { get { return Nodes.Self.PublicAddress; } }
        public bool IsSyncing { get; set; }
        public int PingTimeout { get { return 10000; } }
        public INodeCollection Nodes { get; set; }
    }

    public class NodeConfiguration : INodeConfiguration
    {
        public string InternalAddress { get; set; }
        public string PublicAddress { get; set; }
        public IInternalQueueServiceProxy Proxy { get; set; } //todo MM: make private set (currently public set is only for tests)
        public bool IsMaster { get; set; }
        public bool IsSynced { get; set; }
        public bool IsReadonly { get; set; }
        public byte ServerId { get; set; }
        public void CreateProxy()
        {
            if (Proxy == null)
               // Proxy = CommunicationProvider.Instance.CreateChannel<IInternalQueueServiceProxy>(new EndpointAddress(InternalAddress));
                Proxy = new InternalQueueServiceProxy(InternalAddress);
        }
        public void DeclareAsNewMaster()
        {
            if (!IsSynced)
                throw new InvalidOperationException("A node who's in synchronization state cannot declare itself as New Master");
            if (IsMaster)
                throw new InvalidOperationException("A node who's already a Master cannot declare itself as New Master");
            IsMaster = true;
        }
        public void DeclareAsSyncedNode()
        {
            if (IsSynced)
                throw new InvalidOperationException("Already synced node cannot declare itself as Synced");
            if (IsMaster)
                throw new InvalidOperationException("Master node cannot declare itself as Synced");
            IsSynced = true;
        }

        public bool Equals(NodeConfiguration other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return other.ServerId == ServerId;
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != typeof(NodeConfiguration))
            {
                return false;
            }
            return Equals((NodeConfiguration)obj);
        }
        public override int GetHashCode()
        {
            return ServerId.GetHashCode();
        }
    }
}
