using System;
using System.Collections.Generic;
using System.Linq;

namespace FloatingQueue.Server.Core
{
    public interface IConfiguration
    {
        bool IsMaster { get; }
        byte ServerId { get; }
        string Address { get; }
    }

    public interface IServerConfiguration : IConfiguration
    {
        NodeCollection Nodes { get; }
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
        public NodeCollection Nodes { get; set; }
    }

    public interface INodeConfiguration : IConfiguration
    {
        void DeclareAsDeadMaster(); // todo: consider if this method is really needed - Node is simply deleted from collection
        void DeclareAsNewMaster();
    }

    public class NodeConfiguration : INodeConfiguration
    {
        public string Address { get; set; }
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
