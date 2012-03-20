using System;
using System.ServiceModel;

namespace FloatingQueue.Common
{
    public class ConnectionErrorException : Exception
    {
        public ConnectionErrorException(Exception innerException) : base("Communication Error", innerException)
        {
        }
    }


    public interface ICommunicationProvider
    {
        T CreateChannel<T>(EndpointAddress endpointAddress);
        ICommunicationObject CreateHost<T>(string displayName, string address);
        void SafeNetworkCall(Action action);
    }

    public static class CommunicationProvider
    {
        private static ICommunicationProvider ms_Instance;

        public static ICommunicationProvider Instance
        {
            get { return ms_Instance; }
        }

        public static void Init(ICommunicationProvider instance)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");
            if (ms_Instance != null)
                throw new InvalidOperationException("Communication provider is already installed");

            ms_Instance = instance;
        }
    }
}
