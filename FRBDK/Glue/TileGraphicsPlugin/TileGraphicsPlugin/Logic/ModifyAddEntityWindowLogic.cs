using FlatRedBall.Glue.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TileGraphicsPlugin.ViewModels;
using TileGraphicsPlugin.Views;

namespace TileGraphicsPlugin.Logic
{
    class ModifyAddEntityWindowLogic
    {
        public static void HandleModifyAddEntityWindow(AddEntityWindow window)
        {
            var viewModel = new AdditionalEntitiesViewModel();


            var control = new AdditionalEntitiesControls();
            control.DataContext = viewModel;
            window.AddToStackPanel(control);
        }
    }
}
