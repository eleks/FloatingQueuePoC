using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using FloatingQueue.Common;
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
        static void Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                ShowUsage();
                return;
            }

            InitializeCommunicationProvider(useTCP : false);

            Initialize(args);
            RunHosts();
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

        private static void Initialize(string[] args)
        {
            var configuration = ParseConfiguration(args);

            var componentsManager = new ComponentsManager();
            var container = componentsManager.GetContainer(configuration);
            Core.Server.Init(container);

            
        }

        private static ServerConfiguration ParseConfiguration(string[] args)
        {
            const string addressMask = "{0}:{1}";
            const string localAddress = "net.tcp://localhost";

            var configuration = new ServerConfiguration { ServerId = 0 };
            var nodes = new List<INodeConfiguration>();
            int publicPort = 80, internalPort = 81;
            bool isMaster = false, isSynced = false;

            //TODO MM: cleanup this messy arguments.
            var p = new OptionSet()
                    {
                        {"pp|pubport=", v => int.TryParse(v, out publicPort)},
                        {"ip|intport=", v => int.TryParse(v, out internalPort)},
                        {"m|master", v => isMaster = !string.IsNullOrEmpty(v) },
                        {"s|sync", v => isSynced = !string.IsNullOrEmpty(v) },
                        {"id=",v => configuration.ServerId = byte.Parse(v) },
                        {"n|nodes=", v => nodes.AddRange(v.Split(';').Select(
                                          node => 
                                          { 
                                              var info = node.Split('$');
                                              var address = info[0];
                                              int pubPort = int.Parse(info[1]), 
                                                  intPort= int.Parse(info[2]);
                                              var pubAddress = string.Format(addressMask, address, pubPort);
                                              var intAddress = string.Format(addressMask, address, intPort);
                                              byte id = byte.Parse(info[3]);
                                              bool master = info.Length == 5 && info[4].ToLower() == "master";
                                              
                                              return new NodeConfiguration
                                              {
                                                  InternalAddress = intAddress,
                                                  PublicAddress = pubAddress,
                                                  IsMaster = master,
                                                  IsSynced = true,
                                                  IsReadonly = false,
                                                  ServerId = id
                                              };
                                          }))}
                    };
            p.Parse(args);

            // liaise nodes collection and current node
            nodes.Add(new NodeConfiguration()
                  {
                      InternalAddress = string.Format(addressMask, localAddress, internalPort),
                      PublicAddress = string.Format(addressMask, localAddress, publicPort),
                      IsMaster = isMaster,
                      IsSynced = isSynced,
                      IsReadonly = false,
                      ServerId = configuration.ServerId
                  });
            var allNodes = new NodeCollection(nodes);

            EnsureNodesConfigurationIsValid(allNodes);

            configuration.Nodes = allNodes;

            return configuration;
        }

        private static void EnsureNodesConfigurationIsValid(INodeCollection nodes)
        {
            int mastersCount = nodes.All.Count(n => n.IsMaster);
            if (mastersCount != 1)
                throw new BadConfigurationException("There must be exactly 1 master node");

            byte maxServerId = nodes.All.Max(n => n.ServerId);
            byte[] idCounter = new byte[maxServerId + 1];
            if (nodes.All.Any(node => ++idCounter[node.ServerId] > 1))
                throw new BadConfigurationException("Every node must have unique Id");
        }

        private static void RunHosts()
        {
            var publicHost = CreateHost<PublicQueueService>(Core.Server.Configuration.PublicAddress);
            var internalHost = CreateHost<InternalQueueService>(Core.Server.Configuration.InternalAddress);
            
            internalHost.Open();
            publicHost.Open();

            Core.Server.Log.Info("I am {0}", Core.Server.Configuration.IsMaster ? "master" : "slave");
            Core.Server.Log.Info("Listening:");

            //Core.Server.Log.Info("\tpublic:");
            //foreach (var uri in publicHost.BaseAddresses)
            //    Core.Server.Log.Info("\t\t{0}", uri);
            
            //Core.Server.Log.Info("\tinternal:");
            //foreach (var uri in internalHost.BaseAddresses)
            //    Core.Server.Log.Info("\t\t{0}", uri);

            DoPostInitializations();

            Core.Server.Log.Info("Press <ENTER> to terminate Host");
            Console.ReadLine();
            Core.Server.Resolve<IConnectionManager>().CloseOutcomingConnections();
            //
            publicHost.Close();
            internalHost.Close();
        }

        private static void DoPostInitializations()
        {
            //todo MM: find a better place for such inits
            Core.Server.Resolve<IMasterElections>().Init();
            Core.Server.Resolve<INodeSynchronizer>().Init();
        }

        private static ICommunicationObject CreateHost<T>(string address)
        {
            var host = CommunicationProvider.Instance.CreateHost<T>(address);
            return host;
        }

        private static void CreateProxies()
        {
            var siblings = Core.Server.Configuration.Nodes.Siblings;

            if (siblings.Count == 0)
            {
                Core.Server.Log.Info("No other servers found in cluster");
                return;
            }
                

            Core.Server.Log.Info("Nodes:");
            foreach (var node in siblings)
            {
                node.CreateProxy();
                Core.Server.Log.Info("\t{0}, {1}, {2}", 
                    node.InternalAddress,
                    node.IsMaster ? "master" : "slave");
            }
        }

        private static void ShowUsage()
        {
            // todo : use NDesk.Options.WriteOptionDescriptions method here
            Console.WriteLine("Usage: {0} <arg1> .. <argN>", AppDomain.CurrentDomain.FriendlyName);
            Console.WriteLine("Arguments:");
            Console.WriteLine("\tp|port=(int16) - port to run server");
            Console.WriteLine("\tm|master - mark server as master");
        }
    }
}
