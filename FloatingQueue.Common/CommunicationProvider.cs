using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace FloatingQueue.Common
{
    public interface ICommunicationProvider
    {
        T CreateChannel<T>(EndpointAddress endpointAddress);

        ICommunicationObject CreateHost<T>(string address);
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
