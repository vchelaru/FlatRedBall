using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TmxEditor
{
    public class ProvidedContext
    {
        public List<string> AvailableTsxFiles
        {
            get;
            private set;
        }

        public ProvidedContext()
        {
            AvailableTsxFiles = new List<string>();



        }

    }
}
