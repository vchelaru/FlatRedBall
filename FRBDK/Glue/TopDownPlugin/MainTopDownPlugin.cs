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
using TopDownPlugin.Logic;
using TopDownPlugin.ViewModels;
using TopDownPlugin.Views;
using TopDownPluginCore.CodeGenerators;

namespace TopDownPlugin
{
    public class MainTopDownPlugin //: PluginBase
    {
        #region Fields/Properties

        //public override string FriendlyName => "Top Down Plugin";

        // 1.1 - Added support for 0 time speedup and slowdown
        // 1.2 - Fixed direction being reset when not moving with 
        //       a slowdown time of 0.
        // 1.3 - Added ability to get direction from velocity
        //     - Added ability to invert direction and mirror direction
        // 1.4 - Added InitializeTopDownInput which takes an IInputDevice
        // 1.4.1 - Added TopDownDirection.ToString
        // 1.5 - Added TopDownAiInput.IsActive which can disabled AI input if set to false
        // 1.6 - InitializeTopDownInput now calls a partial method allowing custom code to
        //       add its own logic.
        // 1.7 - Added TopDownSpeedMultiplier allowing speed to be multiplied easily based on terrain or power-ups
        // 1.7.1 - Will ask the user if plugin should be a required plugin when marking an entity as top-down
        // 2.0 - New UI for editing top down values
        //  - TopDownAiInput.cs is now saved in TopDownAiInput.Generated.cs
        // 2.0.1 - Fixed crash occurring if trying to save CSV while it's open in excel.
        //public override Version Version => 
        //    new Version(2, 0, 1);

        //MainEntityView topDownControl;

        //PluginTab topDownPluginTab;

        #endregion

        //public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason)
        //{
        //    return true;
        //}

        //public override void StartUp()
        //{
        //    //MainController.Self.MainPlugin = this;

        //    // Needed for deserialization:

        //    //base.RegisterCodeGenerator(new EntityCodeGenerator());
        //    AssignEvents();
        //}

        //private void AssignEvents()
        //{
        //    this.ReactToLoadedGlux += HandleGluxLoaded;
        //    //this.ReactToItemSelectHandler += HandleItemSelected;
        //    this.ReactToEntityRemoved += HandleElementRemoved;
        //    this.ReactToElementRenamed += HandleElementRenamed;
        //    this.ModifyAddEntityWindow += ModifyAddEntityWindowLogic.HandleModifyAddEntityWindow;
        //    this.NewEntityCreatedWithUi += NewEntityCreatedReactionLogic.ReactToNewEntityCreated;
        //}

        private void HandleElementRenamed(IElement renamedElement, string oldName)
        {
            if (renamedElement is EntitySave renamedEntity)
            {
                MainController.Self.HandleElementRenamed(renamedElement, oldName);
            }
        }

        //private void HandleItemSelected(System.Windows.Forms.TreeNode selectedTreeNode)
        //{
        //    bool shouldShow = GlueState.Self.CurrentEntitySave != null &&
        //        // So this only shows if the entity itself is selected:
        //        selectedTreeNode?.Tag == GlueState.Self.CurrentEntitySave;


        //    if (shouldShow)
        //    {
        //        if (topDownControl == null)
        //        {
        //            topDownControl = MainController.Self.GetExistingOrNewControl();
        //            topDownPluginTab = this.CreateTab(topDownControl, "Top Down");
        //            this.ShowTab(topDownPluginTab, TabLocation.Center);
        //        }
        //        else
        //        {
        //            this.ShowTab(topDownPluginTab);
        //        }
        //        MainController.Self.UpdateTo(GlueState.Self.CurrentEntitySave);
        //    }
        //    else
        //    {
        //        this.RemoveTab(topDownPluginTab);
        //    }
        //}

        private void HandleElementRemoved(EntitySave removedElement, List<string> additionalFiles)
        {
            // This could be the very last entity that was a top-down, but isn't
            // anymore.
            MainController.Self.CheckForNoTopDownEntities();
        }
    }
}
