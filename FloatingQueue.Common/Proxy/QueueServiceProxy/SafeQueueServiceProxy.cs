using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.ServiceModel;
using System.Threading;

namespace FloatingQueue.Common.Proxy.QueueServiceProxy
{
    public class SafeQueueServiceProxy : QueueServiceProxyBase
    {
        public delegate void ClientCallFailedHandler();
        public delegate void MasterChangedHandler(string newMasterAddress);
        public delegate void ConnectionLostHandler();

        public event MasterChangedHandler OnMasterChanged;
        public event ClientCallFailedHandler OnClientCallFailed;
        public event ConnectionLostHandler OnConnectionLost;

        private bool m_KeepConnectionOpened;
        private bool m_CancelFireClientCall = false;
        private bool m_Initialized;
        private bool m_ConnectionLost = false;

        //todo: save information that address is dead
        private static readonly List<string> ms_SharedAddressPool = new List<string>();
        private static readonly object ms_SyncRoot = new object();

        public SafeQueueServiceProxy(string address)
            : base(address)
        {
            //todo: think about using WCF's tools to detect failures
            //var a = Client as ICommunicationObject;
            //a.Faulted += (sender, args) => { var b = 5; };
        }

        public bool Init(bool keepConnectionOpened = false)
        {
            if (m_Initialized)
                throw new InvalidOperationException("Proxy is already initialized");

            m_Initialized = true;
            m_KeepConnectionOpened = keepConnectionOpened;
            OnClientCallFailed += HandleClientCallFailed;
            OnConnectionLost += HandleConnectionLost;

            var metadata = this.GetClusterMetadata();
            if (metadata == null)
                return false;

            m_CancelFireClientCall = false;
            AddSharedAddresses(metadata.Nodes.Select(n => n.Address));
            return true;
        }

        public override void Push(string aggregateId, int version, object e)
        {
            SafeCall(() =>
            {
                base.Push(aggregateId, version, e); return 0;
            },
            failoverAction: () => 0);
        }

        public override bool TryGetNext(string aggregateId, int version, out object next)
        {
            // can't use ref or out inside lambda
            next = null;
            object hack = null;

            bool result = SafeCall(() =>
            {
                object n;
                bool success = base.TryGetNext(aggregateId, version, out n);
                hack = n;
                return success;
            },
            failoverAction: () => false);

            next = hack;
            return result;
        }

        public override IEnumerable<object> GetAllNext(string aggregateId, int version)
        {
            return SafeCall(() => base.GetAllNext(aggregateId, version),
            failoverAction: () => null);
        }

        public override ClusterMetadata GetClusterMetadata()
        {
            return SafeCall(() => base.GetClusterMetadata(),
            failoverAction: () => null);
        }

        private void FireMasterChanged(string newMasterAddress)
        {
            if (OnMasterChanged != null)
                OnMasterChanged(newMasterAddress);
        }

        private void FireConnectionLost()
        {
            if (OnConnectionLost != null)
                OnConnectionLost();
        }

        private void FireClientCallFailed()
        {
            if (OnClientCallFailed != null)
                OnClientCallFailed();
        }

        private void HandleClientCallFailed()
        {
            bool success = false;
            List<string> newAddresses = null;
            string newMasterAddress = string.Empty;

            m_CancelFireClientCall = true;

            string[] addressPool = GetSharedAddresses();
            foreach (var nodeAddress in addressPool)
            {
                //todo: wrap this in method
                DoClose();
                EndpointAddress = new EndpointAddress(nodeAddress);
                CreateClient();

                var metadata = this.GetClusterMetadata();
                if (metadata == null)
                {
                    RemoveSharedAddress(nodeAddress);
                    continue;
                }

                var master = metadata.Nodes.SingleOrDefault(n => n.IsMaster);
                if (master == null)
                    throw new ApplicationException("Critical Error! There's no master in cluster");

                DoClose();
                EndpointAddress = new EndpointAddress(master.Address);
                CreateClient();

                var testCall = this.GetClusterMetadata();
                if (testCall == null || testCall.Nodes == null)
                {
                    RemoveSharedAddress(master.Address);
                    continue;
                }

                newAddresses = testCall.Nodes.Select(n => n.Address).ToList();
                newMasterAddress = master.Address;
                success = true;
                break;
            }

            m_CancelFireClientCall = false;

            if (success)
            {
                FireMasterChanged(newMasterAddress);
                AddSharedAddresses(newAddresses);
            }
            else
            {
                FireConnectionLost();
            }
        }

        private void HandleConnectionLost()
        {
            m_ConnectionLost = true;
        }

        private T SafeCall<T>(Func<T> action, Func<T> failoverAction)
        {
            if (!m_Initialized)
                throw new InvalidOperationException("Proxy has to be initialized first");

            //if (m_ConnectionLost)
            //    return failoverAction();

            try
            {
                //todo: retry action on exceptions(and a bug with handled init crash returning false would be fixed)
                return action();
            }
            catch (FaultException)
            {
                throw;
            }
            catch(IOException)
            {
                if (!m_CancelFireClientCall)
                    FireClientCallFailed();
                return failoverAction();
            }
            catch(SocketException)
            {
                if (!m_CancelFireClientCall)
                    FireClientCallFailed();
                return failoverAction();
            }
            catch (CommunicationException)
            {
                if (!m_CancelFireClientCall)
                    FireClientCallFailed();
                return failoverAction();
            }
            catch (TimeoutException)
            {
                if (!m_CancelFireClientCall)
                    FireClientCallFailed();
                return failoverAction();
            }
            finally
            {
                if (!m_KeepConnectionOpened)
                    DoClose();
            }
        }

        #region Shared Address Pool

        private static void AddSharedAddresses(IEnumerable<string> addresses)
        {
            lock (ms_SyncRoot)
            {
                foreach (var address in addresses)
                {
                    if (!ms_SharedAddressPool.Contains(address))
                        ms_SharedAddressPool.Add(address);
                }
            }
        }

        private static void RemoveSharedAddress(string address)
        {
            lock (ms_SyncRoot)
            {
                ms_SharedAddressPool.Remove(address);
            }
        }

        private static string[] GetSharedAddresses()
        {
            string[] addressPool;
            lock (ms_SyncRoot)
            {
                addressPool = new string[ms_SharedAddressPool.Count];
                ms_SharedAddressPool.CopyTo(addressPool);
            }
            return addressPool;
        }

        #endregion

    }
}