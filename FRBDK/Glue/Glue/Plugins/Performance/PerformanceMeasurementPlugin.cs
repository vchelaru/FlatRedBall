using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.CodeGeneration;
using System.Windows.Forms;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.SaveClasses;
using System.Threading.Tasks;

namespace PluginTestbed.PerformanceMeasurement
{
    [Export(typeof(ICodeGeneratorPlugin)), Export(typeof(IMenuStripPlugin))]
    public class PerformanceMeasurementPlugin : ICodeGeneratorPlugin, IMenuStripPlugin
    {

        StartTimingCodeGenerator mStartTimingCodeGenerator;
        EndTimingCodeGenerator mEndTimingCodeGenerator;
        List<ElementComponentCodeGenerator> mCodeGenerators;

        [Import("GlueCommands")]
        public IGlueCommands GlueCommands
        {
            get;
            set;
        }

        public bool Active
        {
            get
            {
                return mStartTimingCodeGenerator.Active;
            }
            set
            {
                bool valueChanged = value != Active;

                mStartTimingCodeGenerator.Active = value;
                mEndTimingCodeGenerator.Active = value;

                if (valueChanged)
                {
                    GlueCommands.GenerateCodeCommands.GenerateAllCode();
                }
            }


        }


        #region ICodeGeneratorPlugin Members

        public void CodeGenerationStart(IElement element)
        {

        }

        public IEnumerable<ElementComponentCodeGenerator> CodeGeneratorList
        {
            get { return mCodeGenerators; }
        }

        public ElementComponentCodeGenerator CodeGenerator
        {
            get
            {
                return null;
            }
        }

        #endregion

        #region IPlugin Members

        public string FriendlyName
        {
            get { return "Performance Measurement Plugin"; }
        }

        public Version Version
        {
            get { return new Version(1, 0); }
        }

        public string GithubRepoOwner => null;
        public string GithubRepoName => null;
        public bool CheckGithubForNewRelease => false;

        public void StartUp()
        {
        }

        public bool ShutDown(PluginShutDownReason shutDownReason)
        {

            ToolStripMenuItem itemToAddTo = GetItem("Plugins");
            itemToAddTo.DropDownItems.Remove(mMenuItem);
            return true;
        }

        #endregion

        public PerformanceMeasurementPlugin()
        {
            mCodeGenerators = new List<ElementComponentCodeGenerator>();
            mStartTimingCodeGenerator = new StartTimingCodeGenerator();
            mEndTimingCodeGenerator = new EndTimingCodeGenerator();
            mCodeGenerators.Add(mStartTimingCodeGenerator);
            mCodeGenerators.Add(mEndTimingCodeGenerator);
        }

        #region IMenuStripPlugin Members

        ToolStripMenuItem mMenuItem;
        MenuStrip mMenuStrip;

        public event Action<IPlugin, string, string> ReactToPluginEventAction;
        public event Action<IPlugin, string, string> ReactToPluginEventWithReturnAction;

        public void InitializeMenu(System.Windows.Forms.MenuStrip menuStrip)
        {
            mMenuStrip = menuStrip;

            mMenuItem = new ToolStripMenuItem("Performance measurement...");
            ToolStripMenuItem itemToAddTo = GetItem("Plugins");

            itemToAddTo.DropDownItems.Add(mMenuItem);
            mMenuItem.Click += new EventHandler(mMenuItem_Click);

        }

        void mMenuItem_Click(object sender, EventArgs e)
        {
            PerformanceForm form = new PerformanceForm();
            form.Show();
            form.PerformanceMeasurementPlugin = this;
        }

        ToolStripMenuItem GetItem(string name)
        {
            foreach (ToolStripMenuItem item in mMenuStrip.Items)
            {
                if (item.Text == name)
                {
                    return item;
                }
            }
            return null;
        }

        public void HandleEvent(string eventName, string payload)
        {
        }

        public Task<string> HandleEventWithReturn(string eventName, string payload)
        {
            return Task.FromResult((string)null);
        }

        public void HandleEventResponseWithReturn(string payload)
        {
        }

        #endregion
    }
}
