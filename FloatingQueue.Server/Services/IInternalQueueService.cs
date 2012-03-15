using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using FloatingQueue.Common;

namespace FloatingQueue.Server.Services
{
    [ServiceContract]
    public interface IInternalQueueService: IQueueService
    {
        [OperationContract]
        int Ping();
        [OperationContract]
        void IntroduceNewNode(NodeInfo nodeInfo);
        [OperationContract]
        void RequestSynchronization(int serverId, IDictionary<string, int> aggregateVersions);
        [OperationContract]
        void NotificateNodeIsSynchronized(int serverId);
        [OperationContract]
        void ReceiveAggregateEvents(string aggregateId, int version, int expectedLastVersion, IEnumerable<object> events);
        [OperationContract]
        void NotificateAllAggregatesSent(IDictionary<string, int> writtenAggregatesVersions);
    }

    [DataContract]
    public class NodeInfo
    {
        [DataMember] public string Address;
        [DataMember] public byte ServerId;
    }
}