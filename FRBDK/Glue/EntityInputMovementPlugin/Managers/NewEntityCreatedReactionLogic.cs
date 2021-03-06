using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using TopDownPlugin.ViewModels;
using TopDownPlugin.Views;

namespace EntityInputMovementPlugin.Logic
{
    public class NewEntityCreatedReactionLogic
    {
        public static void ReactToNewEntityCreated(EntitySave newEntity, AddEntityWindow window)
        {
            var control = window
                .UserControlChildren
                .FirstOrDefault(item => item is AdditionalEntitiesControls);

            var viewModel = control?.DataContext as AdditionalEntitiesControlViewModel;

            if(viewModel.AllUiVisibility == System.Windows.Visibility.Visible)
            {
                if(viewModel?.MovementType == EntityInputMovementPlugin.ViewModels.MovementType.TopDown)
                {
                    TopDownPlugin.Controllers.MainController.Self.MakeCurrentEntityTopDown();
                }
                else if(viewModel?.MovementType == EntityInputMovementPlugin.ViewModels.MovementType.Platformer)
                {
                    FlatRedBall.PlatformerPlugin.Controllers.MainController.Self.MakeCurrentEntityPlatformer();
                }
            }

        }
    }
}
