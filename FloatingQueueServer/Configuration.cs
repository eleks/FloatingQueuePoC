using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FloatingQueueServer
{
    public interface IConfiguration
    {
        int Port { get; }
        bool IsMaster { get; }
    }

    public class Configuration : IConfiguration
    {
        public int Port { get; set; }
        public bool IsMaster { get; set; }
    }
}
