using System;
using System.Collections.Generic;
using FlatRedBall.Glue.Parsing;

namespace GlueCsvEditor.KnownValues
{
    public class ParsedEnumValueRetriever : IKnownValueRetriever
    {
        protected IEnumerable<ParsedEnum> _parsedEnums;

        public ParsedEnumValueRetriever(IEnumerable<ParsedEnum> parsedEnums)
        {
            _parsedEnums = parsedEnums ?? new ParsedEnum[0];
        }

        public IEnumerable<string> GetKnownValues(string fullTypeName)
        {
            foreach (var enm in _parsedEnums)
            {
                var enumFullType = string.Concat(enm.Namespace, ".", enm.Name);
                if (!enumFullType.EndsWith(fullTypeName, StringComparison.OrdinalIgnoreCase))
                    continue;

                // return all the values for the enum
                return enm.Values;
            }

            return new string[0];
        }
    }
}
