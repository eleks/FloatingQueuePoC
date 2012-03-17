using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using FloatingQueue.Common.TCP;

namespace FloatingQueue.Common.TCPProvider
{
    public class TCPQueueServiceProxy : TCPClientBase, IQueueService
    {
        public void Push(string aggregateId, int version, object e)
        {
            var req = CreateRequest("Push");
            req.Write(aggregateId);
            req.Write(version);
            req.WriteObject(e);
            var resp = SendReceive(req);
        }

        public bool TryGetNext(string aggregateId, int version, out object next)
        {
            var req = CreateRequest("TryGetNext");
            req.Write(aggregateId);
            req.Write(version);
            var resp = SendReceive(req);
            var res = resp.ReadBoolean();
            next = res ? resp.ReadObject() : null;
            return res;
        }

        public IEnumerable<object> GetAllNext(string aggregateId, int version)
        {
            var req = CreateRequest("TryGetNext");
            req.Write(aggregateId);
            req.Write(version);
            var resp = SendReceive(req);
            var sz = resp.ReadInt32();
            var res = new object[sz];
            for(int i=0; i<sz; i++)
            {
                res[i] = resp.ReadObject();
            }
            return res;
        }

        public ClusterMetadata GetClusterMetadata()
        {
            var req = CreateRequest("GetClusterMetadata");
            var resp = SendReceive(req);
            var sz = resp.ReadInt32();
            var list = new List<Node>(sz);
            for (int i = 0; i < sz; i++)
            {
                var node = new Node();
                node.Address = resp.ReadString();
                node.IsMaster = resp.ReadBoolean();
                list.Add(node);
            }
            return new ClusterMetadata(list);
        }
    }
}
