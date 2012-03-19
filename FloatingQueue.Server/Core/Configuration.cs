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
        byte ServerId { get; }
        string InternalAddress { get; }
        string PublicAddress { get; }
    }

    public interface INodeConfiguration : IConfiguration
    {
        IInternalQueueServiceProxy Proxy { get; }
        void CreateProxy();
        void DeclareAsNewMaster();
    }

    public interface IServerConfiguration : IConfiguration
    {
        INodeCollection Nodes { get; }
        int PingTimeout { get; }
        void EnterReadonlyMode();
        void ExitReadonlyMode();
        bool IsReadonly { get; }
        bool IsSynced { get; }
        void DeclareAsSyncedNode();
    }

    public class ServerConfiguration : IServerConfiguration
    {
        public bool IsMaster { get { return Nodes.Self.IsMaster; } }
        public string InternalAddress { get { return Nodes.Self.InternalAddress; } }
        public string PublicAddress { get { return Nodes.Self.PublicAddress; } }
        public byte ServerId { get; set; }
        public void EnterReadonlyMode()
        {
            IsReadonly = true;
        }
        public void ExitReadonlyMode()
        {
            IsReadonly = false;
        }
        public bool IsReadonly { get; set; }
        public bool IsSynced { get; set; }
        public void DeclareAsSyncedNode()
        {
            if (IsSynced)
                throw new InvalidOperationException("Cannot declare already synced node as synced");
            IsSynced = true;
        }
        public int PingTimeout { get { return 10000; } }
        public INodeCollection Nodes { get; set; }
    }

    public class NodeConfiguration : INodeConfiguration
    {
        public string InternalAddress { get; set; }
        public string PublicAddress { get; set; }
        public IInternalQueueServiceProxy Proxy { get; set; } //todo MM: make private set (currently public set is only for tests)
        public bool IsMaster { get; set; }
        public bool IsSelf { get; set; }
        public byte ServerId { get; set; }
        public void CreateProxy()
        {
            if (Proxy != null)
                throw new InvalidOperationException("Proxy cannot be be created more than 1 time");
            if (!IsSelf)
                Proxy = new InternalQueueServiceProxy(InternalAddress);
        }
        public void DeclareAsNewMaster()
        {
            if (IsMaster)
                throw new InvalidOperationException("A node who's already a Master cannot declare itself as New Master");
            IsMaster = true;
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
