using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FloatingQueue.Common.TCP;
using FloatingQueue.Common.TCPProvider;
using FloatingQueue.Server.Services;
using FloatingQueue.Server.Services.Proxy;

namespace FloatingQueue.Server.TCP
{
    public class TCPInternalQueueServiceProxy : TCPQueueServiceProxy, IInternalQueueServiceProxy
    {
        public int Ping()
        {
            var req = CreateRequest("Ping");
            var resp = SendReceive(req);
            var res = resp.ReadInt32();
            return res;
        }

        private static void WriteNodeInfo(TCPBinaryWriter req, NodeInfo nodeInfo)
        {
            req.Write(nodeInfo.Address);
            req.Write((int)nodeInfo.ServerId);
        }

        public void IntroduceNewNode(NodeInfo nodeInfo)
        {
            var req = CreateRequest("IntroduceNewNode");
            WriteNodeInfo(req, nodeInfo);
            SendReceive(req);
        }

        public void RequestSynchronization(NodeInfo nodeInfo)
        {
            var req = CreateRequest("RequestSynchronization");
            WriteNodeInfo(req, nodeInfo);
            SendReceive(req);
        }

        public void NotificateSlaveSynchronized(NodeInfo nodeInfo)
        {
            var req = CreateRequest("NotificateSlaveSynchronized");
            WriteNodeInfo(req, nodeInfo);
            SendReceive(req);
        }
    }
}
