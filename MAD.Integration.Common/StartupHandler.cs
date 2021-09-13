using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MAD.Integration.Common
{
    internal class StartupHandler
    {
        public object Startup { get; private set; }

        public void SetStartup<T>() where T : class, new()
        {
            this.Startup = new T();
        }

        public void ConfigureServices(IIntegrationHostBuilder integrationHostBuilder)
        {
            if (this.Startup is null)
                return;

            var configureServices = this.Startup.GetType().GetMethod("ConfigureServices", new[] { typeof(IServiceCollection) });

            if (configureServices != null)
            {
                integrationHostBuilder.ConfigureServices(sc =>
                {
                    configureServices.Invoke(this.Startup, new[] { sc });
                });
            }
        }

        public void Configure(IServiceProvider serviceProvider)
        {
            if (this.Startup is null)
                return;

            var configureMethod = this.Startup.GetType().GetMethod("Configure");

            if (configureMethod != null)
            {
                using var startupScope = serviceProvider.CreateScope();
                var paramsToInject = configureMethod.GetParameters()
                    .Select(y => startupScope.ServiceProvider.GetRequiredService(y.ParameterType));

                object invokeResult = configureMethod.Invoke(this.Startup, paramsToInject.ToArray());

                if (invokeResult is Task t)
                    t.Wait();
            }
        }
        
    }
}
