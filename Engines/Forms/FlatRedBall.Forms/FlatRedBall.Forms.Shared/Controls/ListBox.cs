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

namespace FlatRedBall.Forms.Controls
{
    public class ListBox : ItemsControl , IInputReceiver
    {
        #region Fields/Properties

        protected bool isFocused;
        public override bool IsFocused
        {
            get { return isFocused; }
            set
            {
                isFocused = value && IsEnabled;

                if (isFocused)
                {
                    FlatRedBall.Input.InputManager.InputReceiver = this;
                }

                UpdateState();
            }
        }

        bool DoListItemsHaveFocus { get; set; }

        int selectedIndex = -1;

        public Type ListBoxItemGumType
        {
            get
            {
                return base.ItemGumType;
            }
            set
            {
                base.ItemGumType = value;
            }
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
                var index = Items.IndexOf(value);

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
                if(value > -1 && value < listBoxItems.Count)
                {
                    listBoxItems[value].IsSelected = true;

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

                    for(int i = 0; i < listBoxItems.Count; i++)
                    {
                        var listBoxItem = listBoxItems[i];

                        if (listBoxItem.IsSelected)
                        {
                            selectionChangedArgs.RemovedItems.Add(Items[i]);
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

            selectedIndex = listBoxItems.IndexOf(sender as ListBoxItem);
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
                var visual = listBoxItems[itemIndex];

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
            if (IsEnabled == false)
            {
                Visual.SetProperty("ListBoxCategoryState", "Disabled");
            }
            else if (IsFocused)
            {
                Visual.SetProperty("ListBoxCategoryState", "Focused");
            }
            else
            {
                Visual.SetProperty("ListBoxCategoryState", "Enabled");
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
        }

        private void DoListItemFocusUpdate()
        {
            for (int i = 0; i < FlatRedBall.Input.InputManager.Xbox360GamePads.Length; i++)
            {
                var gamepad = FlatRedBall.Input.InputManager.Xbox360GamePads[i];

                if (gamepad.ButtonPushed(FlatRedBall.Input.Xbox360GamePad.Button.DPadDown) ||
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
                    }
                    // selectindex++
                    //this.HandleTab(TabDirection.Down, this);
                }
                else if (gamepad.ButtonPushed(FlatRedBall.Input.Xbox360GamePad.Button.DPadUp) ||
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
                    }

                    //this.HandleTab(TabDirection.Up, this);
                    // selectindex--
                }

                if (gamepad.ButtonPushed(FlatRedBall.Input.Xbox360GamePad.Button.A))
                {
                    DoListItemsHaveFocus = false;
                    //this.HandleTab(TabDirection.Down, this);
                    //this.HandlePush(null);
                }
                if (gamepad.ButtonReleased(FlatRedBall.Input.Xbox360GamePad.Button.A))
                {
                    //this.HandleClick(null);
                }
            }

        }

        private void DoTopLevelFocusUpdate()
        {
            for (int i = 0; i < FlatRedBall.Input.InputManager.Xbox360GamePads.Length; i++)
            {
                var gamepad = FlatRedBall.Input.InputManager.Xbox360GamePads[i];

                if (gamepad.ButtonPushed(FlatRedBall.Input.Xbox360GamePad.Button.DPadDown) ||
                    gamepad.LeftStick.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Down))
                {
                    this.HandleTab(TabDirection.Down, this);
                }
                else if (gamepad.ButtonPushed(FlatRedBall.Input.Xbox360GamePad.Button.DPadUp) ||
                    gamepad.LeftStick.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Up))
                {
                    this.HandleTab(TabDirection.Up, this);
                }

                if (gamepad.ButtonPushed(FlatRedBall.Input.Xbox360GamePad.Button.A))
                {
                    DoListItemsHaveFocus = true;
                }
                if (gamepad.ButtonReleased(FlatRedBall.Input.Xbox360GamePad.Button.A))
                {
                    //this.HandleClick(null);
                }
            }

        }

        public void OnGainFocus()
        {
        }

        public void LoseFocus()
        {
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
