using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using Microsoft.Xna.Framework;
using OfficialPluginsCore.PointEditingPlugin;
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
        PointEditControl pointEditControl; // This is the control we created
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

        private void HandleItemSelected(ITreeNode selectedTreeNode)
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
                var instructions = namedObjectSave.GetCustomVariable("Points");

                if (instructions == null)
                {
                    instructions = new CustomVariableInNamedObject();
                    instructions.Member = "Points";
                    instructions.Value = new List<Vector2>();

                    namedObjectSave.InstructionSaves.Add(instructions);
                }

                pointEditControl.Data = instructions.Value as List<Vector2>;

                mTab.Show();
            }
            else
            {
                mTab?.Hide();
            }
        }

        void InitializeTab()
        {

            pointEditControl = new PointEditControl();
            pointEditControl.DataChanged += HandleDataChanged;

            mTab = this.CreateTab(pointEditControl, "Points");

        }

        private void HandleDataChanged(object sender, EventArgs e)
        {
            //GlueCommands.Self.GluxCommands.SetVariableOn
            // there is no old value, the object is the same as before so just pass
            // the object:
            var nos = GlueState.Self.CurrentNamedObjectSave;
            if(nos != null)
            {
                PluginManager.ReactToNamedObjectChangedValue("Points", nos.GetCustomVariable("Points")?.Value, nos);
            }

            GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
            GlueCommands.Self.GluxCommands.SaveGlux();
        }

    }
}
