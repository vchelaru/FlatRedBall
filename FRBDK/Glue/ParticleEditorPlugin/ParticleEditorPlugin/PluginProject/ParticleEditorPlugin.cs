using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins;
using ParticleEditorControls;
using System.Windows.Forms;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.IO;
using FlatRedBall.Glue.SaveClasses;
using System.Diagnostics;

namespace PluginProject
{
    [Export(typeof(PluginBase))]
    public class ParticleEditorPlugin : PluginBase
    {
        #region Fields

        MainControl mControl;
        TabControl mContainer;
        PluginTab mTab;
        string mLastFile;

        #endregion

        int mReloadsToIgnore = 0;

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
            get { return "Particle Editor"; }
        }

        public override Version Version
        {
            get { return new Version(1, 4); }
        }


        public override void StartUp()
        {
            // Do anything your plugin needs to do when it first starts up
            this.InitializeCenterTabHandler += HandleInitializeTab;

            this.ReactToItemSelectHandler += HandleItemSelect;

            this.ReactToFileChangeHandler += HandleFileChange;

        }

        public override bool ShutDown(PluginShutDownReason reason)
        {
            if (mTab != null)
            {
                mContainer.Controls.Remove(mTab);
            }
            mContainer = null;
            mTab = null;
            mControl = null;
            return true;
        }

        void HandleFileChange(string fileName)
        {
            string standardizedChangedFile = FileManager.Standardize(fileName, null, false);
            string standardizedCurrent =
                FileManager.Standardize(ParticleEditorControls.Managers.ProjectManager.Self.FileName, null, false);

            if (standardizedChangedFile == standardizedCurrent)
            {
                if (mReloadsToIgnore == 0)
                {
                    mControl.LoadEmitterSave(standardizedCurrent);
                }
                else
                {
                    mReloadsToIgnore--;
                }
            }
        }

        void HandleItemSelect(TreeNode selectedTreeNode)
        {
            ReferencedFileSave rfs = selectedTreeNode?.Tag as ReferencedFileSave;
            if (mContainer.Controls.Contains(mTab))
            {
                mContainer.Controls.Remove(mTab);
            }


            if (rfs != null && rfs.Name != null && FileManager.GetExtension(rfs.Name) == "emix")
            {
                if (!mContainer.Controls.Contains(mTab))
                {
                    mContainer.Controls.Add(mTab);

                    mContainer.SelectTab(mContainer.Controls.Count - 1);
                }

                string fullFileName = FlatRedBall.Glue.ProjectManager.MakeAbsolute(rfs.Name);
                mLastFile = fullFileName;
                mControl.LoadEmitterSave(fullFileName);
            }
        }

        void HandleInitializeTab(TabControl tabControl)
        {
            mControl = new MainControl();
            mControl.PropertyValueChanged += new EventHandler(HandleValueChanged);
            mControl.ListAddOrRemove += new EventHandler(HandleValueChanged);
            mControl.EmitAllClick += new EventHandler(HandleEmitAllClick);
            mControl.EmitCurrentClick += new EventHandler(HandleEmitCurrentClick);

            mTab = new PluginTab();
            mContainer = tabControl;

            mTab.ClosedByUser += new PluginTab.ClosedByUserDelegate(OnClosedByUser);

            mTab.Text = "  Emitters"; // add spaces to make room for the X to close the plugin
            mTab.Controls.Add(mControl);
            mControl.Dock = DockStyle.Fill;
        }

        void HandleEmitAllClick(object sender, EventArgs e)
        {
            ReferencedFileSave rfs = GlueState.CurrentReferencedFileSave;
            if (rfs != null)
            {
                string name = FileManager.RemovePath(FileManager.RemoveExtension(rfs.Name));
                GlueCommands.GlueViewCommands.SendScript(name + ".Emit();");
            }
        }

        void HandleEmitCurrentClick(object sender, EventArgs e)
        {
            ReferencedFileSave rfs = GlueState.CurrentReferencedFileSave;
            if (rfs != null)
            {
                string emitterSaveName = null;
                if (mControl.SelectedEmitterSave != null)
                {
                    emitterSaveName = mControl.SelectedEmitterSave.Name;
                }
                string name = FileManager.RemovePath(FileManager.RemoveExtension(rfs.Name));


                string command = name + ".FindByName(\"" + emitterSaveName + "\").Emit();";

                GlueCommands.GlueViewCommands.SendScript(command);
            }

        }

        void HandleValueChanged(object sender, EventArgs e)
        {
            mReloadsToIgnore++;
            mControl.SaveCurrentEmitter();
        }

        void OnClosedByUser(object sender)
        {
            PluginManager.ShutDownPlugin(this);
        }
    }
}
