using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;

namespace FloatingQueueServer.Core
{
    public class ComponentsManager
    {
        public IContainer GetContainer()
        {
            var containerBuilder = new ContainerBuilder();

            RegisterCoreServices(containerBuilder);

            var container = containerBuilder.Build();

            return container;
        }

        private void RegisterCoreServices(ContainerBuilder containerBuilder)
        {
            containerBuilder.Register(b => Logger.Instance).As<ILogger>();
        }
    }
}
