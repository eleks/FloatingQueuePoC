﻿using System;
using System.Threading;
using Autofac;
using FloatingQueue.Common.Common;
using FloatingQueue.Server.Exceptions;

namespace FloatingQueue.Server.Core
{
    public class Server
    {
        private static long ms_TransactionCounter = 0;

        public static IContainer ServicesContainer { get; private set; }

        public static void Init(IContainer container)
        {
            if(Initialized)
            {
                #if !UNITTESTS

                //throw new ServerInitializationException("Server is already initialized");

                #endif
            }
            if(container == null)
            {
                throw new ArgumentNullException("container");
            }

            ServicesContainer = container;
            Initialized = true;
        }

        public static bool Initialized { get; private set; }

        public static ILogger Log
        {
            get { return ServicesContainer.Resolve<ILogger>(); }
        }

        public static IServerConfiguration Configuration
        {
            get { return ServicesContainer.Resolve<IServerConfiguration>(); }
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
