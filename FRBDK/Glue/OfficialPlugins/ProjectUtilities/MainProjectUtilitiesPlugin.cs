using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ForceSavePlugin
{
    [Export(typeof(PluginBase))]
    internal class MainProjectUtilitiesPlugin : PluginBase
    {
        public override string FriendlyName => "Force Save Plugin";

        public override void StartUp()
        {
            this.AddMenuItemTo("Force Save Project", HandleForceSaveProject, "Project");
            this.AddMenuItemTo("Set All Variables to SetByDerived = true", HandleSetAllVariablesToSetByDerived, "Project");

        }

        private void HandleForceSaveProject(object sender, EventArgs e)
        {
            if(GlueState.Self.CurrentGlueProject != null)
            {
                GlueCommands.Self.GluxCommands.SaveGlux();
            }
        }

        private void HandleSetAllVariablesToSetByDerived(object sender, EventArgs e)
        {
            var message = "Setting all variables to SetByDerived = true is useful if you would like to " +
                "reduce the size of your JSON files. By setting them all to SetByDerived, then more derived " +
                "variables will match their base definitions exactly, allowing them to be excluded from JSON files. " +
                "This should be a safe operation, but it is recommended that you first back up your project, or push" +
                "a commit so you have a clean commit to revert if anything goes wrong.\n\nContinue?";

            var result = GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(message);

            if(result == System.Windows.MessageBoxResult.Yes)
            {
                TaskManager.Self.AddAsync(() =>
                {

                    foreach (var screen in GlueState.Self.CurrentGlueProject.Screens)
                    {
                        foreach (var variable in screen.CustomVariables)
                        {
                            variable.SetByDerived = true;
                        }
                    }
                    foreach (var entity in GlueState.Self.CurrentGlueProject.Entities)
                    {
                        foreach (var variable in entity.CustomVariables)
                        {
                            variable.SetByDerived = true;
                        }
                    }

                    GlueCommands.Self.GluxCommands.SaveGlux();
                }, "Setting all variable SetByDerived = true");
            }
        }
    }
}
