using Autofac;

namespace FloatingQueue.Server.Core
{
    public class Server
    {
        public static IContainer ServicesContainer { get; private set; }

        public static void Init(IContainer container)
        {
            ServicesContainer = container;
        }

        public static ILogger Log
        {
            get { return ServicesContainer.Resolve<ILogger>(); }
        }

        public static IConfiguration Configuration
        {
            get { return ServicesContainer.Resolve<IConfiguration>(); }
        }

        public static T Resolve<T>()
        {
            return ServicesContainer.Resolve<T>();
        }
    }
}
