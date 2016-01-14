using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.GuiDisplay.Facades
{
    public interface IApplicationSettings
    {

        List<string> AvailableApplications
        {
            get;
        }

        List<string> AvailableBuildTools
        {
            get;
        }
    }
}
