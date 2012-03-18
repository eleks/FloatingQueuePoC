using System;
using Autofac;
using FloatingQueue.Common.Common;
using FloatingQueue.Server.EventsLogic;
using FloatingQueue.Server.Replication;

namespace FloatingQueue.Server.Core
{
    public class ComponentsManager
    {
        public IContainer GetContainer(IServerConfiguration configuration)
        {
            if(configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            var containerBuilder = new ContainerBuilder();

            containerBuilder.Register(b => Logger.Instance).As<ILogger>();
            containerBuilder.RegisterType<ConnectionManager>().As<IConnectionManager>().SingleInstance();
            containerBuilder.RegisterType<NodeSynchronizer>().As<INodeSynchronizer>().SingleInstance();
            containerBuilder.RegisterType<MasterElections>().As<IMasterElections>().SingleInstance();
            containerBuilder.RegisterType<AggregateRepository>().As<IAggregateRepository>().SingleInstance();
            containerBuilder.RegisterType<EventAggregate>().As<IEventAggregate>().InstancePerDependency();
            containerBuilder.RegisterInstance(configuration).As<IServerConfiguration>();

            var container = containerBuilder.Build();

            return container;
        }
    }
}
