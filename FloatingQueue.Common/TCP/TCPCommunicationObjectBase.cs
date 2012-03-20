using System;
using System.ServiceModel;

namespace FloatingQueue.Common.TCP
{
    public abstract class TCPCommunicationObjectBase : ICommunicationObject, IDisposable
    {
        #region Not Implemented members

        public void Close(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginClose(AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public void EndClose(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        public void Open(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginOpen(AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public void EndOpen(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        public event EventHandler Closed;
        public event EventHandler Closing;
        public event EventHandler Faulted;
        public event EventHandler Opened;
        public event EventHandler Opening;

        #endregion

        public CommunicationState State { get; protected set; }

        public abstract void Abort();
        public abstract void Close();
        public abstract void Open();

        public void Dispose()
        {
            Close();
        }
    }


    public static class TCPCommunicationSignature
    {
        public static readonly int Request = 0x34567890;
        public static readonly int Response = 0x67234519;
    }


}
