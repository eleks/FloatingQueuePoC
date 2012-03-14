using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FloatingQueue.Common;

namespace FloatingQueue.Server.Replication
{
    public static class PingHelper
    {
        public static PingParams CheckConnectionPingParams
        {
            get
            {
                return new PingParams()
                           {
                               NodeInfo = CurrentNodeInfo
                           };
            }
        }

        public static PingParams IntroductionOfNewNodePingParams
        {
            get
            {
                return new PingParams(PingReason.IntroductionOfNewNode)
                           {
                               NodeInfo = CurrentNodeInfo
                           };
            }
        }

        public static PingParams RequestForSyncPingParams
        {
            get
            {
                return new PingParams(PingReason.RequestForSyncronization)
                           {
                               NodeInfo = CurrentNodeInfo
                           };
            }
        }


        public static NodeInfo CurrentNodeInfo
        {
            get
            {
                return new NodeInfo()
                           {
                               Address = Core.Server.Configuration.Address,
                               ServerId = Core.Server.Configuration.ServerId
                           };
            }
        }

    }
}
