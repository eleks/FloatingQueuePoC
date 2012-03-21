using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using FloatingQueue.Common;
using FloatingQueue.Common.TCP;
using FloatingQueue.Common.TCPProvider;
using FloatingQueue.Common.WCF;
using FloatingQueue.Server.Core;
using FloatingQueue.Server.Exceptions;
using FloatingQueue.Server.Replication;
using FloatingQueue.Server.Services;
using FloatingQueue.Server.Services.Implementation;
using FloatingQueue.Server.Services.Proxy;
using FloatingQueue.Server.TCP;
using NDesk.Options;

namespace FloatingQueue.Server
{
    class Program
    {
        private static CommunicationObjectBase ms_InternalHost;
        private static CommunicationObjectBase ms_PublicHost;
        private static List<string> ms_NodesAddresses;

        static void Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                ShowUsage();
                return;
            }

            try
            {
                if (!Initialize(args))
                {
                    Core.Server.Log.Error("Couldn't initialize server.");
                    Console.ReadLine();
                    return;
                }
                if (!RunHosts())
                {
                    Core.Server.Log.Error("Couldn't run hosts.");
                    Console.ReadLine();
                    return;
                }
                if (!JoinCluster())
                {
                    Core.Server.Log.Error("Couldn't connect to cluster");
                    Console.ReadLine();
                    return;
                }
                WaitForTerminate();
            }
            catch (Exception ex)
            {
                // logger may be not initialized here
                Console.WriteLine("AHTUNG! Unhandled Exception!!!{0}{1}", Environment.NewLine, ex);
                Console.ReadLine();
            }
        }

        #region Main Logic

        private static bool Initialize(string[] args)
        {
            try
            {
                InitializeCommunicationProvider(useTCP: true);

                var configuration = ParseConfiguration(args);

                var componentsManager = new ComponentsManager();
                var container = componentsManager.GetContainer(configuration);
                Core.Server.Init(container);
            }
            catch (Exception ex)
            {
                Core.Server.Log.Warn("Error while initializing server{0}{1}{0}{2}", Environment.NewLine, ex.Message, ex.StackTrace);
                return false;
            }
            return true;
        }

        private static bool RunHosts()
        {
            try
            {
                ms_PublicHost = CreateHost<PublicQueueService>(Core.Server.Configuration.PublicAddress);
                ms_InternalHost = CreateHost<InternalQueueService>(Core.Server.Configuration.InternalAddress);

                ms_InternalHost.Open();
                ms_PublicHost.Open();

                Core.Server.Log.Info("I am {0}", Core.Server.Configuration.IsMaster ? "master" : "slave");
                Core.Server.Log.Info("Listening: ");
                Core.Server.Log.Info("\tpublic: {0}", Core.Server.Configuration.PublicAddress);
                Core.Server.Log.Info("\tinternal: {0}", Core.Server.Configuration.InternalAddress);
            }
            catch (Exception ex)
            {
                Core.Server.Log.Warn("Error while running hosts{0}{1}{0}{2}", Environment.NewLine, ex.Message, ex.StackTrace); ;
                return false;
            }
            return true;
        }

        private static bool JoinCluster()
        {
            try
            {
                if (IsMaster)
                {
                    Core.Server.Resolve<IConnectionManager>().OpenOutcomingConnections();
                }
                else
                {
                    Core.Server.Resolve<INodeInitializer>().CollectClusterMetadata(ms_NodesAddresses);
                    Core.Server.Resolve<INodeInitializer>().CreateProxies();
                    Core.Server.Resolve<INodeInitializer>().StartSynchronization();
                }

                Core.Server.Resolve<IMasterElections>().Init();
            }
            catch (BadConfigurationException ex)
            {
                Core.Server.Log.Warn("Error while joining cluster{0}{1}{0}{2}", Environment.NewLine, ex.Message, ex.StackTrace);
                return false;
            }
            return true;
        }

        private static void WaitForTerminate()
        {
            try
            {
                Core.Server.Log.Info("Press <ENTER> to terminate Host{0}", Environment.NewLine);
                Console.ReadLine();

                ms_PublicHost.Close();
                ms_InternalHost.Close();

                Core.Server.Resolve<IConnectionManager>().CloseOutcomingConnections();
            }
            catch (Exception ex)
            {
                Core.Server.Log.Warn("Didn't terminate hosts properly{0}{1}{0}{2}", Environment.NewLine, ex.Message, ex.StackTrace);
                throw;
            }
        }

        #endregion

        #region Helper Methods

        private static void InitializeCommunicationProvider(bool useTCP)
        {
            if (useTCP)
            {
                // TCP
                var provider = new TCPCommunicationProvider();
                provider.RegisterHostImplementation<PublicQueueService>(() => new TCPPublicQueueService());
                provider.RegisterHostImplementation<InternalQueueService>(() => new TCPInternalQueueService());
                provider.RegisterChannelImplementation<IInternalQueueService>(() => new TCPInternalQueueServiceProxy());
                CommunicationProvider.Init(provider);
            }
            else
            {
                // WCF
                var provider = new WCFCommunicationProvider();
                CommunicationProvider.Init(provider);
            }
        }

        private static ServerConfiguration ParseConfiguration(string[] args)
        {
            const string addressMask = "{0}:{1}";
            const string localAddress = "net.tcp://localhost";

            var configuration = new ServerConfiguration { ServerId = 0};
            int publicPort = 80, internalPort = 81;
            byte serverId = 255;
            bool isMaster = false,
                 idSet = false;
            ms_NodesAddresses = new List<string>();

            var p = new OptionSet()
                    {
                        {"pp|pubport=", v => int.TryParse(v, out publicPort)},
                        {"ip|intport=", v => int.TryParse(v, out internalPort)},
                        {"m|master", v => isMaster = !string.IsNullOrEmpty(v) },
                        {"id=",v => idSet = byte.TryParse(v, out serverId) },
                        {"n|nodes=", v => ms_NodesAddresses.AddRange( v.Split(';')) }
                    };
            p.Parse(args);

            if (ms_NodesAddresses.Count == 0 && !isMaster)
                throw new BadConfigurationException("At least 1 node should be set at startup");

            if (!idSet)
                throw new BadConfigurationException("Id must be set(from 0 to 255)");


            // init nodes with single node - self
            configuration.Nodes = new NodeCollection(new List<INodeConfiguration> { new NodeConfiguration
                  {
                      InternalAddress = string.Format(addressMask, localAddress, internalPort),
                      PublicAddress = string.Format(addressMask, localAddress, publicPort),
                      IsMaster = isMaster,
                      IsSelf = true,
                      ServerId = serverId
                  }});

            configuration.IsSynced = isMaster; // only master is treated as synced at startup
            configuration.IsReadonly = true; // nothing should be written until new nodes come up
            configuration.ServerId = serverId;

            return configuration;
        }

        private static CommunicationObjectBase CreateHost<T>(string address)
        {
            var host = CommunicationProvider.Instance.CreateHost<T>(typeof(T).Name, address);
            return host;
        }

        private static bool IsMaster
        {
            get { return Core.Server.Configuration.IsMaster; }
        }

        private static void ShowUsage()
        {
            // todo : use NDesk.Options.WriteOptionDescriptions method here
            Console.WriteLine("Usage: {0} <arg1> .. <argN>", AppDomain.CurrentDomain.FriendlyName);
            Console.WriteLine("Arguments:");
            Console.WriteLine("\tp|port=(int16) - port to run server");
            Console.WriteLine("\tm|master - mark server as master");
        }

        #endregion
    }
}