using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FloatingQueue.Common.Proxy;

namespace FloatingQueue.TestClient
{
    class Program
    {
        private static readonly Random ms_Rand = new Random();
        private const string MasterAddress = "net.tcp://localhost:11081";


        static void Main(string[] args)
        {
            var proxy = new AutoQueueProxy(MasterAddress);
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
                            DoPush(proxy, atoms.Skip(1).ToArray());
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
            var proxy = new AutoQueueProxy(MasterAddress);
            for (int i = 0; i < requests; i++)
            {
                proxy.Push(ms_Rand.Next().ToString(), -1, ms_Rand.Next().ToString());
            }
        }

        static void DoPush(QueueServiceProxy proxy, string[] args)
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
