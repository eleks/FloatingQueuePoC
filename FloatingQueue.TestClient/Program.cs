using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using FloatingQueue.Common;
using FloatingQueue.Common.Common;
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


        static void Main(string[] args)
        {
            InitializeCommunicationProvider(useTCP: false);
            //
            Logger.Instance.Info("Test Client");

            if (!TryCreateProxy(MasterAddress, false, out ms_Proxy))
            {
                Console.ReadLine();
                return;
            }

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
                            Logger.Instance.Info("Done. Completed in {0} ms", (DateTime.Now - start).TotalMilliseconds);
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
                            Logger.Instance.Info("Done. Completed in {0} ms", (DateTime.Now - start).TotalMilliseconds);
                            break;
                        case "exit":
                            work = false;
                            break;
                        default:
                            Logger.Instance.Warn("Unknown command");
                            ShowUsage();
                            break;
                    }
                }
            }
        }

        private static void DoFlood(int requests)
        {
            SafeQueueServiceProxy proxy;
            if (!TryCreateProxy(MasterAddress, true, out proxy))
                return;
            using (proxy)
            {
                bool stop = false;
                proxy.ConnectionLost += (sender, e) => stop = true;
                for (int i = 0; i < requests && !stop; i++)
                {
                    proxy.Push(ms_Rand.Next().ToString(), -1, ms_Rand.Next().ToString());
                }
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
                Logger.Instance.Error("Unhandled Exception",ex);
                ShowUsage();
            }
        }

        public static bool TryCreateProxy(string address, bool keepConnectionOpened, out SafeQueueServiceProxy proxy)
        {
            proxy = new SafeQueueServiceProxy(address);
            //proxy.ClientCallFailed += () => Logger.Instance.Warn("Call to {0} failed...", address);//TODO: to be removed during refactoring
            //proxy.ConnectionLost += () => Logger.Instance.Error("Error! Connection to cluster is completely lost");
            proxy.MasterChanged += (sender, e) =>
                                          {
                                              Logger.Instance.Info("New master set on {0}", e.NewMasterAdress);
                                              MasterAddress = e.NewMasterAdress;
                                          };
            if (!proxy.Init(keepConnectionOpened))
            {
                Logger.Instance.Error("Cannot establish connection with server at {0}", MasterAddress);
                return false;
            }
            return true;
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
            Logger.Instance.Info("Usage: <command> <arg1> .. <argN>");
            Logger.Instance.Info("Commands:");
            Logger.Instance.Info("\tpush <aggregateId> <version> <data>");
            Logger.Instance.Info("\tflood <threads> <requests>");
            Logger.Instance.Info("\texit");
        }
    }
}
