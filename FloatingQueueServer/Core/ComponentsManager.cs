using Autofac;

namespace FloatingQueue.Server.Core
{
    public class ComponentsManager
    {
        public IContainer GetContainer(IConfiguration configuration)
        {
            var containerBuilder = new ContainerBuilder();

            RegisterCoreServices(containerBuilder);
            containerBuilder.RegisterInstance(configuration).As<IConfiguration>();

            var container = containerBuilder.Build();

            return container;
        }

        private void RegisterCoreServices(ContainerBuilder containerBuilder)
        {
            containerBuilder.Register(b => Logger.Instance).As<ILogger>();
        }
    }
}
