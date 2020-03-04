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
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Controls;

namespace FlatRedBall.PlatformerPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        #region Fields/Properties

        MainControl control;
        PluginTab pluginTab;

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
                // 1.3 - Added support for multiple entities in a single project being marked as entities
                //       by moving enums to a separate file.
                // 1.3.1 - Removed max/min velocity, so platformer characters can get shot off faster than max velocity and
                //          they will eventually regain control.
                // 1.3.2 - Enum is now automatically generated whenever project is loaded in case it's missing (old projects won't have this, maybe deleted by user).
                // 1.3.3 - Changing the movement values to a set of values with deceleration and 0 max speed
                //         uses the last variable's max speed, so that the character doesn't keep sliding.
                // 1.3.4 - Fixed bug where deceleration would sometimes not use the slowdown speed.
                // 1.3.5 - Fixed possible crash when generating enums.
                // 1.3.6 - New build with latest Glue to address any syntax changes to underlying libs
                // 1.3.7 - Added default input for vertical movement (jumping through clouds)
                // 1.3.8 - Removed "0" in top left corner caused by debug write
                // 1.3.9 - Fixed blend operation setting on maps - if a previous sprite used add, then
                //         the layer was drawn with add
                // 1.4.0 - If the user is holding the down vertical input, in the air, and the movement
                //         values allow falling through clouds, cloud collision will not be tested.
                // 1.5   - Added InitializePlatformerInput which takes an IInputDevice
                // 1.5.1 - Fixed bug where duplicate entries could get added to the CSV
                // 1.6   - Added support for entities that derive from entities which are platformer entities
                // 1.7   - Added variable to keep track of position before last collision
                // 1.7.1 - Made DirectionFacing public
                //       - Added HorizontalDirection extensions
                return new Version(1, 7, 1);
            }
        }

        #endregion

        public override void StartUp()
        {
            base.RegisterCodeGenerator(new EntityCodeGenerator());
            this.ReactToLoadedGlux += HandleGluxLoaded;
            this.ReactToItemSelectHandler += HandleItemSelected;
        }

        private void HandleGluxLoaded()
        {
            var entities = GlueState.Self.CurrentGlueProject.Entities;
            var anyPlatformer = entities.Any(item =>
            {
                var properties = item.Properties;
                return properties.GetValue<bool>("IsPlatformer");
            });

            if(anyPlatformer)
            {
                // just in case it's not there:
                EnumFileGenerator.Self.GenerateAndSaveEnumFile();
            }
        }

        private void HandleItemSelected(TreeNode selectedTreeNode)
        {
            var entity = GlueState.Self.CurrentEntitySave;
            bool shouldShow = entity != null && 
                // So this only shows if the entity itself is selected:
                selectedTreeNode?.Tag == entity;

            if(shouldShow)
            {
                if(control == null)
                {
                    control = MainController.Self.GetControl();
                    pluginTab = this.CreateTab(control, "Platformer");
                    this.ShowTab(pluginTab, TabLocation.Center);
                }
                else
                {
                    this.ShowTab(pluginTab);
                }


                MainController.Self.UpdateTo(entity);

            }
            else
            {
                this.RemoveTab(pluginTab);
            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            base.UnregisterAllCodeGenerators();
            return true;
        }

    }
}
