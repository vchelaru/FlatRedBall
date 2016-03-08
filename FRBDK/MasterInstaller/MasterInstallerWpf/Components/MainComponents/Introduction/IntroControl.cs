using MasterInstaller.Components.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace MasterInstaller.Components.MainComponents.Introduction
{
    public class IntroControl : BasePage
    {
        public IntroControl() : base()
        {
            Title = "Install FlatRedBall Engine";

            SetLeftText("Thank you for downloading FlatRedball. " +
                "This will install the FlatRedBall engine, tools, and dependencies. " +
                "Some dependencies are third-party tools with their own installers. " +
                "You can run this installer again at any time.");
        }
    }
}
