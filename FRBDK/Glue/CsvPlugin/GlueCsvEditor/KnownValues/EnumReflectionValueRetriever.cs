using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GlueCsvEditor.KnownValues
{
    /// <summary>
    /// A value retriever which returns the known fields and properties for any assemblies loaded by Glue. This does not include the game's 
    /// </summary>
    public class EnumReflectionValueRetriever : IKnownValueRetriever
    {
        protected static object _threadLock = new object();
        protected static Dictionary<string, IEnumerable<string>> _cachedTypeValues;

        /// <summary>
        /// All types in all assemblies, cached for performance reasons. This should never change
        /// </summary>
        static Dictionary<string, Type> typeCache;

        public IEnumerable<string> GetKnownValues(string fullTypeName)
        {
            if (string.IsNullOrWhiteSpace(fullTypeName))
                return new string[0];

            // Lock to prevent multiple threads from accessing the type dictionary at the same time
            lock (_threadLock)
            {
                // If the dictionary hasn't been instantiated yet, set it up
                if (_cachedTypeValues == null)
                    _cachedTypeValues = new Dictionary<string, IEnumerable<string>>();

                // Check if this type's value has already been cached
                if (!_cachedTypeValues.ContainsKey(fullTypeName))
                    CacheTypeValues(fullTypeName);

                return _cachedTypeValues[fullTypeName];
            }
        }

        protected void CacheTypeValues(string fullTypeName)
        {
            IEnumerable<string> foundValues;

            if(typeCache == null)
            {
                typeCache = new Dictionary<string, Type>();
                var assemblies =
                    AppDomain.CurrentDomain
                             .GetAssemblies();
                // Use reflection to retrieve the specified enum
                foreach(var typeToAdd in assemblies.SelectMany(x => x.GetTypes()))
                {
                    string name = typeToAdd.Name.ToLowerInvariant();
                    if(typeCache.ContainsKey(name) == false)
                    {
                        typeCache.Add(name, typeToAdd);
                    }
                }

            }


            var fullTypeNameTrimmed = fullTypeName.Trim().ToLowerInvariant();

            Type type = null;
            typeCache.TryGetValue(fullTypeNameTrimmed, out type);

            if (type == null)
            {
                foundValues = new string[0];
            }
            else
            {
                // Get all the enum values
                foundValues = type.GetMembers(BindingFlags.Public | BindingFlags.Static)
                                   .Select(x => x.Name)
                                   .ToList();
            }

            if (_cachedTypeValues.ContainsKey(fullTypeName))
                _cachedTypeValues[fullTypeName] = foundValues;
            else
                _cachedTypeValues.Add(fullTypeName, foundValues);
        }
    }
}
