using System;
using FloatingQueue.Server.Replication;
using FloatingQueue.Server.Services.Proxy;

namespace FloatingQueue.Server.Core
{
    public interface IConfiguration
    {
        bool IsMaster { get; }
        bool IsSynced { get; } //note MM: currently all new nodes are not Cynced, but this assumption may be false in future
        bool IsReadonly { get; }
        byte ServerId { get; }
        string Address { get; }
        IInternalQueueServiceProxy Proxy { get; }
    }

    public interface INodeConfiguration : IConfiguration
    {
        void DeclareAsNewMaster();
        void DeclareAsSyncedNode();
    }

    public interface IServerConfiguration : IConfiguration
    {
        INodeCollection Nodes { get; }
        string PublicAddress { get; }
        bool IsSyncing { get; set; }
        int PingTimeout { get; }
    }

    public class ServerConfiguration : IServerConfiguration
    {
        public bool IsMaster { get { return Nodes.Self.IsMaster; } }
        public bool IsSynced { get { return Nodes.Self.IsSynced; } }
        public bool IsReadonly { get { return Nodes.Self.IsReadonly; } }
        public byte ServerId { get; set; }
        public string Address { get { return Nodes.Self.Address; } }
        public string PublicAddress { get; set; }
        public bool IsSyncing{get; set; }

        public int PingTimeout
        {
            get { return 10000; }
        }

        public IInternalQueueServiceProxy Proxy { get; set; }
        public INodeCollection Nodes { get; set; }
    }

    public class NodeConfiguration : INodeConfiguration
    {
        public string Address { get; set; }
        public IInternalQueueServiceProxy Proxy { get; set; }
        public bool IsMaster { get; set; }
        public bool IsSynced { get; set; }
        public bool IsReadonly { get; set; }
        public byte ServerId { get; set; }
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
    }
}
