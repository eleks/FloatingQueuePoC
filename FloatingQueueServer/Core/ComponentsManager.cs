using Autofac;
using FloatingQueue.Server.EventsLogic;

namespace FloatingQueue.Server.Core
{
    public class ComponentsManager
    {
        public IContainer GetContainer(IConfiguration configuration)
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder.Register(b => Logger.Instance).As<ILogger>();
            containerBuilder.RegisterType<ConnectionManager>().As<IConnectionManager>();
            containerBuilder.RegisterType<EventAggregate>().As<IEventAggregate>().InstancePerDependency();
            containerBuilder.RegisterInstance(configuration).As<IConfiguration>();

            var container = containerBuilder.Build();

            return container;
        }
    }
}
