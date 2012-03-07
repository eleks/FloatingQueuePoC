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

    public class Configuration : IServerConfiguration
    {
        public byte ServerId { get; set; }

        public NodeCollection Nodes { get; set; }

        public string Address
        {
            get { return Nodes.Single(n => n.ServerId == this.ServerId).Address; }
        }

        public bool IsMaster
        {
            get { return Nodes.Single(n => n.ServerId == this.ServerId).IsMaster; }
        }
    }

    public interface INodeConfiguration : IConfiguration
    {
        void DeclareAsDeadMaster();
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
