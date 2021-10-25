using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MAD.Integration.Common.EFCore.Tests
{
    internal class TestConfigFactory
    {
        public static TestConfig Create()
        {
            var testsettingsJson = File.ReadAllText("testsettings.json");
            return JsonConvert.DeserializeObject<TestConfig>(testsettingsJson);
        }
    }
}
