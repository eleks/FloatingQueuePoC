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
            AddDispatcher("GetExtendedMetadata", GetExtendedMetadata);
        }

        protected override IInternalQueueService CreateService()
        {
            var service = new InternalQueueService();
            return service;
        }

        //int Ping();
        protected void Ping(IInternalQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            service.Ping();
        }

        public static ExtendedNodeInfo ReadNodeInfo(TCPBinaryReader request)
        {
            var result = new ExtendedNodeInfo
            {
                InternalAddress = request.ReadString(),
                PublicAddress = request.ReadString(),
                ServerId = (byte)request.ReadInt32(),
                IsMaster = request.ReadBoolean()
            };
            return result;
        }

        public static void WriteNodeInfo(TCPBinaryWriter response, ExtendedNodeInfo nodeInfo)
        {
            response.Write(nodeInfo.InternalAddress);
            response.Write(nodeInfo.PublicAddress);
            response.Write((int) nodeInfo.ServerId);
            response.Write(nodeInfo.IsMaster);
        }

        //void IntroduceNewNode(ExtendedNodeInfo nodeInfo);
        protected void IntroduceNewNode(IInternalQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            var nodeInfo = ReadNodeInfo(request);
            service.IntroduceNewNode(nodeInfo);
        }

        //void RequestSynchronization(ExtendedNodeInfo nodeInfo, Dictionary<string, int> currentAggregateVersions);
        protected void RequestSynchronization(IInternalQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            var nodeInfo = ReadNodeInfo(request);
            var count = request.ReadInt32();
            var dic = new Dictionary<string, int>(count);
            for (int i = 0; i < count; i++)
            {
                var key = request.ReadString();
                var value = request.ReadInt32();
                dic.Add(key, value);
            }
            service.RequestSynchronization(nodeInfo, dic);
        }


        //void ReceiveSingleAggregate(string aggregateId, int version, IEnumerable<object> events);
        protected void ReceiveSingleAggregate(IInternalQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
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
        }

        //bool NotificateSynchronizationFinished(Dictionary<string, int> writtenAggregatesVersions);
        protected void NotificateSynchronizationFinished(IInternalQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            var count = request.ReadInt32();
            var dic = new Dictionary<string, int>(count);
            for (int i = 0; i < count; i++)
            {
                var key = request.ReadString();
                var value = request.ReadInt32();
                dic.Add(key, value);
            }
            var res = service.NotificateSynchronizationFinished(dic);
            response.Write(res);
        }

        // List<ExtendedNodeInfo> GetExtendedMetadata();
        protected void GetExtendedMetadata(IInternalQueueService service, TCPBinaryReader request, TCPBinaryWriter response)
        {
            var res = service.GetExtendedMetadata();
            response.Write(res.Count);
            for(int i=0; i<res.Count; i++)
            {
                WriteNodeInfo(response, res[i]);
            }
        }
    }
}
