using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using OfficialPlugins.Wizard.Views;

namespace OfficialPlugins.Wizard.Managers
{
    /// <summary>The New Project Wizard plugin's <see cref="IWizardCommands"/>.</summary>
    public class WizardCommands : IWizardCommands
    {
        public void RunWizard()
        {
            var window = new WizardWindow();

            GlueCommands.Self.DialogCommands.MoveToCursor(window);

            window.DoneClicked += () => WizardProjectLogic.Self.Apply(window.WizardData);

            window.ShowDialog();
        }
    }
}
