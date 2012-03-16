using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FloatingQueue.Common;
using FloatingQueue.Common.TCP;
using FloatingQueue.Server.Services.Implementation;

namespace FloatingQueue.Server.TCP
{
    public class TCPPublicQueueService : TCPServerBase
    {
        public override bool Dispatch(TCPBinaryReader request, TCPBinaryWriter response)
        {
            var service = new PublicQueueService();
            return DoPublicQueueServiceDispatch(service, request, response);
        }

        protected bool DoPublicQueueServiceDispatch(IQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            if (request.Command == "Push".GetHashCode())
                return Push(service, request, response);
            if (request.Command == "TryGetNext".GetHashCode())
                return TryGetNext(service, request, response);
            if (request.Command == "GetAllNext".GetHashCode())
                return GetAllNext(service, request, response);
            return false;
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
    }
}
