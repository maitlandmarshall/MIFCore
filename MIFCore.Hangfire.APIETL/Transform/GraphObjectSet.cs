using System.Collections.Generic;

namespace MIFCore.Hangfire.APIETL.Transform
{
    public class GraphObjectSet
    {
        public IDictionary<string, object> Parent { get; internal set; }
        public GraphObjectSet ParentSet { get; internal set; }
        public string ParentKey { get; internal set; }

        public IList<IDictionary<string, object>> Objects { get; internal set; }
    }
}
