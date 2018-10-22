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
        public override Version Version
        {
            get { return new Version(1, 3); }
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
            // Do anything your plugin needs to do when it first starts up
            mCoreItemAdder = new CodeBuildItemAdder();
            mCoreItemAdder.Add("GameScriptingPlugin.EmbeddedCodeFiles.AfterThatDecision.cs");
            mCoreItemAdder.Add("GameScriptingPlugin.EmbeddedCodeFiles.DecisionAndList.cs");
            mCoreItemAdder.Add("GameScriptingPlugin.EmbeddedCodeFiles.DecisionOrList.cs");
            mCoreItemAdder.Add("GameScriptingPlugin.EmbeddedCodeFiles.DelegateDecision.cs");
            mCoreItemAdder.Add("GameScriptingPlugin.EmbeddedCodeFiles.GeneralAction.cs");
            mCoreItemAdder.Add("GameScriptingPlugin.EmbeddedCodeFiles.GeneralDecision.cs");
            mCoreItemAdder.Add("GameScriptingPlugin.EmbeddedCodeFiles.IDecisionList.cs");
            mCoreItemAdder.Add("GameScriptingPlugin.EmbeddedCodeFiles.IDoScriptEngine.cs");
            mCoreItemAdder.Add("GameScriptingPlugin.EmbeddedCodeFiles.IIfScriptEngine.cs");
            mCoreItemAdder.Add("GameScriptingPlugin.EmbeddedCodeFiles.IScriptAction.cs");
            mCoreItemAdder.Add("GameScriptingPlugin.EmbeddedCodeFiles.IScriptDecision.cs");
            mCoreItemAdder.Add("GameScriptingPlugin.EmbeddedCodeFiles.Script.cs");
            mCoreItemAdder.Add("GameScriptingPlugin.EmbeddedCodeFiles.ScriptEngine.cs");

            mCoreItemAdder.OutputFolderInProject = "GameScriptingCore";
            //this.ReactToLoadedGlux += HandleOpenProject;

            this.AddMenuItemTo("Add Game Script Core Classes", HandleAddGameScript, "Plugins");

            mDebuggingItemAdder = new CodeBuildItemAdder();
            mDebuggingItemAdder.Add(
                "GameScriptingPlugin/EmbeddedCodeFilesDebugging/ScriptDebuggingForm.cs");
            mDebuggingItemAdder.Add(
                "GameScriptingPlugin/EmbeddedCodeFilesDebugging/ScriptDebuggingForm.Designer.cs");

            mDebuggingItemAdder.OutputFolderInProject = "GameScriptingDebugging";

            this.AddMenuItemTo("Add Game Script Debugging Classes", HandleAddGameScriptDebugging, "Plugins");
        }





        void HandleAddGameScript(object sender, EventArgs args)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            mCoreItemAdder.PerformAddAndSave(assembly);
        }

        private void HandleAddGameScriptDebugging(object sender, EventArgs e)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            mDebuggingItemAdder.PerformAddAndSave(assembly);
        }
    }
}
