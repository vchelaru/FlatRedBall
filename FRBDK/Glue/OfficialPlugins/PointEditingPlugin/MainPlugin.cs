using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OfficialPlugins.PointEditingPlugin
{
    [Export(typeof(PluginBase)), Export(typeof(ICenterTab))]
    public class MainPlugin : EmbeddedPlugin, ICenterTab
    {
        PointEditWindow mPointEditWindow; // This is the control we created
        TabControl mContainer; // This is the tab control for all tabs on the left
        PluginTab mTab; // This is the tab that will hold our control


        public override void StartUp()
        {
            this.ReactToItemSelectHandler += HandleItemSelected;


        }

        private void HandleItemSelected(TreeNode selectedTreeNode)
        {
            NamedObjectSave namedObjectSave = null;

            if (selectedTreeNode != null)
            {
                namedObjectSave = selectedTreeNode.Tag as NamedObjectSave;
            }



            bool shouldShow = namedObjectSave != null;

            if (shouldShow)
            {
                shouldShow = namedObjectSave.SourceClassType == "Polygon";

            }

            if (shouldShow)
            {
                var instructions = namedObjectSave.GetInstructionFromMember("Points");

                if (instructions == null)
                {
                    instructions = new CustomVariableInNamedObject();
                    instructions.Member = "Points";
                    instructions.Value = new List<Vector2>();

                    namedObjectSave.InstructionSaves.Add(instructions);
                }

                mPointEditWindow.Data = instructions.Value as List<Vector2>;

                ShowTab();
            }
            else
            {
                HideTab();
            }
        }

        public void InitializeTab(System.Windows.Forms.TabControl tabControl)
        {
            mPointEditWindow = new PointEditWindow();
            mPointEditWindow.DataChanged += HandleDataChanged;

            mTab = new PluginTab();
            mContainer = tabControl;

            mTab.ClosedByUser += new PluginTab.ClosedByUserDelegate(OnClosedByUser);

            mTab.Text = "  Points"; // add spaces to make room for the X to close the plugin
            mTab.Controls.Add(mPointEditWindow);
            mPointEditWindow.Dock = DockStyle.Fill;
        }

        private void HandleDataChanged(object sender, EventArgs e)
        {
            GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
            GlueCommands.Self.GluxCommands.SaveGlux();
        }
        
        private void ShowTab()
        {
            if (mContainer.Controls.Contains(mTab) == false)
            {
                mContainer.Controls.Add(mTab);
            }
        }

        private void HideTab()
        {
            if (mContainer.Controls.Contains(mTab))
            {
                mContainer.Controls.Remove(mTab);
            }

        }

        private void OnClosedByUser(object sender)
        {
            // No, don't shut it down, it's embedded
            //PluginManager.ShutDownPlugin(this);
        }
    }
}
