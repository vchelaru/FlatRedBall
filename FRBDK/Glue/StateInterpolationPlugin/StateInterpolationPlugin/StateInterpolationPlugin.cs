using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Plugins;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using System.IO;
using FlatRedBall.Glue;
using FlatRedBall.IO;
using System.Reflection;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.Glue.Managers;

namespace OfficialPlugins.StateInterpolation
{
    [Export(typeof(PluginBase))]
    public class StateInterpolationPlugin : PluginBase
    {
        #region Fields
        CodeBuildItemAdder mItemAdder;



        public const string VariableName = "HasAdvancedInterpolations";

        //MenuStrip mMenuStrip;
        //ToolStripMenuItem mStateInterpolationEnabledMenuItem;
        StateInterpolationCodeGenerator mCodeGenerator;

        #endregion

        #region Properties

        public override string FriendlyName
        {
            get { return "State Interpolation Plugin"; }
        }

        // 1.2.0 - Added instant interpolation type
        // 1.2.1 - Fixed bug where Running would remain true after its duration
        // 1.2.2 - Removed event that fired when a tweener finished - now we just rely on the TweenerManager to do it.
        // 1.3 - Plugin no longer injects the state interpolation values - that's part of new projects autoatically now
        public override Version Version
        {
            get { return new Version(1, 3, 0); }
        }

        #endregion

        public override void StartUp()
        {
            // We need this to happen when the glux is loaded
            //UpdateCodeInProjectPresence();

            CreateCodeItemAdder();

            CreateMenuItems();

            this.AdjustDisplayedEntity += HandleAdjustDisplayedEntity;
            this.AdjustDisplayedScreen += HandleAdjustDisplayedScreen;
            this.ReactToLoadedGlux += HandleGluxLoad;
            mCodeGenerator = new StateInterpolationCodeGenerator();
            CodeWriter.CodeGenerators.Add(mCodeGenerator);

        }

        private void CreateMenuItems()
        {
            this.AddMenuItemTo(Localization.Texts.AddStateInterpolationClasses, Localization.MenuIds.AddStateInterpolationClassesId, HandleAddStateInterpolationClasses, Localization.MenuIds.PluginId);
        }

        private void HandleAddStateInterpolationClasses(object sender, EventArgs e)
        {
            UpdateCodeInProjectPresence();
        }

        private void CreateCodeItemAdder()
        {
            mItemAdder = new CodeBuildItemAdder();
            mItemAdder.IsVerbose = true;

            mItemAdder.Add("StateInterpolationPlugin.Back.cs");
            mItemAdder.Add("StateInterpolationPlugin.Bounce.cs");
            mItemAdder.Add("StateInterpolationPlugin.Circular.cs");
            mItemAdder.Add("StateInterpolationPlugin.Cubic.cs");
            mItemAdder.Add("StateInterpolationPlugin.Elastic.cs");
            mItemAdder.Add("StateInterpolationPlugin.Exponential.cs");
            mItemAdder.Add("StateInterpolationPlugin.Instant.cs");
            mItemAdder.Add("StateInterpolationPlugin.Linear.cs");
            mItemAdder.Add("StateInterpolationPlugin.Quadratic.cs");
            mItemAdder.Add("StateInterpolationPlugin.Quartic.cs");
            mItemAdder.Add("StateInterpolationPlugin.Quintic.cs");
            mItemAdder.Add("StateInterpolationPlugin.ShakeTweener.cs");
            mItemAdder.Add("StateInterpolationPlugin.Sinusoidal.cs");
            mItemAdder.Add("StateInterpolationPlugin.Tweener.cs");
            mItemAdder.Add("StateInterpolationPlugin.TweenerManager.cs");

            mItemAdder.AddFileBehavior = AddFileBehavior.IfOutOfDate;

            mItemAdder.OutputFolderInProject = "StateInterpolation";
        }

        void HandleGluxLoad()
        {
            // Update March 28, 2018 - we no longer add these files to projects because they're part of new templates automatically
            //UpdateCodeInProjectPresence();
        }

        private void HandleAdjustDisplayedEntity(EntitySave entitySave, EntitySavePropertyGridDisplayer displayer)
        {
            displayer.IncludeCustomPropertyMember(VariableName, typeof(bool));
        }

        private void HandleAdjustDisplayedScreen(ScreenSave screenSave, ScreenSavePropertyGridDisplayer displayer)
        {
            displayer.IncludeCustomPropertyMember(VariableName, typeof(bool));
        }


        private void UpdateCodeInProjectPresence()
        {

            PluginManager.ReceiveOutput("Adding state interpolation plugin code files");
            Assembly assembly = Assembly.GetExecutingAssembly();
            mItemAdder.PerformAddAndSaveTask(assembly);
        }


        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason)
        {
            CodeWriter.CodeGenerators.Remove(mCodeGenerator);

            return true;
        }

    }
}
