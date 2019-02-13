using System;
using System.Collections.Generic;
using System.Linq;

namespace GlueTestProject.GumRuntimes
{
    public partial class BaseGlueScreenNotBaseGumRuntime
    {
        public static int NumberOfTimesCustomInitializeCalled = 0;
        partial void CustomInitialize()
        {
            NumberOfTimesCustomInitializeCalled++;
        }
    }
}
