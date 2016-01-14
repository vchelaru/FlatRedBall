using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Graphics;
using FlatRedBall.IO;
using FlatRedBall.Collections;

namespace SpriteEditor.Gui
{
    public class DeleteTextureWindow : Window
    {
        // Fields
        private List<Texture2D> allTextures;
        private ComboBox comboBox;
        private ComboBox textureList;
        private TextDisplay mMessage;
        private Texture2D textureToDelete;

        // Methods
        public DeleteTextureWindow(Texture2D textureToDelete, List<Texture2D> allTextures)
            : base(GuiManager.Cursor)
        {
            this.textureToDelete = textureToDelete;
            this.allTextures = allTextures;
            GuiManager.AddWindow(this);
            this.ScaleX = 15f;
            this.ScaleY = 10.5f;
            base.HasMoveBar = true;
            base.mName = "Delete " + FileManager.MakeRelative(textureToDelete.Name, FileManager.RelativeDirectory) + "?";

            mMessage = new TextDisplay(mCursor);
            AddWindow(mMessage);
            mMessage.Text = 
                FileManager.MakeRelative(textureToDelete.Name, FileManager.RelativeDirectory) + 
                " is being referenced by other objects.  What would you like to do?";
            mMessage.X = .5f;
            mMessage.Y = 1.3f;
            mMessage.Text = TextManager.InsertNewLines(
                mMessage.Text, GuiManager.TextSpacing, this.ScaleX * 2 - 1, TextManager.DefaultFont);
            //throw new NotImplementedException("Need to support the text field here");


            this.comboBox = new ComboBox(mCursor);
            AddWindow(comboBox);
            this.comboBox.SetPositionTL(this.ScaleX, 13f);
            this.comboBox.ScaleX = 13f;
            this.comboBox.AddItem("Delete objects referencing texture");
            if (allTextures.Count > 1)
            {
                this.comboBox.AddItem("Replace texture");
            }
            this.comboBox.Text = "Delete objects referencing texture";
            this.comboBox.ItemClick += new GuiMessage(this.ComboBoxOptionSelect);

            this.textureList = new ComboBox(mCursor);
            AddWindow(textureList);
            this.textureList.ScaleX = 13f;
            this.textureList.SetPositionTL(this.ScaleX, 15.5f);
            this.textureList.Visible = false;
            foreach (Texture2D frbt in allTextures)
            {
                if (frbt != textureToDelete)
                {
                    if (this.textureList.Count == 0)
                    {
                        this.textureList.Text = frbt.Name;
                    }
                    this.textureList.AddItem(frbt.Name, frbt);
                }
            }
            Button okButton = new Button(mCursor);
            AddWindow(okButton);
            okButton.Text = "Ok";
            okButton.ScaleX = 3.5f;
            okButton.ScaleY = 1.4f;
            okButton.SetPositionTL(7f, 18.5f);
            okButton.Click += new GuiMessage(this.OkButtonClick);

            Button cancelButton = new Button(mCursor);
            AddWindow(cancelButton);
            cancelButton.Text = "Cancel";
            cancelButton.ScaleX = 3.5f;
            cancelButton.ScaleY = 1.4f;
            cancelButton.SetPositionTL(23f, 18.5f);
            cancelButton.Click += new GuiMessage(this.ClosingWindow);
        }

        private void ClosingWindow(Window callingWindow)
        {
            GuiManager.RemoveWindow(this);
        }

        private void ComboBoxOptionSelect(Window callingWindow)
        {
            if (((ComboBox)callingWindow).Text == "Delete objects referencing texture")
            {
                this.textureList.Visible = false;
            }
            else
            {
                this.textureList.Visible = true;
            }
        }

        private void OkButtonClick(Window callingWindow)
        {
            if (this.comboBox.Text == "Delete objects referencing texture")
            {
                GameData.RemoveObjectsReferencing(this.textureToDelete);
                GameData.DeleteTexture(this.textureToDelete);
            }
            else
            {
                Texture2D newTexture = null;
                foreach (Texture2D frbt in this.allTextures)
                {
                    if (frbt.Name == this.textureList.Text)
                    {
                        newTexture = frbt;
                        break;
                    }
                }
                GameData.ReplaceTexture(this.textureToDelete, newTexture);
            }
            this.textureToDelete = null;
            this.ClosingWindow(callingWindow);
             
        }

        private void TextureComboBoxSelect(Window callingWindow)
        {
            if (((ComboBox)callingWindow).Text == "From file. . .")
            {
                FileWindow fileWindow = GuiManager.AddFileWindow();
                fileWindow.SetFileType("graphic");
                fileWindow.SetToLoad();
            }
        }
    }


}
