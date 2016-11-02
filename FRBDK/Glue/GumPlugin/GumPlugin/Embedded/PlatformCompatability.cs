using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumPlugin.Embedded
{
    
}


namespace System.ComponentModel
{
#if WINDOWS_8 || UWP
    public sealed class BrowsableAttribute : Attribute
    {
        public BrowsableAttribute(bool browsable)
        {

        }
    }
#endif
}
