using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall;

#if FRB_XNA
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
#endif

namespace EditorObjects.Gui
{
    public class CopyTexturesMultiButtonMessageBox : Window
    {
        #region Fields

        private ListBox mListBox;

        Texture2D mCopyLocallyTexture;
        Texture2D mMakeRelativeTexture;


        Button mSaveButton;
        Button mCancelButton;

        Button mCopyAllButton;
        Button mMakeAllRelativeButton;

        TextDisplay mTextDisplay;

        string mText;

        List<string> mFilesMarkedDotDotRelative = new List<string>();

        #endregion

        #region Properties

        public List<string> FilesMarkedDotDotRelative
        {
            get { return mFilesMarkedDotDotRelative; }
            set 
            { 
                mFilesMarkedDotDotRelative = value;
                removeFilesAlreadyMarkedRelative();
            }
        }

        private void removeFilesAlreadyMarkedRelative()
        {
            foreach (string s in mFilesMarkedDotDotRelative)
            {
                CollapseItem item = mListBox.GetItemByName(s);
                if(item != null)
                {
                    mListBox.Items.Remove(item);
                }
            }
        }

        public string Text
        {
            get { return mTextDisplay.Text; }
            set 
            {
                mText = value;

                UpdateText();
            }
        }



        public override float ScaleY
        {
            get
            {
                return base.ScaleY;
            }
            set
            {
                base.ScaleY = value;
            }
        }
        

        #endregion

        #region Events

        public event GuiMessage SaveClick;

        #endregion

        #region Event Methods

        void Save(Window callingWindow)
        {
            if (mFilesMarkedDotDotRelative != null)
            {
                foreach (CollapseItem item in mListBox.Items)
                {
                    if (item.Icons[0].Texture == mMakeRelativeTexture)
                    {
                        mFilesMarkedDotDotRelative.Add(item.Text);
                    }
                }
            }
            SaveClick(this);
            GuiManager.RemoveWindow(this);
        }

        void CancelClick(IWindow callingWindow)
        {
            GuiManager.RemoveWindow(this);
        }

        #endregion


        #region Methods

        public CopyTexturesMultiButtonMessageBox()
            : base(GuiManager.Cursor)
        {
            HasMoveBar = true;
            HasCloseButton = true;

            LoadTextures();

            addRelativeToggleButton();
            addTextDisplay();
            addListBox();
            addCopyAllButton();
            addMakeAllRelativeButton();
            addSaveButton();
            addCancelButton();

            ScaleY = 21;
            ScaleX = 23;

            GuiManager.BringToFront(this.mListBox);

            ResizeBox();

            this.Resizable = true;
            this.Resizing += new GuiMessage(ResizingThis);
        }

        private void addMakeAllRelativeButton()
        {
            mMakeAllRelativeButton = new Button(mCursor);
            this.AddWindow(mMakeAllRelativeButton);

            mMakeAllRelativeButton.Text = "Make all relative with \"../\"";
            mMakeAllRelativeButton.ScaleX = 11;
            mMakeAllRelativeButton.ScaleY = 2;

            mMakeAllRelativeButton.Click += new GuiMessage(MakeAllRelativeButtonClick);
        }

        private void addCopyAllButton()
        {
            mCopyAllButton = new Button(mCursor);
            this.AddWindow(mCopyAllButton);

            mCopyAllButton.Text = "Copy All Locally";
            mCopyAllButton.ScaleX = 11;
            mCopyAllButton.ScaleY = 2;
            mCopyAllButton.Click += new GuiMessage(CopyAllButtonClick);
        }

        private void addTextDisplay()
        {
            mTextDisplay = new TextDisplay(mCursor);
            AddWindow(mTextDisplay);
            mTextDisplay.X = .5f;
            mTextDisplay.Y = mRelativeToggle.ScaleY * 2 + mRelativeToggle.Y + 1;
        }

        private void addCancelButton()
        {
            mCancelButton = new Button(mCursor);
            AddWindow(mCancelButton);
            mCancelButton.ScaleX = 3;
            mCancelButton.Text = "Cancel";
            mCancelButton.X = mCancelButton.ScaleX + 2 * mSaveButton.ScaleX + 1;
            mCancelButton.Click += CancelClick;
        }

        private void addSaveButton()
        {
            mSaveButton = new Button(mCursor);
            AddWindow(mSaveButton);
            mSaveButton.ScaleX = 3;
            mSaveButton.Text = "Save";
            mSaveButton.X = mSaveButton.ScaleX + .5f;
            mSaveButton.Click += Save;
        }

        private void addListBox()
        {
            mListBox = new ListBox(mCursor);
            AddWindow(mListBox);
            mListBox.ScaleY = 7;
            mListBox.DistanceBetweenLines = 2.5f;
        }

        ToggleButton mRelativeToggle = null;
        public bool AreAssetsRelative { get { return mRelativeToggle.IsPressed; } }

        private void addRelativeToggleButton()
        {
            mRelativeToggle = new ToggleButton(mCursor);
            mRelativeToggle.Press();
            mRelativeToggle.Push += mRelativeToggle_Push;
            AddWindow(mRelativeToggle);

            mRelativeToggle.Name = "scnRelativeAssets";
            mRelativeToggle.SetText("Use absolute path", ".scnx relative assets");
            mRelativeToggle.ScaleX = 9f;
            mRelativeToggle.X = mRelativeToggle.ScaleX + 1;
            mRelativeToggle.Y = 2f;
            
            //Hide the relative toggle, disabling ability to make absolute; for simpicity and ease of use
            mRelativeToggle.Visible = false;
            mRelativeToggle.Y = 0;
        }

        void mRelativeToggle_Push(Window callingWindow)
        {
            //TODO: disable apropriate controls when toggle IsPressed == false
            mTextDisplay.Enabled = AreAssetsRelative;
            mListBox.Enabled = AreAssetsRelative;
            mCopyAllButton.Enabled = AreAssetsRelative;
            mMakeAllRelativeButton.Enabled = AreAssetsRelative;
        }

        void ResizingThis(Window callingWindow)
        {
            ResizeBox();
        }

        void MakeAllRelativeButtonClick(Window callingWindow)
        {
            foreach (CollapseItem item in mListBox.Items)
            {
                item.Icons[0].Texture = mMakeRelativeTexture;
            }
        }

        void CopyAllButtonClick(Window callingWindow)
        {
            foreach (CollapseItem item in mListBox.Items)
            {
                item.Icons[0].Texture = mCopyLocallyTexture;
            }            
        }

        public int ItemsCount { get { return mListBox.Items.Count; } }


        public CollapseItem AddItem(string fileName)
        {
            CollapseItem newItem = mListBox.AddItem(fileName);
            ListBoxIcon icon = newItem.AddIcon(mCopyLocallyTexture, "ActionIcon");
            icon.IconClick += new ListBoxFunction(IconClick);
            icon.ScaleX = icon.ScaleY = 1.1f;

            removeFilesAlreadyMarkedRelative();

            return newItem;
        }

        void IconClick(CollapseItem collapseItem, ListBoxBase listBoxBase, ListBoxIcon listBoxIcon)
        {
            if (listBoxIcon.Texture == mCopyLocallyTexture)
            {
                listBoxIcon.Texture = mMakeRelativeTexture;
            }
            else
            {
                listBoxIcon.Texture = mCopyLocallyTexture;
            }
        }

        

        protected void ResizeBox()
        {
            mSaveButton.X = mSaveButton.ScaleX + .5f;
            mCancelButton.X = mCancelButton.ScaleX + 2 * mSaveButton.ScaleX + 1;

            UpdateText();

            if (mListBox != null)
            {
                mListBox.ScaleX = mScaleX - .5f;
                mListBox.X = this.ScaleX;
            }

            mSaveButton.Y = this.ScaleY * 2 - mSaveButton.ScaleY - .5f;
            mCancelButton.Y = this.ScaleY * 2 - mCancelButton.ScaleY - .5f;

            mListBox.Y = mTextDisplay.Y + 16.5f;

            mListBox.ScaleX = this.ScaleX - .5f;
            mListBox.X = this.ScaleX;

            mCopyAllButton.Y = this.ScaleY * 2 - 9;
            mCopyAllButton.X = .5f + mCopyAllButton.ScaleX;

            mMakeAllRelativeButton.Y = this.ScaleY * 2 - 5;
            mMakeAllRelativeButton.X = mCopyAllButton.X;


        }

        private void UpdateText()
        {
            mTextDisplay.Text = mText;
            mTextDisplay.Wrap(this.ScaleX * 2 - 1);
        }

        private void LoadTextures()
        {
            try
            {
                mCopyLocallyTexture = FlatRedBallServices.Load<Texture2D>(
                    "Content/CopyLocally.png", FlatRedBallServices.GlobalContentManager);

                mMakeRelativeTexture = FlatRedBallServices.Load<Texture2D>(
                    "Content/MakeRelativeKeepInPlace.png", FlatRedBallServices.GlobalContentManager);
            }
            catch
            {
                throw new Exception(
                    "Could not find the textures for the CopyTexturesMessageBox.  Someone on the FRB team must have forgotten to add these to the release tool.");
            }
        }


        #endregion
    }
}
