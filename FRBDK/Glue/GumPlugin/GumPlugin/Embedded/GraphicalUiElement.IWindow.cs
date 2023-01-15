using FlatRedBall.Gui;
using Gum.Wireframe;
using GumCoreShared.FlatRedBall.Embedded;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Gum.Wireframe
{

    public class BindingContextChangedEventArgs : EventArgs
    {
        public object OldBindingContext { get; set; }
    }
    public partial class GraphicalUiElement : FlatRedBall.Gui.Controls.IControl, FlatRedBall.Graphics.Animation.IAnimatable
    {
        class HandledActions
        {
            public bool HandledMouseWheel;
            public bool HandledRollOver;
        }


        public event WindowEvent Click;
        public event WindowEvent ClickNoSlide;
        public event WindowEvent SlideOnClick;
        public event WindowEvent Push;
        public event WindowEvent RemovedAsPushedWindow;


        /// <summary>
        /// Obsolete - this does not work. Use RollOn, RollOf, or RollOver
        /// for events raised when over an object.
        /// </summary>
        [Obsolete("Use RollOn, RollOff, or RollOver")]
        public event WindowEvent DragOver;

        public event WindowEvent RollOn;
        public event WindowEvent RollOff;
        public event WindowEvent RollOver;
        public event WindowEvent EnabledChange;

        public event EventHandler RemovedFromGuiManager;

        public event Action<IWindow, FlatRedBall.Gui.RoutedEventArgs> MouseWheelScroll;
        public event Action<IWindow, FlatRedBall.Gui.RoutedEventArgs> RollOverBubbling;

        /// <summary>
        /// Event which is raised whenever this loses a push. A push occurs when the
        /// cursor is over this window and the left mouse button is pushed. A push is lost
        /// if the left mouse button is released or if the user moves the cursor so that it
        /// is no longer over this while the mouse button is pressed. 
        /// </summary>
        /// <remarks>
        /// LosePush is often used to change the state of a button back to its regular state.
        /// </remarks>
        public event WindowEvent LosePush;

        public bool RaiseChildrenEventsOutsideOfBounds { get; set; } = false;

        public bool HasEvents { get; set; } = true;
        public bool ExposeChildrenEvents { get; set; } = true;

        // Maybe we'll eventually move this out of IWindow implementation into its own file:
        public virtual void AssignReferences()
        {

        }

        bool IsComponentOrInstanceOfComponent()
        {
            if (Tag is Gum.DataTypes.ComponentSave)
            {
                return true;
            }
            else if (Tag is Gum.DataTypes.InstanceSave)
            {
                var instance = Tag as Gum.DataTypes.InstanceSave;

                if (
                    instance.BaseType == "ColoredRectangle" ||

                    // Vic says - a user may want to click on a container like a track, 
                    // so we prob should allow clicks?
                    // Update - no doing this seems to ruin all kinds of UI because containers
                    // steal clicks from their children. We will check if the container has an explicit
                    // event, otherwise, it will pass it along to its children.
                    instance.BaseType == "Container" ||
                    instance.BaseType == "NineSlice" ||

                    instance.BaseType == "Sprite" ||
                    instance.BaseType == "Text")
                {
                    return false;
                }
                else
                {
                    // If we got here, then it's a component
                    return true;
                }
            }
            return false;
        }

        private void CallCustomInitialize()
        {
            this.Click += (window) => CallLosePush();
            this.RollOff += (window) => CallLosePush();
        }

        partial void CustomAddToManagers()
        {
            // need to add even regular components to the GuiManager since they may contain components
            //if (IsComponentOrInstanceOfComponent() && this.Parent == null)
            if (this.Parent == null)
            {
                GuiManager.AddWindow(this);
            }

            (this.FormsControlAsObject as IFrameworkElement)?.CallLoaded();

            if(mContainedObjectAsIpso is IManagedObject containedAsManaged)
            {
                containedAsManaged.AddToManagers();
            }
        }

        partial void CustomRemoveFromManagers()
        {
            // Always remove it - if it's not a part of it, no big deal, FRB can handle that
            //if (IsComponentOrInstanceOfComponent())

            RemoveBindingContextRecursively();

            GuiManager.RemoveWindow(this);

            RemovedFromGuiManager?.Invoke(this, null);

        }

        private void RemoveBindingContextRecursively()
        {
            this.BindingContext = null;
            if (this.Children != null)
            {
                foreach(var child in this.Children)
                {
                    if(child is GraphicalUiElement gue)
                    {
                        gue.RemoveBindingContextRecursively();
                    }
                }
            }
            else
            {
                foreach (var gue in this.mWhatThisContains)
                {
                    gue.RemoveBindingContextRecursively();
                }
            }


        }

        #region IWindow implementation

        public virtual void Activity(FlatRedBall.Camera camera)
        {

        }

        public void CallClick() => Click?.Invoke(this);
        public void CallRemovedAsPushedWindow() => RemovedAsPushedWindow?.Invoke(this);
        public void CallRollOff() => RollOff?.Invoke(this);
        

        public void CallRollOver() => RollOver?.Invoke(this);

        public void CallRollOn() => RollOn?.Invoke(this);

        void CallLosePush() => LosePush?.Invoke(this);

        System.Collections.ObjectModel.ReadOnlyCollection<IWindow> IWindow.Children
        {
            get  => throw new NotImplementedException();
        }

        public void CloseWindow()
        {
            throw new NotImplementedException();
        }

        private bool mEnabled = true;

        public bool Enabled
        {
            get => mEnabled;
            set
            {
                if(value != mEnabled)
                {
                    mEnabled = value;
                    EnabledChange?.Invoke(this);
                }
            }
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<IWindow> FloatingChildren
        {
            get { return null; }
        }

        public bool GetParentVisibility()
        {
            throw new NotImplementedException();
        }

        public bool GuiManagerDrawn
        {
            get
            {
                return false;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool IgnoredByCursor
        {
            get
            {
                return false;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool IsPointOnWindow(float x, float y)
        {
            throw new NotImplementedException();
        }

        public bool MovesWhenGrabbed
        {
            get
            {
                return false;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void OnDragging()
        {
            DragOver?.Invoke(this);
        }

        public void OnLosingFocus()
        {

        }

        public void OnResize()
        {
            throw new NotImplementedException();
        }

        public void OnResizeEnd()
        {
            throw new NotImplementedException();
        }

        public bool OverlapsWindow(IWindow otherWindow)
        {
            throw new NotImplementedException();
        }

        IWindow IWindow.Parent
        {
            get
            {
                return EffectiveParentGue as IWindow;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float ScaleX
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float ScaleY
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void SetScaleTL(float newScaleX, float newScaleY, bool keepTopLeftStatic)
        {
            throw new NotImplementedException();
        }

        public void SetScaleTL(float newScaleX, float newScaleY)
        {
            throw new NotImplementedException();
        }

        // Vic asks - can we kill this? I think we can....
        FlatRedBall.ManagedSpriteGroups.SpriteFrame SpriteFrame
        {
            get
            {
                return null;
            }
            set
            {
                throw new NotImplementedException();
            }
        }


        /// <summary>
        /// Tries to handle cursor activity. If this returns true, then either this element or one of its
        /// children handled the activity. 
        /// </summary>
        /// <param name="cursor">Reference to the cursor object</param>
        /// <returns>Whether this or one of its children handled the cursor activity, blocking other windows from receiving cursor input this frame.</returns>
        /// <remarks>This method will always allow children to handle the activity first, as children draw in front of their parents. Only components
        /// can have UI elements. Standard elements such as Sprites or Containers cannot themselves handle the activity, but they do give their children the
        /// opportunity to handle activity. This is because components (such as buttons) may be part of a container for stacking or other organization.
        /// 
        /// Ultimately this hierarchical logic exists because only the top-most parent is added to the GuiManager, and it is responsible for
        /// giving its children the opportunity to perform cursor-related input. </remarks>
        private bool TryHandleCursorActivity(Cursor cursor, HandledActions handledActions)
        {
            bool handledByChild = false;
            bool handledByThis = false;

            bool isOver = HasCursorOver(cursor);


            // Even though the cursor is over "this", we need to check if the cursor is over any children in case "this" exposes its children events:
            if (isOver && this.ExposeChildrenEvents)
            {
                #region Try handling by children

                // Let's see if any children have the cursor over:
                for (int i = this.Children.Count - 1; i > -1; i--)
                {
                    var child = this.Children[i];
                    if (child is GraphicalUiElement)
                    {
                        var asGue = child as GraphicalUiElement;
                        // Children should always have the opportunity to handle activity,
                        // even if they are not components, because they may contain components as their children

                        // If the child either has events or exposes children events, then give it a chance to handle this activity.

                        if ((asGue.HasEvents || asGue.ExposeChildrenEvents) && asGue.HasCursorOver(cursor))
                        {
                            handledByChild = asGue.TryHandleCursorActivity(cursor, handledActions);

                            if (handledByChild)
                            {
                                break;
                            }
                        }
                    }
                }

                #endregion
            }

            if (isOver)
            {
                if(this.IsComponentOrInstanceOfComponent()
                    ||
                    Push != null ||
                    Click != null ||
                    ClickNoSlide != null ||
                    SlideOnClick != null
                    )
                {
                    if(cursor.PrimaryPush)
                    {
                        int m = 3;
                    }
                    if (!handledByChild)
                    {
                        // Feb. 21, 2018
                        // If not handled by
                        // children, then this
                        // can only handle if this
                        // exposes events. Otherwise,
                        // it shouldn't handle anything
                        // and the parent should be given
                        // the opportunity.
                        // I'm not sure why this was outside
                        // of the if(this.HasEvents)...seems intentional
                        // but it causes problems when the rootmost object
                        // exposes children events but doesn't handle its own
                        // events...
                        //handledByThis = true;

                        if (this.HasEvents)
                        {
                            // moved from above, see comments there...
                            handledByThis = true;
                            cursor.WindowOver = this;

                            if (cursor.PrimaryPush && Enabled)
                            {

                                cursor.WindowPushed = this;

                                if (Push != null)
                                    Push(this);


                                cursor.GrabWindow(this);

                            }

                            if (cursor.PrimaryClick && Enabled) // both pushing and clicking can occur in one frame because of buffered input
                            {
                                if (cursor.WindowPushed == this)
                                {
                                    if (Click != null)
                                    {
                                        Click(this);
                                    }
                                    if (cursor.PrimaryClickNoSlide && ClickNoSlide != null)
                                    {
                                        ClickNoSlide(this);
                                    }

                                    // if (cursor.PrimaryDoubleClick && DoubleClick != null)
                                    //   DoubleClick(this);
                                }
                                else
                                {
                                    if (SlideOnClick != null)
                                    {
                                        SlideOnClick(this);
                                    }
                                }
                            }

                        }
                    }
                    if (HasEvents && Enabled)
                    {
                        if (handledActions.HandledRollOver == false)
                        {
                            FlatRedBall.Gui.RoutedEventArgs args = new FlatRedBall.Gui.RoutedEventArgs();
                            RollOverBubbling?.Invoke(this, args);
                            handledActions.HandledRollOver = args.Handled;
                        }


                        if (cursor.ZVelocity != 0 && handledActions.HandledMouseWheel == false)
                        {
                            FlatRedBall.Gui.RoutedEventArgs args = new FlatRedBall.Gui.RoutedEventArgs();
                            MouseWheelScroll?.Invoke(this, args);
                            handledActions.HandledMouseWheel = args.Handled;
                        }
                    }
                }
            }

            return handledByThis || handledByChild;
        }


        public void TestCollision(Cursor cursor)
        {
            TryHandleCursorActivity(cursor, new HandledActions());
        }

        public void UpdateDependencies()
        {
        }


        public float WorldUnitRelativeX
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float WorldUnitRelativeY
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float WorldUnitX
        {
            get
            {
                return this.AbsoluteX;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float WorldUnitY
        {
            get
            {
                return this.AbsoluteY;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool HasCursorOver(Cursor cursor)
        {
            bool toReturn = false;

            // October 7, 2018
            // If this is a touch screen, then the only way the cursor is over any
            // UI element is if the cursor is being pressed.
            // Even though the finger is technically not over any UI element when 
            // the user lifts it, we still want to consider UI logic so that the click action
            // can apply and events can be raised
            var shouldConsider = cursor.LastInputDevice != InputDevice.TouchScreen ||
                cursor.PrimaryDown ||
                cursor.PrimaryClick;

            if (((IWindow)this).AbsoluteVisible && shouldConsider)
            {
                int cursorScreenX = cursor.ScreenX;
                int cursorScreenY = cursor.ScreenY;
                float worldX;
                float worldY;

                var managers = this.EffectiveManagers;


                // If there are no managers, we an still fall back to the default:
                if(managers == null)
                {
                    managers = global::RenderingLibrary.SystemManagers.Default;
                }

                if(managers != null)
                {
                    // Adjust by viewport values:
                    cursorScreenX -= managers.Renderer.GraphicsDevice.Viewport.X;
                    cursorScreenY -= managers.Renderer.GraphicsDevice.Viewport.Y;

                    var camera = managers.Renderer.Camera;

                    if(this.mLayer != null)
                    {
                        mLayer.ScreenToWorld(
                            camera,
                            cursorScreenX, cursorScreenY,
                            out worldX, out worldY);
                    }
                    else
                    {
                        camera.ScreenToWorld(
                            cursorScreenX, cursorScreenY,
                            out worldX, out worldY);
                    }


                    // for now we'll just rely on the bounds of the GUE itself

                    toReturn = global::RenderingLibrary.IPositionedSizedObjectExtensionMethods.HasCursorOver(
                        this, worldX, worldY);
                }
                else
                {
                    string message =
                        "Could not determine whether the cursor is over this instance because" +
                        "this instance is not on any camera, nor is a default camera set up";
                    throw new Exception(message);
                }
            }

            if (!toReturn && RaiseChildrenEventsOutsideOfBounds)
            {
                for (int i = 0; i < Children.Count; i++)
                {
                    var child = Children[i] as GraphicalUiElement;

                    if (child != null && child.HasCursorOver(cursor))
                    {
                        toReturn = true;
                        break;
                    }
                }
            }

            return toReturn;
        }

        FlatRedBall.Graphics.Layer frbLayer;
        FlatRedBall.Graphics.Layer FlatRedBall.Graphics.ILayered.Layer
        {
            get { return frbLayer; }
        }

        #endregion

        FlatRedBall.Graphics.IVisible FlatRedBall.Graphics.IVisible.Parent
        {
            get { return this.Parent as FlatRedBall.Graphics.IVisible; }
        }

        bool FlatRedBall.Graphics.IVisible.AbsoluteVisible
        {
            get { return mContainedObjectAsIVisible != null && mContainedObjectAsIVisible.AbsoluteVisible; }
        }

        bool FlatRedBall.Graphics.IVisible.IgnoresParentVisibility
        {
            get;
            set;
        }

        public void MoveToFrbLayer(FlatRedBall.Graphics.Layer frbLayer, global::RenderingLibrary.Graphics.Layer gumLayer)
        {
            this.frbLayer = frbLayer;
            if (gumLayer != null)
            {
                this.MoveToLayer(gumLayer);
            }

        }

        public void MoveToFrbLayer(FlatRedBall.Graphics.Layer layer, FlatRedBall.Gum.GumIdb containingScreen)
        {
            var gumLayer = containingScreen.GumLayersOnFrbLayer(layer).FirstOrDefault();



#if DEBUG
            if(gumLayer == null)
            {
                string message = "There is no associated Gum layer for the FRB Layer " + layer + ".\n" +
                    "To fix this, either add the Layer to Glue, or call AddGumLayerToFrbLayer on the GumIdb with " +
                    "a new instance of a Gum layer. To see an example of how to use AddGumLayerToFrbLayer, add a FRB Layer to Glue and look at generated code." ;
                    throw new Exception(message);
            }
#endif

            MoveToFrbLayer(layer, gumLayer);
        }

        /// <summary>
        /// Completely removes the GraphicalUiElement from rendering managers, UI 
        /// </summary>
        public void Destroy()
        {
            this.Parent = null;
            this.ParentGue = null;
            this.RemoveFromManagers();
            StopAnimations();
        }

        public virtual void StopAnimations()
        {
        }

        public virtual FlatRedBall.Gum.Animation.GumAnimation GetAnimation(string animationName)
        {
            return null;
        }

        FlatRedBall.Gum.Animation.GumAnimation currentAnimation;

        void global::FlatRedBall.Graphics.Animation.IAnimatable.PlayAnimation(string animationName)
        {
            currentAnimation = GetAnimation(animationName);

            currentAnimation?.Play();
        }

        bool FlatRedBall.Graphics.Animation.IAnimatable.HasAnimation(string animationName)
        {
            return GetAnimation(animationName) != null;
        }

        bool FlatRedBall.Graphics.Animation.IAnimatable.IsPlayingAnimation(string animationName)
        {
            return currentAnimation != null &&
                currentAnimation.IsPlaying() == true;
        }


        bool FlatRedBall.Graphics.Animation.IAnimatable.DidAnimationFinishOrLoop => currentAnimation?.LastScreenTimeAnimationEnd == FlatRedBall.TimeManager.CurrentScreenTime;

        public FlatRedBall.Glue.StateInterpolation.Tweener InterpolateTo(Gum.DataTypes.Variables.StateSave first, Gum.DataTypes.Variables.StateSave second, double secondsToTake, FlatRedBall.Glue.StateInterpolation.InterpolationType interpolationType, FlatRedBall.Glue.StateInterpolation.Easing easing)
        {
            FlatRedBall.Glue.StateInterpolation.Tweener tweener = new FlatRedBall.Glue.StateInterpolation.Tweener(from: 0, to: 1, duration: (float)secondsToTake, type: interpolationType, easing: easing);
            tweener.Owner = this;
            tweener.PositionChanged = newPosition => this.InterpolateBetween(first, second, newPosition);
            tweener.Start();
            StateInterpolationPlugin.TweenerManager.Self.Add(tweener);
            return tweener;
        }

        void FlatRedBall.Gui.Controls.IControl.SetState(string stateName)
        {
            this.ApplyState(stateName);
        }

        public virtual object FormsControlAsObject { get; set; }

        #region Binding
        // Apr 19 2020:
        // Vic says I could
        // put this in GraphicalUiElement
        // class or in the .IWindow partial.
        // I don't know if I want this in all
        // Gum implementations yet, so I'm going
        // to put it here for now. I may eventually
        // migrate this to the common Gum code but we'll
        // see

        Dictionary<string, string> vmPropsToUiProps = new Dictionary<string, string>();

        object mInheritedBindingContext;
        internal object InheritedBindingContext
        {
            get => mInheritedBindingContext;
            set
            {
                if (value != mInheritedBindingContext)
                {
                    var oldEffectiveBindingContext = EffectiveBindingContext;
                    mInheritedBindingContext = value;
                    HandleBindingContextChangedInternal(oldEffectiveBindingContext);

                }
            }
        }

        object mBindingContext;
        public object BindingContext
        {
            get => EffectiveBindingContext;
            set
            {
                if (value != EffectiveBindingContext)
                {
                    var oldEffectiveBindingContext = EffectiveBindingContext;
                    mBindingContext = value;
                    HandleBindingContextChangedInternal(oldEffectiveBindingContext);
                }

            }
        }

        private void HandleBindingContextChangedInternal(object oldBindingContext)
        {
            if (oldBindingContext is INotifyPropertyChanged oldViewModel)
            {
                oldViewModel.PropertyChanged -= HandleViewModelPropertyChanged;
            }
            if (EffectiveBindingContext is INotifyPropertyChanged viewModel)
            {
                viewModel.PropertyChanged += HandleViewModelPropertyChanged;

            }
            if (EffectiveBindingContext != null)
            {
                foreach (var vmProperty in vmPropsToUiProps.Keys)
                {
                    UpdateToVmProperty(vmProperty);
                }


            }

            var args = new BindingContextChangedEventArgs();
            args.OldBindingContext = oldBindingContext;


            if (this.Children != null)
            {
                // do the default first...
                UpdateChildrenInheritedBindingContext(this.Children, EffectiveBindingContext);
                // ... then overwrite it
                foreach (var child in this.Children)
                {
                    if (child is GraphicalUiElement gue)
                    {
                        if (gue.BindingContextBinding != null)
                        {
                            gue.BindingContextBindingPropertyOwner = EffectiveBindingContext;

                            gue.UpdateToVmProperty(gue.BindingContextBinding);
                        }
                    }
                }
            }
            else
            {
                // Do the default functionalty first...
                UpdateChildrenInheritedBindingContext(this.ContainedElements, EffectiveBindingContext);
                // ... then overwrite it
                foreach (var gue in this.ContainedElements)
                {
                    if (gue.BindingContextBinding != null)
                    {
                        gue.BindingContextBindingPropertyOwner = EffectiveBindingContext;

                        gue.UpdateToVmProperty(gue.BindingContextBinding);
                    }
                }
            }
            BindingContextChanged?.Invoke(this, args);
        }

        public object BindingContextBindingPropertyOwner { get; private set; }
        public string BindingContextBinding { get; private set; }

        public event Action<object, BindingContextChangedEventArgs> BindingContextChanged;

        object EffectiveBindingContext => mBindingContext ?? InheritedBindingContext;

        private static void UpdateChildrenInheritedBindingContext(IEnumerable<IRenderableIpso> children, object effectiveBindingContext)
        {
            foreach (var child in children)
            {
                if (child is GraphicalUiElement gue)
                {
                    if(gue.InheritedBindingContext != effectiveBindingContext)
                    {
                        var effectiveBeforeChange = gue.EffectiveBindingContext;
                        gue.InheritedBindingContext = effectiveBindingContext;
                        if(effectiveBindingContext != gue.EffectiveBindingContext)
                        {
                            // This saves us some processing. If the parent's effective didn't change, then no need
                            // to notify the children
                            UpdateChildrenInheritedBindingContext(child.Children, gue.EffectiveBindingContext);
                        }
                    }
                }

            }
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
            if(uiProperty == nameof(BindingContext))
            {
                BindingContextBinding = vmProperty;
            }
            else
            {
                if (vmPropsToUiProps.ContainsKey(vmProperty))
                {
                    vmPropsToUiProps.Remove(vmProperty);
                }
                // This prevents single UI properties from being bound to multiple VM properties
                if (vmPropsToUiProps.Any(item => item.Value == uiProperty))
                {
                    var toRemove = vmPropsToUiProps.Where(item => item.Value == uiProperty).ToArray();

                    foreach (var kvp in toRemove)
                    {
                        vmPropsToUiProps.Remove(kvp.Key);
                    }
                }


                vmPropsToUiProps.Add(vmProperty, uiProperty);

                if(EffectiveBindingContext != null)
                {
                    UpdateToVmProperty(vmProperty);
                }
            }
        }

        private bool UpdateToVmProperty(string vmPropertyName)
        {
            var updated = false;

            var isBoundToVmProperty = vmPropsToUiProps.ContainsKey(vmPropertyName) ||
                BindingContextBinding == vmPropertyName;

            if (isBoundToVmProperty)
            {

                var bindingContextObjectToUse = BindingContextBinding == vmPropertyName ?
                    BindingContextBindingPropertyOwner : EffectiveBindingContext;

#if UWP
                var vmProperty = bindingContextObjectToUse?.GetType().GetTypeInfo().GetDeclaredProperty(vmPropertyName);
#else
                var vmProperty = bindingContextObjectToUse?.GetType().GetProperty(vmPropertyName);
#endif
                FieldInfo vmField = null;
                
                if (vmProperty == null)
                {
                    vmField = bindingContextObjectToUse?.GetType().GetField(vmPropertyName);
                }


                if (vmProperty == null && vmField == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not find field or property {vmPropertyName} in {bindingContextObjectToUse?.GetType()}");
                }
                else
                {
                    var vmValue = vmField != null ? vmField.GetValue(bindingContextObjectToUse) :
                        vmProperty.GetValue(bindingContextObjectToUse, null);


                    if(vmPropertyName == BindingContextBinding)
                    {
                        BindingContext = vmValue;
                    }
                    else
                    {
                        var uiPropertyName = vmPropsToUiProps[vmPropertyName];
                        PropertyInfo uiProperty = this.GetType().GetProperty(uiPropertyName);

                        if (uiProperty == null)
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
                    }
                    updated = true;
                }
            }

            TryPushBindingContextChangeToChildren(vmPropertyName);

            return updated;
        }

        private void TryPushBindingContextChangeToChildren(string vmPropertyName)
        {
            if (this.Children != null)
            {
                foreach(var child in Children)
                {
                    if(child is GraphicalUiElement gue)
                    {
                        if (gue.BindingContextBinding == vmPropertyName && gue.BindingContextBindingPropertyOwner == EffectiveBindingContext)
                        {
                            gue.UpdateToVmProperty(vmPropertyName);
                        }
                        gue.TryPushBindingContextChangeToChildren(vmPropertyName);
                    }
                }
            }
            else
            {
                foreach(var gue in ContainedElements)
                {
                    if (gue.BindingContextBinding == vmPropertyName && gue.BindingContextBindingPropertyOwner == EffectiveBindingContext)
                    {
                        gue.UpdateToVmProperty(vmPropertyName);
                    }
                    gue.TryPushBindingContextChangeToChildren(vmPropertyName);
                }
            }
        }

        protected void PushValueToViewModel([CallerMemberName]string uiPropertyName = null)
        {

            var kvp = vmPropsToUiProps.FirstOrDefault(item => item.Value == uiPropertyName);

            if (kvp.Value == uiPropertyName)
            {
                var vmPropName = kvp.Key;

                var vmProperty = EffectiveBindingContext?.GetType().GetProperty(vmPropName);

                if (vmProperty != null)
                {
                    var uiProperty = this.GetType().GetProperty(uiPropertyName);
                    if (uiProperty != null)
                    {
                        var uiValue = uiProperty.GetValue(this, null);

                        vmProperty.SetValue(EffectiveBindingContext, uiValue, null);
                    }
                }
            }
        }

#endregion


    }
}
