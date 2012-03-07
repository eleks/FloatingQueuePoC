using System.Collections.Generic;

namespace FloatingQueue.Server.Core
{
    public interface IConfiguration
    {
        int Port { get; }
        bool IsMaster { get; }
        byte Priority { get; }    //todo: consider replication without slave priorities
        List<INodeInfo> Nodes { get; }
    }

    public class Configuration : IConfiguration
    {
        public int Port { get; set; }
        public bool IsMaster { get; set; }
        public byte Priority { get; set; }
        public List<INodeInfo> Nodes { get; set; }
    }


    public interface INodeInfo
    {
        string Address { get; }
        bool IsMaster { get; }
        byte Priority { get; }
    }

    public class NodeInfo : INodeInfo
    {
        public string Address { get; set; }
        public bool IsMaster { get; set; }
        public byte Priority { get; set; }
    }
}
