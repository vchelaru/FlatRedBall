using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using GlueFormsCore.Plugins.EmbeddedPlugins.AddScreenPlugin;
using GlueFormsCore.ViewModels;
using OfficialPluginsCore.Wizard.Managers;
using OfficialPluginsCore.Wizard.Models;
using OfficialPluginsCore.Wizard.ViewModels;
using OfficialPluginsCore.Wizard.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDataUi;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace OfficialPluginsCore.Wizard
{
    [Export(typeof(PluginBase))]
    public class MainWizardPlugin : PluginBase
    {
        #region Fields/Properties

        public override string FriendlyName => "New Project Wizard";

        public override Version Version => new Version(1, 1);

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            //AddMenuItemTo("Start New Project Wizard", (not, used) => RunWizard(), "Plugins");
        }

        public void RunWizard()
        {
            var window = new WizardWindow();

            GlueCommands.Self.DialogCommands.MoveToCursor(window);

            window.DoneClicked += () => WizardProjectLogic.Self.Apply(window.WizardData);

            window.ShowDialog();

        }

    }
}
