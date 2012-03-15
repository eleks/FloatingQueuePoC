using System.Collections.Generic;
using FloatingQueue.Server.EventsLogic;

namespace FloatingQueue.Server.Services.Proxy
{
    //todo: find a better place and name for this helper
    public static class ProxyHelper
    {
        public static NodeInfo CurrentNodeInfo
        {
            get
            {
                return new NodeInfo()
                           {
                               Address = Core.Server.Configuration.Address,
                               ServerId = CurrentServerId
                           };
            }
        }

        public static byte CurrentServerId
        {
            get { return Core.Server.Configuration.ServerId; }
        }

        public static IDictionary<string,int> CurrentAggregateVersions
        {
            get { return AggregateRepository.Instance.GetLastVersions(); }
        }
    }
}
