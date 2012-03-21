using Autofac;
using FloatingQueue.Common.Common;
using NUnit.Framework;
using ServerClass = FloatingQueue.Server.Core.Server;

namespace FloatingQueue.Tests.Server
{
    public abstract class TestBase
    {
        [SetUp]
        public virtual void Setup()
        {
            var containerBuilder = new ContainerBuilder();

            RegisterMocks(containerBuilder);

            var container = containerBuilder.Build();

            ServerClass.Init(container);
        }

        protected virtual void RegisterMocks(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterInstance(new TestLogger()).As<ILogger>();
        }
    }
}
