using FlatRedBall.Forms.GumExtensions;
using FlatRedBall.Gui;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace FlatRedBall.Forms.Controls
{
    public class FrameworkElement
    {
        #region Fields/Properties

        Dictionary<string, string> vmPropsToUiProps = new Dictionary<string, string>();

        object mInheritedBindingContext;
        internal object InheritedBindingContext
        {
            get => mInheritedBindingContext;
            set
            {
                if (value != EffectiveBindingContext)
                {
                    var oldBindingContext = EffectiveBindingContext;
                    if (oldBindingContext is INotifyPropertyChanged oldViewModel)
                    {
                        oldViewModel.PropertyChanged -= HandleViewModelPropertyChanged;
                    }
                    mInheritedBindingContext = value;

                    if (EffectiveBindingContext is INotifyPropertyChanged viewModel)
                    {
                        viewModel.PropertyChanged += HandleViewModelPropertyChanged;

                        foreach (var vmProperty in vmPropsToUiProps.Keys)
                        {
                            UpdateToVmProperty(vmProperty);
                        }
                    }

                    UpdateChildrenInheritedBindingContext(this.Visual.Children);
                }
            }
        }

        object mBindingContext;
        public object BindingContext
        {
            get => EffectiveBindingContext;
            set
            {
                if(value != EffectiveBindingContext)
                {
                    var oldBindingContext = EffectiveBindingContext;
                    if (oldBindingContext is INotifyPropertyChanged oldViewModel)
                    {
                        oldViewModel.PropertyChanged -= HandleViewModelPropertyChanged;
                    }
                    mBindingContext = value;

                    if (EffectiveBindingContext is INotifyPropertyChanged viewModel)
                    {
                        viewModel.PropertyChanged += HandleViewModelPropertyChanged;

                        foreach (var vmProperty in vmPropsToUiProps.Keys)
                        {
                            UpdateToVmProperty(vmProperty);
                        }
                    }

                    UpdateChildrenInheritedBindingContext(this.Visual.Children);
                }
                
            }
        }

        object EffectiveBindingContext => mBindingContext ?? InheritedBindingContext;

        private void UpdateChildrenInheritedBindingContext(IEnumerable<IRenderableIpso> children)
        {
            foreach(var child in children)
            {
                if(child is GraphicalUiElement gue && 
                    gue.FormsControlAsObject is FrameworkElement frameworkElement)
                {
                    frameworkElement.InheritedBindingContext = this.BindingContext;
                }

                UpdateChildrenInheritedBindingContext(child.Children);
            }
        }




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

        /// <summary>
        /// The X position of the left side of the element in pixels.
        /// </summary>
        public float ActualX => Visual.GetLeft();

        /// <summary>
        /// The Y position of the top of the element in pixels (positive Y is down).
        /// </summary>
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

        bool isEnabled = true;
        public virtual bool IsEnabled
        {
            get => isEnabled;
            set
            {
                if(isEnabled != value)
                {
                    isEnabled = value;
                    Visual.Enabled = value;
                }
            }
        }

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

        /// <summary>
        /// Contains the default association between Forms Controls and Gum Runtime Types. 
        /// This dictionary enabled forms controls (like TextBox) to automatically create their own visuals.
        /// The key in the dictionary is the type of Forms control.
        /// </summary>
        /// <remarks>
        /// This dictionary simplifies working with FlatRedBall.Forms in code. It allows one piece of code (which may be generated
        /// by Glue) to associate the Forms controls with a Gum runtime type. Once this association is made, controls can be created without
        /// specifying a gum runtime. For example:
        /// var button = new Button();
        /// button.Visual.AddToManagers();
        /// button.Click += HandleButtonClick;
        /// </remarks>
        /// <example>
        /// FrameworkElement.DefaultFormsComponents[typeof(FlatRedBall.Forms.Controls.Button)] = 
        ///     typeof(ProjectName.GumRuntimes.LargeMenuButtonRuntime);
        /// </example>
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
#if UWP
                var baseType = type.GetTypeInfo().BaseType;
#else
                var baseType = type.BaseType;
#endif
                if (baseType == typeof(object))
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

        public void AddChild(FrameworkElement child)
        {
            if(child.Visual == null)
            {
                throw new InvalidOperationException("The child must have a Visual before being added to the parent");
            }
            if(this.Visual == null)
            {
                throw new InvalidOperationException("This must have its Visual set before having children added");
            }

            child.Visual.Parent = this.Visual;
        }

        protected bool GetIfIsOnThisOrChildVisual(Gui.Cursor cursor)
        {
            var isOnThisOrChild =
                cursor.WindowOver == this.Visual ||
                (cursor.WindowOver != null && cursor.WindowOver.IsInParentChain(this.Visual));

            return isOnThisOrChild;
        }

        public void RepositionToKeepInScreen()
        {
#if DEBUG
            if(Visual == null)
            {
                throw new InvalidOperationException("Visual hasn't yet been set");
            }
            if(Visual.Parent != null)
            {
                throw new InvalidOperationException("This cannot be moved to keep in screen because it depends on its parent's position");
            }
#endif
            var cameraTop = 0;
            var cameraBottom = Renderer.Self.Camera.ClientHeight;
            var cameraLeft = 0;
            var cameraRight = Renderer.Self.Camera.ClientWidth;

            var amountXToShift = 0;
            var amountYToShift = 0;

            var thisBottom = this.Visual.AbsoluteY + this.Visual.GetAbsoluteHeight();
            if (thisBottom > cameraBottom)
            {
                // assume absolute positioning (for now?)
                this.Y -= (thisBottom - cameraBottom);
            }
        }

        protected virtual void ReactToVisualChanged()
        {

        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var vmPropertyName = e.PropertyName;
            var updated = UpdateToVmProperty(vmPropertyName);
            //if (updated)
            //{
            //    this.EffectiveManagers?.InvalidateSurface();
            //}
        }

        public void SetBinding(string uiProperty, string vmProperty)
        {
            if(vmPropsToUiProps.ContainsKey(vmProperty))
            {
                vmPropsToUiProps.Remove(vmProperty);
            }
            vmPropsToUiProps.Add(vmProperty, uiProperty);
        }

        private bool UpdateToVmProperty(string vmPropertyName)
        {
            var updated = false;
            if (vmPropsToUiProps.ContainsKey(vmPropertyName))
            {
                var vmProperty = EffectiveBindingContext.GetType().GetProperty(vmPropertyName);
                if (vmProperty == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not find property {vmPropertyName} in {mBindingContext.GetType()}");
                }
                else
                {
                    var vmValue = vmProperty.GetValue(EffectiveBindingContext, null);

                    var uiProperty = this.GetType().GetProperty(vmPropsToUiProps[vmPropertyName]);

                    if(uiProperty == null)
                    {
                        throw new Exception($"The type {this.GetType()} does not have a property {vmPropsToUiProps[vmPropertyName]}");
                    }

                    if (uiProperty.PropertyType == typeof(string))
                    {
                        uiProperty.SetValue(this, vmValue?.ToString(), null);
                    }
                    else
                    {
                        uiProperty.SetValue(this, vmValue, null);
                    }
                    updated = true;
                }
            }
            return updated;
        }

        protected void PushValueToViewModel([CallerMemberName]string uiPropertyName = null)
        {

            var kvp = vmPropsToUiProps.FirstOrDefault(item => item.Value == uiPropertyName);

            if(kvp.Value == uiPropertyName)
            {
                var vmPropName = kvp.Key;

                var vmProperty = EffectiveBindingContext?.GetType().GetProperty(vmPropName);

                if(vmProperty != null)
                {
                    var uiProperty = this.GetType().GetProperty(uiPropertyName);
                    if(uiProperty != null)
                    {
                        var uiValue = uiProperty.GetValue(this, null);

                        vmProperty.SetValue(EffectiveBindingContext, uiValue, null);
                    }
                }
            }
        }
    }
}
