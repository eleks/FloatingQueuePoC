using System;
using System.Collections.Generic;

namespace FloatingQueue.Server.Core
{
    public interface IConfiguration
    {
        int Port { get; }
        bool IsMaster { get; }
        byte Priority { get; }    //todo: consider replication without slave priorities
        List<INodeInfo> Nodes { get; }
        void DeclareServerAsNewMaster();
    }

    public class Configuration : IConfiguration
    {
        public int Port { get; set; }
        public bool IsMaster { get; set; }
        public byte Priority { get; set; }
        public List<INodeInfo> Nodes { get; set; }
        public void DeclareServerAsNewMaster()
        {
            if (IsMaster)
                throw new InvalidOperationException("A server who's already a Master cannot declare himself as New Master");
            IsMaster = true;
        }
    }

    // todo: reuse similar logic, rename priority to server id
    public interface INodeInfo
    {
        string Address { get; }
        bool IsMaster { get; }
        byte Priority { get; }
        void DeclareAsNewMaster();
        void DeclareAsDeadMaster();
    }

    public class NodeInfo : INodeInfo
    {
        public string Address { get; set; }
        public bool IsMaster { get; set; }
        public byte Priority { get; set; }
        public void DeclareAsNewMaster()
        {
            if (IsMaster)
                throw new InvalidOperationException("A server who's already a Master cannot be declared as New Master");
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
