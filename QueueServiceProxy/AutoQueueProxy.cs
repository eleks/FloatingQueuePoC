using System.Collections.Generic;
using FloatingQueue.ServiceProxy.GeneratedClient;

namespace FloatingQueue.ServiceProxy
{
    public class AutoQueueProxy : QueueServiceProxy
    {
        public override void Push(string aggregateId, int version, object e)
        {
            try
            {
                Client.Push(aggregateId, version, e);
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
                return Client.TryGetNext(out next, aggregateId, version);
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
                return Client.GetAllNext(aggregateId, version);
            }
            finally
            {
                DoClose();
            }
        }
    }
}