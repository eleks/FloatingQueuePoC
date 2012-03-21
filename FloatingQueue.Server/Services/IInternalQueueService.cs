using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using FloatingQueue.Common;
using FloatingQueue.Server.Core;

namespace FloatingQueue.Server.Services
{
    [ServiceContract]
    public interface IInternalQueueService : IQueueService
    {
        [OperationContract]
        int Ping();
        [OperationContract]
        void IntroduceNewNode(ExtendedNodeInfo nodeInfo);
        [OperationContract]
        void RequestSynchronization(ExtendedNodeInfo nodeInfo, Dictionary<string, int> currentAggregateVersions);
        [OperationContract]
        void ReceiveSingleAggregate(string aggregateId, int version, IEnumerable<object> events);
        [OperationContract]
        bool NotificateSynchronizationFinished(Dictionary<string, int> writtenAggregatesVersions); //todo MM: use hashCode isntead of dictionary versions
        [OperationContract]
        List<ExtendedNodeInfo> GetExtendedMetadata();
    }

    [DataContract]
    public class ExtendedNodeInfo
    {
        [DataMember]
        public string InternalAddress;
        [DataMember]
        public string PublicAddress;
        [DataMember]
        public byte ServerId;
        [DataMember]
        public bool IsMaster;
    }
}