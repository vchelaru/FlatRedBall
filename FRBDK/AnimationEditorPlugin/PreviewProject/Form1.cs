using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.AnimationEditorForms;
using FlatRedBall.IO;
using PreviewProject.IO;

namespace PreviewProject
{
    public partial class Form1 : Form
    {
        FlatRedBall.AnimationEditorForms.MainControl mMainControl;

        public Form1()
        {
            InitializeComponent();

            mMainControl = new FlatRedBall.AnimationEditorForms.MainControl();
            // We want to make sure to always have a good animation chain:
            this.Controls.Add(mMainControl);
            mMainControl.Dock = DockStyle.Fill;

            CreateToolStripMenuItems();

            mMainControl.XnaInitialize += new Action(HandleXnaInitialize);

        }

        private void ProcessCommandLines(out bool wasAnimationLoaded)
        {
            string[] commandLineArgs = Environment.GetCommandLineArgs();

            wasAnimationLoaded = false;
            if (commandLineArgs.Length == 2)
            {
                mMainControl.LoadAnimationChain(commandLineArgs[1]);
                SetFormTextToLoadedFile();

                wasAnimationLoaded = true;
            }

        }

        private void SetFormTextToLoadedFile()
        {
            this.Text = "AnimationEditor - " + ProjectManager.Self.FileName;
        }

        private void CreateToolStripMenuItems()
        {
            ToolStripMenuItem newToolStripItem = new ToolStripMenuItem("New AnimationChain List", null, HandleNewClick);

            ToolStripMenuItem loadToolStripItem = new ToolStripMenuItem("Load...", null, HandleLoadClick);
            ToolStripMenuItem saveToolStripItem = new ToolStripMenuItem("Save", null, HandleSaveClick);
            ToolStripMenuItem saveAsToolStripItem = new ToolStripMenuItem("Save As...", null, HandleSaveAsClick);

            // Going backwards so I can just insert at index 0 every time
            mMainControl.AddToolStripMenuItem(newToolStripItem, "File");
            mMainControl.AddToolStripMenuItem(loadToolStripItem, "File");
            mMainControl.AddToolStripMenuItem(saveToolStripItem, "File");
            mMainControl.AddToolStripMenuItem(saveAsToolStripItem, "File");
        }

        private void HandleNewClick(object sender, EventArgs e)
        {
            mMainControl.AnimationChainList = new FlatRedBall.Content.AnimationChain.AnimationChainListSave();
            ProjectManager.Self.FileName = null;
            HandleSaveAsClick(null, null);
        }

        private void HandleLoadClick(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Animation Chain (*.achx)|*.achx";
            string fileName = null;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                fileName = dialog.FileName;
            }

            if (!string.IsNullOrEmpty(fileName))
            {
                mMainControl.LoadAnimationChain(fileName);
            }

            SetFormTextToLoadedFile();
        }

        private void HandleSaveClick(object sender, EventArgs e)
        {
            if (mMainControl.AnimationChainList == null)
            {
                MessageBox.Show("No animation file loaded");
            }
            else if (string.IsNullOrEmpty(ProjectManager.Self.FileName))
            {
                HandleSaveAsClick(sender, e);
            }
            else
            {
                mMainControl.SaveCurrentAnimationChain();

                this.Text = "AnimationEditor - " + ProjectManager.Self.FileName;
            }
        }


        private void HandleSaveAsClick(object sender, EventArgs e)
        {
            AchxSaver.Self.InitateSaveProcess(ProjectManager.Self.FileName, mMainControl);
            this.Text = "AnimationEditor - " + ProjectManager.Self.FileName;
        }

        void HandleXnaInitialize()
        {
            //string fileName =
            //    @"C:\FlatRedBallProjects\Games\Baron\NewBaron\NewBaronContent\Entities\GameScreen\CreepBase\AnimationChains\CrocBloc1\CrocBloc1Animations.achx";

            //mMainControl.LoadAnimationChain(fileName);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            bool wasAnimationLoaded;
            ProcessCommandLines(out wasAnimationLoaded);
            if (!wasAnimationLoaded)
            {
                mMainControl.AnimationChainList = new FlatRedBall.Content.AnimationChain.AnimationChainListSave();
            }
            WireframeManager.Self.RefreshAll();
        }
    }
}
