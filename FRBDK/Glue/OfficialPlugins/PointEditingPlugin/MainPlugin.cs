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
    [Export(typeof(PluginBase))]
    public class MainPlugin : EmbeddedPlugin
    {
        PointEditWindow mPointEditWindow; // This is the control we created
        PluginTab mTab; // This is the tab that will hold our control


        public override void StartUp()
        {
            this.ReactToItemSelectHandler += HandleItemSelected;

            this.ReactToChangedPropertyHandler += HandlePropertyChanged;

            InitializeTab();
        }

        private void HandlePropertyChanged(string changedMember, object oldValue)
        {
            var namedObjectSave = GlueState.Self.CurrentNamedObjectSave;
            if(changedMember == "Points" && namedObjectSave != null)
            {
                RefreshToNamedObject(namedObjectSave);
            }
        }

        private void HandleItemSelected(TreeNode selectedTreeNode)
        {
            NamedObjectSave namedObjectSave = null;

            if (selectedTreeNode != null)
            {
                namedObjectSave = selectedTreeNode.Tag as NamedObjectSave;
            }

            RefreshToNamedObject(namedObjectSave);
        }

        private void RefreshToNamedObject(NamedObjectSave namedObjectSave)
        {
            bool shouldShow = namedObjectSave != null;

            if (shouldShow)
            {
                shouldShow =
                    namedObjectSave.SourceClassType == "Polygon" ||
                    namedObjectSave.SourceClassType == "FlatRedBall.Math.Geometry.Polygon";
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

                base.ShowTab(mTab);
            }
            else
            {
                base.RemoveTab(mTab);
            }
        }

        void InitializeTab()
        {

            mPointEditWindow = new PointEditWindow();
            mPointEditWindow.DataChanged += HandleDataChanged;

            mTab = this.AddToTab(PluginManager.CenterTab, mPointEditWindow, "Points");

            mTab.ClosedByUser += new PluginTab.ClosedByUserDelegate(OnClosedByUser);

            mPointEditWindow.Dock = DockStyle.Fill;

            base.RemoveTab(mTab);

        }

        private void HandleDataChanged(object sender, EventArgs e)
        {
            GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
            GlueCommands.Self.GluxCommands.SaveGlux();
        }
        
        private void OnClosedByUser(object sender)
        {
            // No, don't shut it down, it's embedded
            //PluginManager.ShutDownPlugin(this);
        }
    }
}
