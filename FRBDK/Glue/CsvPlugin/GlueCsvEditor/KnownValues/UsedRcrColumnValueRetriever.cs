using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.IO.Csv;

namespace GlueCsvEditor.KnownValues
{
    public class UsedRcrColumnValueRetriever : IKnownValueRetriever
    {
        protected RuntimeCsvRepresentation _rcr;
        protected int _columnIndex;

        public UsedRcrColumnValueRetriever(RuntimeCsvRepresentation rcr, int columnIndex)
        {
            if (rcr == null)
                throw new ArgumentNullException("rcr");

            if (columnIndex < 0 || columnIndex >= rcr.Headers.Count())
                throw new IndexOutOfRangeException("Column index not in range for the RCR");

            _rcr = rcr;
            _columnIndex = columnIndex;
        }

        public IEnumerable<string> GetKnownValues(string fullTypeName)
        {
            return _rcr.Records
                       .Select(x => x[_columnIndex])
                       .Distinct()
                       .OrderBy(x => x)
                       .ToList();
        }
    }
}
