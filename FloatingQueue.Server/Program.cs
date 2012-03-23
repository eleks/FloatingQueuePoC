using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using FloatingQueue.Common;
using FloatingQueue.Common.Common;
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
    public static class Program
    {
        private static CommunicationObjectBase ms_InternalHost;
        private static CommunicationObjectBase ms_PublicHost;
        private static List<string> ms_NodesAddresses;
        private static OptionSet ms_Arguments;

        public static void Main(string[] args)
        {
            try
            {
                if (!Initialize(args))
                {
                    // Core.Server.Log may be not initialized yet
                    Logger.Instance.Error("Couldn't initialize server.");
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
                Logger.Instance.Error("AHTUNG! Unhandled Exception!!!{0}{1}", Environment.NewLine, ex);
                Console.ReadLine();
            }
        }

        #region Main Logic

        private static bool Initialize(string[] args)
        {
            try
            {
                var configuration = ParseConfiguration(args);

                InitializeCommunicationProvider(useTCP: true);

                var componentsManager = new ComponentsManager();
                var container = componentsManager.GetContainer(configuration);
                Core.Server.Init(container);
            }
            catch (BadConfigurationException ex)
            {
                Logger.Instance.Warn("Bad configuration: {0}",ex.Message);
                ShowUsage();
                return false;
            }
            catch (Exception ex)
            {
                Logger.Instance.Warn("Error while initializing server{0}{1}{0}{2}", Environment.NewLine, ex.Message, ex.StackTrace);
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
                Core.Server.Log.Info("Terminating...");
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

        private static ServerConfiguration ParseConfiguration(string[] args)
        {
            if (args == null || args.Length ==0)
                throw new BadConfigurationException("arguments are empty");

            const string addressMask = "{0}:{1}";
            const string localAddress = "net.tcp://localhost";

            var configuration = new ServerConfiguration { ServerId = 0 };
            int publicPort = 80, internalPort = 81;
            byte serverId = 255;
            bool isMaster = false,
                 idSet = false;
            ms_NodesAddresses = new List<string>();

            ms_Arguments = new OptionSet
                    {
                        {"pp|pubport=", "int16 {PORT} for listening public requests",
                            v => int.TryParse(v, out publicPort)},
                        {"ip|intport=", "int16 {PORT} for listening internal requests",
                            v => int.TryParse(v, out internalPort)},
                        {"m|master", "mark current server as master ",
                            v => isMaster = !string.IsNullOrEmpty(v) },
                        {"id=", "unique int16 {ID} of this server",
                            v => idSet = byte.TryParse(v, out serverId) },
                        {"n|nodes=", "a list of {NODES} for initial connect to cluster",
                            v => ms_NodesAddresses.AddRange( v.Split(';')) }
                    };
            try
            {
                ms_Arguments.Parse(args);
            }
            catch (OptionException ex)
            {
                throw new BadConfigurationException(ex.Message);
            }
            
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
            Logger.Instance.Warn("Usage:");
            Logger.Instance.Warn("{0} <arg1> .. <argN>", AppDomain.CurrentDomain.FriendlyName);
            Logger.Instance.Warn("Arguments:");
            if (ms_Arguments != null)
            {
                using (var writer = new StringWriter())
                {
                    ms_Arguments.WriteOptionDescriptions(writer);
                    Logger.Instance.Warn("{0}{1}",Environment.NewLine, writer.ToString());
                }
            }
            else
            {
                Logger.Instance.Warn("Oops! Error occured before parsing arguments");
            }
        }

        #endregion
    }
}