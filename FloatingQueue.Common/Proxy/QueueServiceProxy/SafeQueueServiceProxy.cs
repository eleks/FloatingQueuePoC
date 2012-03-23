using System;
using System.Collections.Generic;
using System.Linq;

namespace FloatingQueue.Common.Proxy.QueueServiceProxy
{
    public class SafeQueueServiceProxy : SafeServiceProxyBase<QueueServiceProxy, IQueueService>, IQueueService
    {
        private const int MaxRetry = 3;

        public SafeQueueServiceProxy(string address, bool keepConnectionOpened)
            : base(new QueueServiceProxy(address), keepConnectionOpened, true, MaxRetry)
        {
        }

        public void Push(string aggregateId, int version, object e)
        {
            SafeCall(() => Proxy.Push(aggregateId, version, e));
        }

        public bool TryGetNext(string aggregateId, int version, out object next)
        {
            // can't use ref or out inside lambda
            next = null;
            object hack = null;
            bool result = false;

            SafeCall(() =>
            {
                object n;
                result = Proxy.TryGetNext(aggregateId, version, out n);
                hack = n;
            });
            next = hack;
            return result;
        }

        public IEnumerable<object> GetAllNext(string aggregateId, int version)
        {
            IEnumerable<object> result = null;
            SafeCall(() => result = Proxy.GetAllNext(aggregateId, version));
            return result;
        }

        public ClusterMetadata GetClusterMetadata()
        {
            ClusterMetadata result = null;
            SafeCall(() => result = Proxy.GetClusterMetadata());
            return result;
        }

        protected override void ReConnect()
        {
            Exception lastError = null;
            bool atLeastOneConnectionHasBeenEsteblished = false;
            ClusterMetadata newMetadata = null;

            Func<string, ClusterMetadata> connectToMasterViaAddrFunc = addr =>
            {
                try
                {
                    var meta = ObtainNewMetadata(addr);
                    atLeastOneConnectionHasBeenEsteblished = true;
                    var master = meta.Nodes.SingleOrDefault(n => n.IsMaster);
                    if (master == null)
                        throw new ServerIsReadonlyException("No master found");
                    if (master.Address != Proxy.Address)
                    {
                        meta = ObtainNewMetadata(master.Address); // reconnect to obtained master and take its metadata
                        master = meta.Nodes.SingleOrDefault(n => n.IsMaster);
                        if (master == null || master.Address != Proxy.Address)
                            throw new Exception("Master say - he is not a master ???");
                    }
                    return meta;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    return null;
                }
            };

            // use shared metadata to esteblish master connection
            ClusterMetadata oldMetadata = GetSharedClusterMetadata();
            if (oldMetadata != null)
            {
                // try to connect to last known master
                var oldMaster = oldMetadata.Nodes.FirstOrDefault(v => v.IsMaster);
                if (oldMaster != null)
                {
                    newMetadata = connectToMasterViaAddrFunc(oldMaster.Address);
                }
                // try to find master anywhere in slaves
                if (newMetadata == null)
                {
                    foreach (var node in oldMetadata.Nodes.Where(v => !v.IsMaster))
                    {
                        newMetadata = connectToMasterViaAddrFunc(node.Address);
                        if (newMetadata != null)
                            break;
                    }
                }
            }

            // try to use specified address (if provided)
            if (newMetadata == null && Proxy.Address != null)
            {
                newMetadata = connectToMasterViaAddrFunc(Proxy.Address);
            }

            // Oops! we still havn't found any valid master
            if (newMetadata == null)
            {
                Proxy.CloseClient(); // ensure client is closed
                if (atLeastOneConnectionHasBeenEsteblished)
                    throw lastError;
                throw new ServerUnavailableException("No any server has been found");
            }
            RegisterSharedClusterMetadata(newMetadata);
        }

        private ClusterMetadata ObtainNewMetadata(string address)
        {
            Proxy.CloseClient(); // ensure prev client is closed
            Proxy.SetNewAddress(address);
            //
            ClusterMetadata result = null;
            ExecuteInNoRetryMode(() => result = GetClusterMetadata());
            return result;
        }


        #region Shared Address Pool

        private static ClusterMetadata ms_SharedClusterMetadata;
        private static readonly object ms_SyncRoot = new object();

        private static void RegisterSharedClusterMetadata(ClusterMetadata metadata)
        {
            lock (ms_SyncRoot)
            {
                ms_SharedClusterMetadata = metadata;
            }
        }

        private static ClusterMetadata GetSharedClusterMetadata()
        {
            lock (ms_SyncRoot)
            {
                return ms_SharedClusterMetadata;
            }
        }

        #endregion

    }
}