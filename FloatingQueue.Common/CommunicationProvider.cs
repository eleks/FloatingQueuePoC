using System;
using System.ServiceModel;
using FloatingQueue.Common.TCP;

namespace FloatingQueue.Common
{
    public class ConnectionErrorException : Exception
    {
        public ConnectionErrorException()
        {
        }

        public ConnectionErrorException(Exception innerException) : base("Communication Error", innerException)
        {
        }
    }


    public interface ICommunicationProvider
    {
        T CreateChannel<T>(EndpointAddress endpointAddress);
        CommunicationObjectBase CreateHost<T>(string displayName, string address);
        //
        void OpenChannel<T>(T client);
        void CloseChannel<T>(T client);
        //
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
