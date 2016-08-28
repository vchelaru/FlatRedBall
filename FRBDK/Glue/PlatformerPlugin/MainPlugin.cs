using FlatRedBall.Glue.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.PlatformerPlugin.Controllers;
using FlatRedBall.PlatformerPlugin.Views;

namespace FlatRedBall.PlatformerPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        MainControl control;

        public override string FriendlyName
        {
            get
            {
                return "Platformer Plugin";
            }
        }

        public override Version Version
        {
            get
            {
                return new Version(1, 0);
            }
        }

        public override void StartUp()
        {
            this.ReactToItemSelectHandler += HandleItemSelected;
        }

        private void HandleItemSelected(TreeNode selectedTreeNode)
        {
            bool shouldShow = GlueState.Self.CurrentEntitySave != null && 
                // So this only shows if the entity itself is selected:
                selectedTreeNode?.Tag == GlueState.Self.CurrentEntitySave;

            if(shouldShow)
            {
                if(control == null)
                {
                    control = MainController.Self.GetControl();
                    this.AddToTab(PluginManager.CenterTab, control, "Platformer");
                }
                else
                {
                    this.AddTab();
                }
                MainController.Self.UpdateTo(GlueState.Self.CurrentEntitySave);
            }
            else
            {
                this.RemoveTab();
            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

    }
}
