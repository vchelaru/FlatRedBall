using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        PointEditingViewModel ViewModel;

        public override void StartUp()
        {
            this.ReactToItemSelectHandler += HandleItemSelected;

            //this.ReactToChangedPropertyHandler += HandlePropertyChanged;

            InitializeTab();
        }

        // Vic asks - why do we need this? Who else will modify points?
        // This causes infinite recursive calls.
        //private void HandlePropertyChanged(string changedMember, object oldValue, GlueElement glueElement)
        //{
        //    var namedObjectSave = GlueState.Self.CurrentNamedObjectSave;
        //    if(changedMember == "Points" && namedObjectSave != null)
        //    {
        //        RefreshToNamedObject(namedObjectSave);
        //    }
        //}

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
                respondToVmChanges = false;
                {
                    ViewModel.Points.Clear();
                    var pointsToAdd = instructions.Value as List<Vector2>;
                    ViewModel.Points.AddRange(pointsToAdd);
                }
                respondToVmChanges = true;

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

            ViewModel = new PointEditingViewModel();
            ViewModel.PropertyChanged += HandleDataChanged;
            pointEditControl.DataContext = ViewModel;

            mTab = this.CreateTab(pointEditControl, "Points");

        }

        bool respondToVmChanges = true;
        private async void HandleDataChanged(object sender, PropertyChangedEventArgs e)
        {
            var shouldRespondToProperty = e.PropertyName == nameof(ViewModel.Points);
            //GlueCommands.Self.GluxCommands.SetVariableOn
            // there is no old value, the object is the same as before so just pass
            // the object:
            var nos = GlueState.Self.CurrentNamedObjectSave;
            if(nos != null && respondToVmChanges && shouldRespondToProperty)
            {
                var newValue = ViewModel.Points.ToList();
                await GlueCommands.Self.GluxCommands.SetVariableOnAsync(
                    nos, "Points", newValue);
            }
        }

    }
}
