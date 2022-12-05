using System;

namespace MIFCore.Hangfire.APIETL
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ApiEndpointSelectorAttribute : Attribute
    {
        public ApiEndpointSelectorAttribute(string regex)
        {
            this.Regex = regex;
        }

        public ApiEndpointSelectorAttribute(string regex, string inputPath)
        {
            this.Regex = regex;
            this.InputPath = inputPath;
        }

        public string Regex { get; }
        public string InputPath { get; }
    }
}
