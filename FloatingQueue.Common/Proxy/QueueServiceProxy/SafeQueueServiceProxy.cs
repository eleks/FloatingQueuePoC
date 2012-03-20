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

        public event EventHandler<MasterChangedEventArgs> MasterChanged;
        public event EventHandler ClientCallFailed;
        public event EventHandler ConnectionLost;

        public bool Init(bool keepConnectionOpened = false)
        {
            if (m_Initialized)
                throw new InvalidOperationException("Proxy is already initialized");

            m_Initialized = true;
            m_KeepConnectionOpened = keepConnectionOpened;
            ClientCallFailed += HandleClientCallFailed;
            ConnectionLost += HandleConnectionLost;

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

        private void HandleClientCallFailed(object sender, EventArgs e)
        {
            bool success = false;
            List<NodeInfo> newNodes = null;
            string newAddress = string.Empty;

            m_CancelFireClientCall = true;

            foreach (var node in m_Nodes)
            {
                
                CloseClient();
                //EndpointAddress = new EndpointAddress(node.Address);//TODO: to be removed during refactoring
                //CreateClient();

                var metadata = this.GetClusterMetadata();
                if (metadata == null)
                    continue;

                var master = metadata.Nodes.SingleOrDefault(n => n.IsMaster);
                if (master == null)
                    throw new ApplicationException("Critical Error! There's no master in cluster");

                CloseClient();
                //EndpointAddress = new EndpointAddress(master.Address);//TODO: to be removed during refactoring
                //CreateClient();

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
                OnMasterChanged(this, new MasterChangedEventArgs(newAddress));
                m_Nodes = newNodes;
            }
            else
            {
                OnConnectionLost(this, EventArgs.Empty);
            }
        }

        private void HandleConnectionLost(object sender, EventArgs e)
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
            catch(IOException)
            {
                if (!m_CancelFireClientCall)
                    OnClientCallFailed(this, EventArgs.Empty);
                return failoverAction();
            }
            catch(SocketException)
            {
                if (!m_CancelFireClientCall)
                    OnClientCallFailed(this, EventArgs.Empty);
                return failoverAction();
            }
            catch (CommunicationException)
            {
                if (!m_CancelFireClientCall)
                    OnClientCallFailed(this, EventArgs.Empty);
                return failoverAction();
            }
            catch (TimeoutException)
            {
                if (!m_CancelFireClientCall)
                    OnClientCallFailed(this, EventArgs.Empty);
                return failoverAction();
            }
            finally
            {
                if (!m_KeepConnectionOpened)
                    CloseClient();
            }
        }

        protected virtual void OnMasterChanged(object sender, MasterChangedEventArgs e)
        {
            var handler = MasterChanged;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        protected virtual void OnClientCallFailed(object sender, EventArgs e)
        {
            var handler = ClientCallFailed;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        protected virtual void OnConnectionLost(object sender, EventArgs e)
        {
            var handler = ConnectionLost;
            if (handler != null)
            {
                handler(sender, e);
            }
        }
    }
}