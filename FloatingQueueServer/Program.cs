using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;

namespace FloatingQueueServer
{
    class Program
    {
        static void Main(string[] args)
        {
            RunHost();
        }

        private static void RunHost()
        {
            var serviceType = typeof (QueueService);
            var serviceUri = new Uri("http://localhost:8080/");

            var host = new ServiceHost(serviceType, serviceUri);

            host.Open();

            Console.WriteLine("Listening:");
            foreach (var uri in host.BaseAddresses)
            {
                Console.WriteLine("\t{0}", uri);
            }

            Console.WriteLine("Press <ENTER> to terminate Host");
            Console.ReadLine();
        }
    }
}
