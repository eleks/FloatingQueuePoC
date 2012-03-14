using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace FloatingQueue.Common
{
    [ServiceContract]
    public interface IQueueService
    {
        [OperationContract]
        void Push(string aggregateId, int version, object e);
        [OperationContract]
        bool TryGetNext(string aggregateId, int version, out object next);
        [OperationContract]
        IEnumerable<object> GetAllNext(string aggregateId, int version);
        [OperationContract]
        PingResult Ping(PingParams pingParams);
    }

    // TODO MM: move ping to another service

    [DataContract]
    public class PingResult
    {
        [DataMember]
        public int ErrorCode;
    }

    [DataContract]
    public class PingParams
    {
        public PingParams(PingReason reason = PingReason.ConnectionCheck)
        {
            Reason = reason;
        }
        [DataMember]
        public PingReason Reason;
        [DataMember] 
        public NodeInfo NodeInfo;// todo MM: try to get node info from received address automatically
    }

    [DataContract]
    public class NodeInfo
    {
        [DataMember]
        public string Address;
        [DataMember]
        public byte ServerId;
    }

    [DataContract]
    public enum PingReason
    {
        [EnumMember]
        ConnectionCheck,
        [EnumMember]
        IntroductionOfNewNode,
        [EnumMember]
        RequestForSyncronization,
        [EnumMember]
        NotificationOfSlaveReadiness
    }

}
