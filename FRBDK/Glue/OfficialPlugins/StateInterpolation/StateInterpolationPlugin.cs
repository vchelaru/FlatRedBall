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


namespace OfficialPlugins.StateInterpolation
{
    [Export(typeof(PluginBase))]
    public class StateInterpolationPlugin : PluginBase
    {
        List<string> filesNeeded = new List<string>
        {
            "Back",
            "Bounce",
            "Circular",
            "Cubic",
            "Elastic",
            "Exponential",
            "Linear",
            "Quadratic",
            "Quartic",
            "Quintic",
            "Sinusoidal",
            "Tweener"
        };


        MenuStrip mMenuStrip;
        ToolStripMenuItem mStateInterpolationEnabledMenuItem;

        public bool Enabled
        {
            get
            {
                return mStateInterpolationEnabledMenuItem.Checked;
            }
            set
            {
                mStateInterpolationEnabledMenuItem.Checked = value;
                UpdateMenuItemText();
            }
        }

        public override string FriendlyName
        {
            get { return "State Interpolation Plugin"; }
        }

        public override Version Version
        {
            get { return new Version(1, 0); }
        }

        public override void StartUp()
        {
            this.InitializeMenuHandler += HandleInitializeMenu;


            // Add the events here:
        }

        private void HandleInitializeMenu(MenuStrip menuStrip)
        {
            mMenuStrip = menuStrip;
            ToolStripMenuItem itemToAddTo = mMenuStrip.GetItem("Plugins");


            mStateInterpolationEnabledMenuItem = new ToolStripMenuItem("", null, HandleClickMenuItem);
            UpdateMenuItemText();



            mStateInterpolationEnabledMenuItem.CheckOnClick = true;
            itemToAddTo.DropDownItems.Add(mStateInterpolationEnabledMenuItem);

        }

        void HandleClickMenuItem(object sender, EventArgs e)
        {
            UpdateMenuItemText();

            UpdateCodeInProjectPresence();
        }

        private void UpdateCodeInProjectPresence()
        {
            bool succeeded = true;

            foreach (string file in filesNeeded)
            {
                string fullFileName = null;
                try
                {

                    string executableDirectory = this.PluginFolder;

                    fullFileName = executableDirectory + "Plugins/StateInterpolation/" + file + ".cs";



                    string destinationDirectory = ProjectManager.ProjectRootDirectory + "StateInterpolation/";
                    string destination = destinationDirectory + file + ".cs";
                    Directory.CreateDirectory(destinationDirectory);

                    System.IO.File.Copy(fullFileName, destinationDirectory, true);
                }
                catch (Exception e)
                {
                    succeeded = false;

                    MessageBox.Show("Could not copy the file " + fullFileName + "\n\n" + e.ToString());
                    break;

                }
            }


            if (succeeded)
            {
                // Add these files to the project and any synced project

            }

        }

        private void UpdateMenuItemText()
        {
            if (Enabled)
            {
                mStateInterpolationEnabledMenuItem.Text = "Advanced State Interpolation Enabled";
            }
            else
            {
                mStateInterpolationEnabledMenuItem.Text = "Advanced State Interpolation Disabled";
            }
        }

        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason)
        {
            ToolStripMenuItem toRemoveFrom = mMenuStrip.GetItem("Plugins");

            toRemoveFrom.DropDownItems.Remove(mStateInterpolationEnabledMenuItem);
            // Remove UI here
            return true;
        }
    }
}
