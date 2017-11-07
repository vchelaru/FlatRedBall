using FlatRedBall.Forms.GumExtensions;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Controls
{
    public class FrameworkElement
    {
        /// <summary>
        /// The height in pixels. This is a calculated value considering HeightUnits and Height.
        /// </summary>
        public float ActualHeight => Visual.GetAbsoluteHeight();
        /// <summary>
        /// The width in pixels. This is a calculated value considering WidthUnits and Width;
        /// </summary>
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

        public float ActualX => Visual.GetLeft();
        public float ActualY => Visual.GetTop();

        public float X
        {
            get { return Visual.X; }
            set { Visual.X = value; }
        }
        public float Y
        {
            get { return Visual.Y; }
            set { Visual.Y = value; }
        }

        public virtual bool IsEnabled
        {
            get;
            set;
        } = true;

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
            set
            {
#if DEBUG
                if(value == null)
                {
                    throw new ArgumentNullException("Visual cannot be assigned to null");
                }
#endif
                visual = value; ReactToVisualChanged();
            }
        }

        protected virtual void ReactToVisualChanged()
        {

        }
    }
}
