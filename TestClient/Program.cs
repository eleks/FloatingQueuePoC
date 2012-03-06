using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new QueueServiceClient();
            try
            {
                bool work = true;
                while (work)
                {
                    var str = Console.ReadLine();
                    var atoms = str.Split(new[] { ' ' }, 4, StringSplitOptions.RemoveEmptyEntries);
                    if(atoms.Length > 0)
                    {
                        var cmd = atoms[0];
                        switch (cmd.ToLower())
                        {
                            case "push":
                                DoPush(client, atoms.Skip(1).ToArray());
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
            finally
            {
                if (client.State == CommunicationState.Faulted)
                    client.Abort();
                client.Close();
            }
        }

        static void DoPush(QueueServiceClient client, string[] args)
        {
            try
            {
                client.Push(args[0], int.Parse(args[1]), args[2]);
            }
            catch(Exception ex)
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
