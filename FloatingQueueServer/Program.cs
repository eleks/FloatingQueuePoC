using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using FloatingQueueServer.Core;
using NDesk.Options;

namespace FloatingQueueServer
{
    class Program
    {
        // example calling
        // -p=8080 -m -n=http://localhost:8081;http://localhost:8082
        // -p=8081 -n=http://localhost:8080$master;http://localhost:8082

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
            Server.Init(container);
        }

        private static Configuration ParseConfiguration(string[] args)
        {
            var configuration = new Configuration {Port = 80};
            int port;
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
                                                  IsMaster = ( info.Length == 2 ) && ( info[1].ToLower() == "master" )
                                              };
                                          }).OfType<INodeInfo>().ToList()}
                    };
            p.Parse(args);

            // validate args
            int mastersCount = configuration.Nodes.Where(n => n.IsMaster).Count() + (configuration.IsMaster ? 1 : 0);
            if (mastersCount != 1)
                throw new BadConfigurationException("There must be exactly 1 master node");

            return configuration;
        }

        private static void RunHost()
        {
            var serviceType = typeof (QueueService);
            var serviceUri = new Uri(string.Format("http://localhost:{0}/", Server.Configuration.Port));

            var host = new ServiceHost(serviceType, serviceUri);

            host.Open();

            Server.Log.Info("I am {0}", Server.Configuration.IsMaster ? "master" : "slave");
            Server.Log.Info("Listening:");
            foreach (var uri in host.BaseAddresses)
            {
                Server.Log.Info("\t{0}", uri);
            }

            Server.Log.Info("Press <ENTER> to terminate Host");
            Console.ReadLine();
        }

        private static void ShowUsage()
        {
            Console.WriteLine("Usage: {0} <arg1> .. <argN>", AppDomain.CurrentDomain.FriendlyName);
            Console.WriteLine("Arguments:");
            Console.WriteLine("\tp|port=(int16) - port to run server");
            Console.WriteLine("\tm|master - mark server as master");
        }
    }
}
