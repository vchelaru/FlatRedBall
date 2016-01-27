using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuildServerUploaderConsole
{
    public class RuntimeSettings
    {
        static RuntimeSettings mSelf = new RuntimeSettings();


        public static RuntimeSettings Self
        {
            get
            {
                return mSelf;
            }
        }

    }
}
