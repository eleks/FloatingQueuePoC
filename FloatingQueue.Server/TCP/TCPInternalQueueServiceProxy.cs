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
        public void Ping()
        {
            var req = CreateRequest("Ping");
            SendReceive(req);
        }

        public void IntroduceNewNode(ExtendedNodeInfo nodeInfo)
        {
            var req = CreateRequest("IntroduceNewNode");
            TCPInternalQueueService.WriteNodeInfo(req, nodeInfo);
            SendReceive(req);
        }

        public void RequestSynchronization(ExtendedNodeInfo nodeInfo, Dictionary<string, int> currentAggregateVersions)
        {
            var req = CreateRequest("RequestSynchronization");
            TCPInternalQueueService.WriteNodeInfo(req, nodeInfo);
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

        public void ReceiveSingleAggregate(string aggregateId, int version, IEnumerable<object> events)
        {
            var req = CreateRequest("ReceiveSingleAggregate");
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

        public bool NotificateSynchronizationFinished(Dictionary<string, int> writtenAggregatesVersions)
        {
            var req = CreateRequest("NotificateSynchronizationFinished");
            req.Write(writtenAggregatesVersions.Count);
            foreach (var pair in writtenAggregatesVersions)
            {
                req.Write(pair.Key);
                req.Write(pair.Value);
            }
            SendReceive(req);
            return true;
        }

        public List<ExtendedNodeInfo> GetExtendedMetadata()
        {
            var req = CreateRequest("GetExtendedMetadata");
            var res = SendReceive(req);
            var cnt = res.ReadInt32();
            var result = new List<ExtendedNodeInfo>(cnt);
            for(int i=0; i<cnt; i++)
            {
                var node = TCPInternalQueueService.ReadNodeInfo(res);
                result.Add(node);
            }
            return result;
        }
    }
}
