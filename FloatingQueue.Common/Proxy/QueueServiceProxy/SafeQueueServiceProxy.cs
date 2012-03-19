using System;
using System.Collections.Generic;
using System.Linq;
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

        private List<NodeInfo> m_Nodes;
        private bool m_KeepConnectionOpened;
        private bool m_CancelFireClientCall;
        private bool m_Initialized;
        private bool m_ConnectionLost = false;

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

            m_CancelFireClientCall = true;
            var metadata = this.GetClusterMetadata();
            if (metadata == null)
                return false;

            m_CancelFireClientCall = false;
            m_Nodes = metadata.Nodes;
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
            List<NodeInfo> newNodes = null;
            string newAddress = string.Empty;

            m_CancelFireClientCall = true;

            foreach (var node in m_Nodes)
            {
                
                DoClose();
                EndpointAddress = new EndpointAddress(node.Address);
                CreateClient();

                var metadata = this.GetClusterMetadata();
                if (metadata == null)
                    continue;

                var master = metadata.Nodes.SingleOrDefault(n => n.IsMaster);
                if (master == null)
                    throw new ApplicationException("Critical Error! There's no master in cluster");

                DoClose();
                EndpointAddress = new EndpointAddress(master.Address);
                CreateClient();

                var testCall = this.GetClusterMetadata();
                if (testCall == null || testCall.Nodes == null) continue;

                newNodes = testCall.Nodes;
                newAddress = master.Address;
                success = true;
                break;
            }

            m_CancelFireClientCall = false;

            if (success)
            {
                FireMasterChanged(newAddress);
                m_Nodes = newNodes;
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
                return action();
            }
            catch (FaultException)
            {
                throw;
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

    }
}