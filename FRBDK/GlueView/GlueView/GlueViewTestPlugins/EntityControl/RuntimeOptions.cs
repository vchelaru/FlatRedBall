using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlueViewTestPlugins.EntityControl
{
    public class RuntimeOptions
    {
        public bool ShouldSave
        {
            get;
            set;
        }

        public RuntimeOptions()
        {
            ShouldSave = true;
        }
    }
}
