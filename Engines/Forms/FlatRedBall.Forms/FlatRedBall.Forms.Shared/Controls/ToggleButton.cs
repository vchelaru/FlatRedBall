using FlatRedBall.Forms.Controls.Primitives;
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

        #endregion

        #region Events
        public event EventHandler Checked;

        public event EventHandler Indeterminate;

        public event EventHandler Unchecked;

        #endregion
    }
}
