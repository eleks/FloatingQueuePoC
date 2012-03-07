using System;
using System.Linq;
using System.ServiceModel;
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
            //Core.Server.ConnectToSiblings();

            Core.Server.Log.Info("Nodes:");
            foreach (var node in configuration.Nodes)
            {
                Core.Server.Log.Info(node.Address);
            }
        }

        private static Configuration ParseConfiguration(string[] args)
        {
            var configuration = new Configuration { Port = 80, Priority = 0};
            int port;
            byte priority;
            var p = new OptionSet()
                    {
                        {"p|port=", v => configuration.Port = int.TryParse(v, out port) ? port : 80},
                        {"m|master", v => configuration.IsMaster = !string.IsNullOrEmpty(v) },
                        {"n|nodes=", v => configuration.Nodes = v.Split(';').Select(
                                          node => 
                                          { 
                                              var info = node.Split('$');
                                              return new NodeInfo
                                              {
                                                  Address = info[0],
                                                  IsMaster = info[1].ToLower() == "master",
                                                  Priority = (byte.TryParse(info[1], out priority) ? priority : (byte)0)
                                              };
                                          }).OfType<INodeInfo>().ToList()},
                        {"pr|priority=",v => configuration.Priority = (byte.TryParse(v, out priority) ? priority : (byte)0)}
                    };
            p.Parse(args);

            

            // validate args
            int mastersCount = configuration.Nodes.Count(n => n.IsMaster) + (configuration.IsMaster ? 1 : 0);
            if (mastersCount != 1)
                throw new BadConfigurationException("There must be exactly 1 master node");
            // todo: ensure that every node has it's own unique priority

            return configuration;
        }

        private static void RunHost()
        {
            var serviceType = typeof(QueueService);
            var serviceUri = new Uri(string.Format("net.tcp://localhost:{0}/", Core.Server.Configuration.Port));

            var host = new ServiceHost(serviceType, serviceUri);

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
