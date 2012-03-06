using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;

namespace FloatingQueueServer.Core
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

        public static T Resolve<T>()
        {
            return ServicesContainer.Resolve<T>();
        }
    }
}
