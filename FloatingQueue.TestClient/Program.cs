﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
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
        

        static void Main(string[] args)
        {
            InitializeCommunicationProvider(useTCP: false);
            //
            Console.Out.WriteLine("Test Client");


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
            try
            {
                SafeQueueServiceProxy proxy;
                if (!TryCreateProxy(MasterAddress, true, out proxy))
                    return;
                using (proxy)
                {
                    bool stop = false;
                    proxy.OnConnectionLost += () => stop = true;
                    for (int i = 0; i < requests && !stop; i++)
                    {
                        proxy.Push(ms_Rand.Next().ToString(), -1, ms_Rand.Next().ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
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

        public static bool TryCreateProxy(string address, bool keepConnectionOpened, out SafeQueueServiceProxy proxy)
        {
            proxy = new SafeQueueServiceProxy(address);
            proxy.OnClientCallFailed += () => Console.WriteLine("Call to {0} failed...", address);
            proxy.OnConnectionLost += () => Console.WriteLine("Error! Connection to cluster is completely lost");
            proxy.OnMasterChanged += newMasterAddress =>
                                          {
                                              Console.WriteLine("New master set on {0}", newMasterAddress);
                                              MasterAddress = newMasterAddress;
                                          };
            if (!proxy.Init(keepConnectionOpened))
            {
                Console.WriteLine("Cannot establish connection with server at {0}", MasterAddress);
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
            Console.WriteLine("Usage: <command> <arg1> .. <argN>");
            Console.WriteLine("Commands:");
            Console.WriteLine("\tpush <aggregateId> <version> <data>");
            Console.WriteLine("\tflood <threads> <requests>");
            Console.WriteLine("\texit");
        }
    }
}
