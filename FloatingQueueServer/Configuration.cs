﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FloatingQueueServer
{
    public interface IConfiguration
    {
        int Port { get; }
        bool IsMaster { get; }
        List<INodeInfo> Nodes { get; }
    }

    public class Configuration : IConfiguration
    {
        public int Port { get; set; }
        public bool IsMaster { get; set; }
        public List<INodeInfo> Nodes { get; set; }
    }


    public interface INodeInfo
    {
        string Address { get; }
        bool IsMaster { get; }
    }

    public class NodeInfo : INodeInfo
    {
        public string Address { get; set; }
        public bool IsMaster { get; set; }
    }
}
