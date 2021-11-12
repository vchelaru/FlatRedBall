using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.FormHelpers;
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
        #region Fields/Properties

        public override string FriendlyName => "Racing Plugin";

        // 1.0.1 - Whenever the project is loaded the collision history file is generated if it's not there
        // 1.1 - Gas is now a I1DInput instead of pressable
        public override Version Version =>
            new Version(1, 1, 0);

        // view here
        MainEntityView control;

        PluginTab pluginTab;

        #endregion

        public override void StartUp()
        {
            MainController.Self.MainPlugin = this;

            base.RegisterCodeGenerator(new EntityCodeGenerator());
            this.ReactToLoadedGlux += HandleGluxLoaded;
            this.ReactToItemSelectHandler += HandleItemSelected;
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
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
                MainController.Self.AddCollisionHistoryFile();
                EnumFileGenerator.Self.GenerateAndSaveEnumFile();
                // just in case it's not there:
                //InterfacesFileGenerator.Self.GenerateAndSave();
                //AiCodeGenerator.Self.GenerateAndSave();

            }
        }

        private void HandleItemSelected(ITreeNode selectedTreeNode)
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
                }
                pluginTab.Show();
                MainController.Self.UpdateTo(GlueState.Self.CurrentEntitySave);
            }
            else
            {
                pluginTab?.Hide();
            }
        }
    }
}
