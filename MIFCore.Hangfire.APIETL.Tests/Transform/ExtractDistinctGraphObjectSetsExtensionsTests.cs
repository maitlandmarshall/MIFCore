using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Dynamic;

namespace MIFCore.Hangfire.APIETL.Transform.Tests
{
    [TestClass()]
    public class ExtractDistinctGraphObjectSetsExtensionsTests
    {
        [TestMethod()]
        public void ExtractDistinctGraphObjectSetsTest()
        {
            var rootGraph = new
            {
                Id = "abc123",
                Items = new[]
                {
                    new { Id = "def456", Plums = true, Children = new[] { new { Value = "abc123" } } },
                    new { Id = "def456", Plums = true, Children = new[] { new { Value = "def132" } } },
                    new { Id = "def456", Plums = true, Children = new[] { new { Value = "ggjg35" } } },
                }
            };

            var rootGraphDynamic = JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(rootGraph), new ExpandoObjectConverter());
            var sets = rootGraphDynamic.ExtractDistinctGraphObjectSets().ToList();
        }
    }
}