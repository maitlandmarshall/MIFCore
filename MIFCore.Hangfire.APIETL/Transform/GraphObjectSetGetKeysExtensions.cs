using System;
using System.Collections.Generic;

namespace MIFCore.Hangfire.APIETL.Transform
{
    public static class GraphObjectSetGetKeysExtensions
    {
        public static IDictionary<string, HashSet<Type>> GetKeyTypes(this IEnumerable<GraphObjectSet> graphObjectSets)
        {
            var allKeys = new Dictionary<string, HashSet<Type>>();

            foreach (var o in graphObjectSets)
            {
                var keys = o.GetKeyTypes();

                foreach (var (key, value) in keys)
                {
                    if (allKeys.TryGetValue(key, out var existingObjectValueType) == false)
                    {
                        existingObjectValueType = new HashSet<Type>();
                        allKeys[key] = existingObjectValueType;
                    }

                    foreach (var t in value)
                    {
                        existingObjectValueType.Add(t);
                    }
                }
            }

            return allKeys;
        }

        public static IDictionary<string, HashSet<Type>> GetKeyTypes(this GraphObjectSet graphObjectSet)
        {
            var keys = new Dictionary<string, HashSet<Type>>();

            foreach (var obj in graphObjectSet.Objects)
            {
                foreach (var (objKey, objectValue) in obj)
                {
                    var objectValueType = objectValue == null ? null : objectValue.GetType();

                    if (keys.TryGetValue(objKey, out var existingObjectValueType) == false)
                    {
                        existingObjectValueType = new HashSet<Type>();
                        keys[objKey] = existingObjectValueType;
                    }

                    existingObjectValueType.Add(objectValueType);
                }
            }

            return keys;
        }
    }
}
