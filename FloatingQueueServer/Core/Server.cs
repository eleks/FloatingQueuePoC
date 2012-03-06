using System.Collections.Generic;
using System.ServiceModel;
using Autofac;
using FloatingQueue.ServiceProxy;

namespace FloatingQueue.Server.Core
{
    public class Server
    {
        public static IContainer ServicesContainer { get; private set; }

        public static void Init(IContainer container)
        {
            ServicesContainer = container;
        }

        public static ILogger Log
        {
            get { return ServicesContainer.Resolve<ILogger>(); }
        }

        public static IConfiguration Configuration
        {
            get { return ServicesContainer.Resolve<IConfiguration>(); }
        }

        public static T Resolve<T>()
        {
            return ServicesContainer.Resolve<T>();
        }

        // todo: consider synchronizations issues
        private static readonly List<ManualQueueProxy> m_Siblings = new List<ManualQueueProxy>();

        private static bool m_IsConnectionOpened = false;
        public static void ConnectToSiblings()
        {
            foreach (var node in Configuration.Nodes)
            {
                var proxy = new ManualQueueProxy(node.Address);
                m_Siblings.Add(proxy);
                proxy.Open();
                Log.Info("Connected to \t{0}", node.Address);
            }
            m_IsConnectionOpened = true;
        }

        //todo: find a better name, structure code better
        public static void CloseOutcomingConnections()
        {
            foreach (var proxy in m_Siblings)
            {
                proxy.Close();
            }
            m_IsConnectionOpened = false;
        }

        public static void Broadcast(string aggregateId, int version, object e)
        {
            if (!m_IsConnectionOpened)
            {
                ConnectToSiblings();
            }
            foreach (var proxy in m_Siblings)
            {
                proxy.Push(aggregateId, version, e);
            }
        }


    }
}
