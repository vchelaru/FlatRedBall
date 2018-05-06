using FlatRedBall.Forms.GumExtensions;
using FlatRedBall.Gui;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Controls
{
    public class FrameworkElement
    {
        #region Fields/Properties

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
            set
            {
#if DEBUG
                if(float.IsNaN(value))
                {
                    throw new Exception("NaN value not supported for FrameworkElement Height");
                }
                if(float.IsPositiveInfinity(value) || float.IsNegativeInfinity(value))
                {
                    throw new Exception();
                }
#endif
                Visual.Height = value;
            }
        }
        public float Width
        {
            get { return Visual.Width; }
            set
            {
#if DEBUG
                if (float.IsNaN(value))
                {
                    throw new Exception("NaN value not supported for FrameworkElement Width");
                }
                if (float.IsPositiveInfinity(value) || float.IsNegativeInfinity(value))
                {
                    throw new Exception();
                }
#endif
                Visual.Width = value;
            }
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

        public static Dictionary<Type, Type> DefaultFormsComponents { get; private set; } = new Dictionary<Type, Type>();

        protected static GraphicalUiElement GetGraphicalUiElementFor(FrameworkElement element)
        {
            var type = element.GetType();
            return GetGraphicalUiElementForFrameworkElement(type);
        }

        private static GraphicalUiElement GetGraphicalUiElementForFrameworkElement(Type type)
        {
            if (DefaultFormsComponents.ContainsKey(type))
            {
                var gumType = DefaultFormsComponents[type];
                var gumConstructor = gumType.GetConstructor(new[] { typeof(bool), typeof(bool) });

                return gumConstructor.Invoke(new object[] { true, false }) as GraphicalUiElement;
            }
            else
            {
                var baseType = type.BaseType;
                if(baseType == typeof(object))
                {
                    throw new Exception($"Could not find default Gum Component for {type}. You can solve this by adding a Gum type for {type} to {nameof(DefaultFormsComponents)}, or constructing the Gum object itself.");
                }
                else
                {
                    return GetGraphicalUiElementForFrameworkElement(baseType);
                }
            }
        }

        #endregion

        public FrameworkElement() 
        {
            Visual = GetGraphicalUiElementFor(this);
        }

        public FrameworkElement(GraphicalUiElement visual)
        {
            if(visual != null)
            {
                this.visual = visual;
                ReactToVisualChanged();

            }
        }


        protected bool GetIfIsOnThisOrChildVisual(Gui.Cursor cursor)
        {
            var isOnThisOrChild =
                cursor.WindowOver == this.Visual ||
                (cursor.WindowOver != null && cursor.WindowOver.IsInParentChain(this.Visual));

            return isOnThisOrChild;
        }


        protected virtual void ReactToVisualChanged()
        {

        }
    }
}
