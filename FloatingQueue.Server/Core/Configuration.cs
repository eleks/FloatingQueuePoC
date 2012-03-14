using System;
using System.Collections.Generic;
using System.Linq;
using FloatingQueue.Common.Proxy;
using FloatingQueue.Server.Replication;
using FloatingQueue.Server.Service;

namespace FloatingQueue.Server.Core
{
    public interface IConfiguration
    {
        bool IsMaster { get; }
        byte ServerId { get; }
        string Address { get; }
        IQueueServiceProxy Proxy { get; }
    }

    public interface INodeConfiguration : IConfiguration
    {
        void DeclareAsNewMaster();
    }

    public interface IServerConfiguration : IConfiguration
    {
        INodeCollection Nodes { get; }
    }

    public class ServerConfiguration : IServerConfiguration
    {
        public bool IsMaster
        {
            get { return Nodes.Self.IsMaster; }
        }
        public byte ServerId { get; set; }
        public string Address
        {
            get { return Nodes.Self.Address; }
        }
        public IQueueServiceProxy Proxy { get; set; }
        public INodeCollection Nodes { get; set; }
    }

    public class NodeConfiguration : INodeConfiguration
    {
        public string Address { get; set; }
        public IQueueServiceProxy Proxy { get; set; }
        public bool IsMaster { get; set; }
        public byte ServerId { get; set; }
        public void DeclareAsNewMaster()
        {
            if (IsMaster)
                throw new InvalidOperationException("A server who's already a Master cannot declare himself as New Master");
            IsMaster = true;
        }
        public void DeclareAsDeadMaster()
        {
            if (!IsMaster)
                throw new InvalidOperationException("A server who's not a Master cannot declare himself as dead master");
            IsMaster = false;
        }
    }
}
