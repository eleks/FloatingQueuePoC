using System;
using System.Collections.Generic;
using System.ServiceModel;
using FloatingQueue.Common.Proxy;

namespace FloatingQueue.Server.Services.Proxy
{
    public class InternalQueueServiceProxy : ProxyBase<IInternalQueueService>, IInternalQueueServiceProxy
    {
        public InternalQueueServiceProxy(string address)
        {
            EndpointAddress = new EndpointAddress(address);
        }

        #region Standard Queue Service Functionality

        //note MM: multiple inheritance would be useful here

        public void Push(string aggregateId, int version, object e)
        {
            Client.Push(aggregateId, version, e);
        }

        public bool TryGetNext(string aggregateId, int version, out object next)
        {
            return Client.TryGetNext(aggregateId, version, out next);
        }

        public IEnumerable<object> GetAllNext(string aggregateId, int version)
        {
            return Client.GetAllNext(aggregateId, version);
        }

        #endregion

        #region Additional Functionality

        public int Ping()
        {
            // todo create enumeration for fault reasons
            try
            {
                return Client.Ping();
            }
            catch (CommunicationException)
            {
                return 1;
            }
            catch (TimeoutException)
            {
                return 2;
            }
        }

        public void IntroduceNewNode(NodeInfo nodeInfo)
        {
            throw new NotImplementedException();
        }

        public void RequestSynchronization(NodeInfo nodeInfo)
        {
            throw new NotImplementedException();
        }

        public void NotificateSlaveSynchronized(NodeInfo nodeInfo)
        {
            throw new NotImplementedException();
        }

        #endregion

        public void Open()
        {
            DoOpen();
        }

        public void Close()
        {
            DoClose();
        }
    }
}
