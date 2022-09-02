using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.SaveClasses;
using Glue;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.ContentPipeline;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.SaveClasses.Helpers;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Glue.Factories;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Graphics;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;

namespace FlatRedBall.Glue.FormHelpers
{
    public static class PropertyGridHelper
	{
		#region Fields

		private static System.Windows.Forms.PropertyGrid mPropertyGrid;

        private static NamedObjectPropertyGridDisplayer mNosDisplayer = new NamedObjectPropertyGridDisplayer();
        private static StateSavePropertyGridDisplayer mStateSaveDisplayer = new StateSavePropertyGridDisplayer();
        private static StateSaveCategoryPropertyGridDisplayer mStateSaveCategoryDisplayer = new StateSaveCategoryPropertyGridDisplayer();
        private static CustomVariablePropertyGridDisplayer mCustomVariableDisplayer = new CustomVariablePropertyGridDisplayer();
        private static EntitySavePropertyGridDisplayer mEntitySaveDisplayer = new EntitySavePropertyGridDisplayer();
        private static ScreenSavePropertyGridDisplayer mScreenSaveDisplayer = new ScreenSavePropertyGridDisplayer();
        private static ReferencedFileSavePropertyGridDisplayer mReferencedFileSaveDisplay = new ReferencedFileSavePropertyGridDisplayer();
        private static EventResponseSavePropertyGridDisplayer mEventResponseSaveDisplayer = new EventResponseSavePropertyGridDisplayer();


		#endregion


		public static void Initialize(System.Windows.Forms.PropertyGrid propertyGrid)
        {

            mNosDisplayer.RefreshOnTimer = false;
            mStateSaveDisplayer.RefreshOnTimer = false;
            mCustomVariableDisplayer.RefreshOnTimer = false;
            mEntitySaveDisplayer.RefreshOnTimer = false;
            mScreenSaveDisplayer.RefreshOnTimer = false;
            mReferencedFileSaveDisplay.RefreshOnTimer = false;
            mEventResponseSaveDisplayer.RefreshOnTimer = false;

            mPropertyGrid = propertyGrid;


            //mPropertyGrid.PropertyTabs.AddTabType(typeof(EntityBroadcastingPropertyTab));

            
        }





        public static void UpdateNamedObjectDisplay()
        {
            bool didInstanceChange = mNosDisplayer.UpdateToState(GlueState.Self);
            
            PluginManager.AdjustDisplayedNamedObject(GlueState.Self.CurrentNamedObjectSave, mNosDisplayer);

            GlueCommands.Self.DoOnUiThread(() =>
            {
                mNosDisplayer.PropertyGrid = MainGlueWindow.Self.PropertyGrid;

                // It seems as if the PropertyGrid will scroll to properties when they're added.
                // That is, the PropertyGrid selects the last property added (I think).
                // If I don't scroll to the top then the PropertyGrid always selects the "SourceType"
                // property, which is annoying because users don't expect this. I used to have the ScrollToTop
                // function inside of the UpdateToState method, but the PropertyGrid is assigned *after* UpdateToState.
                // Therefore the ScrollToTop must be called after the PropertyGrid is assigned. I don't like that we have
                // to manually do this but it might be the only option.
                if (didInstanceChange)
                {
                    mNosDisplayer.ScrollToTop();
                }
            });

        }

        internal static void UpdateStateSaveDisplay()
        {
            // Set the CurrentElement *before* setting the Instance
            mStateSaveDisplayer.CurrentElement = GlueState.Self.CurrentElement;

            mStateSaveDisplayer.Instance = GlueState.Self.CurrentStateSave;

            mStateSaveDisplayer.PropertyGrid = MainGlueWindow.Self.PropertyGrid;
        }

        public static void UpdateStateCategorySave()
        {

            mStateSaveCategoryDisplayer.Instance = GlueState.Self.CurrentStateSaveCategory;
            mStateSaveCategoryDisplayer.PropertyGrid = MainGlueWindow.Self.PropertyGrid;
        }
        
        internal static void UpdateCustomVariableDisplay()
        {
            // Set the CurrentElement *before* setting the Instance
            mCustomVariableDisplayer.CurrentElement = GlueState.Self.CurrentElement;

            mCustomVariableDisplayer.Instance = GlueState.Self.CurrentCustomVariable;

            mCustomVariableDisplayer.PropertyGrid = MainGlueWindow.Self.PropertyGrid;
        }

        internal static void UpdateEntitySaveDisplay()
        {
            mEntitySaveDisplayer.Instance = GlueState.Self.CurrentEntitySave;
            PluginManager.AdjustDisplayedEntity(GlueState.Self.CurrentEntitySave, mEntitySaveDisplayer);

            mEntitySaveDisplayer.PropertyGrid = MainGlueWindow.Self.PropertyGrid;

        }

        internal static void UpdateScreenSaveDisplay()
        {
            mScreenSaveDisplayer.Instance = GlueState.Self.CurrentScreenSave;
            PluginManager.AdjustDisplayedScreen(GlueState.Self.CurrentScreenSave, mScreenSaveDisplayer);

            mScreenSaveDisplayer.PropertyGrid = MainGlueWindow.Self.PropertyGrid;

        }

        internal static void UpdateReferencedFileSaveDisplay()
        {
            mReferencedFileSaveDisplay.Instance = GlueState.Self.CurrentReferencedFileSave;
            PluginManager.AdjustDisplayedReferencedFile(GlueState.Self.CurrentReferencedFileSave, mReferencedFileSaveDisplay);
            mReferencedFileSaveDisplay.PropertyGrid = MainGlueWindow.Self.PropertyGrid;
        }

        internal static void UpdateEventResponseSaveDisplayer()
        {
            mEventResponseSaveDisplayer.CurrentElement = GlueState.Self.CurrentElement;
            mEventResponseSaveDisplayer.Instance = GlueState.Self.CurrentEventResponseSave;

            mEventResponseSaveDisplayer.PropertyGrid = MainGlueWindow.Self.PropertyGrid;
        }

    }
}
