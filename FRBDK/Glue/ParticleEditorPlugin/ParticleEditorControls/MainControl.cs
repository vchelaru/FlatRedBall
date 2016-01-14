using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ParticleEditorControls.Managers;
using FlatRedBall.Content.Particle;

namespace ParticleEditorControls
{
    public partial class MainControl : UserControl
    {
        #region Properties

        public EmitterSave SelectedEmitterSave
        {
            get
            {
                return ApplicationState.Self.SelectedEmitterSave;
            }
        }

        #endregion

        #region Events

        public event EventHandler PropertyValueChanged;
        public event EventHandler ListAddOrRemove;
        public event EventHandler EmitAllClick;
        public event EventHandler EmitCurrentClick;

        #endregion

        public MainControl()
        {
            InitializeComponent();

            PropertyGridManager.Self.Initialize(MainPropertyGrid, EmissionSettingsPropertyGrid);
            PropertyGridManager.Self.PropertyValueChanged += new EventHandler(HandlePropertyValueChangedInternal);
            TreeViewManager.Self.Initialize(EmitterTreeView);
            TreeViewRightClickManager.Self.ListAddOrRemove += new EventHandler(HandleListAddOrRemoveInternal);

            TreeViewRightClickManager.Self.Initialize(EmitterTreeView);
        }

        void HandleListAddOrRemoveInternal(object sender, EventArgs e)
        {
            if (ListAddOrRemove != null)
            {
                ListAddOrRemove(this, null);
            }
        }

        public void LoadEmitterSave(string fileName)
        {
            ProjectManager.Self.Load(fileName);
        }

        public void SaveCurrentEmitter()
        {
            ProjectManager.Self.SaveLastLoaded();
        }

        void HandlePropertyValueChangedInternal(object sender, EventArgs e)
        {
            var selectedNode = TreeViewManager.Self.SelectedTreeNode;

            if(selectedNode != null)
            {
                var emitterSave = TreeViewManager.Self.SelectedTreeNode.Tag as EmitterSave;
                TreeViewManager.Self.RefreshTreeViewFor(emitterSave);
            }

            if (PropertyValueChanged != null)
            {
                PropertyValueChanged(this, null);
            }
        }

        private void EmitterTreeView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                TreeViewManager.Self.SelectedTreeNode = 
                    this.EmitterTreeView.GetNodeAt(e.X, e.Y);
                TreeViewRightClickManager.Self.HandleRightClick();
            }
        }

        private void EmitAllButton_Click(object sender, EventArgs e)
        {
            if (this.EmitAllClick != null)
            {
                EmitAllClick(this, null);
            }
        }

        private void EmitCurrentButton_Click(object sender, EventArgs e)
        {
            if (this.EmitCurrentClick != null)
            {
                EmitCurrentClick(this, null);
            }
        }


    }
}
