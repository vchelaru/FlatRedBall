using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace OfficialPlugins.PointEditingPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPointEditingPlugin : EmbeddedPlugin
    {
        #region Fields/Properties

        PointEditControl pointEditControl; // This is the control we created
        PluginTab mTab; // This is the tab that will hold our control
        PointEditingViewModel ViewModel;
        bool IsReactingToGluePropertyChangeEvent = true;

        #endregion

        public override void StartUp()
        {
            this.ReactToItemsSelected += HandleItemsSelected;
            this.ReactToNamedObjectChangedValue += HandleNamedObjectValueChange;
            this.ReactToSelectedSubIndexChanged += HandleSelectedSubIndexChanged;

            InitializeTab();
        }

        // Vic asks - why do we need this? Who else will modify points?
        // This causes infinite recursive calls.
        // Update July 6, 2022
        // This is needed so the UI can update in response to UI changes 
        private void HandleNamedObjectValueChange(string changedMember, object oldValue, NamedObjectSave namedObject)
        {
            if(namedObject != null & changedMember == nameof(Polygon.Points) && IsReactingToGluePropertyChangeEvent)
            {
                RefreshToNamedObject(namedObject);
            }
        }

        private void HandleItemsSelected(List<ITreeNode> list)
        {
            var firstNos = list.FirstOrDefault(item => item.Tag is NamedObjectSave)?.Tag as NamedObjectSave;

            RefreshToNamedObject(firstNos);
        }

        private void HandleSelectedSubIndexChanged(int? nullable)
        {
            ViewModel.SelectedIndex = nullable ?? -1;
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
                    // if the points have changed (like from live edit) but not in count, then we should only 
                    // update the individual points rather than clearing them:
                    var pointsToAdd = instructions.Value as List<Vector2>;
                    var hasCountChanged = ViewModel.Points.Count != pointsToAdd.Count;
                    if(hasCountChanged)
                    {
                        ViewModel.Points.Clear();
                        ViewModel.Points.AddRange(pointsToAdd);
                    }
                    else
                    {
                        // Changing the points seems to wipe out the selected index, so preserve the selected index:
                        var indexBefore = ViewModel.SelectedIndex;
                        for (int i = 0; i < pointsToAdd.Count; i++)
                        {
                            ViewModel.Points[i] = pointsToAdd[i];
                        }
                        ViewModel.SelectedIndex = indexBefore;
                    }
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
        private void HandleDataChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(ViewModel.Points):
                    //GlueCommands.Self.GluxCommands.SetVariableOn
                    // there is no old value, the object is the same as before so just pass
                    // the object:
                    var nos = GlueState.Self.CurrentNamedObjectSave;
                    if(nos != null && respondToVmChanges)
                    {
                        TaskManager.Self.Add(async () =>
                        {
                            var newValue = ViewModel.Points.ToList();
                            IsReactingToGluePropertyChangeEvent = false;
                            await GlueCommands.Self.GluxCommands.SetVariableOnAsync(
                                nos, "Points", newValue);
                            IsReactingToGluePropertyChangeEvent = true;

                        }, $"Responding to property changed {e.PropertyName}");
                    }

                    break;
                    case nameof(ViewModel.SelectedIndex):
                        GlueState.Self.SelectedSubIndex = ViewModel.SelectedIndex;

                        break;
            }
        }

    }
}
