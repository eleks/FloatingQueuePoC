using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using FloatingQueue.Server.Core;
using FloatingQueue.Server.Exceptions;
using FloatingQueue.Server.Service;
using FloatingQueue.ServiceProxy;
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
            Replication.Init();

            Core.Server.Log.Info("Nodes:");
            foreach (var node in configuration.Nodes.Siblings)
            {
                Core.Server.Log.Info(node.Address);
            }

        }

        private static Configuration ParseConfiguration(string[] args)
        {
            var configuration = new Configuration { ServerId = 0, Nodes = new NodeCollection() };
            int port = 80;
            bool isMaster = false;
            byte serverId;

            var p = new OptionSet()
                    {
                        {"p|port=", v => int.TryParse(v, out port)},
                        {"m|master", v => isMaster = !string.IsNullOrEmpty(v)},
                        {"pr|priority|id=",v => configuration.ServerId = (byte.TryParse(v, out serverId) ? serverId : (byte)0)},
                        {"n|nodes=", v => configuration.Nodes.AddRange(v.Split(';').Select(
                                          node => 
                                          { 
                                              var info = node.Split('$');
                                              return new NodeConfiguration
                                              {
                                                  Address = info[0],
                                                  IsMaster = info[1].ToLower() == "master",
                                                  ServerId = (byte.TryParse(info[1], out serverId) ? serverId : (byte)0)
                                              };
                                          }).OfType<INodeConfiguration>().ToList())}
                    };
            p.Parse(args);
            // liaise nodes collection and current node
            configuration.Nodes.Add(new NodeConfiguration()
                                        {
                                            Address = string.Format("net.tcp://localhost:{0}", port),
                                            IsMaster = isMaster,
                                            ServerId = configuration.ServerId
                                        });

            // validate args
            int mastersCount = configuration.Nodes.Count(n => n.IsMaster);
            if (mastersCount != 1)
                throw new BadConfigurationException("There must be exactly 1 master node");
            // todo: ensure that every node has it's own unique id

            return configuration;
        }

        private static void RunHost()
        {
            ServiceHost host = Core.Server.Configuration.IsMaster ?
                CreateHostWithMex() :
                CreateHostWithoutMex();
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

        private static ServiceHost CreateHostWithoutMex()
        {
            var serviceType = typeof(QueueService);
            var serviceUri = new Uri(Core.Server.Configuration.Address);

            var host = new ServiceHost(serviceType, serviceUri);

            return host;
        }

        // for updating service reference, can be turned off when finished
        private static ServiceHost CreateHostWithMex()
        {
            var serviceType = typeof(QueueService);
            var serviceUri = new Uri(Core.Server.Configuration.Address);

            var mexUri = new Uri("http://localhost:11011/");

            var host = new ServiceHost(serviceType, serviceUri, mexUri);

            host.AddServiceEndpoint(typeof(IQueueService), new WSHttpBinding(), "");
            host.AddServiceEndpoint(typeof(IQueueService), new NetTcpBinding(), "");

            var metadataBehavior = new ServiceMetadataBehavior { HttpGetEnabled = true };
            host.Description.Behaviors.Add(metadataBehavior);

            Binding mexBinding = MetadataExchangeBindings.CreateMexTcpBinding();
            host.AddServiceEndpoint(
                typeof(IMetadataExchange),
                mexBinding,
                serviceUri + "/mex");

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
