using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using FloatingQueue.ServiceProxy.GeneratedClient;

namespace FloatingQueue.ServiceProxy
{
    public class ManualQueueProxy : QueueServiceProxy
    {
        private readonly Binding m_Binding;
        private readonly EndpointAddress m_EndpointAddress;

        public ManualQueueProxy(string address)
        {
            m_EndpointAddress = new EndpointAddress(address);
            m_Binding = new NetTcpBinding();
            CreateClient();
            var a = Client as ICommunicationObject;
            a.Faulted += (sender, args) => { var b = 5; };
        }

        protected override QueueServiceClient CreateClientCore()
        {
            return new QueueServiceClient(m_Binding, m_EndpointAddress);
        }

        public override void Push(string aggregateId, int version, object e)
        {
            Client.Push(aggregateId, version, e);
        }

        public override bool TryGetNext(string aggregateId, int version, out object next)
        {
            return Client.TryGetNext(out next, aggregateId, version);
        }

        public override IEnumerable<object> GetAllNext(string aggregateId, int version)
        {
            return Client.GetAllNext(aggregateId, version);
        }

        public void Open()
        {
             Client.Open();
        }

        public void Close()
        {
            DoClose();
        }

    }
}