using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.ServiceModel;

namespace FloatingQueue.Common.Proxy.QueueServiceProxy
{
    public class MasterChangedEventArgs : EventArgs
    {
        public MasterChangedEventArgs(string newMasterAdress)
        {
            NewMasterAdress = newMasterAdress;
        }

        public string NewMasterAdress { get; private set; }
    }

    public class SafeQueueServiceProxy : QueueServiceProxyBase
    {
        //todo: save information that address is dead and try to connect to it in case all current nodes are dead
        private static ClusterMetadata ms_SharedClusterMetadata = null;
        private static readonly object ms_SyncRoot = new object();

        private bool m_KeepConnectionOpened;
        private bool m_Initialized;
        private bool m_NoRetryMode;

        public SafeQueueServiceProxy(string address)
            : base(address)
        {
            //todo: think about using WCF's tools to detect failures
            //var a = Client as ICommunicationObject;
            //a.Faulted += (sender, args) => { var b = 5; };
        }

        public void Init(bool keepConnectionOpened = false)
        {
            if (m_Initialized)
                throw new InvalidOperationException("Proxy is already initialized");

            m_Initialized = true;
            m_KeepConnectionOpened = keepConnectionOpened;

            ConnectToMaster();
        }

        public override void Push(string aggregateId, int version, object e)
        {
            SafeCall(() =>
            {
                base.Push(aggregateId, version, e);
            });
        }

        public override bool TryGetNext(string aggregateId, int version, out object next)
        {
            // can't use ref or out inside lambda
            next = null;
            object hack = null;
            bool result = false;

            SafeCall(() =>
            {
                object n;
                result = base.TryGetNext(aggregateId, version, out n);
                hack = n;
            });
            next = hack;
            return result;
        }

        public override IEnumerable<object> GetAllNext(string aggregateId, int version)
        {
            IEnumerable<object> result = null;
            SafeCall(() => result = base.GetAllNext(aggregateId, version));
            return result;
        }

        public override ClusterMetadata GetClusterMetadata()
        {
            ClusterMetadata result = null;
            SafeCall(() => result = base.GetClusterMetadata());
            return result;
        }

        private void SafeCall(Action action)
        {
            if (!m_Initialized)
                throw new InvalidOperationException("Proxy has to be initialized first");
            //
            if (m_NoRetryMode)
            {
                SafeNetworkOperation(action); // do the operation
            }
            else
            {
                const int maxRetry = 3;
                int retry = 0;
                while (retry < maxRetry)
                {
                    try
                    {
                        if (retry > 0)
                        {
                            ConnectToMaster();
                        }
                        SafeNetworkOperation(action); // try do operation
                        break; // no exceptions - success
                    }
                    catch (ConnectionErrorException)
                    {
                        if (retry == maxRetry - 1)
                            throw;
                    }
                    retry++;
                }
            }
        }

        private void SafeNetworkOperation(Action action)
        {
            try
            {
                CommunicationProvider.Instance.SafeNetworkCall(action);
            }
            finally
            {
                if (!m_KeepConnectionOpened)
                    CloseClient();
            }
        }

        private void ConnectToMaster()
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
                    if (master.Address != Address)
                    {
                        meta = ObtainNewMetadata(master.Address); // reconnect to obtained master and take its metadata
                        master = meta.Nodes.SingleOrDefault(n => n.IsMaster);
                        if (master == null || master.Address != Address)
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
            if (newMetadata == null && Address != null)
            {
                newMetadata = connectToMasterViaAddrFunc(Address);
            }

            // Oops! we still havn't found any valid master
            if (newMetadata == null)
            {
                CloseClient(); // ensure client is closed
                if (atLeastOneConnectionHasBeenEsteblished)
                    throw lastError;
                throw new ServerUnavailableException("No any server has been found");
            }
            RegisterSharedClusterMetadata(newMetadata);
        }

        private ClusterMetadata ObtainNewMetadata(string address)
        {
            CloseClient(); // ensure prev client is closed
            SetNewAddress(address);
            //
            ClusterMetadata result;
            var prevNoRetryMode = m_NoRetryMode;
            m_NoRetryMode = true;
            try
            {
                result = this.GetClusterMetadata();
            }
            finally
            {
                m_NoRetryMode = prevNoRetryMode;
            }
            return result;
        }


        #region Shared Address Pool

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