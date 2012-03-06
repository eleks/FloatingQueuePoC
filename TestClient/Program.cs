using System;
using System.Linq;
using FloatingQueue.ServiceProxy;

namespace FloatingQueue.TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var proxy = new AutoQueueProxy();
            bool work = true;
            while (work)
            {
                var str = Console.ReadLine();
                var atoms = str.Split(new[] { ' ' }, 4, StringSplitOptions.RemoveEmptyEntries);
                if (atoms.Length > 0)
                {
                    var cmd = atoms[0];
                    switch (cmd.ToLower())
                    {
                        case "push":
                            DoPush(proxy, atoms.Skip(1).ToArray());
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
            Console.WriteLine("\texit");
        }
    }
}
