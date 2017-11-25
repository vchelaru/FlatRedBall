using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Controls
{
    class ToggleButton : FrameworkElement
    {
        #region Fields/Properties

        private bool _checkedState;

        public bool IsChecked
        {
            get => _checkedState;
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
