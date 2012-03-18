using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using FloatingQueue.Common;
using FloatingQueue.Common.Proxy.QueueServiceProxy;
using FloatingQueue.Common.TCPProvider;
using FloatingQueue.Common.WCF;

namespace FloatingQueue.TestClient
{
    class Program
    {
        private static readonly Random ms_Rand = new Random();
        private static string MasterAddress = "net.tcp://localhost:10080";
        private static SafeQueueServiceProxy ms_Proxy;
        private static List<Node> ms_Nodes;

        static void Main(string[] args)
        {
            InitializeCommunicationProvider(useTCP: true);
            //
            Console.Out.WriteLine("Test Client");
            ms_Proxy = CreateProxy(MasterAddress);

            var metadata = ms_Proxy.GetClusterMetadata();
            if (metadata == null)
            {
                Console.WriteLine("Cannot establish connection with server at {0}", MasterAddress);
                Console.ReadLine();
                return;
            }

            ms_Nodes = metadata.Nodes;

            bool work = true;
            while (work)
            {
                var str = Console.ReadLine();
                var start = DateTime.Now;
                var atoms = str.Split(new[] { ' ' }, 4, StringSplitOptions.RemoveEmptyEntries);
                if (atoms.Length > 0)
                {
                    var cmd = atoms[0];
                    switch (cmd.ToLower())
                    {
                        case "push":
                            DoPush(ms_Proxy, atoms.Skip(1).ToArray());
                            Console.WriteLine("Done. Completed in {0} ms", (DateTime.Now - start).TotalMilliseconds);
                            break;
                        case "flood":
                            int threads = int.Parse(atoms[1]);
                            int requests = int.Parse(atoms[2]);
                            var tasks = new List<Task>();
                            for (int i = 0; i < threads; i++)
                            {
                                tasks.Add(new Task(() => DoFlood(requests)));
                            }
                            foreach (var task in tasks)
                            {
                                task.Start();
                            }
                            Task.WaitAll(tasks.ToArray());
                            Console.WriteLine("Done. Completed in {0} ms", (DateTime.Now - start).TotalMilliseconds);
                            break;
                        case "exit":
                            work = false;
                            break;
                        default:
                            Console.WriteLine("Unknown command");
                            ShowUsage();
                            break;
                    }
                }
            }
        }

        private static void DoFlood(int requests)
        {
            using (var proxy = CreateProxy(MasterAddress))
            for (int i = 0; i < requests; i++)
            {
                proxy.Push(ms_Rand.Next().ToString(), -1, ms_Rand.Next().ToString());
            }
        }

        static void DoPush(QueueServiceProxyBase proxy, string[] args)
        {
            try
            {
                proxy.Push(args[0], int.Parse(args[1]), args[2]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                ShowUsage();
            }
        }

        private static void HandleClientCallFailed()
        {
            Console.WriteLine("Connection lost with {0}. Trying to establish new connection...", MasterAddress);

            bool success = false;
            List<Node> newNodes = null;

            foreach (var node in ms_Nodes)
            {
                var proxy = new SafeQueueServiceProxy(node.Address);

                var metadata = proxy.GetClusterMetadata();
                if (metadata == null)
                    continue;

                var master = metadata.Nodes.SingleOrDefault(n => n.IsMaster);
                if (master == null)
                {
                    Console.WriteLine("Critical error: there's no master in cluster");
                    return;
                    // throw new ApplicationException("Critical Error! There's no master in cluster");
                }

                ms_Proxy.Dispose();


                ms_Proxy = new SafeQueueServiceProxy(master.Address);
                var testCall = ms_Proxy.GetClusterMetadata();
                if (testCall == null) continue;

                MasterAddress = master.Address;
                newNodes = metadata.Nodes;
                ms_Proxy.OnClientCallFailed += HandleClientCallFailed;

                success = true;
            }

            ms_Nodes = newNodes;

            if (success)
                Console.WriteLine("Found new master on {0}", MasterAddress);
            else
                Console.WriteLine("Connection is lost to all servers!!!");
        }

        public static SafeQueueServiceProxy CreateProxy(string address)
        {
            var proxy = new SafeQueueServiceProxy(address);
            proxy.OnClientCallFailed += HandleClientCallFailed;
            return proxy;
        }

        private static void InitializeCommunicationProvider(bool useTCP)
        {
            if (useTCP)
            {
                // TCP
                var provider = new TCPCommunicationProvider();
                provider.RegisterChannelImplementation<IQueueService>(() => new TCPQueueServiceProxy());
                CommunicationProvider.Init(provider);
            }
            else
            {
                // WCF
                var provider = new WCFCommunicationProvider();
                CommunicationProvider.Init(provider);
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine("Usage: <command> <arg1> .. <argN>");
            Console.WriteLine("Commands:");
            Console.WriteLine("\tpush <aggregateId> <version> <data>");
            Console.WriteLine("\tflood <threads> <requests>");
            Console.WriteLine("\texit");
        }
    }
}
