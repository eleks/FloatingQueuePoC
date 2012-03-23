using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FloatingQueue.Server.Core;
using FloatingQueue.Server.EventsLogic;
using FloatingQueue.Server.Exceptions;
using FloatingQueue.Server.Services;
using FloatingQueue.Server.Services.Proxy;

namespace FloatingQueue.Server.Replication
{
    public interface INodeInitializer
    {
        void CollectClusterMetadata(IEnumerable<string> nodesAddresses);
        void CreateProxies();
        void StartSynchronization();
        void EnsureNodesConfigurationIsValid(INodeCollection nodes);
    }

    public class NodeInitializer : INodeInitializer
    {
        public void CollectClusterMetadata(IEnumerable<string> nodesAddresses)
        {
            Core.Server.Log.Debug("Collecting cluster metadata");

            if (nodesAddresses == null || nodesAddresses.Count() == 0)
                throw new BadConfigurationException("There are no addresses to get metadata from");

            List<ExtendedNodeInfo> nodesInfo = null;
            foreach (var address in nodesAddresses)
            {
                using (var proxy = new SafeInternalQueueServiceProxy(address))
                {
                    try
                    {
                        nodesInfo = proxy.GetExtendedMetadata();
                        if (nodesInfo != null)
                            break; //todo: it would be nice to check if all the data is the same
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }
            if (nodesInfo == null || nodesInfo.Count == 0)
                throw new BadConfigurationException("No nodes were found in cluster");

            foreach (var nodeInfo in nodesInfo)
            {
                var node = ProxyHelper.TranslateNodeInfo(nodeInfo);
                if (!Core.Server.Configuration.Nodes.All.Contains(node))
                {
                    Core.Server.Log.Debug("Adding {1} at {0} to cluster", node.InternalAddress, nodeInfo.IsMaster ? "Master" : "Slave");
                    Core.Server.Configuration.Nodes.AddNewNode(node);
                }
            }

            ProxyHelper.EnsureNodesConfigurationIsValid();

            Core.Server.Log.Debug("Collecting cluster metadata has finished. Proxies are still not created");
        }

        public void StartSynchronization()
        {
            var masterProxy = Core.Server.Configuration.Nodes.Master.Proxy;
            try
            {
                masterProxy.Open();
                masterProxy.RequestSynchronization(ProxyHelper.CurrentNodeInfo, ProxyHelper.CurrentAggregateVersions);
            }
            finally
            {
                masterProxy.Close();
            }
        }

        public void CreateProxies()
        {
            Core.Server.Log.Debug("Creating proxies...");

            var siblings = Core.Server.Configuration.Nodes.Siblings;

            if (siblings.Count == 0)
            {
                Core.Server.Log.Info("No other servers found in cluster");
                return;
            }

            foreach (var node in siblings.Where(node => node.Proxy == null))
            {
                node.CreateProxy();
                Core.Server.Log.Debug("Created proxy for {0}", node.InternalAddress);
            }

            Core.Server.Log.Debug("Finished creating proxies");
        }

        public void EnsureNodesConfigurationIsValid(INodeCollection nodes)
        {
            int mastersCount = nodes.All.Count(n => n.IsMaster);
            if (mastersCount != 1)
                throw new BadConfigurationException("There must be exactly 1 master node");

            if (nodes.All.Select(n => n.ServerId).Distinct().Count() < nodes.All.Count())
                throw new BadConfigurationException("Every node must have unique Id");
        }
    }
}
