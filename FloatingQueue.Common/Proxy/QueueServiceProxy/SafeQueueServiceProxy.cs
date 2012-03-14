using System.Collections.Generic;
using System.ServiceModel;

namespace FloatingQueue.Common.Proxy.QueueServiceProxy
{
    public class SafeQueueServiceProxy : QueueServiceProxyBase
    {
        public SafeQueueServiceProxy(string address)
        {
            EndpointAddress = new EndpointAddress(address);

            //todo: think about using WCF's tools to detect failures
            //var a = Client as ICommunicationObject;
            //a.Faulted += (sender, args) => { var b = 5; };
        }

        public override void Push(string aggregateId, int version, object e)
        {
            try
            {
                base.Push(aggregateId, version, e);
            }
            finally
            {
                DoClose();
            }
        }

        public override bool TryGetNext(string aggregateId, int version, out object next)
        {
            try
            {
                // out parameters cannot be used inside anonymous methods,
                // otherwise  wrapper would be used
                return base.TryGetNext(aggregateId, version, out next);
            }
            finally
            {
                DoClose();
            }
        }

        public override IEnumerable<object> GetAllNext(string aggregateId, int version)
        {
            try
            {
                return base.GetAllNext(aggregateId, version);
            }
            finally
            {
                DoClose();
            }
        }
    }
}