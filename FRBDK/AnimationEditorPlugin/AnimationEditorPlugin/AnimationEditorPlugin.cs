using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins;
using System.Windows.Forms;
using FlatRedBall.Glue.Controls;
using FlatRedBall.AnimationEditorForms;
using FlatRedBall.IO;
using FlatRedBall.AnimationEditorForms.CommandsAndState;
using FlatRedBall.Content.AnimationChain;


namespace AnimationEditorPlugin
{

    [Export(typeof(PluginBase))]
    public class AnimationEditorPlugin : PluginBase
    {
        #region Fields

        MainControl mAchxControl;
        TextureCoordinateSelectionWindow mTextureCoordinateControl;
        TabControl mContainer; // This is the tab control for all tabs on the left
        PluginTab mTab; // This is the tab that will hold our control
        string mLastFile;
        TextureCoordinateSelectionLogic textureCoordinateSelectionLogic;

        int mReloadsToIgnore = 0;

        #endregion

        #region Properties


        [Import("GlueCommands")]
        public IGlueCommands GlueCommands
        {
            get;
            set;
        }

        [Import("GlueState")]
        public IGlueState GlueState
        {
            get;
            set;
        }

        public override string FriendlyName
        {
            get { return "Animation Editor"; }
        }

        public override Version Version
        {
            get { return new Version(1, 4, 6); }
        }

        public bool IsSelectedItemSprite
        {
            get
            {
                NamedObjectSave nos = GlueState.CurrentNamedObjectSave;

                return nos != null && nos.SourceType == SourceType.FlatRedBallType &&
                    nos.SourceClassType == "Sprite";

                
            }
        }

        #endregion

        #region Methods

        public AnimationEditorPlugin() : base()
        {
            textureCoordinateSelectionLogic = new TextureCoordinateSelectionLogic();
        }

        public override void StartUp()
        {
            // Do anything your plugin needs to do when it first starts up
            this.InitializeCenterTabHandler += HandleInitializeTab;

            this.ReactToItemSelectHandler += HandleItemSelect;

            this.ReactToFileChangeHandler += HandleFileChange;

            this.ReactToLoadedGlux += HandleGluxLoad;

            this.ReactToChangedPropertyHandler += HandleChangedProperty;

        }

        bool ignoringChanges = false;
        private void HandleChangedProperty(string changedMember, object oldValue)
        {
            if (!ignoringChanges)
            {

                if (IsSelectedItemSprite)
                {
                    if (changedMember == "Texture" ||

                        changedMember == "LeftTextureCoordinate" ||
                        changedMember == "RightTextureCoordinate" ||
                        changedMember == "TopTextureCoordinate" ||
                        changedMember == "BottomTextureCoordinate" ||

                        changedMember == "LeftTexturePixel" ||
                        changedMember == "RightTexturePixel" ||
                        changedMember == "TopTexturePixel" ||
                        changedMember == "BottomTexturePixel"
                        )
                    {
                        textureCoordinateSelectionLogic.RefreshSpriteDisplay(mTextureCoordinateControl);
                    }
                }
            }
        }

        private void HandleGluxLoad()
        {
            ApplicationState.Self.ProjectFolder =
                FlatRedBall.Glue.ProjectManager.ContentDirectory;
        }
        
        public override bool ShutDown(PluginShutDownReason reason)
        {
            // Do anything your plugin needs to do to shut down
            // or don't shut down and return false
            if (mTab != null)
            {
                mContainer.Controls.Remove(mTab);
            }
            mContainer = null;
            mTab = null;
            mAchxControl = null;
            mTextureCoordinateControl = null;
            return true;
        }

        void HandleFileChange(string fileName)
        {
            string standardizedChangedFile = FileManager.Standardize(fileName, null, false);
            string standardizedCurrent = 
                FileManager.Standardize(FlatRedBall.AnimationEditorForms.ProjectManager.Self.FileName, null, false);

            if (standardizedChangedFile == standardizedCurrent)
            {
                if (mReloadsToIgnore == 0)
                {
                    mAchxControl.LoadAnimationChain(standardizedCurrent);
                }
                else
                {
                    mReloadsToIgnore--;
                }
            }
        }

        void HandleItemSelect(TreeNode selectedTreeNode)
        {
            HandleIfAchx(selectedTreeNode);

            HandleIfSprite(selectedTreeNode);
        }

        private void HandleIfSprite(TreeNode selectedTreeNode)
        {
            if (IsSelectedItemSprite)
            {
                if (!mContainer.Controls.Contains(mTab))
                {
                    mContainer.Controls.Add(mTab);

                    //mContainer.SelectTab(mContainer.Controls.Count - 1);
                }

                mTab.Text = "  Texture Coordinates"; // add spaces to make room for the X to close the plugin

                if (mTextureCoordinateControl == null)
                {
                    mTextureCoordinateControl = new TextureCoordinateSelectionWindow();
                    mTextureCoordinateControl.Dock = DockStyle.Fill;
                    mTextureCoordinateControl.RegionChanged += HandleRegionChanged;

                }

                if (!mTab.Controls.Contains(mTextureCoordinateControl))
                {
                    mTab.Controls.Add(mTextureCoordinateControl);
                }
                if (mTab.Controls.Contains(mAchxControl))
                {
                    mTab.Controls.Remove(mAchxControl);
                }

                textureCoordinateSelectionLogic.RefreshSpriteDisplay(mTextureCoordinateControl);
            }
        }



        private void HandleRegionChanged()
        {
            ignoringChanges = true;

            textureCoordinateSelectionLogic.HandleCoordinateChanged(
                mTextureCoordinateControl, GlueState.CurrentNamedObjectSave);

            ignoringChanges = false;
        }


        private void HandleIfAchx(TreeNode selectedTreeNode)
        {
            ReferencedFileSave rfs = selectedTreeNode?.Tag as ReferencedFileSave;

            bool shouldShowAnimationChainUi = rfs != null && rfs.Name != null && FileManager.GetExtension(rfs.Name) == "achx";




            if (shouldShowAnimationChainUi)
            {
                if (!mContainer.Controls.Contains(mTab))
                {
                    mContainer.Controls.Add(mTab);

                    mContainer.SelectTab(mContainer.Controls.Count - 1);
                }

                if (mAchxControl == null)
                {
                    mAchxControl = new MainControl();

                    ToolStripMenuItem saveToolStripItem = new ToolStripMenuItem("Force Save", null, HandleSaveClick);
                    mAchxControl.AddToolStripMenuItem(saveToolStripItem, "File");

                    ToolStripMenuItem forceSaveAllItem = new ToolStripMenuItem("Re-Save all .achx files in this Glue project", null, HandleForceSaveAll);
                    mAchxControl.AddToolStripMenuItem(forceSaveAllItem, "File");


                    mAchxControl.AnimationChainChange += new EventHandler(HandleAnimationChainChange);
                    mAchxControl.Dock = DockStyle.Fill;
                }

                mTab.Text = "  Animation"; // add spaces to make room for the X to close the plugin

                if(!mTab.Controls.Contains(mAchxControl))
                {
                    mTab.Controls.Add(mAchxControl);
                }
                if(mTab.Controls.Contains(mTextureCoordinateControl))
                {
                    mTab.Controls.Remove(mTextureCoordinateControl);
                }

                string fullFileName = FlatRedBall.Glue.ProjectManager.MakeAbsolute(rfs.Name);
                mLastFile = fullFileName;

                if (System.IO.File.Exists(fullFileName))
                {
                    mAchxControl.LoadAnimationChain(fullFileName);
                }
            }
            else if (mContainer.Controls.Contains(mTab))
            {
                mContainer.Controls.Remove(mTab);
            }
        }

        private void HandleForceSaveAll(object sender, EventArgs e)
        {
            foreach(var rfs in FlatRedBall.Glue.Elements.ObjectFinder.Self.GetAllReferencedFiles()
                .Where(item=>FileManager.GetExtension(item.Name) == "achx"))
            {
                string fullFileName = FlatRedBall.Glue.ProjectManager.MakeAbsolute(rfs.Name);

                if (System.IO.File.Exists(fullFileName))
                {
                    try
                    {
                        AnimationChainListSave acls = AnimationChainListSave.FromFile(fullFileName);

                        acls.Save(fullFileName);

                        PluginManager.ReceiveOutput("Re-saved " + rfs.ToString());
                    }
                    catch (Exception exc)
                    {
                        PluginManager.ReceiveError(exc.ToString());
                    }
                }

            }
        }

        private void HandleSaveClick(object sender, EventArgs e)
        {
            mReloadsToIgnore++;
            mAchxControl.SaveCurrentAnimationChain();
        }

        void HandleInitializeTab(TabControl tabControl)
        {
            mTab = new PluginTab();
            mContainer = tabControl;

            mTab.ClosedByUser += new PluginTab.ClosedByUserDelegate(OnClosedByUser);

        
        
        }

        void HandleAnimationChainChange(object sender, EventArgs e)
        {
            mReloadsToIgnore++;
            mAchxControl.SaveCurrentAnimationChain();
        }

        void OnClosedByUser(object sender)
        {
            PluginManager.ShutDownPlugin(this);
        }

        #endregion
    }
}
