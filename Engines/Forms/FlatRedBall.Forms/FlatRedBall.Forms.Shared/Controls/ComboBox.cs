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

        public ObservableCollection<object> Items => listBox.Items;
        
        ListBox listBox;
        GraphicalUiElement textComponent;
        RenderingLibrary.Graphics.Text coreTextObject;

        // todo:
        public string Text
        {
            get
            {
                return listBox.SelectedObject?.ToString();
            }
            set
            {
                var foundItem = Items.FirstOrDefault(item => item.ToString() == value);

                listBox.SelectedIndex = Items.IndexOf(foundItem);
            }
        }

        #endregion

        public ComboBox()
        {
            Visual.Click += this.HandleClick;
            Visual.Push += this.HandlePush;
            Visual.LosePush += this.HandleLosePush;
            Visual.RollOn += this.HandleRollOn;
            Visual.RollOff += this.HandleRollOff;
        }

        protected override void ReactToVisualChanged()
        {
            listBox = new ListBox();
            listBox.Visual = Visual.GetGraphicalUiElementByName("ListBoxInstance");


#if DEBUG

#endif
            listBox.Visual.EffectiveParentGue.RaiseChildrenEventsOutsideOfBounds = true;
            listBox.NewItemSelected += HandleNewItemSelected;


            base.ReactToVisualChanged();
        }

        private void HandleClick(IWindow window)
        {
            listBox.IsVisible = !listBox.IsVisible;

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
            UpdateState();
        }

        private void HandleLosePush(IWindow window)
        {
            UpdateState();
        }

        private void HandleNewItemSelected(object sender, EventArgs e)
        {
            coreTextObject.RawText = listBox.SelectedObject?.ToString();

            listBox.IsVisible = false;
        }


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
