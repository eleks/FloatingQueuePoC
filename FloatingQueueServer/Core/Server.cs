using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;
using Autofac;
using FloatingQueue.ServiceProxy;

namespace FloatingQueue.Server.Core
{
    public class Server
    {
        private static long ms_TransactionCounter = 0;

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

        public static long TransactionCounter
        {
            get { return ms_TransactionCounter; }
        }

        public static void FireTransactionCommited()
        {
            Interlocked.Increment(ref ms_TransactionCounter);
            Log.Debug("Transaction commited: {0}", ms_TransactionCounter);
        }

        public static T Resolve<T>()
        {
            return ServicesContainer.Resolve<T>();
        }
    }
}
