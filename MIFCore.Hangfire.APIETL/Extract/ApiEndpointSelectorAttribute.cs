using System;

namespace MIFCore.Hangfire.APIETL.Extract
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ApiEndpointSelectorAttribute : Attribute
    {
        public ApiEndpointSelectorAttribute(string regex)
        {
            this.Regex = regex;
        }

        public string Regex { get; }
    }
}
