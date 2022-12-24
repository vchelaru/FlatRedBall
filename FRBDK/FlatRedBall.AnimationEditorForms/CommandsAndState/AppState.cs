using FlatRedBall.Content.AnimationChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.AnimationEditorForms.CommandsAndState
{
    public class AppState : Singleton<AppState>
    {
        /// <summary>
        /// The absolute path of the project which this .achx is a part of.  If this is not null/empty, then
        /// the tool won't ask the user to copy any files which are part of this project.
        /// </summary>
        public string ProjectFolder
        {
            get;
            set;
        }

        public UnitType UnitType
        {
            get
            {
                return PropertyGridManager.Self.UnitType;
            }
            set
            {
                PropertyGridManager.Self.UnitType = value;

                if(MainControl.Self.UnitTypeComboBox.SelectedValue == null || ((UnitType)MainControl.Self.UnitTypeComboBox.SelectedValue) != value)
                {
                    MainControl.Self.UnitTypeComboBox.SelectedItem = value;
                }

            }
        }

        public int WireframeZoomValue
        {
            get => WireframeManager.Self.ZoomValue ;
            set
            {
                WireframeManager.Self.ZoomValue = value;
            }
        }

        public bool IsSnapToGridChecked
        {
            // View model maybe hasn't been assigned yet...
            get => WireframeManager.Self.WireframeEditControlsViewModel?.IsSnapToGridChecked == true;
            set => WireframeManager.Self.WireframeEditControlsViewModel.IsSnapToGridChecked = value;
        }

        public int GridSize
        {
            get => WireframeManager.Self.WireframeEditControlsViewModel.GridSize;
            set => WireframeManager.Self.WireframeEditControlsViewModel.GridSize = value;
        }

        public AnimationFrameSave CurrentFrame
        {
            get => SelectedState.Self.SelectedFrame;
        }

    }
}
