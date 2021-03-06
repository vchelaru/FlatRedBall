using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.MVVM;
using GlueFormsCore.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopDownPlugin.ViewModels;
using TopDownPlugin.Views;

namespace TopDownPlugin.Logic
{
    public class ModifyAddEntityWindowLogic
    {
        public static void HandleModifyAddEntityWindow(AddEntityWindow window)
        {
            var viewModel = new AdditionalEntitiesControlViewModel();

            var commonViewModel = window.DataContext as AddEntityViewModel;
            commonViewModel.PropertyChanged += (sender, args) =>
            {
                switch (args.PropertyName)
                {
                    case nameof(AddEntityViewModel.HasInheritance):
                        viewModel.AllUiVisibility = (commonViewModel.HasInheritance == false).ToVisibility();
                        break;
                }
            };

            var control = new AdditionalEntitiesControls();
            control.DataContext = viewModel;
            window.AddControl(control);
        }
    }
}
