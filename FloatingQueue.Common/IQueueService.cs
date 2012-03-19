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
        ClusterMetadata GetClusterMetadata();
    }
}

[DataContract]
public class ClusterMetadata
{
    public ClusterMetadata(List<NodeInfo> nodes)
    {
        Nodes = nodes;
    }
    [DataMember]
    public List<NodeInfo> Nodes;
}

[DataContract]
public class NodeInfo
{
    [DataMember]
    public string Address {get ;set;}
    [DataMember]
    public bool IsMaster { get; set; }
}
