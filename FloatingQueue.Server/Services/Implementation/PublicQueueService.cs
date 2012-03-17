using System;

namespace FloatingQueue.Server.Services.Implementation
{
    public class PublicQueueService : QueueServiceBase
    {
        public override void Push(string aggregateId, int version, object e)
        {
            if (!Core.Server.Configuration.IsMaster)
                throw new ApplicationException("Clients cannot write directly to slaves.");
            base.Push(aggregateId, version, e);
        }
    }
}
