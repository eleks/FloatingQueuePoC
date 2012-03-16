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
    public ClusterMetadata(List<Node> nodes)
    {
        Nodes = nodes;
    }
    [DataMember]
    public List<Node> Nodes;
}

[DataContract]
public class Node
{
    [DataMember]
    public string Address;
    [DataMember]
    public bool IsMaster;
}
