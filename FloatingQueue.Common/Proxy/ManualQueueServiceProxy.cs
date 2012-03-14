using System;
using System.ServiceModel;

namespace FloatingQueue.Common.Proxy
{
    //todo MM : consider using ICommunicationObject if logic extends
    public interface IManualProxy
    {
        void Open();
        void Close();
    }

    public interface IQueueServiceProxy : IManualProxy, IQueueService { }

    public class ManualQueueServiceProxy : QueueServiceProxyBase, IQueueServiceProxy, IEquatable<ManualQueueServiceProxy>
    {
        public ManualQueueServiceProxy(string address)
        {
            EndpointAddress = new EndpointAddress(address);

            //todo: think about using WCF's tools to detect failures
            //Channel.Faulted += (sender, args) => { /*do logic*/ };
        }

        public void Open()
        {
            Channel.Open();
        }
        public void Close()
        {
            DoClose();
        }

        public bool Equals(ManualQueueServiceProxy other)
        {
            return this.EndpointAddress == other.EndpointAddress;
        }
    }
}