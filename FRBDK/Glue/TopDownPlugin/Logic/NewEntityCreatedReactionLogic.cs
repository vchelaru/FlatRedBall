using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using TopDownPlugin.Controllers;
using TopDownPlugin.ViewModels;
using TopDownPlugin.Views;

namespace TopDownPlugin.Logic
{
    class NewEntityCreatedReactionLogic
    {
        internal static void ReactToNewEntityCreated(EntitySave newEntity, AddEntityWindow window)
        {
            var control = window
                .UserControlChildren
                .FirstOrDefault(item => item is AdditionalEntitiesControls);

            var viewModel = control?.DataContext as AdditionalEntitiesControlViewModel;

            if(viewModel != null)
            {
                if(viewModel.IsTopDownEntity)
                {
                    GlueCommands.Self.DialogCommands.FocusDialog("Top Down");
                    MainController.Self.MakeCurrentEntityTopDown();

                }
            }
        }
    }
}
