using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;

namespace SpriteEditor.Gui
{
    public class AttributesWindow : Window
    {
        // Fields
        private Button mAddAttribute;
        private ListBox mListBox;

        // Methods
        public AttributesWindow(GuiMessages messages)
            : base(GuiManager.Cursor)
        {
            this.ScaleX = 11f;
            this.ScaleY = 20f;
            base.HasMoveBar = true;
            base.HasCloseButton = true;
            base.Name = "Attributes";

            this.mListBox = new ListBox(mCursor);
            AddWindow(mListBox);
            this.mListBox.ScaleX = this.ScaleX - 0.5f;
            this.mListBox.ScaleY = this.ScaleY - 3f;
            this.mListBox.SetPositionTL(this.ScaleX, this.ScaleY - 0.5f);
            this.mListBox.Click += new GuiMessage(this.AttributesListBoxClick);
            this.mListBox.Name = "Attributes ListBox";

            this.mAddAttribute = new Button(mCursor);
            AddWindow(mAddAttribute);
            this.mAddAttribute.ScaleX = 8f;
            this.mAddAttribute.ScaleY = 1.4f;
            this.mAddAttribute.SetPositionTL(8.5f, (2f * this.ScaleY) - 2f);
            this.mAddAttribute.Text = "Create New Attribute";
            this.mAddAttribute.Click += new GuiMessage(this.AddAttribute);
        }

        private void AddAttribute(Window callingWindow)
        {
            GuiManager.ShowTextInputWindow("Enter new attribute name.  Attributes usually begin with _ (underscore)", "New Attribute").OkClick += new GuiMessage(this.AddAttributeOk);
        }

        private void AddAttributeOk(Window callingWindow)
        {
            string newAttribute = ((TextInputWindow)callingWindow).Text;
            this.mListBox.AddItem(newAttribute);
        }

        private void AttributesListBoxClick(Window callingWindow)
        {
        }
    }
}
