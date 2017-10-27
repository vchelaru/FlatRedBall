using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Controls
{
    public class FrameworkElement
    {
        public float ActualHeight => Visual.GetAbsoluteHeight();
        public float ActualWidth => Visual.GetAbsoluteWidth();

        public float Height
        {
            get { return Visual.Height; }
            set { Visual.Height = value; }
        }

        public float Width
        {
            get { return Visual.Width; }
            set { Visual.Width = value; }
        }

        public bool IsEnabled { get; set; }

        public bool IsMouseOver { get; set; }

        public bool IsVisible
        {
            get { return Visual.Visible; }
            set { Visual.Visible = value; }
        }

        public string Name
        {
            get { return Visual.Name; }
            set { Visual.Name = value; }
        }

        GraphicalUiElement visual;
        public GraphicalUiElement Visual
        {
            get { return visual; }
            set { visual = value; ReactToVisualChanged(); }
        }

        protected virtual void ReactToVisualChanged()
        {

        }
    }
}
