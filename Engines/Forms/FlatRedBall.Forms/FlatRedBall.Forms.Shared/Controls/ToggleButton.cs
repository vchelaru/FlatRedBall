using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Controls
{
    public class ToggleButton : FrameworkElement
    {
        #region Fields/Properties

        private bool _checkedState;

        public bool IsChecked
        {
            get
            {
                return _checkedState;
            }
            set
            {
                _checkedState = value;
                UpdateState();
            }
        }

        #endregion

        protected virtual void UpdateState()
        {
            
        }
    }
}
