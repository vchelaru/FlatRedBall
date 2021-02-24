using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewProjectCreator
{
    public class PlatformProjectInfo
    {
        public string FriendlyName;
        public string Namespace;
        public string ZipName;
        public string Url;
        public bool SupportedInGlue;

        public override string ToString()
        {
            string toReturn = FriendlyName;

			return toReturn;
        }
    }

}
