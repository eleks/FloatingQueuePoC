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
        void RequestSynchronization(NodeInfo nodeInfo);
        [OperationContract]
        void NotificateSlaveSynchronized(NodeInfo nodeInfo);
    }

    [DataContract]
    public class NodeInfo
    {
        [DataMember] public string Address;
        [DataMember] public byte ServerId;
    }
}