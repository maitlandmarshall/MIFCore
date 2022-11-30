using System.Collections.Generic;
using System.Linq;

namespace MIFCore.Hangfire.APIETL.Transform
{
    public static class ExtractDistinctGraphObjectSetsExtensions
    {
        public static IEnumerable<GraphObjectSet> ExtractDistinctGraphObjectSets(this IEnumerable<IDictionary<string, object>> root)
        {
            var rootObjectSet = new GraphObjectSet
            {
                Objects = root
            };

            yield return rootObjectSet;

            foreach (var rootItem in root)
            {
                var nestedObjectSets = rootItem.ExtractDistinctGraphObjectSets(rootObjectSet);

                foreach (var nos in nestedObjectSets)
                {
                    if (nos.Parent is null)
                        continue;

                    yield return nos;
                }
            }
        }

        public static IEnumerable<GraphObjectSet> ExtractDistinctGraphObjectSets(this IDictionary<string, object> rootItem, GraphObjectSet rootObjectSet = null)
        {
            // If no root object set was passed in (passed in from the IEnumerable version of ExtractDistinctGraphObjectSets)
            // create a new GraphObjectSet to represent this single entity (root entity of the graph)
            if (rootObjectSet is null)
            {
                rootObjectSet = new GraphObjectSet
                {
                    Objects = new[] { rootItem }
                };

                yield return rootObjectSet;
            }

            // loop through each key to find enumerable values
            foreach (var (rootKey, rootValue) in rootItem)
            {
                IEnumerable<GraphObjectSet> nestedObjectSets;

                if (rootValue is IEnumerable<object> childObjects)
                {
                    var childDicts = childObjects.Cast<IDictionary<string, object>>();

                    // Get the children of the children
                    nestedObjectSets = childDicts.ExtractDistinctGraphObjectSets();
                }
                else if (rootValue is IDictionary<string, object> childDict)
                {
                    nestedObjectSets = childDict.ExtractDistinctGraphObjectSets();
                }
                else
                {
                    continue;
                }

                // Link the children of the children via keys
                foreach (var n in nestedObjectSets)
                {
                    if (n.Parent is null)
                    {
                        n.Parent = rootItem;
                        n.ParentSet = rootObjectSet;
                        n.ParentKey = rootKey;
                    }
                    else
                    {
                        n.ParentKey = $"{rootKey}.{n.ParentKey}";
                    }

                    yield return n;
                }
            }
        }
    }
}
