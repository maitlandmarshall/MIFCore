using System;

namespace MIFCore.Hangfire.APIETL.Load
{
    public class SourcePropertyNameAttribute : Attribute
    {
        public SourcePropertyNameAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
    }
}
