using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using FlatRedBall.Gui;
using Gum.DataTypes;
using Gum.Converters;
using System.Collections;
using Microsoft.Xna.Framework.Input;

namespace FlatRedBall.Forms.Controls
{
    public class ComboBox : FrameworkElement, IInputReceiver
    {
        #region Fields/Properties

        ListBox listBox;
        GraphicalUiElement textComponent;
        RenderingLibrary.Graphics.Text coreTextObject;


        public string Text
        {
            get => coreTextObject.RawText;
            set
            {
                coreTextObject.RawText = value;
            }
        }

        public IList Items
        {
            get => listBox.Items;
            set => listBox.Items = value;
        }

        public Type ListBoxItemGumType
        {
            get { return listBox.ListBoxItemGumType; }
            set
            {
#if DEBUG
                if(listBox == null)
                {
                    throw new Exception("Visual must be set before assigning the ListBoxItemType");
                }
#endif
                listBox.ListBoxItemGumType = value;
            }
        }

        public Type ListBoxItemFormsType
        {
            get { return listBox.ListBoxItemFormsType; }
            set
            {
#if DEBUG
                if (listBox == null)
                {
                    throw new Exception("Visual must be set before assigning the ListBoxItemType");
                }
#endif
                listBox.ListBoxItemFormsType = value;
            }
        } 

        public object SelectedObject
        {
            get { return listBox.SelectedObject; }
            set { listBox.SelectedObject = value; }
        }
        public int SelectedIndex
        {
            get { return listBox.SelectedIndex; }
            set { listBox.SelectedIndex = value; }
        }

        public List<Keys> IgnoredKeys => throw new NotImplementedException();

        public bool TakingInput => throw new NotImplementedException();

        public IInputReceiver NextInTabSequence { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        // These values are the list box values before the list box has been expanded.
        // The reason we need these is because the list box may have values (like Width)
        // which depend on the combo box. Normal gum objects automatically handle layout,
        // but the ListBox needs to be detached from its parent ComboBox when it is shown so
        // that it can be moved above all other UI (a sorting issue). But since the ListBox is
        // detached from its parent, we can't have it automatically update its size to the parent.
        // Therefore, the list box showing process is as follows:
        // 1. Apply the values below to the list box (force it to have the same values it had before it was shown)
        // 2. Let the Gum layout engine do its updating
        // 3. Read the absolute x,y,width, and height (this happens in the show function)
        // 4. Detach the list box from its parent and add it with a positive infinity Z so it sorts on top
        // 5. Apply the absolute values to the list box
        // This will result in the list box shown at the proper location and size. Then when the list
        DimensionUnitType listBoxWidthUnits;
        DimensionUnitType listBoxHeightUnits;
        GeneralUnitType listBoxXUnits;
        GeneralUnitType listBoxYUnits;
        float listBoxX;
        float listBoxY;
        float listBoxWidth;
        float listBoxHeight;

        public bool IsDropDownOpen
        {
            get => listBox.IsVisible;
            set
            {
                if(value && IsDropDownOpen == false)
                {
                    ShowListBox();
                }
                else if(!value && IsDropDownOpen)
                {
                    HideListBox();
                }
            }
        }

        #endregion

        #region Events

        public event Action<object, SelectionChangedEventArgs> SelectionChanged;
        public event FocusUpdateDelegate FocusUpdate;

        #endregion

        #region Initialize Methods

        public ComboBox() : base() { }

        public ComboBox(GraphicalUiElement visual) : base(visual) { }

        protected override void ReactToVisualChanged()
        {
            var listBoxInstance = Visual.GetGraphicalUiElementByName("ListBoxInstance");
            textComponent = base.Visual.GetGraphicalUiElementByName("TextInstance");

#if DEBUG
            if (listBoxInstance == null)
            {
                throw new Exception("Gum object must have an object called \"ListBoxInstance\"");
            }

            if(textComponent == null)
            {
                throw new Exception("Gum object must have an object called \"Text\"");
            }
#endif
            // remove it because it's gotta be a "popup"

            Visual.Children.Remove(listBoxInstance);
            listBoxInstance.RemoveFromManagers();

            coreTextObject = textComponent.RenderableComponent as RenderingLibrary.Graphics.Text;

            if(listBoxInstance.FormsControlAsObject == null)
            {
                listBox = new ListBox(listBoxInstance);
            }
            else
            {
                listBox = listBoxInstance.FormsControlAsObject as ListBox;

#if DEBUG
                if(listBox == null)
                {
                    var message = $"The ListBoxInstance Gum component inside the combo box {Visual.Name} is of type {listBoxInstance.FormsControlAsObject.GetType().Name}, but it should be of type ListBox";
                    throw new Exception(message);
                }
#endif
            }


            Visual.Click += this.HandleClick;
            Visual.Push += this.HandlePush;
            Visual.LosePush += this.HandleLosePush;
            Visual.RollOn += this.HandleRollOn;
            Visual.RollOff += this.HandleRollOff;

            listBox.Visual.EffectiveParentGue.RaiseChildrenEventsOutsideOfBounds = true;
            listBox.SelectionChanged += HandleSelectionChanged;
            listBox.ItemClicked += HandleListBoxItemClicked;

            listBox.IsVisible = false;
            Text = null;

            base.ReactToVisualChanged();

            UpdateState();
        }

        #endregion

        #region Event Handler Methods

        private void HandleClick(IWindow window)
        {
            UpdateState();
        }

        private void HandleRollOn(IWindow window)
        {
            UpdateState();
        }

        private void HandleRollOff(IWindow window)
        {
            UpdateState();
        }

        private void HandlePush(IWindow window)
        {
            if(IsDropDownOpen)
            {
                HideListBox();
            }
            else
            {
                ShowListBox();
            }
            
        }

        private void ShowListBox()
        {
            listBox.IsVisible = true;
            // this thing is going to be in front of everything:
            listBox.Visual.Z = float.PositiveInfinity;

            // Adding this to the Visual will force a layout....
            this.Visual.Children.Add(listBox.Visual);
            // ... so grab the absolute values:
            var x = listBox.Visual.AbsoluteX;
            var y = listBox.Visual.AbsoluteY;
            var width = listBox.Visual.GetAbsoluteWidth();
            var height = listBox.Visual.GetAbsoluteHeight();

            listBoxWidthUnits = listBox.Visual.WidthUnits;
            listBoxHeightUnits = listBox.Visual.HeightUnits;
            listBoxXUnits = listBox.Visual.XUnits;
            listBoxYUnits = listBox.Visual.YUnits;

            listBoxX = listBox.Visual.X;
            listBoxY = listBox.Visual.Y;
            listBoxWidth = listBox.Visual.Width;
            listBoxHeight = listBox.Visual.Height;

            // Now that we have the values, detach it:
            this.Visual.Children.Remove(listBox.Visual);

            // and apply the absolutes:
            listBox.Visual.XUnits = GeneralUnitType.PixelsFromSmall;
            listBox.Visual.YUnits = GeneralUnitType.PixelsFromSmall;
            listBox.Visual.WidthUnits = DimensionUnitType.Absolute;
            listBox.Visual.HeightUnits = DimensionUnitType.Absolute;

            listBox.X = x;
            listBox.Y = y;
            listBox.Width = width;
            listBox.Height = height;

            // let's just make sure it's removed
            listBox.Visual.RemoveFromManagers();

            // todo - what if this is on a layer?
            listBox.Visual.AddToManagers(Visual.Managers,
                Visual.Managers.Renderer.MainLayer);

            var rootParent = listBox.Visual.GetParentRoot();

            var parent = Visual.Parent as IWindow;
            var isDominant = false;
            while(parent != null)
            {
                if(GuiManager.DominantWindows.Contains(parent))
                {
                    isDominant = true;
                    break;
                }

                parent = parent.Parent;
            }

            if(isDominant)
            {
                GuiManager.AddDominantWindow(listBox.Visual);
            }

            GuiManager.AddNextPushAction(TryHideFromPush);
            GuiManager.SortZAndLayerBased();

            listBox.RepositionToKeepInScreen();

            UpdateState();
        }

        private void TryHideFromPush()
        {
            var cursor = GuiManager.Cursor;


            var clickedOnThisOrChild =
                cursor.WindowOver == this.Visual ||
                (cursor.WindowOver != null && cursor.WindowOver.IsInParentChain(this.Visual));

            if (clickedOnThisOrChild == false)
            {
                HideListBox();
            }
            else
            {
                GuiManager.AddNextPushAction(TryHideFromPush);
            }
        }

        private void HideListBox()
        {
            if(Visual.Managers != null && listBox.IsVisible)
            {
                listBox.IsVisible = false;
                listBox.Visual.RemoveFromManagers();

                listBox.Visual.XUnits = listBoxXUnits;
                listBox.Visual.YUnits = listBoxYUnits;
                listBox.Visual.WidthUnits = listBoxWidthUnits;
                listBox.Visual.HeightUnits = listBoxHeightUnits;

                listBox.Visual.X = listBoxX;
                listBox.Visual.Y = listBoxY;
                listBox.Visual.Width = listBoxWidth;
                listBox.Visual.Height = listBoxHeight;

                Visual.Managers.Renderer.MainLayer.Remove(listBox.Visual);


                UpdateState();
            }
        }

        private void HandleLosePush(IWindow window)
        {
            UpdateState();
        }

        private void HandleSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            // If we bind the Text, then don't set this here, the binding will take care of it
            var isTextBound = vmPropsToUiProps?.Values.Any(item => item == nameof(Text)) == true;
            if(isTextBound == false)
            {
                coreTextObject.RawText = listBox.SelectedObject?.ToString();
            }

            // Why do we hide the list box here? We don't want to do it if
            // the user uses the arrow keys to change a selection do we?
            //HideListBox();

            PushValueToViewModel(nameof(SelectedObject));
            PushValueToViewModel(nameof(SelectedIndex));

            SelectionChanged?.Invoke(this, args);

            
        }

        private void HandleListBoxItemClicked(object sender, EventArgs args)
        {
            HideListBox();
        }

        #endregion

        #region UpdateTo Methods

        protected override void UpdateState()
        {
            var cursor = GuiManager.Cursor;

            const string category = "ComboBoxCategoryState";

            if (IsEnabled == false)
            {
                Visual.SetProperty(category, "Disabled");
            }
            else if (IsFocused)
            {
                Visual.SetProperty(category, "Focused");
            }
            else if (cursor.WindowOver == Visual)
            {
                if (cursor.WindowPushed == Visual && cursor.PrimaryDown)
                {
                    Visual.SetProperty(category, "Pushed");
                }
                else if (cursor.LastInputDevice != InputDevice.TouchScreen)
                {
                    Visual.SetProperty(category, "Highlighted");
                }
                else
                {
                    Visual.SetProperty(category, "Enabled");
                }
            }
            else
            {
                Visual.SetProperty(category, "Enabled");
            }
        }

        #endregion

        #region IInputReceiver Methods

        public void OnFocusUpdate()
        {
            if(IsDropDownOpen)
            {
                DoOpenDropDownFocusUpdate();
            }
            else
            {
                DoClosedDropDownFocusUpdate();
            }

        }

        private void DoOpenDropDownFocusUpdate()
        {
            for (int i = 0; i < FlatRedBall.Input.InputManager.Xbox360GamePads.Length; i++)
            {
                var gamepad = FlatRedBall.Input.InputManager.Xbox360GamePads[i];

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

                        this.listBox.ListBoxItems[SelectedIndex].IsFocused = true;
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

                        this.listBox.ListBoxItems[SelectedIndex].IsFocused = true;
                    }
                }

                if (gamepad.ButtonPushed(FlatRedBall.Input.Xbox360GamePad.Button.A))
                {
                    // select...

                    // and close...
                    IsDropDownOpen = false;

                    //this.HandleTab(TabDirection.Down, this);
                    //this.HandlePush(null);
                }
                if (gamepad.ButtonReleased(FlatRedBall.Input.Xbox360GamePad.Button.A))
                {
                    //this.HandleClick(null);
                }
            }
        }

        private void DoClosedDropDownFocusUpdate()
        {
            for (int i = 0; i < FlatRedBall.Input.InputManager.Xbox360GamePads.Length; i++)
            {
                var gamepad = FlatRedBall.Input.InputManager.Xbox360GamePads[i];

                if (gamepad.ButtonRepeatRate(FlatRedBall.Input.Xbox360GamePad.Button.DPadDown) ||
                    gamepad.LeftStick.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Down))
                {
                    this.HandleTab(TabDirection.Down, this);
                    // selectindex++
                    //this.HandleTab(TabDirection.Down, this);
                }
                else if (gamepad.ButtonRepeatRate(FlatRedBall.Input.Xbox360GamePad.Button.DPadUp) ||
                    gamepad.LeftStick.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Up))
                {
                    this.HandleTab(TabDirection.Up, this);


                    //this.HandleTab(TabDirection.Up, this);
                    // selectindex--
                }

                if (gamepad.ButtonPushed(FlatRedBall.Input.Xbox360GamePad.Button.A))
                {
                    // select...

                    // and close...
                    IsDropDownOpen = IsDropDownOpen = true;

                    //this.HandleTab(TabDirection.Down, this);
                    //this.HandlePush(null);
                    if(SelectedIndex > -1 && SelectedIndex < this.Items.Count)
                    {
                        this.listBox.ListBoxItems[SelectedIndex].IsFocused = true;
                    }
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
            IsFocused = false;
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
