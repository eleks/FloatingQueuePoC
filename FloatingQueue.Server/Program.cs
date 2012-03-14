using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using FloatingQueue.Common;
using FloatingQueue.Common.Proxy;
using FloatingQueue.Server.Core;
using FloatingQueue.Server.Exceptions;
using FloatingQueue.Server.Replication;
using FloatingQueue.Server.Service;
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
            Initialize(args);
            RunHost();
        }

        private static void Initialize(string[] args)
        {
            var configuration = ParseConfiguration(args);

            var componentsManager = new ComponentsManager();
            var container = componentsManager.GetContainer(configuration);
            Core.Server.Init(container);
            Replication.ReplicationCore.Init();

            Core.Server.Log.Info("Nodes:");
            foreach (var node in configuration.Nodes.SyncedSiblings)
            {
                Core.Server.Log.Info(node.Address);
            }

        }

        private static ServerConfiguration ParseConfiguration(string[] args)
        {
            var configuration = new ServerConfiguration { ServerId = 0 };
            var nodes = new List<INodeConfiguration>();
            int port = 80;
            bool isMaster = false, isSynced = true;
            byte serverId;

            var p = new OptionSet()
                    {
                        {"p|port=", v => int.TryParse(v, out port)},
                        {"m|master", v => isMaster = !string.IsNullOrEmpty(v) },
                        {"s|sync", v => isSynced = string.IsNullOrEmpty(v) },
                        {"id=",v => configuration.ServerId = (byte.TryParse(v, out serverId) ? serverId : (byte)0)},
                        {"n|nodes=", v => nodes.AddRange(v.Split(';').Select(
                                          node => 
                                          { 
                                              var info = node.Split('$');
                                              return new NodeConfiguration
                                              {
                                                  Address = info[0],
                                                  Proxy = new ManualQueueServiceProxy(info[0]),
                                                  IsMaster = info[1].ToLower() == "master",
                                                  IsSynced = true,
                                                  IsReadonly = false,
                                                  ServerId = (byte.TryParse(info[1], out serverId) ? serverId : (byte)0)
                                              };
                                          }).OfType<INodeConfiguration>())}
                    };
            p.Parse(args);

            // liaise nodes collection and current node
            nodes.Add(new NodeConfiguration()
                  {
                      Address = string.Format("net.tcp://localhost:{0}", port),
                      Proxy = null, // we don't want a circular reference
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

        private static void EnsureNodesConfigurationIsValid(NodeCollection nodes)
        {
            int mastersCount = nodes.All.Count(n => n.IsMaster);
            if (mastersCount != 1)
                throw new BadConfigurationException("There must be exactly 1 master node");

            byte maxServerId = nodes.All.Max(n => n.ServerId);
            byte[] idCounter = new byte[maxServerId + 1];
            if (nodes.All.Any(node => ++idCounter[node.ServerId] > 1))
                throw new BadConfigurationException("Every node must have unique Id");
        }

        private static void RunHost()
        {
            ServiceHost host = CreateHost();
            host.Open();

            Core.Server.Log.Info("I am {0}", Core.Server.Configuration.IsMaster ? "master" : "slave");
            Core.Server.Log.Info("Listening:");
            foreach (var uri in host.BaseAddresses)
            {
                Core.Server.Log.Info("\t{0}", uri);
            }

            Core.Server.Log.Info("Press <ENTER> to terminate Host");
            Console.ReadLine();
            Core.Server.Resolve<IConnectionManager>().CloseOutcomingConnections();
        }

        private static ServiceHost CreateHost()
        {
            var serviceType = typeof(QueueService);
            var serviceUri = new Uri(Core.Server.Configuration.Address);

            var host = new ServiceHost(serviceType, serviceUri);

            return host;
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
