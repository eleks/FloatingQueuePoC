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
            AddDispatcher("ReceiveSingleAggregate", ReceiveSingleAggregate);
            AddDispatcher("NotificateSynchronizationFinished", NotificateSynchronizationFinished);
            throw new NotImplementedException("A Dispatcher is required for GetExtended Metadata");
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
            throw new NotImplementedException("NodeInfo has new structure");
            var result = new NodeInfo
            {
                Address = request.ReadString()
              //  ServerId = (byte)request.ReadInt32()
            };
            return result;
        }

        //void IntroduceNewNode(NodeInfo nodeInfo);
        protected bool IntroduceNewNode(IInternalQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            throw new NotImplementedException("NodeInfo has new structure");
            var nodeInfo = ReadNodeInfo(request);
            //service.IntroduceNewNode(nodeInfo);
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
            //service.RequestSynchronization(serverId, dic);
            return true;
        }


        //void ReceiveSingleAggregate(string aggregateId, int version, IEnumerable<object> events) 
        protected bool ReceiveSingleAggregate(IInternalQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            throw new NotImplementedException("ReceiveSingleAggregate's signature may have changed");
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

        //void NotificateSynchronizationFinished(IDictionary<string, int> writtenAggregatesVersions);
        protected bool NotificateSynchronizationFinished(IInternalQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            throw new NotImplementedException("NotificateSynchronizationFinished's signature may have changed");
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
