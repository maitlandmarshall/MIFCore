using System.Collections.Generic;
using System.Linq;

namespace MIFCore.Hangfire.APIETL.Transform
{
    public static class ExtractDistinctGraphObjectSetsExtensions
    {
        public delegate void TransformObjectDelegate(TransformObjectArgs args);

        public static IEnumerable<GraphObjectSet> ExtractDistinctGraphObjectSets(this IEnumerable<IDictionary<string, object>> root, ExtractDistinctGraphObjectSetsArgs args = null)
        {
            var objects = new List<IDictionary<string, object>>();

            args ??= new ExtractDistinctGraphObjectSetsArgs();
            args.RootObjectSet ??= new GraphObjectSet
            {
                Objects = objects
            };

            yield return args.RootObjectSet;

            foreach (var rootItem in root)
            {
                objects.Add(rootItem);

                var nestedObjectSets = rootItem.ExtractDistinctGraphObjectSets(args);

                foreach (var nos in nestedObjectSets)
                {
                    yield return nos;
                }
            }
        }

        public static IEnumerable<GraphObjectSet> ExtractDistinctGraphObjectSets(this IDictionary<string, object> rootItem, ExtractDistinctGraphObjectSetsArgs args = null)
        {
            args ??= new ExtractDistinctGraphObjectSetsArgs();
            var rootObjectSet = args.RootObjectSet;

            // If no root object set was passed in (passed in from the IEnumerable version of ExtractDistinctGraphObjectSets)
            // create a new GraphObjectSet to represent this single entity (root entity of the graph)
            if (rootObjectSet is null)
            {
                rootObjectSet = new GraphObjectSet
                {
                    Objects = new[] { rootItem }
                };

                args.RootObjectSet = rootObjectSet;

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
                    nestedObjectSets = childDicts.ExtractDistinctGraphObjectSets(new ExtractDistinctGraphObjectSetsArgs
                    {
                        Transform = args.Transform
                    });
                }
                else if (rootValue is IDictionary<string, object> childDict)
                {
                    nestedObjectSets = childDict.ExtractDistinctGraphObjectSets(new ExtractDistinctGraphObjectSetsArgs
                    {
                        Transform = args.Transform
                    });
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

            args.Transform?.Invoke(new TransformObjectArgs
            {
                Object = rootItem,
                GraphObjectSet = rootObjectSet
            });
        }

        public class TransformObjectArgs
        {
            public IDictionary<string, object> Object { get; set; }
            public GraphObjectSet GraphObjectSet { get; set; }
        }

        public class ExtractDistinctGraphObjectSetsArgs
        {
            internal GraphObjectSet RootObjectSet { get; set; }
            public TransformObjectDelegate Transform { get; set; }
        }
    }
}
