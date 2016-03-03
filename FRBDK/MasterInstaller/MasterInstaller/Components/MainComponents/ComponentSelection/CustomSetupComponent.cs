using System;
using MasterInstaller.Components.Controls;

namespace MasterInstaller.Components.MainComponents.CustomSetup
{
    public class CustomSetupComponent : ComponentBase
    {
        ComponentSelectionControl control;

        public CustomSetupComponent()
        {
            //Control = new CustomSetupControl();

            this.NextClicked += delegate
            {
                ApplySelection();
            };

        }

        protected override BasePage CreateControl()
        {
            if(control == null)
            {
                control = new ComponentSelectionControl();
            }
            return control;
        }

        void ApplySelection()
        {
            foreach(var item in control.ViewModels)
            {
                bool selected = item.IsSelected;

                ComponentStorage.SetValue(item.BackingData.Key, selected);

            }
        }
    }
}
