using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;

namespace EntityInputMovementPlugin.Logic
{
    /// <summary>
    /// The plugin's <see cref="IEntityInputMovementCommands"/>. It goes through the controllers' view models, the
    /// ones the movement tab binds to once it exists, because their PropertyChanged handlers hold the logic that
    /// adds the movement variables and CSV files. The tab's own view models are null until the tab is first shown
    /// and always null headless, so this does not use them.
    /// </summary>
    public class EntityInputMovementCommands : IEntityInputMovementCommands
    {
        public void MakeEntityPlatformer(EntitySave entity)
        {
            var viewModel = FlatRedBall.PlatformerPlugin.Controllers.MainController.Self.GetViewModel();
            viewModel.BackingData = entity;
            viewModel.IsPlatformer = true;
        }

        public void MakeEntityTopDown(EntitySave entity)
        {
            var viewModel = TopDownPlugin.Controllers.MainController.Self.GetViewModel();
            viewModel.BackingData = entity;
            viewModel.IsTopDown = true;
        }
    }
}
