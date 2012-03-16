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

        public void RequestSynchronization(int serverId, IDictionary<string, int> currentAggregateVersions)
        {
            var req = CreateRequest("RequestSynchronization");
            req.Write(serverId);
            req.Write(currentAggregateVersions.Count);
            foreach (var pair in currentAggregateVersions)
            {
                req.Write(pair.Key);
                req.Write(pair.Value);
            }
            SendReceive(req);
        }

        public void NotificateNodeIsSynchronized(int serverId)
        {
            var req = CreateRequest("NotificateNodeIsSynchronized");
            req.Write(serverId);
            SendReceive(req);
        }

        public void ReceiveAggregateEvents(string aggregateId, int version, IEnumerable<object> events)
        {
            var req = CreateRequest("ReceiveAggregateEvents");
            req.Write(aggregateId);
            req.Write(version);
            var arr = events.ToArray();
            req.Write(arr.Length);
            for (int i = 0; i < arr.Length; i++)
            {
                req.WriteObject(arr[i]);
            }
            SendReceive(req);
        }

        public void NotificateAllAggregatesSent(IDictionary<string, int> writtenAggregatesVersions)
        {
            var req = CreateRequest("NotificateAllAggregatesSent");
            req.Write(writtenAggregatesVersions.Count);
            foreach (var pair in writtenAggregatesVersions)
            {
                req.Write(pair.Key);
                req.Write(pair.Value);
            }
            SendReceive(req);
        }


        //public void RequestSynchronization(NodeInfo nodeInfo)
        //{
        //    var req = CreateRequest("RequestSynchronization");
        //    WriteNodeInfo(req, nodeInfo);
        //    SendReceive(req);
        //}

        //public void NotificateSlaveSynchronized(NodeInfo nodeInfo)
        //{
        //    var req = CreateRequest("NotificateSlaveSynchronized");
        //    WriteNodeInfo(req, nodeInfo);
        //    SendReceive(req);
        //}
    }
}
