using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MIFCore.Hangfire.APIETL.Tests
{
    [TestClass]
    public class ApiEndpointTests
    {
        [TestMethod]
        public void ApiEndpoint_ParseRouteParameters_ShouldExtractParameterNames()
        {
            var endpoint = new ApiEndpoint("some/api/{param1}/{param2}");

            Assert.AreEqual(2, endpoint.RouteParameters.Count());
            Assert.IsTrue(endpoint.RouteParameters.Contains("param1"));
            Assert.IsTrue(endpoint.RouteParameters.Contains("param2"));
        }
    }
}
