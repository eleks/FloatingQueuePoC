using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FloatingQueue.Common.TCP;
using FloatingQueue.Server.Services;
using FloatingQueue.Server.Services.Implementation;

namespace FloatingQueue.Server.TCP
{
    public class TCPInternalQueueService : TCPPublicQueueServiceBase<IInternalQueueService>
    {
        protected override void InitializeDispatcher()
        {
            base.InitializeDispatcher();

            AddDispatcher("Ping", Ping);
            AddDispatcher("IntroduceNewNode", IntroduceNewNode);
            AddDispatcher("RequestSynchronization", RequestSynchronization);
            AddDispatcher("NotificateNodeIsSynchronized", NotificateNodeIsSynchronized);
            AddDispatcher("ReceiveAggregateEvents", ReceiveAggregateEvents);
            AddDispatcher("NotificateAllAggregatesSent", NotificateAllAggregatesSent);
        }

        protected override IInternalQueueService CreateService()
        {
            var service = new InternalQueueService();
            return service;
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
            var nodeInfo = ReadNodeInfo(request);
            service.IntroduceNewNode(nodeInfo);
            return true;
        }

        //void RequestSynchronization(int serverId, IDictionary<string, int> currentAggregateVersions)
        protected bool RequestSynchronization(IInternalQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            var serverId = request.ReadInt32();
            var count = request.ReadInt32();
            var dic = new Dictionary<string, int>(count);
            for (int i = 0; i < count; i++)
            {
                var key = request.ReadString();
                var value = request.ReadInt32();
                dic.Add(key, value);
            }
            service.RequestSynchronization(serverId, dic);
            return true;
        }


        //void NotificateNodeIsSynchronized(int serverId)
        protected bool NotificateNodeIsSynchronized(IInternalQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            var serverId = request.ReadInt32();
            service.NotificateNodeIsSynchronized(serverId);
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
            service.ReceiveAggregateEvents(aggId, version, dic);
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
            service.NotificateAllAggregatesSent(dic);
            return true;
        }

    }
}
