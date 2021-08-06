using System.Collections.Generic;

namespace GlueCsvEditor.KnownValues
{
    public interface IKnownValueRetriever
    {
        IEnumerable<string> GetKnownValues(string fullTypeName);
    }
}
