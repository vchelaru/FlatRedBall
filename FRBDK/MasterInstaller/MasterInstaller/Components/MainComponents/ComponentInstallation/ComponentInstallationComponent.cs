using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MasterInstaller.Components.Controls;
using System.Windows;
using System.Threading.Tasks;

namespace MasterInstaller.Components.MainComponents.ComponentInstallation
{
    class ComponentInstallationComponent : ComponentBase
    {
        ComponentInstallationControl control;

        protected override BasePage CreateControl()
        {
            if(control == null)
            {
                control = new ComponentInstallationControl();
                control.LeftText = "Preparing to install...";

            }

            return control;
        }

        public override async Task Show()
        {
            var toInstalls = ComponentStorage.GetInstallableComponents()
                .Where(item => ComponentStorage.GetValue(item.Key))
                .ToList();

            int oneBasedIndex = 1;
            int count = toInstalls.Count;

            foreach (var toInstall in toInstalls)
            {
                control.LeftText = $"Installing {oneBasedIndex}/{count}: " + toInstall.Name + "\nPlease wait...";

                var result = await toInstall.Install();

                if (result != 0)
                {
                    MessageBox.Show("Failed to install " + toInstall.Name);
                }

                oneBasedIndex++;
            }

            // If it's finished, go to the next page:
            OnNextClicked();

        }
    }
}
