using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using RenderingLibrary;
using FlatRedBall.Gui;
using Microsoft.Xna.Framework.Input;
using FlatRedBall.Input;
using FlatRedBall.Math.Geometry;

namespace FlatRedBall.Forms.Controls
{
    public class ListBox : ItemsControl , IInputReceiver
    {
        #region Fields/Properties

        bool doListBoxItemsHaveFocus;
        public bool DoListItemsHaveFocus 
        {
            get => doListBoxItemsHaveFocus;
            private set
            {
                doListBoxItemsHaveFocus = value;

                if (SelectedIndex > -1 && SelectedIndex < ListBoxItemsInternal.Count)
                {
                    ListBoxItemsInternal[SelectedIndex].IsFocused = doListBoxItemsHaveFocus;
                }
            }
        }

        int selectedIndex = -1;

        public Type ListBoxItemGumType
        {
            get => base.ItemGumType;
            set => base.ItemGumType = value;
        }

        public Type ListBoxItemFormsType
        {
            get { return base.ItemFormsType; }
            set { base.ItemFormsType = value; }
        }

        public object SelectedObject
        {
            get
            {
                if (selectedIndex > -1 && selectedIndex < Items.Count)
                {
                    return Items[selectedIndex];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                var index = Items?.IndexOf(value) ?? -1;

                SelectedIndex = index;

                PushValueToViewModel();
            }
        }

        public int SelectedIndex
        {
            get
            {
                return selectedIndex;
            }
            set
            {
                if(value > -1 && value < ListBoxItemsInternal.Count)
                {
                    if(ListBoxItemsInternal[value].IsEnabled)
                    {
                        ListBoxItemsInternal[value].IsSelected = true;
                    }

                    if(SelectedObject != null)
                    {
                        ScrollIntoView(SelectedObject);
                    }
                }
                else if(value == -1)
                {
                    // do we just set it to the value before doing any logic?
                    selectedIndex = -1;

                    var selectionChangedArgs = new SelectionChangedEventArgs();

                    for(int i = 0; i < ListBoxItemsInternal.Count; i++)
                    {
                        var listBoxItem = ListBoxItemsInternal[i];

                        if (listBoxItem.IsSelected)
                        {
                            selectionChangedArgs.RemovedItems.Add(ListBoxItemsInternal[i]);
                            listBoxItem.IsSelected = false;
                        }
                    }

    
                    SelectionChanged?.Invoke(this, selectionChangedArgs);

                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
        }

        public List<Keys> IgnoredKeys => throw new NotImplementedException();

        public bool TakingInput => throw new NotImplementedException();

        public IInputReceiver NextInTabSequence { get; set; }

        /// <summary>
        /// Whether the primary input button (usually the A button) results in the highlighted list box item
        /// being selected and in the ListBox focus moving outside of the individual items.
        /// </summary>
        /// <remarks>
        /// This value is true, but can be changed to false if the A button should perform actions on the highlighted
        /// list box item (such as toggling a check box) without focus being moved out of the individual items.
        /// </remarks>
        public bool LoseListItemFocusOnPrimaryInput { get; set; } = true;

        #endregion

        #region Events

        /// <summary>
        /// Event raised whenever the selection changes. The object parameter is the sender (list box) and the SelectionChangedeventArgs
        /// contains information about the changed selected items.
        /// </summary>
        public event Action<object, SelectionChangedEventArgs> SelectionChanged;
        public event FocusUpdateDelegate FocusUpdate;
        public event Action<Xbox360GamePad.Button> ControllerButtonPushed;
        public event Action<int> GenericGamepadButtonPushed;

        #endregion

        #region Initialize Methods

        public ListBox() : base()
        {
        }

        public ListBox(GraphicalUiElement visual) : base(visual) 
        {
        }

        protected override void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.HandleCollectionChanged(sender, e);

            if(e.Action == NotifyCollectionChangedAction.Remove && 
                (e.OldStartingIndex == selectedIndex ||
                    selectedIndex >= Items.Count))
            {
                // we removed the selected item, so update the VM:

                PushValueToViewModel(nameof(SelectedObject));
                PushValueToViewModel(nameof(SelectedIndex));
            }
            else if(e.Action == NotifyCollectionChangedAction.Reset && selectedIndex >= 0)
            {
                SelectedIndex = -1;
                PushValueToViewModel(nameof(SelectedObject));
            }
        }

        #endregion

        protected override void OnItemSelected(object sender, SelectionChangedEventArgs args)
        {
            base.OnItemSelected(sender, args);

            selectedIndex = ListBoxItemsInternal.IndexOf(sender as ListBoxItem);
            if (selectedIndex > -1)
            {
                args.AddedItems.Add(Items[selectedIndex]);
            }

            SelectionChanged?.Invoke(this, args);

            PushValueToViewModel(nameof(SelectedObject));
            PushValueToViewModel(nameof(SelectedIndex));
        }

        public void ScrollIntoView(object item)
        {
            var itemIndex = Items.IndexOf(item);

            if(itemIndex != -1)
            {
                var visual = ListBoxItemsInternal[itemIndex];

                var visualAsIpso = (IPositionedSizedObject)visual.Visual;
                var visualTop = visualAsIpso.Y;
                var visualBottom = visualAsIpso.Y + visualAsIpso.Height;

                var viewTop = -InnerPanel.Y;
                var viewBottom = -InnerPanel.Y + clipContainer.GetAbsoluteHeight();
                var isAboveView = visualTop < viewTop;
                var isBelowView = visualBottom > viewBottom;

                if(isAboveView)
                {
                    var amountToScroll = visualTop - viewTop;
                    verticalScrollBar.Value += amountToScroll;
                }
                else if(isBelowView)
                {
                    var amountToScroll = visualBottom - viewBottom;
                    verticalScrollBar.Value += amountToScroll;
                }
            }
        }

        protected override void UpdateState()
        {
            var category = "ListBoxCategoryState";
            if (IsEnabled == false)
            {
                if (IsFocused)
                {
                    Visual.SetProperty(category, "DisabledFocused");
                }
                else
                {
                    Visual.SetProperty(category, "Disabled");
                }
            }
            else if (IsFocused)
            {
                Visual.SetProperty(category, "Focused");
            }
            else
            {
                Visual.SetProperty(category, "Enabled");
            }
        }

        #region IInputReceiver Methods

        public void OnFocusUpdate()
        {
            if(DoListItemsHaveFocus)
            {
                DoListItemFocusUpdate();
            }
            else
            {
                DoTopLevelFocusUpdate();
            }

            FocusUpdate?.Invoke(this);
        }

        private void DoListItemFocusUpdate()
        {
            var xboxGamepads = GuiManager.GamePadsForUiControl;


            for (int i = 0; i < xboxGamepads.Count; i++)
            {
                var gamepad = xboxGamepads[i];

                RepositionDirections? direction = null;

                if (gamepad.ButtonRepeatRate(FlatRedBall.Input.Xbox360GamePad.Button.DPadDown) ||
                    gamepad.LeftStick.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Down))
                {
                    direction = RepositionDirections.Down;
                }
                
                if (gamepad.ButtonRepeatRate(FlatRedBall.Input.Xbox360GamePad.Button.DPadRight) ||
                    gamepad.LeftStick.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Right))
                {
                    direction = RepositionDirections.Right;
                }

                if (gamepad.ButtonRepeatRate(FlatRedBall.Input.Xbox360GamePad.Button.DPadUp) ||
                    gamepad.LeftStick.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Up))
                {
                    direction = RepositionDirections.Up;
                }

                if (gamepad.ButtonRepeatRate(FlatRedBall.Input.Xbox360GamePad.Button.DPadLeft) ||
                    gamepad.LeftStick.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Left))
                {
                    direction = RepositionDirections.Left;
                }


                var pressedButton = (LoseListItemFocusOnPrimaryInput && gamepad.ButtonPushed(FlatRedBall.Input.Xbox360GamePad.Button.A)) ||
                    gamepad.ButtonPushed(FlatRedBall.Input.Xbox360GamePad.Button.B);

                DoListItemFocusUpdate(direction, pressedButton);
            }

            var genericGamePads = GuiManager.GenericGamePadsForUiControl;

            for (int i = 0; i < genericGamePads.Count; i++)
            {
                var gamepad = genericGamePads[i];

                RepositionDirections? direction = null;

                if(gamepad.DPadRepeatRate(Xbox360GamePad.DPadDirection.Down) ||
                    (gamepad.AnalogSticks.Length > 0 && gamepad.AnalogSticks[0].AsDPadPushedRepeatRate(Xbox360GamePad.DPadDirection.Down)))
                {
                    direction = RepositionDirections.Down;
                }

                if (gamepad.DPadRepeatRate(Xbox360GamePad.DPadDirection.Right) ||
                    (gamepad.AnalogSticks.Length > 0 && gamepad.AnalogSticks[0].AsDPadPushedRepeatRate(Xbox360GamePad.DPadDirection.Right)))
                {
                    direction = RepositionDirections.Right;
                }

                if(gamepad.DPadRepeatRate(Xbox360GamePad.DPadDirection.Up) ||
                    (gamepad.AnalogSticks.Length > 0 && gamepad.AnalogSticks[0].AsDPadPushedRepeatRate(Xbox360GamePad.DPadDirection.Up)))
                {
                    direction = RepositionDirections.Up;
                }
                
                if(gamepad.DPadRepeatRate(Xbox360GamePad.DPadDirection.Left) ||
                    (gamepad.AnalogSticks.Length > 0 && gamepad.AnalogSticks[0].AsDPadPushedRepeatRate(Xbox360GamePad.DPadDirection.Left)))
                {
                    direction = RepositionDirections.Left;
                }

                var inputDevice = gamepad as IInputDevice;

                var pressedButton = 
                    (LoseListItemFocusOnPrimaryInput && inputDevice.DefaultPrimaryActionInput.WasJustPressed) || 
                    inputDevice.DefaultBackInput.WasJustPressed;

                DoListItemFocusUpdate(direction, pressedButton);

            }

        }

        private int? GetListBoxIndexAt(float x, float y)
        {
            for (int i = 0; i < ListBoxItemsInternal.Count; i++)
            {
                ListBoxItem listBoxItem = ListBoxItemsInternal[i];
                var item = listBoxItem.Visual;
                if (item.Visible)
                {
                    var widthHalf = item.GetAbsoluteWidth() / 2.0f;
                    var heightHalf = item.GetAbsoluteHeight() / 2.0f;

                    var absoluteX = item.GetAbsoluteCenterX();
                    var absoluteY = item.GetAbsoluteCenterY();

                    if (x > absoluteX - widthHalf && x < absoluteX + widthHalf &&
                        y > absoluteY - heightHalf && y < absoluteY + heightHalf)
                    {
                        return i;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// The additional offset to check when attempting to find a control when performing DPad navigation. This value is added
        /// to the InnerPanel's StackSpacing. Increasing this value is useful if objects in the ListBox are not of uniform size, but
        /// if the value is too large then navigation may skip rows or columns.
        /// </summary>
        public float AdditionalOffsetToCheckForDPadNavigation { get; set; } = 4;

        private void DoListItemFocusUpdate(RepositionDirections? direction, bool pressedButton)
        {
            var wraps = InnerPanel.WrapsChildren;
            var handledByWrapping = false;
            if(wraps && direction != null)
            {
                if(SelectedIndex > -1 && SelectedIndex < ListBoxItems.Count)
                {
                    var currentSelection = this.ListBoxItemsInternal[SelectedIndex].Visual;

                    var offsetToCheck = InnerPanel.StackSpacing + AdditionalOffsetToCheckForDPadNavigation;

                    float xCenter = currentSelection.GetAbsoluteX() + currentSelection.GetAbsoluteWidth() / 2.0f;
                    float yCenter = currentSelection.GetAbsoluteY() + currentSelection.GetAbsoluteHeight() / 2.0f;

                    float x = xCenter;
                    float y = yCenter;
                    switch(direction.Value)
                    {
                        case RepositionDirections.Left:
                            x = currentSelection.GetAbsoluteLeft() - offsetToCheck;
                            break;

                        case RepositionDirections.Right:
                            x = currentSelection.GetAbsoluteRight() + offsetToCheck;
                            break;

                        case RepositionDirections.Up:
                            y = currentSelection.GetAbsoluteTop() - offsetToCheck;
                            break;

                        case RepositionDirections.Down:
                            y = currentSelection.GetAbsoluteBottom() + offsetToCheck;
                            break;
                    }

                    var index = GetListBoxIndexAt(x, y);

                    if(index != null)
                    {
                        SelectedIndex = index.Value;
                        this.ListBoxItemsInternal[SelectedIndex].IsFocused = true;
                    }
                }
                else
                {
                    SelectedIndex = 0;
                    this.ListBoxItemsInternal[SelectedIndex].IsFocused = true;
                }

                // The fallback behavior is kinda confusing, so let's just handle it if it wraps.
                // This could change in the future?
                handledByWrapping = true;
                
            }


            if(!handledByWrapping)
            {
                if (direction == RepositionDirections.Down || direction == RepositionDirections.Right)
                {
                    if (Items.Count > 0)
                    {
                        if (SelectedIndex < 0 && Items.Count > 0)
                        {
                            SelectedIndex = 0;
                        }
                        else if (SelectedIndex < Items.Count - 1)
                        {
                            SelectedIndex++;
                        }
                        this.ListBoxItemsInternal[SelectedIndex].IsFocused = true;
                    }
                }
                else if (direction == RepositionDirections.Up || direction == RepositionDirections.Left)
                {
                    if (Items.Count > 0)
                    {
                        if (SelectedIndex < 0 && Items.Count > 0)
                        {
                            SelectedIndex = 0;
                        }
                        else if (SelectedIndex > 0)
                        {
                            SelectedIndex--;
                        }

                        this.ListBoxItemsInternal[SelectedIndex].IsFocused = true;
                    }
                }
            }

            if (pressedButton)
            {
                DoListItemsHaveFocus = false;
            }
        }

        private void DoTopLevelFocusUpdate()
        {
            var gamepads = GuiManager.GamePadsForUiControl;

            for (int i = 0; i < gamepads.Count; i++)
            {
                var gamepad = gamepads[i];

                HandleGamepadNavigation(gamepad);

                if (gamepad.ButtonPushed(FlatRedBall.Input.Xbox360GamePad.Button.A))
                {
                    DoListItemsHaveFocus = true;
                }

                void RaiseIfPushedAndEnabled(FlatRedBall.Input.Xbox360GamePad.Button button)
                {
                    if (IsEnabled && gamepad.ButtonPushed(button))
                    {
                        ControllerButtonPushed?.Invoke(button);
                    }
                }

                RaiseIfPushedAndEnabled(Xbox360GamePad.Button.B);
                RaiseIfPushedAndEnabled(Xbox360GamePad.Button.X);
                RaiseIfPushedAndEnabled(Xbox360GamePad.Button.Y);
                RaiseIfPushedAndEnabled(Xbox360GamePad.Button.Start);
                RaiseIfPushedAndEnabled(Xbox360GamePad.Button.Back);
                RaiseIfPushedAndEnabled(Xbox360GamePad.Button.DPadLeft);
                RaiseIfPushedAndEnabled(Xbox360GamePad.Button.DPadRight);

                RaiseIfPushedAndEnabled(Xbox360GamePad.Button.LeftStickAsDPadLeft);
                RaiseIfPushedAndEnabled(Xbox360GamePad.Button.LeftStickAsDPadRight);
            }

            var genericGamepads = GuiManager.GenericGamePadsForUiControl;

            for (int i = 0; i < genericGamepads.Count; i++)
            {
                var gamepad = genericGamepads[i];

                HandleGamepadNavigation(gamepad);

                if ((gamepad as IInputDevice).DefaultConfirmInput.WasJustPressed)
                {
                    DoListItemsHaveFocus = true;
                }

                if(IsEnabled)
                {
                    for(var buttonIndex = 0; buttonIndex < gamepad.NumberOfButtons; i++)
                    {
                        if(gamepad.ButtonPushed(buttonIndex))
                        {
                            GenericGamepadButtonPushed?.Invoke(buttonIndex);
                        }
                    }
                }
            }

        }

        public void OnGainFocus()
        {
        }

        public void LoseFocus()
        {
            IsFocused = false;

            if(DoListItemsHaveFocus)
            {
                DoListItemsHaveFocus = false;
            }
        }

        public void ReceiveInput()
        {
        }

        public void HandleKeyDown(Keys key, bool isShiftDown, bool isAltDown, bool isCtrlDown)
        {
        }

        public void HandleCharEntered(char character)
        {
        }

        #endregion
    }
}
