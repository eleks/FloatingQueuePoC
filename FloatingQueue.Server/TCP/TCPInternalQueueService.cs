using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FloatingQueue.Common.TCP;
using FloatingQueue.Server.Services;
using FloatingQueue.Server.Services.Implementation;

namespace FloatingQueue.Server.TCP
{
    public class TCPInternalQueueService : TCPPublicQueueService
    {
        public override bool Dispatch(TCPBinaryReader request, TCPBinaryWriter response)
        {
            var service = new InternalQueueService();
            if (DoPublicQueueServiceDispatch(service, request, response))
                return true;
            if (DoInternalQueueServiceDispatch(service, request, response))
                return true;
            return false;
        }


        private bool DoInternalQueueServiceDispatch(IInternalQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            if (request.Command == "Ping".GetHashCode())
                return Ping(service, request, response);
            if (request.Command == "IntroduceNewNode".GetHashCode())
                return IntroduceNewNode(service, request, response);
            if (request.Command == "RequestSynchronization".GetHashCode())
                    return RequestSynchronization(service, request, response);
            if (request.Command == "ReceiveAggregateEvents".GetHashCode())
                    return ReceiveAggregateEvents(service, request, response);
            if (request.Command == "NotificateAllAggregatesSent".GetHashCode())
                return NotificateAllAggregatesSent(service, request, response);

            return false;
        }

        //int Ping();
        protected bool Ping(IInternalQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            var ping = service.Ping();
            //
            response.Write(ping);
            return true;
        }

        private static ExtendedNodeInfo ReadNodeInfo(TCPBinaryReader request)
        {
            throw new NotImplementedException("ExtendedNodeInfo has new fields");
            var result = new ExtendedNodeInfo 
            {
                //Address = request.ReadString(), 
                ServerId = (byte) request.ReadInt32()
            };
            return result;
        }

        //void IntroduceNewNode(NodeInfo nodeInfo);
        protected bool IntroduceNewNode(IInternalQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            var nodeInfo = ReadNodeInfo(request);
            service.IntroduceNewNode(nodeInfo);
            return true;
        }

        //void RequestSynchronization(int serverId, IDictionary<string, int> currentAggregateVersions)
        protected bool RequestSynchronization(IInternalQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            throw new NotImplementedException("RequestSynchronization has new signature");
            var serverId = request.ReadInt32();
            var count = request.ReadInt32();
            var dic = new Dictionary<string, int>(count);
            for (int i = 0; i < count; i++)
            {
                var key = request.ReadString();
                var value = request.ReadInt32();
                dic.Add(key, value);
            }
           // service.RequestSynchronization(serverId, dic);
            return true;
        }

        //void ReceiveAggregateEvents(string aggregateId, int version, IEnumerable<object> events) 
        protected bool ReceiveAggregateEvents(IInternalQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            var aggId = request.ReadString();
            var version = request.ReadInt32();
            var count = request.ReadInt32();
            var dic = new List<object>(count);
            for (int i = 0; i < count; i++)
            {
                var obj = request.ReadObject();
                dic.Add(obj);
            }
            service.ReceiveSingleAggregate(aggId, version, dic);
            return true;
            
        }

        //void NotificateAllAggregatesSent(IDictionary<string, int> writtenAggregatesVersions);
        protected bool NotificateAllAggregatesSent(IInternalQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            var count = request.ReadInt32();
            var dic = new Dictionary<string, int>(count);
            for (int i = 0; i < count; i++)
            {
                var key = request.ReadString();
                var value = request.ReadInt32();
                dic.Add(key, value);
            }
            service.NotificateSynchronizationFinished(dic);
            return true;
        }

    }
}
