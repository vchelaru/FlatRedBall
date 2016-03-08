using System;
using MasterInstaller.Components.Controls;

namespace MasterInstaller.Components.MainComponents.Completed
{
    public class CompletedComponent : ComponentBase
    {
        public CompletedComponent()
        {
        }

        protected override BasePage CreateControl()
        {
            return new CompletedControl();
        }
    }
}
