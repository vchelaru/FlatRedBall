using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Content.Particle;
using FlatRedBall.Graphics.Particle;

namespace ParticleEditorControls.Managers
{
    public class TreeViewRightClickManager : Singleton<TreeViewRightClickManager>
    {
        TreeView mTreeView;

        ContextMenuStrip mMenu;

        public event EventHandler ListAddOrRemove;

        public void Initialize(TreeView treeView)
        {
            mTreeView = treeView;

            mMenu = mTreeView.ContextMenuStrip;
        }

        public void HandleRightClick()
        {
            mMenu.Items.Clear();

            if (ApplicationState.Self.SelectedEmitterSave != null)
            {

                mMenu.Items.Add("Delete Emitter", null, DeleteEmitterClick);
            }
            else
            {
                mMenu.Items.Add("Add Emitter...", null, AddEmitterClick);
            }
        }


        public void AddEmitterClick(object sender, EventArgs args)
        {
            TextInputWindow tiw = new TextInputWindow();
            tiw.DisplayText = "Enter new Emitter name";

            if (tiw.ShowDialog() == DialogResult.OK)
            {
                string result = tiw.Result;

                string whyIsntValid = GetWhyNameIsntValid(result);

                if (!string.IsNullOrEmpty(whyIsntValid))
                {
                    MessageBox.Show(whyIsntValid);
                }
                else
                {
                    EmitterSave emitterSave = new EmitterSave();
                    emitterSave.Name = result;

                    // let's set some useful defaults:
                    emitterSave.RemovalEvent = nameof(Emitter.RemovalEventType.Timed);
                    emitterSave.SecondsLasting = 5;
                    emitterSave.EmissionSettings.RadialVelocity = 20;
                    emitterSave.EmissionSettings.RadialVelocityRange = 20;
                    emitterSave.TimedEmission = true;
                    emitterSave.SecondFrequency = .2f;

                    ProjectManager.Self.EmitterSaveList.emitters.Add(emitterSave);


                    TreeViewManager.Self.RefreshTreeView();
                    ApplicationState.Self.SelectedEmitterSave = emitterSave;

                    CallListAddOrRemove();
                }
            }
        }

        public void DeleteEmitterClick(object sender, EventArgs args)
        {

            if (ApplicationState.Self.SelectedEmitterSave != null)
            {
                DialogResult result = 
                    MessageBox.Show("Delete Emitter " + ApplicationState.Self.SelectedEmitterSave.Name + "?",
                    "Delete?", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    EmitterSave emitterToRemove = ApplicationState.Self.SelectedEmitterSave;

                    ApplicationState.Self.SelectedEmitterSave = null;
                    ProjectManager.Self.EmitterSaveList.emitters.Remove(emitterToRemove);

                    TreeViewManager.Self.RefreshTreeView();

                    CallListAddOrRemove();

                }
            }


        }

        private void CallListAddOrRemove()
        {
            if (ListAddOrRemove != null)
            {
                ListAddOrRemove(this, null);
            }
        }

        string GetWhyNameIsntValid(string emitterName)
        {
            foreach (var emitter in ProjectManager.Self.EmitterSaveList.emitters)
            {
                if (emitter.Name == emitterName)
                {
                    return "The name " + emitterName + " is already being used by another Emitter";
                }

            }
            if (string.IsNullOrEmpty(emitterName))
            {
                return "The name can not be empty.";
            }

            return null;

        }

    }
}
