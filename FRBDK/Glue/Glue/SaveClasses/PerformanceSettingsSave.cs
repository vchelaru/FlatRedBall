using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.SaveClasses
{
    public class PerformanceSettingsSave
    {
        public bool ThrowExceptionOnPostInitializeContentLoad
        {
            get;
            set;
        }

        public bool RecordInitializeSegments
        {
            get;
            set;
        }
    }
}
