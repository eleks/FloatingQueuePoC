using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace FloatingQueue.Common.Proxy.QueueServiceProxy
{
    public class SafeQueueServiceProxy : QueueServiceProxyBase
    {
        public delegate void ClientCallFailedHandler();

        public event ClientCallFailedHandler OnClientCallFailed;

        public SafeQueueServiceProxy(string address)
            : base(address)
        {

            //todo: think about using WCF's tools to detect failures
            //var a = Client as ICommunicationObject;
            //a.Faulted += (sender, args) => { var b = 5; };
        }

        public override void Push(string aggregateId, int version, object e)
        {
            try
            {
                base.Push(aggregateId, version, e);
            }
            catch (FaultException)
            {
                throw;
            }
            catch (Exception)
            {
                // todo MM: catch more concrete exceptions
                FireClientCallFailed();
            }
            finally
            {
                DoClose();
            }
        }

        public override bool TryGetNext(string aggregateId, int version, out object next)
        {
            try
            {
                // out parameters cannot be used inside anonymous methods,
                // otherwise wrapper action would be used
                return base.TryGetNext(aggregateId, version, out next);
            }
            catch (FaultException)
            {
                throw;
            }
            catch (Exception)
            {
                FireClientCallFailed();
                next = null;
                return false;
            }
            finally
            {
                DoClose();
            }
        }

        public override IEnumerable<object> GetAllNext(string aggregateId, int version)
        {
            try
            {
                return base.GetAllNext(aggregateId, version);
            }
            catch (FaultException)
            {
                throw;
            }
            catch (Exception)
            {
                FireClientCallFailed();
                return null;
            }
            finally
            {
                DoClose();
            }
        }

        public override ClusterMetadata GetClusterMetadata()
        {
            try
            {
                return base.GetClusterMetadata();
            }
            catch (Exception)
            {
                FireClientCallFailed();
                return null;
            }
            finally
            {
                DoClose();
            }
        }

        private void FireClientCallFailed()
        {
            if (OnClientCallFailed != null)
                OnClientCallFailed();
        }
    }
}