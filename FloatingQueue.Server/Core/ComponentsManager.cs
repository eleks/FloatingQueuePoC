using Autofac;
using FloatingQueue.Server.EventsLogic;
using FloatingQueue.Server.Replication;

namespace FloatingQueue.Server.Core
{
    public class ComponentsManager
    {
        public IContainer GetContainer(IServerConfiguration configuration)
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder.Register(b => Logger.Instance).As<ILogger>();
            containerBuilder.RegisterType<ConnectionManager>().As<IConnectionManager>().SingleInstance();
            containerBuilder.RegisterType<EventAggregate>().As<IEventAggregate>().InstancePerDependency();
            containerBuilder.RegisterInstance(configuration).As<IServerConfiguration>();

            var container = containerBuilder.Build();

            return container;
        }
    }
}
