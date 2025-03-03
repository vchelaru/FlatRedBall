using System;
using System.Collections.Generic;
using System.Linq;

namespace GlueTestProject.GumRuntimes;

public partial class EventBindingComponentRuntime
{
    public int TimesEventRaised { get; private set; }
    partial void CustomInitialize () 
    {
    }

    public void BoundEventHandler()
    {
        TimesEventRaised++;
    }
}
