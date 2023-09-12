using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Dynamic;

namespace MIFCore.Hangfire.APIETL.Transform.Tests
{
    [TestClass()]
    public class FlattenGraphExtensionsTests
    {
        [TestMethod()]
        public void FlattenGraphTest()
        {
            dynamic expando = new ExpandoObject();
            expando.Id = "abc123";
            expando.Description = "does stuff";

            dynamic nestedExpando = new ExpandoObject();
            nestedExpando.Id = "def456";
            nestedExpando.Plums = true;

            dynamic moreNested = new ExpandoObject();
            moreNested.Id = Guid.Empty;
            moreNested.Date = DateTime.MaxValue;
            moreNested.Items = new[] { "one", "two", "three" };

            nestedExpando.Action = moreNested;
            expando.Nested = nestedExpando;

            (expando as ExpandoObject).FlattenGraph();

            Assert.AreEqual("def456", expando.Nested_Id);
            Assert.AreEqual(true, expando.Nested_Plums);

            Assert.AreEqual(Guid.Empty, expando.Nested_Action_Id);
            Assert.AreEqual(DateTime.MaxValue, expando.Nested_Action_Date);
            Assert.AreEqual(3, expando.Nested_Action_Items.Length);
        }
    }
}