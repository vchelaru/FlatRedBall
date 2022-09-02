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

        #endregion

        #region Events

        /// <summary>
        /// Event raised whenever the selection changes. The object parameter is the sender (list box) and the SelectionChangedeventArgs
        /// contains information about the changed selected items.
        /// </summary>
        public event Action<object, SelectionChangedEventArgs> SelectionChanged;
        public event FocusUpdateDelegate FocusUpdate;
        public event Action<Xbox360GamePad.Button> ControllerButtonPushed;


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
            var gamepads = GuiManager.GamePadsForUiControl;

            for (int i = 0; i < gamepads.Count; i++)
            {
                var gamepad = gamepads[i];

                if (gamepad.ButtonRepeatRate(FlatRedBall.Input.Xbox360GamePad.Button.DPadDown) ||
                    gamepad.LeftStick.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Down))
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
                else if (gamepad.ButtonRepeatRate(FlatRedBall.Input.Xbox360GamePad.Button.DPadUp) ||
                         gamepad.LeftStick.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Up))
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

                if (gamepad.ButtonPushed(FlatRedBall.Input.Xbox360GamePad.Button.A))
                {
                    DoListItemsHaveFocus = false;
                }
                if (gamepad.ButtonPushed(FlatRedBall.Input.Xbox360GamePad.Button.B))
                {
                    DoListItemsHaveFocus = false;
                }

                if (gamepad.ButtonReleased(FlatRedBall.Input.Xbox360GamePad.Button.A))
                {
                    //this.HandleClick(null);
                }
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
