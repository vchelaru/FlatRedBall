using FlatRedBall.Forms.Controls.Primitives;
using FlatRedBall.Gui;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Controls
{
    public class ToggleButton : ButtonBase
    {
        #region Fields/Properties


        public bool IsThreeState { get; set; }

        private bool? isChecked = false;

        public bool? IsChecked
        {
            get
            {
                return isChecked;
            }
            set
            {
                if(isChecked != value)
                {
                    isChecked = value;
                    UpdateState();

                    if(isChecked == true)
                    {
                        Checked?.Invoke(this, null);
                    }
                    else if(isChecked == false)
                    {
                        Unchecked?.Invoke(this, null);
                    }
                    else if(isChecked == null)
                    {
                        Indeterminate?.Invoke(this, null);
                    }
                }
            }
        }

        #endregion

        #region Events
        /// <summary>
        /// Event raised when the IsChecked value is set to true.
        /// </summary>
        public event EventHandler Checked;

        /// <summary>
        /// Event raised when the IsChecked value is set to null.
        /// </summary>
        public event EventHandler Indeterminate;

        /// <summary>
        /// Event raised when the IsChecked value is set to false;
        /// </summary>
        public event EventHandler Unchecked;

        #endregion

        #region Initialize

        public ToggleButton() : base()
        {
            IsChecked = false;
        }

        public ToggleButton(GraphicalUiElement visual) : base(visual)
        {
            IsChecked = false;
        }

        protected override void ReactToVisualChanged()
        {
            base.ReactToVisualChanged();

            // This forces the initial state to be correct, making sure the button is unchecked
            UpdateState();
        }

        #endregion

        #region Update To Methods

        protected override void UpdateState()
        {
            var cursor = GuiManager.Cursor;

            if (IsEnabled == false)
            {
                SetPropertyConsideringOn("Disabled");
            }
            //else if (HasFocus)
            //{
            //}
            else if (GetIfIsOnThisOrChildVisual(cursor))
            {
                if (cursor.WindowPushed == Visual && cursor.PrimaryDown)
                {
                    SetPropertyConsideringOn("Pushed");
                }
                else
                {
                    SetPropertyConsideringOn("Highlighted");
                }
            }
            else
            {
                SetPropertyConsideringOn("Enabled");
            }
        }

        private void SetPropertyConsideringOn(string stateName)
        {
            if(isChecked == true)
            {
                stateName += "On";
            }
            else
            {
                stateName += "Off";
            }
            Visual.SetProperty("ToggleCategoryState", stateName);

        }

        #endregion

        protected override void OnClick()
        {
            if (IsChecked == true)
            {
                IsChecked = false;
            }
            else // false or indeterminte
            {
                IsChecked = true;
            }
        }
    }
}
