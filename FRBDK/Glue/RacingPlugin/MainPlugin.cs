using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using RacingPlugin.CodeGenerators;
using RacingPlugin.Controllers;
using RacingPlugin.ViewModels;
using RacingPlugin.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RacingPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        public override string FriendlyName => "Racing Plugin";

        public override Version Version =>
            new Version(1, 0, 0);

        // view here
        MainEntityView control;
        PluginTab pluginTab;

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            MainController.Self.MainPlugin = this;

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
                return properties.GetValue<bool>(nameof(RacingEntityViewModel.IsRacingEntity));
            });

            if (anyTopDownEntities)
            {
                // just in case it's not there:
                //EnumFileGenerator.Self.GenerateAndSaveEnumFile();
                //InterfacesFileGenerator.Self.GenerateAndSave();
                //AiCodeGenerator.Self.GenerateAndSave();

            }
        }

        private void HandleItemSelected(TreeNode selectedTreeNode)
        {
            bool shouldShow = GlueState.Self.CurrentEntitySave != null &&
                // So this only shows if the entity itself is selected:
                selectedTreeNode?.Tag == GlueState.Self.CurrentEntitySave;


            if (shouldShow)
            {
                if (control == null)
                {
                    control = MainController.Self.GetControl();
                    pluginTab = this.CreateTab(control, "Racing");
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
