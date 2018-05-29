using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.ComponentModel.Composition;
using FlatRedBall.IO;
using FlatRedBall.Glue;

namespace GlueView.Plugin
{
    public class PluginManager : PluginManagerBase
    {
        #region Properties

        public static PluginManager GlobalInstance
        {
            get
            {
                return GetGlobal() as PluginManager;
            }
        }


        [ImportMany(AllowRecomposition = true)]
        public IList<Plugin.GlueViewPlugin> Plugins
        {
            get;
            set;
        }


        #endregion

        #region Constructor and Intialization

        public PluginManager(bool global)
            : base(global)
        {
            Plugins = new List<GlueViewPlugin>();
        }

        protected override void InstantiateAllListsAsEmpty()
        {
            Plugins = new List<GlueViewPlugin>();
        }

        internal static void Initialize()
        {
            if (mGlobalInstance == null)
            {
                mGlobalInstance = new PluginManager(true);
                mGlobalInstance.LoadPlugins(@"GlueView\Plugins");
            }

            if (mProjectInstance != null)
            {
                foreach (IPlugin plugin in ((PluginManager)mProjectInstance).mPluginContainers.Keys)
                {
                    ShutDownPlugin(plugin, PluginShutDownReason.GlueShutDown);
                }
            }

            mProjectInstance = new PluginManager(false);

            mInstances.Clear();
            mInstances.Add(mGlobalInstance);
            mInstances.Add(mProjectInstance);

            GluxManager.BeforeVariableSet += OnBeforeVariableSet;
            GluxManager.AfterVariableSet += OnAfterVariableSet;

            mProjectInstance.LoadPlugins(@"GlueView\Plugins");
        }

        protected override void LoadReferenceLists()
        {
            string executablePath = FileManager.GetDirectory(System.Windows.Forms.Application.ExecutablePath);

            base.LoadReferenceLists();
            AddIfExists(executablePath + "StaffDotNet.CollapsiblePanel.dll");
            AddIfExists(executablePath + "NCalc.dll");
            AddIfExists(executablePath + "GluePropertyGridClasses.dll");
            AddIfExists(executablePath + "InteractiveInterface.dll");
            AddIfExists(executablePath + "FlatRedBall.PropertyGrid.dll");
            AddIfExists(executablePath + "Glue.exe");

        }
        protected override void StartAllPlugins(List<string> pluginsToIgnore = null)
        {
            foreach(var plugin in Plugins)
            {
                StartupPlugin(plugin);
            }
        }

        #endregion

        public static void OnBeforeVariableSet(object sender, VariableSetArgs variableSetArgs)
        {
            CallMethodOnPlugin(plugin => plugin.CallBeforeVariableSet(sender, variableSetArgs), "CallBeforeVariableSet");
        }

        public static void OnAfterVariableSet(object sender, VariableSetArgs variableSetArgs)
        {
            CallMethodOnPlugin(plugin => plugin.CallAfterVariableSet(sender, variableSetArgs), "CallAfterVariableSet");
        }

        public static void ReactToCursorPush()
        {
            CallMethodOnPlugin(plugin => plugin.CallPush(), "CallPush");
        }

        public static void ReactToCursorDrag()
        {
            CallMethodOnPlugin(plugin => plugin.CallDrag(), "CallDrag");
        }

        public static void ReactToCursorMove()
        {
            if (FlatRedBall.FlatRedBallServices.Game.IsActive)
            {
                CallMethodOnPlugin(plugin => plugin.CallMouseMove(), "CallMouseMove");
            }
        }

        public static void ReactToCursorClick()
        {
            if (FlatRedBall.FlatRedBallServices.Game.IsActive)
            {
                CallMethodOnPlugin(plugin => plugin.CallClick(), "CallClick");
            }
        }

        public static void ReactToCursorRightClick()
        {
            if (FlatRedBall.FlatRedBallServices.Game.IsActive)
            {
                CallMethodOnPlugin(plugin => plugin.CallRightClick(), "CallRightClick");
            }
        }

        public static void ReactToElementLoad()
        {
            CallMethodOnPlugin(plugin => plugin.CallElementLoaded(), "CallElementLoaded");
        }

        public static void BeforeElementRemoved()
        {
            CallMethodOnPlugin(plugin => plugin.CallBeforeElementRemoved(), "CallBeforeElementRemoved");

        }

        public static void ReactToElementHighlight()
        {
            CallMethodOnPlugin(plugin => plugin.CallElementHiglight(), "CallElementHighlight");
        }

		public static void ReactToCursorMiddleScroll()
		{
            CallMethodOnPlugin(plugin => plugin.CallMiddleScroll(), "CallMiddleScroll");
        }


        public static void ReactToResolutionChange()
        {
            CallMethodOnPlugin(plugin => plugin.CallResolutionChange(), "CallResolutionChange");
        }

		public static void ReactToUpdate()
		{
			foreach (PluginManager manager in mInstances)
			{
				foreach (GlueViewPlugin plugin in manager.Plugins)
				{
					PluginContainer container = manager.mPluginContainers[plugin];

					if (container.IsEnabled)
					{
						try
						{
							plugin.CallUpdate();
						}
						catch (Exception e)
						{
							container.Fail(e, "Failed in CallUpdate");
						}
					}
				}
			}
		}

        static void CallMethodOnPlugin(Action<GlueViewPlugin> methodToCall, string methodName)
        {
            foreach (PluginManager manager in mInstances)
            {
                foreach (var plugin in manager.Plugins)
                {
                    PluginContainer container = manager.PluginContainers[plugin];

                    if (container.IsEnabled)
                    {
                        try
                        {
                            methodToCall(plugin);
                        }
                        catch (Exception e)
                        {
                            container.Fail(e, "Failed in " + methodName);
                        }
                    }
                }
            }
        }
    }
}
