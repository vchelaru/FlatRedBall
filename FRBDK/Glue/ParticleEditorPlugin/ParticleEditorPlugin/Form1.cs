using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ParticleEditorControls.Managers;
using FlatRedBall.IO;
using System.Threading;
using ParticleEditorControls;
using System.IO;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Content.Particle;

namespace ParticleEditorPlugin
{
    public partial class Form1 : Form
    {
        public string FileName
        {
            get
            {
                return fileName;
            }
            private set
            {
                fileName = value;
                this.Text = Path.GetFileName(fileName);
            }
        }
        private string fileName = null;

        public Form1()
        {
            InitializeComponent();
            StartPreviewer();

            mainControl1.PropertyValueChanged += EmitterStateChanged;
        }

        /// <summary>
        /// Saves the current emitter list to the current filename
        /// </summary>
        private void SaveFile()
        {
            FileManager.XmlSerialize(ProjectManager.Self.EmitterSaveList, FileName);
        }

        /// <summary>
        /// Make sure the user is okay with potentially losing changes.
        /// Usually called when loading or creating a new particle list
        /// before old list is unloaded.
        /// </summary>
        /// <returns>True if user is okay with losing changes.</returns>
        private bool ConfirmLosingChanges()
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                if (MessageBox.Show("Are you sure you want to open a new file? Unsaved changes will be lost!", "Confirm New", MessageBoxButtons.YesNo) != System.Windows.Forms.DialogResult.Yes)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Starts a flatredball game instance to preview emitters.
        /// </summary>
        private void StartPreviewer()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                ParticlePreviewer.Instance.TryRun();
            });
        }

        /// <summary>
        /// Opens a dialog to get a new filename path
        /// </summary>
        /// <returns>The path the user chose for the new file.</returns>
        private string GetSavePath()
        {
            string returnPath = null;

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.FileName = Path.GetFileName(fileName);
            saveDialog.DefaultExt = ".emix";
            saveDialog.AddExtension = true;

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                returnPath = saveDialog.FileName;
            }

            return returnPath;
        }

        /// <summary>
        /// Sends the current save object to the previewer for live preview.
        /// </summary>
        private void DisplaySelectedEmitter()
        {
            //if (!ParticlePreviewer.Instance.Running)
            //{
            //    MessageBox.Show("It appears that the particle previewer is not running. Please start the previewer from the Preview menu.");
            //}
            //else
            //{
                try
                {
                    ParticlePreviewer.Instance.DisplayEmitter(ApplicationState.Self.SelectedEmitterSave, Path.GetDirectoryName(fileName));
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("primary thread"))
                    {
                        // TODO: Improve this dirty hack! Swallow messages about adding textures on primary thread
                    }
                    else
                    {
                        MessageBox.Show("Failed to preview emitter: " + ex.Message + ":\n" + ex.StackTrace);
                    }
                }
            //}
        }


        #region Event Handlers
        /// <summary>
        /// Duplicates the selected emitter
        /// </summary>
        private void duplicateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EmitterSave duplicate = FlatRedBall.IO.FileManager.CloneObject(ApplicationState.Self.SelectedEmitterSave);
            duplicate.Name += "_duplicate";
            ProjectManager.Self.EmitterSaveList.emitters.Add(duplicate);
            
            // Manually trigger state change event.
            EmitterStateChanged(sender, e);
        }

        /// <summary>
        /// Opens a file dialog to load an emix.
        /// </summary>
        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // early out if user doesn't want to lose changes
            if (ConfirmLosingChanges() == false)
            {
                return;
            }

            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "*.emix|*.*";
            openDialog.RestoreDirectory = true;

            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                FileName = openDialog.FileName;
                ProjectManager.Self.Load(fileName);
            }

        }

        /// <summary>
        /// Creates a new particle list.
        /// </summary>
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // early out if user doesn't want to lose changes
            if (ConfirmLosingChanges() == false)
            {
                return;
            }

            string path = GetSavePath();
            if (path == null)
            {
                MessageBox.Show("The particle editor requires a valid path to map particle textures. Please choose a valid path when creating a new emitter list.");
            }
            else
            {
                // early out if file already exists and user doesn't want to overwrite
                if (File.Exists(path) && MessageBox.Show("This file appears to already exist. Do you want to save over it?", "Confirm Overwrite", MessageBoxButtons.YesNo) != System.Windows.Forms.DialogResult.Yes)
                {
                    return;
                }

                // This is a bit of a hack to create a new list
                // because the editor expects to load a list from a file.
                FileName = path;
                EmitterSaveList newList = EmitterSaveList.FromEmitterList(new EmitterList());
                FileManager.XmlSerialize(newList, fileName);
                ProjectManager.Self.Load(fileName);
            }
        }

        /// <summary>
        /// Starts the previwer
        /// </summary>
        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartPreviewer();
        }

        /// <summary>
        /// Forces a reload of the currently previewing emitter.
        /// </summary>
        private void previewSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                MessageBox.Show("No file path exists to specify where content manager should get particle textures! Did you load an emitter file using File>Load?");
            }
            else if (ApplicationState.Self.SelectedEmitterSave == null)
            {
                MessageBox.Show("No valid emitter selected to preview.");
            }
            else
            {
                DisplaySelectedEmitter();
            }
        }

        /// <summary>
        /// Handle a property change: auto-save and display
        /// </summary>
        void EmitterStateChanged(object sender, EventArgs e)
        {
            TreeViewManager.Self.RefreshTreeView();
            SaveFile();
            DisplaySelectedEmitter();
        }

        /// <summary>
        /// Saves the emitter list over the existing file.
        /// </summary>
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile();
        }

        /// <summary>
        /// Saves the emitter list as a new file and 
        /// updates the filename reference to point to the new file
        /// </summary>
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string newPath = GetSavePath();
            if (newPath != null)
            {
                FileName = newPath;
                SaveFile();
            }
        }

        #endregion
    }
}
