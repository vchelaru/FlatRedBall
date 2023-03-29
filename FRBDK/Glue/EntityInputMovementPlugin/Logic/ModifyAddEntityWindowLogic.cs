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

            viewModel.PropertyChanged += (sender, args) =>
            {
                switch(args.PropertyName)
                {
                    case nameof(viewModel.IsTopDownRadioChecked):
                    case nameof(viewModel.IsPlatformerRadioChecked):
                        if (viewModel.IsPlatformerRadioChecked || viewModel.IsTopDownRadioChecked)
                        {
                            if(commonViewModel.ObjectsDisablingCollidableCheckbox.Contains(viewModel) == false)
                            {
                                commonViewModel.ObjectsDisablingCollidableCheckbox.Add(viewModel);
                            }
                            commonViewModel.IsICollidableChecked = true;
                        }
                        else
                        {

                            if (commonViewModel.ObjectsDisablingCollidableCheckbox.Contains(viewModel))
                            {
                                commonViewModel.ObjectsDisablingCollidableCheckbox.Remove(viewModel);
                            }
                        }
                        break;
                }
            };

            var control = new AdditionalEntitiesControls();
            control.DataContext = viewModel;
            window.AddControl(control);
        }
    }
}
