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
            if (request.Command == "NotificateSlaveSynchronized".GetHashCode())
                return NotificateSlaveSynchronized(service, request, response);
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

        private static NodeInfo ReadNodeInfo(TCPBinaryReader request)
        {
            var result = new NodeInfo 
            {
                Address = request.ReadString(), 
                ServerId = (byte) request.ReadInt32()
            };
            return result;
        }

        //void IntroduceNewNode(NodeInfo nodeInfo);
        protected bool IntroduceNewNode(IInternalQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            throw new NotImplementedException();
            //var nodeInfo = ReadNodeInfo(request);
            //service.IntroduceNewNode(nodeInfo);
            //
            return true;
        }

        //RequestSynchronization(int serverId, IDictionary<string, int> currentAggregateVersions)
        protected bool RequestSynchronization(IInternalQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            throw new NotImplementedException();
            
            //var nodeInfo = ReadNodeInfo(request);
            //service.RequestSynchronization(nodeInfo);
            //
            return true;
        }


        //void NotificateNodeIsSynchronized(int serverId)
        protected bool NotificateSlaveSynchronized(IInternalQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            throw new NotImplementedException();
            var nodeInfo = ReadNodeInfo(request);
            //service.NotificateSlaveSynchronized(nodeInfo);
            //
            return true;
        }


        //void ReceiveAggregateEvents(string aggregateId, int version, IEnumerable<object> events) 

    }
}
