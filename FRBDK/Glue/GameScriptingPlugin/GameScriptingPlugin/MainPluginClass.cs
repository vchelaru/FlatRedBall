using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.VSHelpers;
using System.Reflection;


namespace GameScriptingPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPluginClass : PluginBase
    {
        #region Fields

        CodeBuildItemAdder mCoreItemAdder;
        CodeBuildItemAdder mDebuggingItemAdder;

        #endregion

        #region Properties

        [Import("GlueProjectSave")]
        public GlueProjectSave GlueProjectSave
        {
            get;
            set;
        }

        [Import("GlueCommands")]
        public IGlueCommands GlueCommands
        {
            get;
            set;
        }
		
		[Import("GlueState")]
		public IGlueState GlueState
		{
		    get;
		    set;
        }

        public override string FriendlyName
        {
            get { return "Game Scripting Plugin"; }
        }

        // 1.3 
        // - Fixed bug where creating If then calling EndActiveIf wouldn't clear out the if's, and they'd "or" with
        //   the next if
        // - Added check for PC precompile directive so scripting engine could be used on Linux and Mac
        // 1.4
        // - Added new ScreenScript.cs file
        // 1.5
        // - Added WaitAction and Do.Wait
        // 1.6 
        // - If.Check now returns the general action, allowing setting of complete actions. 
        public override Version Version
        {
            get { return new Version(1, 6); }
        }
        #endregion

        public override bool ShutDown(PluginShutDownReason reason)
        {
            // Do anything your plugin needs to do to shut down
            // or don't shut down and return false
            this.RemoveAllMenuItems();
            return true;
        }

        public override void StartUp()
        {
            string prefix = "GameScriptingPluginCore.EmbeddedCodeFiles";
            // Do anything your plugin needs to do when it first starts up
            mCoreItemAdder = new CodeBuildItemAdder();
            mCoreItemAdder.Add($"{prefix}.AfterThatDecision.cs");
            mCoreItemAdder.Add($"{prefix}.DecisionAndList.cs");
            mCoreItemAdder.Add($"{prefix}.DecisionOrList.cs");
            mCoreItemAdder.Add($"{prefix}.DelegateDecision.cs");
            mCoreItemAdder.Add($"{prefix}.GeneralAction.cs");
            mCoreItemAdder.Add($"{prefix}.GeneralDecision.cs");
            mCoreItemAdder.Add($"{prefix}.IDecisionList.cs");
            mCoreItemAdder.Add($"{prefix}.IDoScriptEngine.cs");
            mCoreItemAdder.Add($"{prefix}.IIfScriptEngine.cs");
            mCoreItemAdder.Add($"{prefix}.IScriptAction.cs");
            mCoreItemAdder.Add($"{prefix}.IScriptDecision.cs");
            mCoreItemAdder.Add($"{prefix}.ScreenScript.cs");
            mCoreItemAdder.Add($"{prefix}.Script.cs");
            mCoreItemAdder.Add($"{prefix}.ScriptEngine.cs");
            mCoreItemAdder.Add($"{prefix}.WaitAction.cs");

            mCoreItemAdder.OutputFolderInProject = "GameScriptingCore";
            //this.ReactToLoadedGlux += HandleOpenProject;

            this.AddMenuItemTo("Add Game Script Core Classes", HandleAddGameScript, "Plugins");

            mDebuggingItemAdder = new CodeBuildItemAdder();
            mDebuggingItemAdder.Add(
                "GameScriptingPluginCore/EmbeddedCodeFilesDebugging/ScriptDebuggingForm.cs");
            mDebuggingItemAdder.Add(
                "GameScriptingPluginCore/EmbeddedCodeFilesDebugging/ScriptDebuggingForm.Designer.cs");

            mDebuggingItemAdder.OutputFolderInProject = "GameScriptingDebugging";

            this.AddMenuItemTo("Add Game Script Debugging Classes", HandleAddGameScriptDebugging, "Plugins");
        }





        void HandleAddGameScript(object sender, EventArgs args)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            mCoreItemAdder.PerformAddAndSaveTask(assembly);
        }

        private void HandleAddGameScriptDebugging(object sender, EventArgs e)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            mDebuggingItemAdder.PerformAddAndSaveTask(assembly);
        }
    }
}
    