using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBallProfiler.Plugins
{
    public class ManagedObjectsCategory
    {
        public string CategoryName { get; set; }
        public Func<IEnumerable<string>> GetParentNamesFunc { get; set; }
    }
}
