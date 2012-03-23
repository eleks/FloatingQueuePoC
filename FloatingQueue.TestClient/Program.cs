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
            InitializeCommunicationProvider(useTCP: true);
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
                        case "get":
                            DoGet(ms_Proxy, atoms.Skip(1).ToArray());
                            Logger.Instance.Info("Done. Completed in {0} ms", (DateTime.Now - start).TotalMilliseconds);
                            break;
                        case "flood":
                            int threads = int.Parse(atoms[1]);
                            int requests = int.Parse(atoms[2]);
                            int maxValue = int.MaxValue;
                            if (atoms.Length == 4)
                                maxValue = int.Parse(atoms[3]);
                            var tasks = new List<Task>();
                            for (int i = 0; i < threads; i++)
                            {
                                tasks.Add(new Task(() => DoFlood(requests, maxValue)));
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

        private static void DoFlood(int requests, int maxValue)
        {
            SafeQueueServiceProxy proxy;
            if (!TryCreateProxy(MasterAddress, true, out proxy))
                return;
            using (proxy)
            {
                int failed = 0; // sample counter - just to stop if many errors occured
                for (int i = 0; i < requests && failed < 3; i++)
                {
                    try
                    {
                        proxy.Push(ms_Rand.Next(maxValue).ToString(), -1, ms_Rand.Next().ToString());
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error("Unhandled Exception", ex);
                        failed++;
                    }
                }
            }
        }

        static void DoPush(IQueueService proxy, string[] args)
        {
            try
            {
                proxy.Push(args[0], int.Parse(args[1]), args[2]);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("Unhandled Exception", ex);
                ShowUsage();
            }
        }

        static void DoGet(IQueueService proxy, string[] args)
        {
            try
            {
                bool getAll = false;
                if (args[0] == "all")
                {
                    getAll = true;
                    args = args.Skip(1).ToArray();
                }

                var aggregateId = args[0];
                int version;
                if (args.Length > 1)
                    version = int.Parse(args[1]);
                else
                {
                    if (getAll)
                        version = -1;
                    else
                        throw new ArgumentException("Version must be specified if exact version is requested");
                }

                if (getAll)
                {
                    var results = proxy.GetAllNext(aggregateId, version);
                    if (results.Count() == 0)
                    {
                        Logger.Instance.Warn("There's no objects in aggregate '{0}' with version > {1}", 
                            aggregateId, version);
                    }
                    else
                    {
                        foreach (var result in results)
                            Logger.Instance.Info(result.ToString());
                    }
                }
                else
                {
                    object obj;
                    if (proxy.TryGetNext(aggregateId,version, out obj))
                    {
                        Logger.Instance.Info(obj.ToString());
                    }
                    else
                    {
                        Logger.Instance.Warn("Theres' no object in aggregate '{0}' with version {1}",aggregateId, version);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("Unhandled Exception", ex);
                ShowUsage();
            }

        }

        public static bool TryCreateProxy(string address, bool keepConnectionOpened, out SafeQueueServiceProxy proxy)
        {
            try
            {
                proxy = new SafeQueueServiceProxy(address, keepConnectionOpened);
                return true;
            }
            catch (Exception e)
            {
                Logger.Instance.Error("Cannot establish connection with server at {0}. Error {1}:{2}", MasterAddress, e.GetType().Name, e.Message);
                proxy = null;
                return false;
            }
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
            Logger.Instance.Info("\tflood <threads> <requests> <maxAggregateId=int.max>");
            Logger.Instance.Info("\tget <all=false> <aggregateId> <version>");
            Logger.Instance.Info("\texit");
        }
    }
}
