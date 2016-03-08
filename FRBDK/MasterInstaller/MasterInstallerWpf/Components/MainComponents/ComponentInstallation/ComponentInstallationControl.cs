using MasterInstaller.Components.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace MasterInstaller.Components.MainComponents.ComponentInstallation
{
    class ComponentInstallationControl : BasePage
    {
        TextBlock leftTextBlock;

        public string LeftText
        {
            get
            {
                return leftTextBlock.Text;
            }
            set
            {
                leftTextBlock.Text = value;
            }
        }

        public ComponentInstallationControl() : base()
        {
            leftTextBlock = SetLeftText("Installing stuff...");

            NextButton.Visibility = System.Windows.Visibility.Collapsed;
        }
    }
}
