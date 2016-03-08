using MasterInstaller.Components.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterInstaller.Components.MainComponents.Completed
{
    class CompletedControl : BasePage
    {
        public CompletedControl() : base()
        {
            CreateTextBox();

            Title = "Installation Complete";

            NextButton.Content = "Exit";
        }

        private void CreateTextBox()
        {
            //base.SetLeftText("Installation has completed.");
        }
    }
}
