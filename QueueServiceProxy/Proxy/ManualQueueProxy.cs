using System;
using System.ServiceModel;

namespace FloatingQueue.Common.Proxy
{
    //todo MM : consider using ICommuniationObject if logic extends
    public interface IManualProxy
    {
        void Open();
        void Close();
    }

    public interface IQueueServiceProxy : IManualProxy, IQueueService { }

    public class ManualQueueServiceProxy : QueueServiceProxy, IQueueServiceProxy, IEquatable<ManualQueueServiceProxy>
    {
        public ManualQueueServiceProxy(string address)
        {
            EndpointAddress = new EndpointAddress(address);

            //todo: think about using WCF's tools to detect failures
            //var a = Client as ICommunicationObject;
            //a.Faulted += (sender, args) => { var b = 5; };
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