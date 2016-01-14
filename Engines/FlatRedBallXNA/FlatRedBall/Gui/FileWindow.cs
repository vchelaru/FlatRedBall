using System;

#if FRB_MDX
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D=Microsoft.DirectX.Direct3D;
using Texture2D = FlatRedBall.Texture2D;
#else
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
#endif

using FlatRedBall.Graphics;
using FlatRedBall.Input;
using System.Collections.Generic;

using FileManager = FlatRedBall.IO.FileManager;
using System.IO;
using System.Reflection;

#if !XBOX360 && !WINDOWS_PHONE && !MONODROID
using FlatRedBall.IO.Remote;
#endif

namespace FlatRedBall.Gui
{
    #region Delegates

    public delegate void BetweenLoadFolder(string lastFileLoaded);

    #endregion



	public class FileWindow : Window
    {
        #region Embedded Classes

        public static class FileWindowTypes
        {
            public static string Graphics = "graphic";
            public static string GraphicsAndAnimation = "graphic and animation";

        }

        #endregion

        #region Fields

        const string Separator = "---------------";

        const string ComputerRoot = "Computer";

		List<string> mFileTypes;
        List<string> mBookmarks = new List<string>();

		int mNumberOfDisplayedDirectories;
        static string mContentManagerName = "FileWindow ContentManager";

		internal ListBox mListBox;
		internal string mDirectory;
		ComboBox mCurrentDirectoryDisplay;
        CollapseItem mLastNonSystemDirectory;

		Button mOkButton;
		Button mCancelButton;
		Button mUpDirectory;
        Button mCreateNewDirectory;

        ///// <summary>
        ///// This button will call the onOkClick event, but will not close the FileWindow.
        ///// </summary>
        ///// <remarks>
        ///// If this button is clicked on a graphical file, the FileWindow assumes that the file is to be loaded and will
        ///// not remove it from memory when another item in the Listbox is selected.  
        ///// CAUTION:  This may cause textures to be loaded through the FileWindow even though they are not loaded
        ///// in the application.  Extra checks may be needed so that the FileWindow removes FrbTextures that are not in use by the application.
        ///// </remarks>
        //Button addButton;


		ToggleButton mShowFileHierarchy;
		ToggleButton mAllRelativeToggleButton;
        ToggleButton mShowRecent;
        ToggleButton mBookmarkToggleButton;

        const float TextureButtonStartingScale = 4;
		Button mTextureDisplayButton;
        // This should not be null.  If it's null, then the textures will not show up properly.

		Button loadDirectory;

        ComboBox mFileTypeBox;

		internal TextBox saveName;
		public string activityToExecute;

        static int MAX_FILES_PER_TYPE = 15;

        static Dictionary<string, string> sLastDirectoriesViewed = new Dictionary<string,string>();
        static string sLastUntypedDirectory = "";

        Window mTextureFloatingWindow;

        string mUserName;
        string mPassword;

		#endregion

		#region Properties

#if !XBOX360 && !WINDOWS_PHONE && !MONODROID
        public bool AreResultsFtp
        {
            get
            {
                if (Results.Count == 0)
                {
                    return FtpManager.IsFtp(mDirectory);
                }
                else
                {
                    return FtpManager.IsFtp(Results[0]);
                }
            }
        }
#endif

		public bool ctrlClickOn
		{
			set	{mListBox.ctrlClickOn = value;		}
		}


        public string Password
        {
            get { return mPassword; }
        }


		public List<string> Results
		{
			get
			{
				List<string> resultsToReturn = new List<string>();
                
				#region loading
                if (saveName.Text == "")
                {
                    if (this.mAllRelativeToggleButton.IsPressed || mShowRecent.IsPressed)
                    {
                        resultsToReturn = mListBox.GetHighlighted();

                        for (int i = 0; i < resultsToReturn.Count; i++)
                        {
                            if (FileManager.IsRelative(resultsToReturn[i]))
                            {
                                if (mAllRelativeToggleButton.IsPressed)
                                {
                                    if (resultsToReturn[i][0] == '\\' || resultsToReturn[i][0] == '/')
                                    {
                                        // CurrentDirectory already ends with a slash, so get rid of it on the results
                                        resultsToReturn[i] = resultsToReturn[i].Substring(1);
                                    }
                                    resultsToReturn[i] = CurrentDirectory + resultsToReturn[i];

                                }
                                else
                                {
                                    if (resultsToReturn[i][0] == '\\' || IO.FileManager.RelativeDirectory.EndsWith(@"\") ||
                                        IO.FileManager.RelativeDirectory.EndsWith("/"))
                                        resultsToReturn[i] = IO.FileManager.RelativeDirectory + resultsToReturn[i];
                                    else
                                        resultsToReturn[i] = IO.FileManager.RelativeDirectory + "/" + resultsToReturn[i];
                                }
                            }
                            // else, there's no need to modify the file name.
                        }

                    }
                    else if (mDirectory != IO.FileManager.RelativeDirectory + "/")
                    {
                        if (mDirectory != IO.FileManager.RelativeDirectory + "/")
                        {
                            resultsToReturn = mListBox.GetHighlighted();

                            for (int i = 0; i < resultsToReturn.Count; i++)
                            {
                                resultsToReturn[i] = mDirectory + resultsToReturn[i];
                            }
                        }
                    }
                    else
                        resultsToReturn = mListBox.GetHighlighted();
                }
                #endregion
                #region saving
                else
                {
                    #region the file window has file types (extensions) specified
                    if (mFileTypes.Count != 0)
                    {
                        string typedExtension = FileManager.GetExtension(saveName.Text);

                        if (FileManager.IsRelative(saveName.Text))
                        {
                            // This is not a full path like c:\path, so return the value as 
                            if (mFileTypes.Contains(typedExtension))
                                resultsToReturn.Add(mDirectory + saveName.Text);
                            else
                                resultsToReturn.Add(mDirectory + saveName.Text + "." + mFileTypes[0]);
                        }
                        else
                        {
                            // This is a full path.  See if it's a directory which exists
                            if (System.IO.Directory.Exists(saveName.Text))
                            {
                                // simply return the full path
                                resultsToReturn.Add(saveName.Text);
                            }
                            else 
                            {
                                // The path as entered in the file window doesn't exist.
                                // Does the parent path exist?
                                string parentOfEnteredText = FileManager.GetDirectory(saveName.Text);
                                if (System.IO.Directory.Exists(parentOfEnteredText))
                                {
                                    resultsToReturn.Add(saveName.Text + "." + mFileTypes[0]);
                                }
                                else
                                {
                                    // Invalid file name so don't add anything.
                                }

                            }
                        }

                    }
                    #endregion

                    #region no file type (extension) specified, so save the text as written
                    else
                        resultsToReturn.Add(mDirectory + saveName.Text);
                    #endregion
                }
				#endregion

				return resultsToReturn;
			}
		}


        public string saveNameText
        {
            set
            {
                this.saveName.Text = value;

            }
        }

        public bool CtrlClickOn
        {
            set { mListBox.CtrlClickOn = value; }
        }
		
		public bool ShiftClickOn
		{
			set	{mListBox.ShiftClickOn = value;	}
		}


        public string UserName
        {
            get { return mUserName; }
        }

        #region XML Docs
        /// <summary>
        /// Specifies the possible filetypes that the FileWindow can save or load.
        /// </summary>
        /// <remarks>
        /// <code>
        /// // assuming someFileWindow is a valid
        /// // This sets a single filter for EXE
        /// someFileWindow.Filter = "Executable File (*.exe)|*.exe";
        /// // Or you can set multiple ones
        /// someFileWindow.Filter = "SpriteEditor Binary Scene (*.scn)|*.scn|SpriteEditor XML Scene (*.scnx)|*.scnx";
        /// </code>
        /// </remarks>
        #endregion
        public string Filter
        {
            
            set
            {
                // sets the filetypes that this window can handle.  Uses the same format
                // as the System.Windows.Forms.FileDialog object. Example:
                // "txt files (*.txt)|*.txt|All files (*.*)|*.*"
                mFileTypeBox.Clear();
                mFileTypes.Clear();

                string[] splitFilter = value.Split('|');

                for (int i = 0; i < splitFilter.Length; i+= 2)
                {
                    mFileTypeBox.AddItem(splitFilter[i], splitFilter[i + 1]);
                    mFileTypes.Add(FileManager.GetExtension(splitFilter[i+1]));
                }

                if (mFileTypeBox.Count != 0)
                    mFileTypeBox.SelectItem(0);

            }
        }


        public string CurrentFileType
        {
            set
            {
                if (mFileTypeBox.ContainsObject("*." + value))
                {
                    mFileTypeBox.SelectItemByObject("*." + value);
                }
                if (mFileTypeBox.ContainsObject(value))
                {
                    mFileTypeBox.SelectItemByObject(value);
                }


            }
        }


        public bool IsSavingFile
        {
            get
            {
                return saveName.Visible;
            }
        }


        public string ContentManagerName
        {
            get { return mContentManagerName; }
            set { mContentManagerName = value; }
        }


        public static string ApplicationFolderForThisProgram
        {
            get
            {
#if XBOX360 || WINDOWS_PHONE || MONODROID
                throw new InvalidOperationException("This isn't allowed on the 360");
#else
                string callingFile =
                    FileManager.RemoveExtension(
                    FileManager.RemovePath(Assembly.GetEntryAssembly().Location));

                return
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                    @"\FlatRedBall\" + callingFile + @"\";
#endif
            }
        }


        public string CurrentDirectory
        {
            get { return mDirectory; }
        }

        #endregion

        #region Events
            
        public event GuiMessage OkClick = null;
        public event BetweenLoadFolder betweenLoadFolder = null;
		#endregion

		#region Event Methods

        private void BookmarkButtonClick(Window callingWindow)
        {
            if (mBookmarkToggleButton.IsPressed)
            {
                // The button is now down, so add this directory to bookmarks
                mBookmarks.Add(CurrentDirectory);
            }
            else
            {
                // The button is not down, so remove this directory from bookmarks
                mBookmarks.Remove(CurrentDirectory);
            }

            ResetCurrentDirectoryComboBox();

            SaveBookmarks();
        }


		private static void OnAllRelativeClick(Window callingWindow)
		{
			FileWindow fileWindow = callingWindow.Parent as FileWindow;
            fileWindow.SetDirectory(fileWindow.CurrentDirectory);
			
		}


		private static void OnFileHierarchyClick(Window callingWindow)
		{
			FileWindow fileWindow = callingWindow.Parent as FileWindow;

            fileWindow.mUpDirectory.Visible = true;
            fileWindow.mCreateNewDirectory.Visible = true;
            fileWindow.mAllRelativeToggleButton.Visible = true;


			fileWindow.SetDirectory(fileWindow.mDirectory);
		}


        private static void OnShowRecent(Window callingWindow)
        {
            FileWindow fileWindow = callingWindow.Parent as FileWindow;

            fileWindow.mAllRelativeToggleButton.Visible = false;

            fileWindow.SetDirectory();

        }


		private static void OnListBoxClick(Window callingWindow)
		{
			ListBox callingListBox = callingWindow as ListBox;
			FileWindow parentFileWindow = callingWindow.Parent as FileWindow;

			#region loadDirectory enable/disable
			int highlightedNum = callingListBox.GetFirstHighlightedIndex();
			if(highlightedNum != -1 && highlightedNum < parentFileWindow.mNumberOfDisplayedDirectories)
				parentFileWindow.loadDirectory.Enabled = true;
			else if(highlightedNum != -1)
				parentFileWindow.loadDirectory.Enabled = false;;
			#endregion
			
			string stringHighlighted = "";
			List<string> a = callingListBox.GetHighlighted();
			if(a.Count != 0)
			{
				stringHighlighted = a[0];
				if(stringHighlighted[0] == '\\')
					stringHighlighted = stringHighlighted.Remove(0, 1);
			}

			#region if file window is a save window, put the name in the save dialog box
            if (parentFileWindow.saveName.Visible)
			{
				if(highlightedNum != -1 && 
                    (parentFileWindow.mShowRecent.IsPressed || highlightedNum >= parentFileWindow.mNumberOfDisplayedDirectories))
					parentFileWindow.saveName.Text = FileManager.RemoveExtension(stringHighlighted);
			}
			#endregion

			#region if the file window is graphical, then we need to load/unload graphics
			if(parentFileWindow.IsGraphicalFileWindow())
			{
				#region  we highlighted something, and it is not a directory, and it is a graphical file:  Show graphic
				if(highlightedNum != -1 && highlightedNum >= parentFileWindow.mNumberOfDisplayedDirectories && stringHighlighted != "" &&
					FileManager.IsGraphicFile(stringHighlighted))
				{
					// store the texture that was loaded before.

					try
					{

                        parentFileWindow.mTextureDisplayButton.SetOverlayTextures(
                            FlatRedBallServices.Load<Texture2D>(parentFileWindow.mDirectory + stringHighlighted, mContentManagerName), null);
                        if (parentFileWindow.mTextureFloatingWindow != null)
                        {
                            parentFileWindow.mTextureFloatingWindow.BaseTexture = parentFileWindow.mTextureDisplayButton.UpOverlayTexture;

                        }

					}
					catch(Exception)
					{
						parentFileWindow.mTextureDisplayButton.SetOverlayTextures(null, null);
					}
				}
				#endregion

				#region else, it is a directory or not a graphical file
				else if(highlightedNum != -1)
				{
					parentFileWindow.mTextureDisplayButton.SetOverlayTextures(null, null);
                    if (parentFileWindow.mTextureFloatingWindow != null)
                    {
                        parentFileWindow.mTextureFloatingWindow.BaseTexture = parentFileWindow.mTextureDisplayButton.UpOverlayTexture;

                    }				
                }
				#endregion
			}
			#endregion


		}  


		private void OnListBoxStrongSelect(Window callingWindow)
		{
			int highlightedNum = mListBox.GetFirstHighlightedIndex();

			#region if graphical window and we already have an overlayTexture showing, we may have to
			// unload the FrbTexture, then set the new FrbTexture
			if(this.IsGraphicalFileWindow())
			{
				mTextureDisplayButton.SetOverlayTextures(null, null);
                if (mTextureFloatingWindow != null)
                {
                    mTextureFloatingWindow.BaseTexture = mTextureDisplayButton.UpOverlayTexture;
                }
			}
			#endregion

            if (mAllRelativeToggleButton.IsPressed)
			{
				if(highlightedNum != -1 && highlightedNum < mListBox.Count && Results.Count != 0)
				{
                    if (saveName.Visible == false)
                    {
                        CallOkClick();

                        CloseWindow();
                    }
                    else
                    {
                        AskToReplace(this);
                    }
				}
			}
            else if (mShowRecent.IsPressed || mAllRelativeToggleButton.IsPressed)
            {
                if (highlightedNum != -1 && highlightedNum < mListBox.Count && Results.Count != 0)
                {
                    if (saveName.Visible == false)
                    {
                        CallOkClick();

                        CloseWindow();
                    }
                    else
                    {
                        AskToReplace(this);
                    }
                }
            }
            else
            {
                if (highlightedNum != -1 && highlightedNum < mNumberOfDisplayedDirectories)
                {
                    if (mDirectory == ComputerRoot)
                    {
                        mDirectory = mListBox.GetHighlighted()[0];
                    }
                    else
                    {
                        mDirectory += mListBox.GetHighlighted()[0];
                        mDirectory += "/";
                    }
                    SetDirectory(mDirectory);
                    mListBox.StartAt = 0;
                    mListBox.AdjustScrollSize();

                }
                else if (highlightedNum != -1 && highlightedNum < mListBox.Count)
                {
                    if (Results.Count != 0)
                    {
                        if (saveName.Visible == false)
                        {
                            CallOkClick();

                            CloseWindow();
                        }
                        else
                        {
                            AskToReplace(this);
                        }
                    }
                }
            }
		}

	
		private void OnFileWindowClose(Window callingWindow)
		{
            InputManager.ReceivingInput = null;

		}
	

        //private void AddButtonClick(Window callingWindow)
        //{
        //    Button addButtonClicked = callingWindow as Button;
        //    FileWindow parentFileWindow = callingWindow.Parent as FileWindow;

        //    if(parentFileWindow.mAllRelativeToggleButton.IsPressed == false &&
        //        parentFileWindow.mListBox.GetFirstHighlightedIndex() != -1 && 
        //        parentFileWindow.mListBox.GetFirstHighlightedIndex() < parentFileWindow.mNumberOfDisplayedDirectories)
        //    {
        //        // do nothing
        //    }
        //    else if(parentFileWindow.mListBox.GetHighlighted().Count != 0 && parentFileWindow.mListBox.GetFirstHighlightedIndex() < parentFileWindow.mListBox.mItems.Count)
        //    {
        //        parentFileWindow.CallOkClick();
        //    }

        //    // When the Add button is pressed the ListBox should regain input focus.  This
        //    // allows the user to click Add with the mouse while pressing the down arrow with
        //    // the other hand - an efficient way to add objects.
        //    InputManager.ReceivingInput = mListBox;
        //}


		private static void LoadDirClick(Window callingWindow)
		{
			
			FileWindow parentFileWindow = callingWindow.Parent as FileWindow;

			parentFileWindow.OnListBoxStrongSelect(parentFileWindow);

			foreach(CollapseItem ci in parentFileWindow.mListBox.mItems)
                if (parentFileWindow.mFileTypes.Contains(FileManager.GetExtension(ci.Text)))
				{
                    parentFileWindow.mListBox.HighlightItem(ci.Text);
					parentFileWindow.CallOkClick();

                    if (parentFileWindow.betweenLoadFolder != null)
                    {
                        parentFileWindow.betweenLoadFolder(parentFileWindow.Results[0]);
                    }

				}
				

		}


		private void OkButtonClick(Window callingWindow)
		{
            // The OnListBoxStrongSelect will enter a folder if a folder is highlighted.
            // While this is desirable, it should not occur if the FileWindow is being used to
            // save a file and the user has typed in a file name in the TextBox.
            if((IsSavingFile == false || saveName.Text == "") &&
             mListBox.mHighlightedItems.Count != 0 && mListBox.mItems.IndexOf(mListBox.mHighlightedItems[0]) < mListBox.mItems.Count)
                OnListBoxStrongSelect(callingWindow);
            else if (saveName.Visible == true && saveName.Text != "")
            {

                AskToReplace(this);
            }
            /*
			#region the FileWindow is a graphical file window, so unload the displayed texture
			if(IsGraphicalFileWindow())
			{
				if(textureDisplayButton.overlayTexture != null)
				{
					if(displayTextureLoadedBefore == false)
						parentFileWindow.SpriteManager.texMan.RemoveTexture(textureDisplayButton.overlayTexture);
				}
				textureDisplayButton.SetOverlayTextures(null, null);
			}
			#endregion

			if(parentFileWindow.saveName.text != "")
			{
				AskToReplace(parentFileWindow);
			}
			else
			{
				if(parentFileWindow.showAvailableRelative.IsPressed == false &&
					parentFileWindow.listBox.GetHighlightedNum().Count != 0 && parentFileWindow.listBox.GetHighlightedNum()[0] < parentFileWindow.numOfDirectories)
				{
					parentFileWindow.directory += parentFileWindow.listBox.GetHighlighted()[0];
					parentFileWindow.directory += "/";
					parentFileWindow.SetDirectory(parentFileWindow.directory);

                    parentFileWindow.listBox.startAt = 0;
                    parentFileWindow.listBox.AdjustScrollSize();

				}
				else if(parentFileWindow.listBox.GetHighlighted().Count != 0 && parentFileWindow.listBox.GetHighlightedNum()[0] < parentFileWindow.listBox.itemArray.Count)
				{
					if(parentFileWindow.OkClick != null)	parentFileWindow.OkClick(parentFileWindow);
					parentFileWindow.Close();
				}
			}
             * */
		}


		private static void AskToReplace(FileWindow windowToUse)
		{

            if (windowToUse.Results.Count == 0)
            {
                GuiManager.ShowMessageBox("Directory does not exist.", "Save Error");
            }
            else if (windowToUse.mDirectory == ComputerRoot)
            {
                GuiManager.ShowMessageBox("Can't save file - not a valid location.", "Save Error");
            }
            else
            {
                string result = windowToUse.Results[0];

                if (System.IO.File.Exists(result))
                {
#if XBOX360 || WINDOWS_PHONE || MONODROID
                    // For now just use false since there's no File.GetAttributes
                    bool isReadOnly = false;
#else
                    bool isReadOnly = (System.IO.File.GetAttributes(result) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
#endif
                    if (isReadOnly)
                    {
                        GuiManager.ShowMessageBox(result + " is read only.  Can't save file.", "Save Error");
                    }
                    else
                    {

                        // attempting to save over a file that already exists.
                        windowToUse.CloseWindow();

                        FlatRedBall.Gui.OkCancelWindow okCancelWindow = GuiManager.AddOkCancelWindow();
                        GuiManager.AddDominantWindow(okCancelWindow);
                        okCancelWindow.HasMoveBar = true;
                        okCancelWindow.ScaleX = 10;
                        okCancelWindow.ScaleY = 6f;
                        okCancelWindow.Message = FileManager.RemovePath(windowToUse.Results[0]) +
                            " already exists.  Overwrite?";
                        okCancelWindow.callingWindow = windowToUse;

                        okCancelWindow.OkClick += new GuiMessage(windowToUse.OkClick);
                    }
                }
                else if (System.IO.Directory.Exists(result))
                {
                    // Set the current directory here
                    windowToUse.SetDirectory(result);
                    // now that we're in the new directory, clear out the save name text box
                    if (windowToUse.saveName != null)
                        windowToUse.saveName.Text = "";
                }
                else
                {
                    windowToUse.CallOkClick();
                    windowToUse.CloseWindow();
                }
            }

		}


		private void OnSaveNameEnter(Window callingWindow)
		{
			#region we pushed enter

            #region the fileWindow is a graphical file window, so unload the displayed texture
            if (this.IsGraphicalFileWindow())
			{

				mTextureDisplayButton.SetOverlayTextures(null, null);
                if (mTextureFloatingWindow != null)
                {
                    mTextureFloatingWindow.BaseTexture = mTextureDisplayButton.UpOverlayTexture;
                }

            }
            #endregion


            if (saveName.Text != "")
			{
                AskToReplace(callingWindow.Parent as FileWindow);

			}
			else
			{
				if(mListBox.GetFirstHighlightedIndex() != -1 && mListBox.GetFirstHighlightedIndex() < mNumberOfDisplayedDirectories)
				{
					mDirectory += mListBox.GetHighlighted()[0];
					mDirectory += "/";
					SetDirectory(mDirectory);
				}
				else
				{
                    CallOkClick();
					CloseWindow();
				}
			}
			#endregion			
		}

		
		private void CloseFileWindow(Window callingWindow)
		{
			callingWindow.Parent.CloseWindow();
		}


        private void AddFileToRecent(Window callingWindow)
        {
            string directory = ApplicationFolderForThisProgram;

            if (Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }

            string type = 
                FileManager.GetExtension(((FileWindow)callingWindow).Results[0]);

            System.IO.StreamWriter sw = null;

            try
            {
                sw =
                    new System.IO.StreamWriter(directory + @"recent" + type + ".txt", true);

                sw.WriteLine(
                    FileManager.Standardize(((FileWindow)callingWindow).Results[0]));
            }
            catch
            {
                // do nothing - probably read-only
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                    sw.Dispose();
                }
            }

            // now check to see if there are more than the max number of items in the file

            System.IO.StreamReader sr = null;
            List<string> sa = new List<string>();
            
            try
            {
                sr = new System.IO.StreamReader(directory + @"recent" + type + ".txt");


                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    sa.Add(line);
                }
            }
            catch
            {
                // do nothing - probably read-only
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
            }

            // now that we have all the lines, see if there are more than the max

            // this is inefficient, but I did it in a hurry.  Maybe I'll fix it one day
            if (sa.Count > MAX_FILES_PER_TYPE)
            {
                while (sa.Count > MAX_FILES_PER_TYPE)
                {
                    sa.RemoveAt(0);
                }

                try
                {
                    sw =
                        new System.IO.StreamWriter(directory + @"recent" + type + ".txt");

                    foreach (string s in sa)
                        sw.WriteLine(s);
                }
                catch
                {
                    // do nothing - probably readonly
                }
                finally
                {
                    if (sw != null)
                    {
                        sw.Close();
                    }
                }
            }



        }


        private void OnClose(Window callingWindow)
        {
            string lastDirectory;

            if (Results.Count != 0)
                lastDirectory = FileManager.GetDirectory(Results[0]);
            else
                lastDirectory = mDirectory;

            foreach (string s in mFileTypes)
            {
                SetLastDirectory(s, lastDirectory);
            }

            // Unload the ContentManager
            FlatRedBallServices.Unload(mContentManagerName);
        }

        /// <summary>
        /// If the user selects an item in the mFileTypesBox 
        /// </summary>
        /// <param name="callingWindow"></param>
        private void OnFileTypeChange(Window callingWindow)
        {
            this.mFileTypes.Clear();
            
            // programs may not be using the dropdown.  If not, then it will be empty
            // but this method will be called if the user clicks on the empty list box.

            // The combo box stores the file types.  mFileTypes is only the current types that are
            // being shown.
            if (mFileTypeBox.SelectedObject != null)
            {
                string selectedText = mFileTypeBox.SelectedObject as string;
                string extension = FileManager.GetExtension(selectedText);

                if (string.IsNullOrEmpty(extension) && !string.IsNullOrEmpty(selectedText))
                {
                    this.mFileTypes.Add(selectedText);                
                }
                else
                {

                    this.mFileTypes.Add(extension);
                }



                if (FileWindow.sLastDirectoriesViewed.ContainsKey(mFileTypes[0]))
                {
                    SetDirectory( sLastDirectoriesViewed[mFileTypes[0]]);

                }
                else
                {
                    SetDirectory(mDirectory);
                }
            }

        }


        private void AddDirectoryClick(Window callingWindow)
        {
            // add the directory here
//            System.IO.Directory.CreateDirectory(
            TextInputWindow textInputWindow = GuiManager.ShowTextInputWindow("Enter name of new directory", "Create Directory");

            textInputWindow.OkClick += AddDirectoryOk;

        }


        private void AddDirectoryOk(Window callingWindow)
        {
            string newDirectoryName = ((TextInputWindow)callingWindow).Text;

            if (FileManager.IsRelative(newDirectoryName))
            {

                newDirectoryName = this.mDirectory + ((TextInputWindow)callingWindow).Text;
            }
            if (System.IO.Directory.Exists(newDirectoryName))
            {
                GuiManager.ShowMessageBox("Directory named " + newDirectoryName + " already exists", "Directory exists");
            }
            else
            {
                System.IO.Directory.CreateDirectory(newDirectoryName);
            }

            SetDirectory( mDirectory );
        }


        private void SetGUIPosition(Window callingWindow)
        {
            mListBox.ScaleX = (float)mScaleX - mTextureDisplayButton.ScaleX - 1;
            mListBox.ScaleY = ScaleY - 7;
            mListBox.SetPositionTL(mListBox.ScaleX + .5f, ScaleY + 1);
            mListBox.AdjustScrollSize();

            mBookmarkToggleButton.X = .5f + mBookmarkToggleButton.ScaleX;
            mBookmarkToggleButton.Y = .5f + mBookmarkToggleButton.ScaleY;

            mCurrentDirectoryDisplay.ScaleX = (mScaleX - .5f) - (.5f + mBookmarkToggleButton.ScaleX);
            mCurrentDirectoryDisplay.X = mScaleX + mBookmarkToggleButton.ScaleX;
            mCurrentDirectoryDisplay.Y = 1.9f;

            //            mCurrentDirectoryDisplay.Text = TextManager.
            //            currentDirectory.mScaleY = 1.4f;
            //          currentDirectory.mScaleX = mScaleX - 1.5f;

            mOkButton.SetPositionTL(2 * mScaleX - 5, 2 * ScaleY - 5);
            mOkButton.ScaleY = 1.5f;
            mOkButton.ScaleX = 4;

            mCancelButton.SetPositionTL(2 * mScaleX - 5, 2 * ScaleY - 2);
            mCancelButton.ScaleY = 1.5f;
            mCancelButton.ScaleX = 4;

            mTextureDisplayButton.SetPositionTL(2 * mListBox.ScaleX + 1 + mTextureDisplayButton.ScaleX, 2 * ScaleY - 17);

            // I tried doing this, but man was it a pain!
            //mTextureDisplayButton.Resizable = true;
            //mTextureDisplayButton.Resizing += TextureDisplayButtonResizing;

            loadDirectory.SetPositionTL(2 * mScaleX - 5, 2 * ScaleY - 8);
            loadDirectory.ScaleY = 1.5f;
            loadDirectory.ScaleX = 4;

            //addButton.SetPositionTL(2 * mScaleX - 5, 2 * ScaleY - 11);

            mUpDirectory.SetPositionTL(2 * ScaleX - 8, 2 * ScaleY - 25);
            mCreateNewDirectory.SetPositionTL(2 * ScaleX - 5.8f, 2 * ScaleY - 25);

            saveName.SetPositionTL(ScaleX - 4, 2 * ScaleY - 4.5f);
            if (saveName.ScaleX != ScaleX - 6)
                saveName.ScaleX = ScaleX - 6;

            mFileTypeBox.ScaleX = (float)mScaleX - 6;
            mFileTypeBox.ScaleY = 1.2f;
            mFileTypeBox.X = mFileTypeBox.ScaleX + 2;
            mFileTypeBox.Y = 2 * ScaleY - 1.7f;



        }


        private void UpOneDirectoryClick(Window callingWindow)
        {
            if (mDirectory == ComputerRoot)
                return;

            // We use newDirectory here instead of setting mDirectory because
            // SetDirectory sometimes reverts back to the previous mDirectory if
            // there is an error (like an invalid FTP).
            string newDirectory = mDirectory.Remove(mDirectory.Length - 1, 1);

            bool isAtRootOfDrive = newDirectory.Length < 4 && newDirectory[1] == ':';

            if (isAtRootOfDrive)
            {
                newDirectory = ComputerRoot;

            }
            else
            {
                newDirectory = FileManager.GetDirectory(newDirectory);

                if (newDirectory[newDirectory.Length - 1] != '\\' &&
                    newDirectory[newDirectory.Length - 1] != '/') newDirectory += "/";
            }

            SetDirectory(newDirectory);

            if (mFileTypes.Contains("bmp") || mFileTypes.Contains("jpg") || mFileTypes.Contains("tga") || mFileTypes.Contains("png"))
            {
                mTextureDisplayButton.SetOverlayTextures(null, null);
                if (mTextureFloatingWindow != null)
                {
                    mTextureFloatingWindow.BaseTexture = mTextureDisplayButton.UpOverlayTexture;
                }

            }

            this.mListBox.StartAt = 0;
            mListBox.AdjustScrollSize();
        }


        private void OnCancelClick(Window callingWindow)
        {
            this.CloseWindow();
        }


        private void ChangeSelectedDirectory(Window callingWindow)
        {
            string text = ((ComboBox)callingWindow).Text;

            if (text == Separator)
            {
                mCurrentDirectoryDisplay.Text = CurrentDirectory;
                // do nothing
                return;
            }

#if !XBOX360 && !WINDOWS_PHONE && !MONODROID
            else if (text == "My Documents")
            {
                SetDirectory(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            }
            else if (text == "Desktop")
            {
                SetDirectory(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            }
			else if (text == "My Computer")
			{
				SetDirectory(ComputerRoot);
				//SetDirectory(System.Environment.GetFolderPath(Environment.SpecialFolder.MyComputer));
			}
			else
#endif
			{
				SetDirectory(text);
			}

        }


        private void SetFtpAfterUsernamePassword(Window callingWindow)
        {
            UserNamePasswordWindow unpw = callingWindow as UserNamePasswordWindow;

            mUserName = unpw.UserName;
            mPassword = unpw.Password;

            SetDirectory(unpw.Name);
        }


        private void ShowChildTextureDisplayWindow(Window callingWindow)
        {
            if (mTextureFloatingWindow == null)
            {
                mTextureFloatingWindow = new Window(mCursor);

                this.AddFloatingWindow(mTextureFloatingWindow);

                mTextureFloatingWindow.HasMoveBar = true;
                mTextureFloatingWindow.HasCloseButton = true;
                mTextureFloatingWindow.ScaleX = 8;
                mTextureFloatingWindow.ScaleY = 8;
                mTextureFloatingWindow.BaseTexture = mTextureDisplayButton.UpOverlayTexture;
                mTextureFloatingWindow.Resizable = true;
            }
            else if (mTextureFloatingWindow.Visible == false)
            {
                mTextureFloatingWindow.Visible = true;
            }

        }

		#endregion

		#region Methods

        #region Constructor

        /// <summary>
		/// Creates a new FileWindow.
		/// </summary>
		/// <remarks>
		/// By default, the save name TextBox is invisible making this a text box for loading.  Call
		/// SetToSave to make the box appear.
		/// </remarks>
		/// <param name="InpMan"></param>
		/// <param name="SprMan"></param>
		internal FileWindow(Cursor cursor) : 
            base(cursor)
        {
            #region Create the "settings" directory
            if (System.IO.Directory.Exists(ApplicationFolderForThisProgram + "settings") == false)
                System.IO.Directory.CreateDirectory(ApplicationFolderForThisProgram + "settings");
            #endregion

            #region Set "this" properties

            this.HasMoveBar = true;
			this.Closing += new GuiMessage(OnFileWindowClose);
			mScaleX = 20;
			mScaleY = 20;

            this.Resizable = true;
            this.Resizing += new GuiMessage(SetGUIPosition);
            this.OkClick += new GuiMessage(AddFileToRecent);
            this.Closing += OnClose;
            MinimumScaleX = 13;
            MinimumScaleY = 16;

            #endregion

            mFileTypes = new List<string>();

            #region Create the Texture display button

            mTextureDisplayButton = new Button(mCursor);
            AddWindow(mTextureDisplayButton);
            mTextureDisplayButton.ScaleY = 4;
            mTextureDisplayButton.ScaleX = 4;
            mTextureDisplayButton.Click += ShowChildTextureDisplayWindow;

            #endregion

            #region Create the Bookmark button

            mBookmarkToggleButton = new ToggleButton(mCursor);
            AddWindow(mBookmarkToggleButton);
            mBookmarkToggleButton.ScaleX = mBookmarkToggleButton.ScaleY = 1.3f;
            mBookmarkToggleButton.SetPositionTL(.5f + mBookmarkToggleButton.ScaleX, .5f + mBookmarkToggleButton.ScaleY);
            mBookmarkToggleButton.Click += BookmarkButtonClick;
            mBookmarkToggleButton.SetOverlayTextures(
                9, 3);

            #endregion

            #region CurrentDirectoryDisplay Combo Box
            mCurrentDirectoryDisplay = new ComboBox(mCursor);
            base.AddWindow(mCurrentDirectoryDisplay);
            mCurrentDirectoryDisplay.ItemClick += ChangeSelectedDirectory;
            mCurrentDirectoryDisplay.TextChange += ChangeSelectedDirectory;
            mCurrentDirectoryDisplay.AllowTypingInTextBox = true;
            ResetCurrentDirectoryComboBox();

            #endregion

            #region Create the ListBox

            mListBox = new ListBox(mCursor);
            AddWindow(mListBox);

            mListBox.SortingStyle = ListBoxBase.Sorting.None;
			mListBox.Highlight += new GuiMessage(OnListBoxClick);
			mListBox.StrongSelect += new GuiMessage(OnListBoxStrongSelect);
			mListBox.EscapeRelease += new GuiMessage(CloseFileWindow);
            mListBox.CurrentToolTipOption = ListBoxBase.ToolTipOption.CursorOver;

            #endregion

            #region Create the Ok Button

            mOkButton = new Button(mCursor);
            AddWindow(mOkButton);
            mOkButton.Text = "Ok";
			mOkButton.Click += new GuiMessage(OkButtonClick);

            #endregion

            #region Create the Cancel button

            mCancelButton = new Button(mCursor);
            AddWindow(mCancelButton);
            mCancelButton.Text = "Cancel";
            mCancelButton.Click += OnCancelClick;

            #endregion

            #region Create the Load Directory Button

            loadDirectory = new Button(mCursor);
            AddWindow(loadDirectory);

            loadDirectory.Text = "Load Dir";
			loadDirectory.Click += new GuiMessage(LoadDirClick);
            loadDirectory.Visible = false;

            #endregion

            //#region Create the "Add" button

            //addButton = new Button(mCursor);
            //AddWindow(addButton);
            //addButton.ScaleX = 4;
            //addButton.ScaleY = 1.5f;
            //addButton.Text = "Add";
            //addButton.Click += new GuiMessage(AddButtonClick);
            //addButton.Visible = false;

            //#endregion

            #region Create the Up Directory button

            mUpDirectory = new Button(mCursor);
            AddWindow(mUpDirectory);

#if FRB_MDX
            // This is always null in the new engines - not sure how this button gets its texture.
			mUpDirectory.SetOverlayTextures(GuiManager.mUpDirectory, null);
#endif
			mUpDirectory.ScaleY = 1;
			mUpDirectory.ScaleX = 1;
            mUpDirectory.Text = "Up One Directory";
            
            mUpDirectory.overlayTL = new FlatRedBall.Math.Geometry.Point(0.38281250, 0.6445312500);
            mUpDirectory.overlayTR = new FlatRedBall.Math.Geometry.Point(0.42968750, 0.6445312500);
            mUpDirectory.overlayBL = new FlatRedBall.Math.Geometry.Point(0.38281250, 0.687500000);
            mUpDirectory.overlayBR = new FlatRedBall.Math.Geometry.Point(0.42968750, 0.687500000);

            mUpDirectory.Click += this.UpOneDirectoryClick;

            #endregion

            mCreateNewDirectory = new Button(mCursor);
            base.AddWindow(mCreateNewDirectory);
            mCreateNewDirectory.SetOverlayTextures(5, 3);
            mCreateNewDirectory.ScaleX = 1;
            mCreateNewDirectory.ScaleY = 1;
            mCreateNewDirectory.Text = "Create New\nDirectory";
            mCreateNewDirectory.Click += AddDirectoryClick;

            saveName = new TextBox(mCursor);
            AddWindow(saveName);
			saveName.ScaleY = 1.4f;
            saveName.Visible = false;
            saveName.fixedLength = false;
			saveName.EnterPressed += new GuiMessage(OnSaveNameEnter);
			saveName.EscapeRelease += new GuiMessage(CloseFileWindow);

            Name = "Loading File";

//			displayTextureLoadedBefore = false;


            mShowFileHierarchy = new ToggleButton(mCursor);
            AddWindow(mShowFileHierarchy);
			mShowFileHierarchy.SetPositionTL(7.0f, 4.5f);
			mShowFileHierarchy.ScaleX = 5.5f;
			mShowFileHierarchy.Text = "File Hierarchy";
            mShowFileHierarchy.SetOneAlwaysDown(true);
			mShowFileHierarchy.Click += new GuiMessage(OnFileHierarchyClick);

            mAllRelativeToggleButton = new ToggleButton(mCursor);
            AddWindow(mAllRelativeToggleButton);
			mAllRelativeToggleButton.SetPositionTL(7.0f, 6.5f);
			mAllRelativeToggleButton.ScaleX = 5.5f;
			mAllRelativeToggleButton.Text = "All Relative";
			mAllRelativeToggleButton.Click += new GuiMessage(OnAllRelativeClick);
			mAllRelativeToggleButton.SetOneAlwaysDown(true);
			mAllRelativeToggleButton.AddToRadioGroup(mShowFileHierarchy);

            mShowRecent = new ToggleButton(mCursor);
            AddWindow(mShowRecent);

            mShowRecent.SetPositionTL(19, 4.5f);
            mShowRecent.ScaleX = 5.5f;
            mShowRecent.Text = "Recent Files";
            mShowRecent.Click += new GuiMessage(OnShowRecent);
            mShowRecent.SetOneAlwaysDown(true);
            mShowRecent.AddToRadioGroup(mShowFileHierarchy);

            // go here!!!
			mShowFileHierarchy.Press();

            mFileTypeBox = new ComboBox(mCursor);
            AddWindow(mFileTypeBox);
            mFileTypeBox.ItemClick += OnFileTypeChange;

#if XBOX360 || WINDOWS_PHONE || MONODROID
            SetDirectory();

#else
            SetDirectory(FileManager.MyDocuments);
#endif


            SetGUIPosition(null);

            
            LoadBookmarks();
        }

        #endregion

        #region Public Methods

        public override void ClearEvents()
        {
            base.ClearEvents();
            OkClick = null;
            betweenLoadFolder = null;
            
        }


		public bool IsGraphicalFileWindow()
		{
			return (mFileTypes.Contains("bmp") || mFileTypes.Contains("jpg") || mFileTypes.Contains("tga") || mFileTypes.Contains("png"));
		}


        public bool IsSpecialFolder(string path)
        {
#if XBOX360 || WINDOWS_PHONE || MONODROID
            return false;
#else
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            // remove an ending / since the GetFolderPath method doesn't return
            // paths with an ending slash
            if (path.EndsWith("\\") || path.EndsWith("/"))
            {
                path = path.Substring(0, path.Length - 1);
            }
			string standardizedPath = FileManager.Standardize(path);

            return FileManager.Standardize(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop)) ==
				standardizedPath ||
                FileManager.Standardize(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)) ==
				standardizedPath ||
				FileManager.Standardize(System.Environment.GetFolderPath(Environment.SpecialFolder.MyComputer)) ==
				standardizedPath;
#endif
        }


        public void SetCredentials(string userName, string password)
        {
            this.mUserName = userName;
            this.mPassword = password;
        }

        // this should probably be replaced by a directory property
		public void SetDirectory(string directoryToSet)
        {
            try
            {

                #region directoryToSet is null, set a valid value
                if (directoryToSet == null)
                {
                    if (string.IsNullOrEmpty(sLastUntypedDirectory))
                    {
                        directoryToSet = FileManager.RelativeDirectory;
                    }
                    else
                    {
                        directoryToSet = sLastUntypedDirectory;
                    }

                }
                #endregion

                #region Make sure that there is a forwardslash at the end of the path.
                if (directoryToSet != ComputerRoot && directoryToSet.EndsWith("/") == false && directoryToSet.EndsWith(@"\") == false)
                {
                    directoryToSet = directoryToSet + "/";
                }

                #endregion

                #region Update Listbox
                if (mAllRelativeToggleButton.IsPressed)
                {
                    SetDirectoryAllRelative(directoryToSet);
                }

                else if (mShowRecent.IsPressed)
                {
                    PopulateWithRecentFiles();
                }

                else
                {
                    FillListDisplayFromDirectory(directoryToSet);
                }

                #endregion

                UpdateBookmarkButtonToCurrentDirectory();

                mListBox.AdjustScrollSize();
            }
            catch (System.UnauthorizedAccessException e)
            {
#if XBOX360 || WINDOWS_PHONE || MONODROID
                GuiManager.ShowMessageBox("Error attempting to set the directory to " +
                    directoryToSet + ":\n" + e.ToString(), "Error");
#else
                if (!IsSpecialFolder(directoryToSet))
                {
                    GuiManager.ShowMessageBox("Unauthorized access to\n\n" +
                        directoryToSet + "\n\nReturning to My Documents", "Error");

                    SetDirectory(
                        FileManager.Standardize(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)));

                }
                else
                {
                    GuiManager.ShowMessageBox("Error attempting to set the directory to " +
                        directoryToSet + ":\n" + e.ToString(), "Error");
                }
#endif
            }
        }


		public void SetDirectory()
		{
            if (string.IsNullOrEmpty(sLastUntypedDirectory))
            {
                SetDirectory(IO.FileManager.RelativeDirectory);
            }
            else
            {
                SetDirectory(sLastUntypedDirectory);
            }
		}


        public static void SetLastDirectory(string fileType, string directory)
        {
            if (sLastDirectoriesViewed.ContainsKey(fileType))
                sLastDirectoriesViewed[fileType] = directory;
            else
                sLastDirectoriesViewed.Add(fileType, directory);

            sLastUntypedDirectory = directory;
        }


        public void SetToLoad()
        {

            Name = "Loading File";

            //showAvailableRelative.Visible = true;

            saveName.Visible = false;

            InputManager.ReceivingInput = mListBox;

        }


        public void SetToSave()
        {
            // This is needed to return to file hierarchy
            // if the user clicks the recent files.
            //mShowFileHierarchy.Visible = false;

            Name = "Saving File";

            //showAvailableRelative.Visible = false;

            saveName.Visible = true;

            InputManager.ReceivingInput = saveName;
        }

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
        public void SetFileType(string FileType)
        {
            mFileTypes.Clear();

            if (FileType == "graphic")
            {
                AddFileType("bmp");
                AddFileType("png");
                AddFileType("jpg");
                AddFileType("tga");
                AddFileType("dds");
                AddFileType("gif");
            }
            else if (FileType == "graphic and animation")
            {
                AddFileType("bmp");
                AddFileType("png");
                AddFileType("jpg");
                AddFileType("tga");
                AddFileType("dds");
                AddFileType("gif");

                AddFileType("ach");
                AddFileType("achx");
            }
            else
                AddFileType(FileType);

            if (sLastDirectoriesViewed.ContainsKey(mFileTypes[0]))
            {
                SetDirectory(sLastDirectoriesViewed[mFileTypes[0]]);
            }
            else if (string.IsNullOrEmpty(sLastUntypedDirectory) == false)
            {
                SetDirectory(sLastUntypedDirectory);
            }
            else if (string.IsNullOrEmpty(mDirectory))
            {
                SetDirectory(FileManager.RelativeDirectory);
            }
            else
            {
                // just to refresh the list
                SetDirectory(mDirectory);
            }

            UpdateFileTypeComboBoxToStringList();

            mFileTypeBox.Text = FileType;
        }


        public void SetFileType(List<string> FileType)
        {
            mFileTypes = FileType;
            SetDirectory(mDirectory);

            UpdateFileTypeComboBoxToStringList();
        }


        //public void ShowAddButton()
        //{
        //    this.addButton.Visible = true;
        //}


        public void ShowLoadDirButton()
        {
            this.loadDirectory.Visible = true;
        }

        #endregion

        #region Private Methods

        private void AddFileType(string type)
        {
            if (mFileTypes.Contains(type) == false)
                this.mFileTypes.Add(type);
        }


        private void CallOkClick()
        {
            if (OkClick != null)
            {
                try
                {
                    OkClick(this);
                }
                catch (System.UnauthorizedAccessException)
                {
                    if(this.Results.Count != 0)
                    {
                        if (Results.Count == 1)
                        {
                            GuiManager.ShowMessageBox("Unauthorized access.  Error saving the file: \n" + Results[0], "Error saving");
                        }
                        else
                        {
                            GuiManager.ShowMessageBox("Unauthorized access.  Error saving all files.", "Error saving");

                        }
                    }
                    else
                    {
                        GuiManager.ShowMessageBox("Unauthorized access.  No data has been saved.", "Error saving");

                    }
                }

            }
        }


        private void FillListDisplayFromDirectory(string directoryToSet)
        {
            string oldDirectory = mCurrentDirectoryDisplay.Text;

            if (directoryToSet != "My Documents" && directoryToSet != "Desktop" && directoryToSet != "My Computer" &&
                oldDirectory != "My Documents" && oldDirectory != "Desktop" && oldDirectory != "My Computer" &&
				!IsSpecialFolder(oldDirectory) &&
                Directory.Exists(directoryToSet))
            {

                mLastNonSystemDirectory.Text = directoryToSet;
            }

            mCurrentDirectoryDisplay.Text = directoryToSet;

            bool isDirectoryRoot = false;
            bool isFtp = false;

#if !XBOX360 && !WINDOWS_PHONE && !MONODROID
            isDirectoryRoot = directoryToSet == ComputerRoot;
            isFtp = FtpManager.IsFtp(directoryToSet);
#endif
            if (isDirectoryRoot)
            {
                SetDirectoryToComputerRoot(directoryToSet);
            }
            else if (isFtp)
            {
                SetDirectoryToFtp(directoryToSet);
            }
            else if (Directory.Exists(directoryToSet))
            {
                SetDirectoryToLocalDirectory(directoryToSet);
            }
            else
            {
                mCurrentDirectoryDisplay.Text = mDirectory;
                GuiManager.ShowMessageBox("Directory doesn't exist.", "Invalid Directory");
            }

            // Add the folder icons, but make sure you don't do it again.
            // Functions called above to set the directory may fail, and if
            // that's the case, then the ListBox won't be cleared and repopulated.
            if (mListBox.Count != 0 && mListBox[0].Icons.Count == 0)
            {
                for (int i = 0; i < mNumberOfDisplayedDirectories; i++ )
                {
                    ListBoxIcon icon = mListBox[i].AddIcon(
                        146 / 256f,
                        157 / 256f, 
                        156 / 256f,
                        167 / 256f,
                        "Folder");

                    icon.Enabled = false;
                }
            }
        }



        private void LoadBookmarks()
        {
            string bookmarkLocation = ApplicationFolderForThisProgram + "/FileBookmarks.txt";

            #region Get if doesFileBookmarkFileExists
            bool doesFileBookmarkFileExist = false;

            doesFileBookmarkFileExist = File.Exists(bookmarkLocation);

            #endregion

            if (doesFileBookmarkFileExist)
            {
                string contentsOfFile = FileManager.FromFileText(bookmarkLocation);

                // replace any /r with nothing
                contentsOfFile = contentsOfFile.Replace("\r", "");

                string[] bookmarks = contentsOfFile.Split('\n');

                if (bookmarks.Length != 0)
                {
                    ResetCurrentDirectoryComboBox();

                    mBookmarks.Clear();

                    mBookmarks.AddRange(bookmarks);

                    // sometimes an empty string creeps in the bookmarks file.  If so, remove it
                    while (mBookmarks.Contains(""))
                    {
                        mBookmarks.Remove("");
                    }

                    FlatRedBall.Utilities.StringFunctions.RemoveDuplicates(mBookmarks);
                }

                ResetCurrentDirectoryComboBox();
            }



        }


        private void PopulateWithRecentFiles()
        {
            mUpDirectory.Visible = false;
            mCreateNewDirectory.Visible = false;

            string directory = ApplicationFolderForThisProgram;

            mListBox.Clear();
            foreach (string type in mFileTypes)
            {
                // first see if the file even exists
                if (System.IO.File.Exists(directory + @"recent" + type + ".txt"))
                {
                    // it does, so read the file into a string
                    using (System.IO.StreamReader sr =
                        new System.IO.StreamReader(directory + @"recent" + type + ".txt"))
                    {
                        string lineRead;
                        while ((lineRead = sr.ReadLine()) != null)
                        {
                            // make sure the recent file both exists and also that it hasn't
                            // been added to the file list yet
                            if (mListBox.GetItemByName(lineRead) == null &&
                                FileManager.FileExists(lineRead))
                            {
                                mListBox.InsertItem(0, lineRead);
                            }
                        }
                    }
                }
            }
        }

        
        private void ResetCurrentDirectoryComboBox()
        {
            mCurrentDirectoryDisplay.Clear();

            mCurrentDirectoryDisplay.AddItem("");
            mLastNonSystemDirectory = mCurrentDirectoryDisplay.FindItemByText("");

            mCurrentDirectoryDisplay.AddItem("Desktop");
            mCurrentDirectoryDisplay.AddItem("My Computer");
            mCurrentDirectoryDisplay.AddItem("My Documents");

            if (CurrentDirectory != "My Documents" && CurrentDirectory != "Desktop" && 
				CurrentDirectory != "My Computer" &&
				!IsSpecialFolder(CurrentDirectory))
            {
                mLastNonSystemDirectory.Text = CurrentDirectory;
            }

            if (mBookmarks != null && mBookmarks.Count != 0)
            {
                mCurrentDirectoryDisplay.AddItem(Separator);

                foreach (string s in mBookmarks)
                {
                    mCurrentDirectoryDisplay.AddItem(s);
                }
            }
        }


        private void SaveBookmarks()
        {
            if (mBookmarks != null)
            {
                string bookmarkLocation = ApplicationFolderForThisProgram + "/FileBookmarks.txt";

                string directory = ApplicationFolderForThisProgram;

                if (Directory.Exists(directory) == false)
                {
                    Directory.CreateDirectory(directory);
                }
                System.IO.StreamWriter sw = null;

                try
                {
                    sw =
                        new System.IO.StreamWriter(bookmarkLocation, true);


                    foreach (string s in mBookmarks)
                    {
                        sw.WriteLine(s);
                    }
                }
                catch
                {
                    // do nothing - probably read-only
                }
                finally
                {
                    if (sw != null)
                    {
                        sw.Close();
                        sw.Dispose();
                    }
                }
            }
        }


        private void SetDirectoryAllRelative(string directoryToSet)
        {
            if (directoryToSet == "Computer")
            {
                GuiManager.ShowMessageBox("You must enter a drive (such as C:\\) before viewing all relative.", "Error");
                mShowFileHierarchy.Press();
            }
            else
            {

                List<string> allFiles =
                                FileManager.GetAllFilesInDirectory(directoryToSet);

                if (mFileTypes.Count != 0)
                {
                    for (int i = allFiles.Count - 1; i > -1; i--)
                    {
                        string extension = FileManager.GetExtension(allFiles[i]);

                        if ( !mFileTypes.Contains(extension))
                            allFiles.RemoveAt(i);
                    }
                }

                for (int i = 0; i < allFiles.Count; i++)
                    allFiles[i] = FileManager.MakeRelative(allFiles[i], directoryToSet);

                mListBox.Clear();

                int fileCount = allFiles.Count;

                for(int i = 0; i < fileCount; i++)
                {
                    mListBox.AddItem(allFiles[i]);
                }


                mUpDirectory.Visible = false;
                mCreateNewDirectory.Visible = false;
                }
        }


        private void SetDirectoryToComputerRoot(string directoryToSet)
        {
#if XBOX360 || WINDOWS_PHONE || MONODROID
            throw new NotImplementedException();
#else
            mDirectory = directoryToSet;
            mListBox.Clear();
            DriveInfo[] drives = System.IO.DriveInfo.GetDrives();

            for (int i = 0; i < drives.Length; i++)
            {
                mListBox.AddItem(drives[i].Name);
            }

            mNumberOfDisplayedDirectories = drives.Length;
#endif
        }





        private void SetDirectoryToFtp(string directoryToSet)
        {
#if !XBOX360 && !WINDOWS_PHONE && !MONODROID

            #region See if the Username/password is valid
            if (string.IsNullOrEmpty(mUserName) || string.IsNullOrEmpty(mPassword))
            {
                // Set the directory back
                mCurrentDirectoryDisplay.Text = mDirectory;

                UserNamePasswordWindow unpw = new UserNamePasswordWindow(mCursor);
                GuiManager.AddDominantWindow(unpw);
                unpw.Name = directoryToSet;

                unpw.OkClick += SetFtpAfterUsernamePassword;


                return;
            }

            #endregion


            FileStruct[] files = null;
            try
            {
                files =
                    FtpManager.GetList(directoryToSet, mUserName, mPassword);
            }
            catch(System.UriFormatException)
            {
                TryToHandleFtpException(directoryToSet);
                return;
            }
            catch(System.Net.WebException exception)
            {
                if (exception.Status == System.Net.WebExceptionStatus.ProtocolError)
                {
                    mUserName = null;
                    mPassword = null;

                    // Set the directory back
                    mCurrentDirectoryDisplay.Text = mDirectory;

                    UserNamePasswordWindow unpw = new UserNamePasswordWindow(mCursor);
                    GuiManager.AddDominantWindow(unpw);
                    unpw.Name = directoryToSet;

                    unpw.OkClick += SetFtpAfterUsernamePassword;


                    return;
                }
                else
                {
                    TryToHandleFtpException(directoryToSet);
                }
                return;
            }

            mDirectory = directoryToSet;
            mListBox.Clear();

            mNumberOfDisplayedDirectories = 0;


            for (int i = 0; i < files.Length; i++)
            {
                FileStruct file = files[i];

                if (file.IsDirectory)
                {
                    mNumberOfDisplayedDirectories++;
                    mListBox.AddItem(file.Name);

                }
            }

            for (int i = 0; i < files.Length; i++)
            {
                FileStruct file = files[i];

                if (!file.IsDirectory)
                {
                    mListBox.AddItem(file.Name);
                }
            }
#endif
        }


        private void SetDirectoryToLocalDirectory(string directoryToSet)
        {
            mDirectory = directoryToSet;
            mListBox.Clear();
            string[] files;

            if (mFileTypes.Contains(""))
            {
                files = System.IO.Directory.GetFileSystemEntries(mDirectory);

                for (int i = 0; i < files.Length; i++)
                    files[i] = files[i].Remove(0, mDirectory.Length);
                mListBox.AddArray(files);
                // now we need to see how many directories we have
                files = System.IO.Directory.GetDirectories(mDirectory);
                mNumberOfDisplayedDirectories = files.Length;
            }
            else
            {
                // Get all directories
                files = System.IO.Directory.GetDirectories(mDirectory);
                for (int i = 0; i < files.Length; i++) files[i] = files[i].Remove(0, mDirectory.Length);
                mNumberOfDisplayedDirectories = files.Length;
                mListBox.AddArray(files);

                if (mFileTypes.Count != 0)
                {
                    // The user has set file types, so loop through them and 
                    // filter the results

                    // loop through all file types and add them to the list
                    for (int i = 0; i < mFileTypes.Count; i++)
                    {
                        files = FlatRedBall.IO.FileManager.GetAllFilesInDirectory(
                            mDirectory, mFileTypes[i], 0).ToArray();

                        for (int j = 0; j < files.Length; j++) files[j] = files[j].Remove(0, mDirectory.Length);
                        mListBox.AddArray(files);
                    }
                }
                else
                {
                    // The user hasn't set any file types, so show all files
                    files = FlatRedBall.IO.FileManager.GetAllFilesInDirectory(mDirectory, "", 0).ToArray();

                    for (int j = 0; j < files.Length; j++) files[j] = files[j].Remove(0, mDirectory.Length);
                    mListBox.AddArray(files);
                }
            }
        }

#if !XBOX360 && !WINDOWS_PHONE && !MONODROID
        private void TryToHandleFtpException(string directoryToSet)
        {
            if (directoryToSet != mDirectory)
            {
                // Give a message and recur once.
                // If this doesn't hit, then we've already tried to recur once, so we'll exit.
                GuiManager.ShowMessageBox("The FTP address:\n\n" + directoryToSet + "\n\nis invalid.", "Bad FTP");

                mCurrentDirectoryDisplay.Text = mDirectory;
                if (FtpManager.IsFtp(mDirectory))
                {
                    SetDirectoryToFtp(mDirectory);
                }
            }
        }
#endif

        private void UpdateBookmarkButtonToCurrentDirectory()
        {
            if (mBookmarks.Contains(CurrentDirectory))
                mBookmarkToggleButton.PressNoCall();
            else
                mBookmarkToggleButton.Unpress();
               

        }


        private void UpdateFileTypeComboBoxToStringList()
        {
            mFileTypeBox.Clear();

            if (IsSavingFile)
            {
                
                // If saving, make one item for each type
                foreach (string fileType in mFileTypes)
                {
                    mFileTypeBox.AddItem(fileType, fileType);

                }
            }
            else
            {
                // For now just show all file types that are available and disable the Combo Box
                mFileTypeBox.Enabled = false;

                string fileTypeString = "";

                for (int i = 0; i < mFileTypes.Count; i++)
                {
                    if (i != 0)
                    {
                        fileTypeString += ", ";
                    }

                    fileTypeString += "*." + mFileTypes[i];
                }
                mFileTypeBox.Text = fileTypeString;
                //mFileTypeBox.AddItem(fileTypeString);
            }
        }


        #endregion

        #endregion
    }
}