using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using FlatRedBall.Gui;

namespace FlatRedBall.Forms.Controls
{
    public class ComboBox : FrameworkElement
    {
        #region Fields/Properties

        
        ListBox listBox;
        GraphicalUiElement textComponent;
        RenderingLibrary.Graphics.Text coreTextObject;

        public string Text
        {
            get
            {
                return listBox.SelectedObject?.ToString();
            }
            set
            {
                var foundItem = Items.FirstOrDefault(item => item.ToString() == value);

                var index = Items.IndexOf(foundItem);
                listBox.SelectedIndex = index;

            }
        }

        public ObservableCollection<object> Items => listBox.Items;
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

        #endregion

        #region Events

        public event Action<object, SelectionChangedEventArgs> SelectionChanged;

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

            listBox.IsVisible = false;
            Text = null;

            base.ReactToVisualChanged();
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
            if(listBox.IsVisible == false)
            {
                ShowListBox();
            }
            else
            {
                HideListBox();
            }
            
        }

        private void ShowListBox()
        {
            listBox.IsVisible = true;

            GuiManager.AddNextPushAction(TryHideFromPush);

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
            listBox.IsVisible = false;

            UpdateState();
        }

        private void HandleLosePush(IWindow window)
        {
            UpdateState();
        }

        private void HandleSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            coreTextObject.RawText = listBox.SelectedObject?.ToString();

            listBox.IsVisible = false;

            SelectionChanged?.Invoke(this, args);
        }

        #endregion

        #region UpdateTo Methods

        private void UpdateState()
        {
            var cursor = GuiManager.Cursor;

            if (IsEnabled == false)
            {
                Visual.SetProperty("ComboBoxCategoryState", "Disabled");
            }
            //else if (HasFocus)
            //{
            //    Visual.SetProperty("TextBoxCategoryState", "Selected");
            //}
            else if (cursor.WindowOver == Visual)
            {
                if (cursor.WindowPushed == Visual && cursor.PrimaryDown)
                {
                    Visual.SetProperty("ComboBoxCategoryState", "Pushed");
                }
                else
                {
                    Visual.SetProperty("ComboBoxCategoryState", "Highlighted");
                }
            }
            else
            {
                Visual.SetProperty("ComboBoxCategoryState", "Enabled");
            }
        }


        #endregion


    }
}
