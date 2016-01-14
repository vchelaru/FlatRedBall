using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall.IO;

namespace FlatRedBall.Gui
{
    #region XML Docs
    /// <summary>
    /// A TextBox-like window which allows the user to browse the folder structure and select a 
    /// file.  The selected file will appear in the TextBox.
    /// </summary>
    #endregion
    public class FileTextBox : Window
    {
        #region Fields
        Button mButton;
        TextBox mTextBox;

        bool mKeepFilesRelative = false;

        List<string> mFileTypes = new List<string>();

        string mDefaultDirectory = "";
        #endregion

        #region Properties

        public string DefaultDirectory
        {
            get { return mDefaultDirectory; }
            set { mDefaultDirectory = value; }
        }

        public bool KeepFilesRelative
        {
            get { return mKeepFilesRelative; }
            set { mKeepFilesRelative = value; }
        }

        public override float ScaleX
        {
            get
            {
                return mScaleX;
            }
            set
            {
                mScaleX = value;

				mButton.SetPositionTL(2*value - 1.3f, mScaleY);

				mTextBox.ScaleX = value - 1.4f;
				mTextBox.SetPositionTL(mScaleX -.9f, mScaleY);

            }
        }

        public string Text
        {
            get { return mTextBox.Text; }
            set { mTextBox.Text = value; }
        }

        public TextBox TextBox
        {
            get { return mTextBox; }
        }
        #endregion

        #region Events

        public event GuiMessage FileSelect;

        #endregion

        #region Event Methods

        private void FileWindowOkClick(Window callingWindow)
        {
#if !SILVERLIGHT
            mTextBox.Text = ((FileWindow)callingWindow).Results[0];

            if (mKeepFilesRelative)
            {
                mTextBox.Text = FileManager.MakeRelative(mTextBox.Text);
            }

            if (FileSelect != null)
            {
                FileSelect(this);
            }
#endif
        }

        private void OpenFileWindow(Window callingWindow)
        {
#if !SILVERLIGHT
            FileWindow fileWindow = GuiManager.AddFileWindow();

            fileWindow.SetToLoad();

            if (string.IsNullOrEmpty(mDefaultDirectory) == false)
            {
                fileWindow.SetDirectory(mDefaultDirectory);
            }

            fileWindow.OkClick += FileWindowOkClick;

            fileWindow.SetFileType(mFileTypes);

            if (!string.IsNullOrEmpty(Text))
            {
                try
                {
                    string directory = FileManager.GetDirectory(Text);

                    if (System.IO.Directory.Exists(directory))
                    {
                        fileWindow.SetDirectory(directory);
                    }
                }
                catch
                {
                    // don't worry about it, this is just for convenience.  
                }

            }
#endif
        }

        #endregion

        #region Methods

        #region Constructor

        public FileTextBox(Cursor cursor)
            : base(cursor)
        {
            mTextBox = new TextBox(mCursor);
            AddWindow(mTextBox);
            mTextBox.fixedLength = false;

            mButton = new Button(mCursor);
            AddWindow(mButton);
            mButton.Text = "...";
            mButton.ScaleX = .9f;
            mButton.Click += OpenFileWindow;

            this.ScaleY = 1.4f;

            ScaleX = 8;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the filetype that the FileWindow can view. 
        /// </summary>
        /// <remarks>
        /// This method also clears all types.  Setting a filter then calling SetFileType clears the filter.
        /// 
        /// Setting the filetype as "graphic" sets all of the file types that FRB can load.
        /// <seealso cref="FlatRedBall.Gui.FileWindow.Filter"/>
        /// </remarks>
        /// <param name="FileType">The file types specified by extension.  </param>
        public void SetFileType(string fileType)
        {
            mFileTypes.Clear();

            if (fileType == "graphic")
            {
                AddFileType("bmp");
                AddFileType("png");
                AddFileType("jpg");
                AddFileType("tga");
                AddFileType("dds");
            }
            else if (fileType == "graphic and animation")
            {
                AddFileType("bmp");
                AddFileType("png");
                AddFileType("jpg");
                AddFileType("tga");
                AddFileType("dds");

                AddFileType("ach");
                AddFileType("achx");
            }
            else
                AddFileType(fileType);
        }


        public void SetFileType(List<string> FileType)
        {
            mFileTypes = FileType;
        }


        #endregion

        #region Private Methods


        private void AddFileType(string type)
        {
            if (mFileTypes.Contains(type) == false)
                this.mFileTypes.Add(type);
        }

        #endregion

        #endregion
    }
}
