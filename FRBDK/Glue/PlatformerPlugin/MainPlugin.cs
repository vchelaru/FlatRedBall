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
using FlatRedBall.PlatformerPlugin.Generators;

namespace FlatRedBall.PlatformerPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        #region Fields/Properties

        MainControl control;

        CodeGenerator codeGenerator;

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
                // 1.1 - Added ability to specify rectangle sub-collision, typically used for cloud collision
                // 1.2 - Added slope collision support
                return new Version(1, 2);
            }
        }

        #endregion

        public override void StartUp()
        {
            base.RegisterCodeGenerator(new CodeGenerator());
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
            base.UnregisterAllCodeGenerators();
            return true;
        }

    }
}
