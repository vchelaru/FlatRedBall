namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    /// <summary>
    /// The New Project Wizard plugin. The plugin assigns its implementation to
    /// <see cref="ExportedImplementations.GlueCommands.WizardCommands"/> at startup.
    /// </summary>
    public interface IWizardCommands
    {
        /// <summary>Shows the wizard window modally and applies its choices when the user clicks Done. Touches UI.</summary>
        void RunWizard();
    }
}
