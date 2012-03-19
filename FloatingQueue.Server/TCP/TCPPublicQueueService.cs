using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FloatingQueue.Common;
using FloatingQueue.Common.TCP;
using FloatingQueue.Server.Services.Implementation;

namespace FloatingQueue.Server.TCP
{
    public abstract class TCPPublicQueueServiceBase<T> : TCPServerAutoDispatchBase<T>
        where T : class, IQueueService
    {
        protected override void InitializeDispatcher()
        {
            AddDispatcher("Push", Push);
            AddDispatcher("TryGetNext", TryGetNext);
            AddDispatcher("GetAllNext", GetAllNext);
            AddDispatcher("GetClusterMetadata", GetClusterMetadata);
        }

        protected bool Push(IQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            var aggregateId = request.ReadString();
            var version = request.ReadInt32();
            var e = request.ReadObject();
            service.Push(aggregateId, version, e);
            return true;
        }

        protected bool TryGetNext(IQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            var aggregateId = request.ReadString();
            var version = request.ReadInt32();
            object next;
            var result = service.TryGetNext(aggregateId, version, out next);
            //
            response.Write(result);
            if (result)
                response.WriteObject(next);
            return true;
        }

        protected bool GetAllNext(IQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            var aggregateId = request.ReadString();
            var version = request.ReadInt32();
            var result = service.GetAllNext(aggregateId, version).ToArray();
            //
            response.Write(result.Length);
            for(int i=0; i<result.Length; i++)
            {
                response.WriteObject(result[i]);
            }
            return true;
        }

        protected bool GetClusterMetadata(IQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            var result = service.GetClusterMetadata();
            //
            response.Write(result.Nodes.Count);
            for (int i = 0; i < result.Nodes.Count; i++)
            {
                response.Write(result.Nodes[i].Address);
                response.Write(result.Nodes[i].IsMaster);
            }
            return true;
        }
    }


    public class TCPPublicQueueService : TCPPublicQueueServiceBase<IQueueService>
    {
        protected override IQueueService CreateService()
        {
            var service = new PublicQueueService();
            return service;
        }
    }

}
