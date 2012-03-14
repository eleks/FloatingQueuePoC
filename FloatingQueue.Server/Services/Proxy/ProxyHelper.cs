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
                               ServerId = Core.Server.Configuration.ServerId
                           };
            }
        }
    }
}
