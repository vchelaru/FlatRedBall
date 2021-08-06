using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.Parsing;

namespace GlueCsvEditor.KnownValues
{
    public class InterfaceImplementationsValueRetriever : IKnownValueRetriever
    {
        protected IEnumerable<ParsedClass> _parsedClasses;

        public InterfaceImplementationsValueRetriever(IEnumerable<ParsedClass> parsedClasses)
        {
            _parsedClasses = parsedClasses ?? new ParsedClass[0];
        }

        public IEnumerable<string> GetKnownValues(string fullTypeName)
        {
            if (string.IsNullOrWhiteSpace(fullTypeName))
                return new string[0];

            string typeName = fullTypeName.Substring(fullTypeName.LastIndexOf(".", StringComparison.Ordinal) + 1);

            // Parse all custom code and find all classes that are implementations of an interface
            var results = new List<string>();
            foreach (var cls in _parsedClasses)
            {
                var isImplementation = cls.ParentClassesAndInterfaces
                                           .Where(x => x.IsInterface)
                                           .Any(x => x.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));

                if (!isImplementation)
                    continue;

                results.Add(string.Concat("new ", cls.Namespace, ".", cls.Name, "()"));
            }

            return results;
        }
    }
}
