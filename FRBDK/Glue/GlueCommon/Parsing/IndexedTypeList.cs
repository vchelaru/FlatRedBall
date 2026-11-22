using System;
using System.Collections;
using System.Collections.Generic;

namespace FlatRedBall.Glue.Parsing
{
    /// <summary>
    /// A fixed list of types (such as every type in an assembly) that can also find a type by name without
    /// scanning. <see cref="TypeResolution"/> uses the lookups when it is handed one of these, and scans
    /// any other list. Each lookup returns the first type in list order, the same one a scan would find.
    /// </summary>
    public sealed class IndexedTypeList : IReadOnlyList<Type>
    {
        private readonly Type[] _types;
        private readonly Dictionary<string, Type> _byName = new Dictionary<string, Type>();
        private readonly Dictionary<string, Type> _byFullName = new Dictionary<string, Type>();
        private readonly Dictionary<string, Type> _byNameOrFullName = new Dictionary<string, Type>();

        public IndexedTypeList(IEnumerable<Type> types)
        {
            _types = new List<Type>(types).ToArray();

            foreach (var type in _types)
            {
                var name = type.Name;
                var fullName = type.FullName;

                if (name != null)
                {
                    _byName.TryAdd(name, type);
                    _byNameOrFullName.TryAdd(name, type);
                }
                if (fullName != null)
                {
                    _byFullName.TryAdd(fullName, type);
                    _byNameOrFullName.TryAdd(fullName, type);
                }
            }
        }

        /// <summary>The first type whose Name is <paramref name="name"/>, or null.</summary>
        public Type FirstWithName(string name) => name != null && _byName.TryGetValue(name, out var type) ? type : null;

        /// <summary>The first type whose FullName is <paramref name="fullName"/>, or null.</summary>
        public Type FirstWithFullName(string fullName) =>
            fullName != null && _byFullName.TryGetValue(fullName, out var type) ? type : null;

        /// <summary>The first type whose Name or FullName is <paramref name="nameOrFullName"/>, or null.</summary>
        public Type FirstWithNameOrFullName(string nameOrFullName) =>
            nameOrFullName != null && _byNameOrFullName.TryGetValue(nameOrFullName, out var type) ? type : null;

        public Type this[int index] => _types[index];

        public int Count => _types.Length;

        public IEnumerator<Type> GetEnumerator() => ((IEnumerable<Type>)_types).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _types.GetEnumerator();
    }
}
