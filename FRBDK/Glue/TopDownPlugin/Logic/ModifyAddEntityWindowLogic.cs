using FlatRedBall.Glue.Controls;
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

            var control = new AdditionalEntitiesControls();
            control.DataContext = viewModel;
            window.AddControl(control);
        }
    }
}
