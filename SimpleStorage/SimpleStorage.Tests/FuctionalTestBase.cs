using System;
using Domain;
using Microsoft.Owin.Hosting;
using NUnit.Framework;
using SimpleStorage.Infrastructure;
using SimpleStorage.IoC;
using StructureMap;

namespace SimpleStorage.Tests
{
    [TestFixture]
    public abstract class FuctionalTestBase
    {
        private IoCFactory iocFactory;
        protected Container container;

        [TestFixtureSetUp]
        public virtual void TestFixtureSetUp()
        {
            iocFactory = new IoCFactory();
            container = iocFactory.GetContainer();
        }

        [SetUp]
        public virtual void SetUp()
        {
            container.Configure(c => c.For<IStateRepository>().Use(new StateRepository()));

            var operationLog = new OperationLog();
            container.Configure(c => c.For<IOperationLog>().Use(operationLog));

            var storage = new Storage(operationLog, new ValueComparer());
            container.Configure(c => c.For<IStorage>().Use(storage));
        }

        protected IDisposable GetStartedWebApp(int port)
        {
            var startOptions = new StartOptions(string.Format("http://+:{0}/", port));
            return WebApp.Start(startOptions, appBuilder => new Startup(iocFactory).Configuration(appBuilder));
        }
    }
}