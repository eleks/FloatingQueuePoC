using System.Collections.Generic;
using FloatingQueue.Server.Core;
using FloatingQueue.Server.EventsLogic;
using FloatingQueue.Server.Replication;

namespace FloatingQueue.Server.Services.Proxy
{
    //todo: find a better place and name for this helper
    public static class ProxyHelper
    {
        public static ExtendedNodeInfo CurrentNodeInfo
        {
            get
            {
                return new ExtendedNodeInfo()
                           {
                               InternalAddress = Core.Server.Configuration.InternalAddress,
                               PublicAddress = Core.Server.Configuration.PublicAddress,
                               ServerId = CurrentServerId,
                               IsMaster = Core.Server.Configuration.IsMaster
                           };
            }
        }

        public static byte CurrentServerId
        {
            get { return Core.Server.Configuration.ServerId; }
        }

        public static Dictionary<string, int> CurrentAggregateVersions
        {
            get { return AggregateRepository.Instance.GetLastVersions(); }
        }

        public static NodeConfiguration TranslateNodeInfo(ExtendedNodeInfo nodeInfo)
        {
            return new NodeConfiguration
            {
                PublicAddress = nodeInfo.PublicAddress,
                InternalAddress = nodeInfo.InternalAddress,
                ServerId = nodeInfo.ServerId,
                IsMaster = nodeInfo.IsMaster,
                IsSelf = false
            };
        }

        public static void EnsureNodesConfigurationIsValid()
        {
            Core.Server.Resolve<INodeInitializer>().EnsureNodesConfigurationIsValid(Core.Server.Configuration.Nodes);
        }
    }
}
