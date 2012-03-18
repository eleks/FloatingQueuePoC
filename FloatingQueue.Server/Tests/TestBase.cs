﻿using Autofac;
using FloatingQueue.Common.Common;
using FloatingQueue.Server.Core;
using NUnit.Framework;

namespace FloatingQueue.Server.Tests
{
    public abstract class TestBase
    {
        [SetUp]
        public virtual void Setup()
        {
            var containerBuilder = new ContainerBuilder();

            RegisterMocks(containerBuilder);

            var container = containerBuilder.Build();

            Core.Server.Init(container);
        }

        protected virtual void RegisterMocks(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterInstance(new TestLogger()).As<ILogger>();
        }
    }
}
