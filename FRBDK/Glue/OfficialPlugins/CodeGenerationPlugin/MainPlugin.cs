using FlatRedBall.Glue.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.Interfaces;

namespace OfficialPlugins.CodeGenerationPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        CodeGenerationControl control;
        PluginTab tab;

        public override string FriendlyName
        {
            get
            {
                return "Code Generation Plugin";
            }
        }

        public override Version Version
        {
            get
            {
                return new Version(1, 0);
            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            base.AddMenuItemTo(Localization.Texts.CodeGenerationPlugin, Localization.MenuIds.CodeGenerationPluginId, HandleOpenClick, Localization.MenuIds.PluginId);
        }

        private void HandleOpenClick(object sender, EventArgs e)
        {
            ShowUi();
        }

        private void ShowUi()
        {
            if (control == null)
            {
                control = new CodeGenerationControl();
                tab = base.CreateTab(control, Localization.Texts.CodeGeneration, TabLocation.Bottom);
            }
            tab.Show();
        }
    }
}
