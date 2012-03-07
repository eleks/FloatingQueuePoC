using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace FloatingQueue.Server.Service
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
        PingResult Ping();
    }

    [DataContract]
    public class PingResult
    {
        [DataMember]
        public int ResultCode;
    }

}
