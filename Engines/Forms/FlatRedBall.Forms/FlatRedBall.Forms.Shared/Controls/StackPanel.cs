using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Controls
{
    public class StackPanel : FrameworkElement
    {
        Orientation orientation = Orientation.Vertical;
        public Orientation Orientation
        {
            get { return orientation; }
            set
            {
                if(value != orientation)
                {
                    orientation = value;
                    UpdateToOrientation();
                }
            }
        }
        protected override void ReactToVisualChanged()
        {
            base.ReactToVisualChanged();

            UpdateToOrientation();
        }

        private void UpdateToOrientation()
        {
            if(Visual != null)
            {
                if(Orientation == Orientation.Horizontal)
                {
                    Visual.ChildrenLayout = 
                        global::Gum.Managers.ChildrenLayout.TopToBottomStack;
                }
                else
                {
                    Visual.ChildrenLayout =
                        global::Gum.Managers.ChildrenLayout.LeftToRightStack;
                }
            }
        }

        
    }
}
