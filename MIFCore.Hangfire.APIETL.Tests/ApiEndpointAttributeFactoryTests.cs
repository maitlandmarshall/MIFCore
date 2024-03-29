﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MIFCore.Hangfire.APIETL.Extract;
using MIFCore.Hangfire.APIETL.Load;

namespace MIFCore.Hangfire.APIETL.Tests
{
    [TestClass()]
    public class ApiEndpointAttributeFactoryTests
    {
        private static string[] Tenants = new[] { "tenant1", "tenant2" };

        [TestMethod()]
        public async Task Create_WithEndpointDefiner_CreatesEndpointsForEachTenant()
        {
            var serviceProvider = this.GetServiceProvider(typeof(Endpoint1), typeof(TenantedEndpointRegister));
            var factory = new ApiEndpointFactory(
                apiEndpointAttributes: serviceProvider.GetRequiredService<IEnumerable<ApiEndpointAttribute>>(),
                endpointDefiners: serviceProvider.GetRequiredService<IEnumerable<IDefineEndpoints>>(),
                apiEndpointModels: new List<ApiEndpointModel>()
            );

            // There should be four endpoints total, api/endpoint1, api/endpoint2 but duplicated for each "tenant"
            var endpoints = await factory.Create().ToListAsync();

            Assert.AreEqual(4, endpoints.Count);
            Assert.AreEqual(2, endpoints.Count(x => x.Name == "api/endpoint1"));
            Assert.AreEqual(2, endpoints.Count(x => x.Name == "api/endpoint2"));

            Assert.AreEqual(1, endpoints.Count(x => x.Name == "api/endpoint1" && x.AdditionalHeaders["tenantId"] == "tenant1"));
            Assert.AreEqual(1, endpoints.Count(x => x.Name == "api/endpoint2" && x.AdditionalHeaders["tenantId"] == "tenant1"));

            Assert.AreEqual(1, endpoints.Count(x => x.Name == "api/endpoint1" && x.AdditionalHeaders["tenantId"] == "tenant2"));
            Assert.AreEqual(1, endpoints.Count(x => x.Name == "api/endpoint2" && x.AdditionalHeaders["tenantId"] == "tenant2"));
        }

        private IServiceProvider GetServiceProvider(params Type[] endpoints)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddApiEndpointsToExtract(endpoints);
            return serviceCollection.BuildServiceProvider();
        }

        [ApiEndpoint("api/endpoint1")]
        class Endpoint1 : IPrepareRequest
        {
            public Task OnPrepareRequest(PrepareRequestArgs args)
            {
                throw new NotImplementedException();
            }
        }

        [ApiEndpoint("api/endpoint2")]
        [ApiEndpointSelector(".*")]
        class TenantedEndpointRegister : IDefineEndpoints
        {
            public async IAsyncEnumerable<ApiEndpoint> DefineEndpoints(string endpointName)
            {
                foreach (var t in Tenants)
                {
                    yield return new ApiEndpoint($"{endpointName} ({t})")
                    {
                        AdditionalHeaders =
                        {
                            { "tenantId", t }
                        }
                    };
                }
            }
        }
    }
}