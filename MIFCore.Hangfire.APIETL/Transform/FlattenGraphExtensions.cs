﻿using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace MIFCore.Hangfire.APIETL.Transform
{
    public static class FlattenGraphExtensions
    {
        public static void FlattenGraph(this ExpandoObject expando)
        {
            (expando as IDictionary<string, object>).FlattenGraph();
        }

        public static void FlattenGraph(this IEnumerable<IDictionary<string, object>> rootArray)
        {
            foreach (var a in rootArray)
                a.FlattenGraph();
        }

        public static void FlattenGraph(this IDictionary<string, object> rootDict)
        {
            var keys = rootDict.Keys.ToList();

            // iterate and check for nested objects
            foreach (var rootKey in keys)
            {
                var rootValue = rootDict[rootKey];

                // Is the property an object with keys and values?
                if (rootValue is IDictionary<string, object> nestedDict)
                {
                    // Flatten the nestedDict so the entire graph flattens recursively
                    nestedDict.FlattenGraph();

                    // Get all the keys in the nestedDict and add them to the rootDict
                    foreach (var nestKey in nestedDict.Keys)
                    {
                        var nestValue = nestedDict[nestKey];

                        // Ensure the key is unique
                        rootDict.TryAdd($"{rootKey}_{nestKey}", nestValue);
                    }

                    // Remove the original rootKey from the rootDict as all the properties have been merged into it
                    rootDict.Remove(rootKey, out _);
                }
                else if (rootValue is IEnumerable<object> array)
                {
                    var items = array
                        .Cast<IDictionary<string, object>>()
                        .ToList();

                    items.FlattenGraph();
                }
            }
        }
    }
}
