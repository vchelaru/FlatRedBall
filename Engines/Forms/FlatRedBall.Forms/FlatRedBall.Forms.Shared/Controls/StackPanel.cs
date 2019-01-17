using RenderingLibrary.Graphics;
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
            Visual.ExposeChildrenEvents = true;
            
            UpdateToOrientation();
        }

        public StackPanel() : 
            base(new global::Gum.Wireframe.GraphicalUiElement(new InvisibleRenderable(), null))
        {
            Width = 200;
            Height = 200;
            IsVisible = true;

        }

        private void UpdateToOrientation()
        {
            if(Visual != null)
            {
                if(Orientation == Orientation.Horizontal)
                {
                    Visual.ChildrenLayout = 
                        global::Gum.Managers.ChildrenLayout.LeftToRightStack;
                }
                else
                {
                    Visual.ChildrenLayout =
                        global::Gum.Managers.ChildrenLayout.TopToBottomStack;
                }
            }
        }
    }
}
