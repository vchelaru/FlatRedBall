using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopDownPlugin.CodeGenerators;
using TopDownPlugin.Controllers;
using TopDownPlugin.ViewModels;
using TopDownPlugin.Views;

namespace TopDownPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        public override string FriendlyName => "Top Down Plugin";

        // 1.1 - Added support for 0 time speedup and slowdown
        // 1.2 - Fixed direction being reset when not moving with 
        //       a slowdown time of 0.
        // 1.3 - Added ability to get direction from velocity
        //     - Added ability to invert direction and mirror direction
        // 1.4 - Added InitializeTopDownInput which takes an IInputDevice
        // 1.4.1 - Added TopDownDirection.ToString
        public override Version Version => 
            new Version(1, 4, 1);

        MainEntityView control;
        PluginTab pluginTab;


        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            base.RegisterCodeGenerator(new EntityCodeGenerator());
            this.ReactToLoadedGlux += HandleGluxLoaded;
            this.ReactToItemSelectHandler += HandleItemSelected;
        }

        private void HandleGluxLoaded()
        {
            var entities = GlueState.Self.CurrentGlueProject.Entities;

            var anyTopDownEntities = entities.Any(item =>
            {
                var properties = item.Properties;
                return properties.GetValue<bool>(nameof(TopDownEntityViewModel.IsTopDown));
            });

            if (anyTopDownEntities)
            {
                // just in case it's not there:
                EnumFileGenerator.Self.GenerateAndSaveEnumFile();
                InterfacesFileGenerator.Self.GenerateAndSave();
                AiCodeGenerator.Self.GenerateAndSave();

            }
        }

        private void HandleItemSelected(System.Windows.Forms.TreeNode selectedTreeNode)
        {
            bool shouldShow = GlueState.Self.CurrentEntitySave != null &&
                // So this only shows if the entity itself is selected:
                selectedTreeNode?.Tag == GlueState.Self.CurrentEntitySave;


            if (shouldShow)
            {
                if (control == null)
                {
                    control = MainController.Self.GetControl();
                    pluginTab = this.CreateTab(control, "Top Down");
                    this.ShowTab(pluginTab, TabLocation.Center);
                }
                else
                {
                    this.ShowTab(pluginTab);
                }
                MainController.Self.UpdateTo(GlueState.Self.CurrentEntitySave);
            }
            else
            {
                this.RemoveTab(pluginTab);
            }
        }
    }
}
