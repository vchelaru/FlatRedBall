using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue;
using FlatRedBall.IO;
using FlatRedBall.Debugging;
using FlatRedBall.Glue.Plugins;
using System.Threading;

namespace OfficialPlugins.ProjectCopier
{
    public partial class CopyToProjectControl : UserControl
    {
        #region Fields

        public event EventHandler CopyClick;
        CopyManager mCopyManager;

        System.Windows.Forms.Timer mTimer;

        #endregion

        public string DestinationDirectory
        {
            get
            {
                return CopyToTextBox.Text;
            }
            set
            {
                CopyToTextBox.Text = value;
            }
        }

        public string SourceDirectory
        {
            get
            {
                if (CopyFromSolutionRootRadioButton.Checked)
                {
                    return ProjectManager.ProjectRootDirectory;
                }
                else
                {
                    return this.CopyFromTextBox.Text;
                }
            }
            set
            {
                this.CopyFromTextBox.Text = value;

                IsUsingCustomSource = !string.IsNullOrEmpty(this.CopyFromTextBox.Text);
            }
        }

        public bool IsUsingCustomSource
        {
            get
            {
                return CopyFromSolutionRootRadioButton.Checked == false;
            }
            set
            {
                if(value)
                {
                    CopyFromCustomFolderRadioButton.Checked = true;
                }
                else
                {
                    CopyFromSolutionRootRadioButton.Checked = true;
                }
            }
        }

        public CopyToProjectControl(CopyManager copyManager)
        {
            InitializeComponent();
            UpdateCopyFromUiEnabledState();
            mCopyManager = copyManager;
            mCopyManager.AfterCopyFinished += HandleAfterCopyFinished;

            mTimer = new System.Windows.Forms.Timer();
            mTimer.Enabled = true;
            mTimer.Interval = 250;
            mTimer.Tick += HandleTimerTick;

            PercentageCompleteLabel.Text = "";

        }

        private void HandleTimerTick(object sender, EventArgs e)
        {

            this.CurrentlyCopyingLabel.Visible = mCopyManager.IsCopying;

            if (mCopyManager.IsCopying)
            {
                PercentageCompleteLabel.Text =
                    mCopyManager.PercentageFinished.ToString("0.00") + "%";
                progressBar1.Value = (int)mCopyManager.PercentageFinished;
                progressBar1.Visible = true;

                CurrentlyCopyingLabel.Text = mCopyManager.CurrentActivityInfo;
            }
            else
            {
                PercentageCompleteLabel.Text = "";
                progressBar1.Visible = false;
            }
        }

        private void HandleAfterCopyFinished(object sender, EventArgs e)
        {
            CopyItButton.Invoke((Action)delegate { CopyItButton.Text = "Copy Projects!"; });
        }

        private void CopyToBrowseButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                this.CopyToTextBox.Text = dialog.SelectedPath + "\\";
            }
        }

        private void CopyItButton_Click(object sender, EventArgs e)
        {
            if (mCopyManager.IsCopying)
            {
                mCopyManager.ShouldContinue = false;
            }
            else
            {
                mCopyManager.Settings.DestinationFolder = CopyToTextBox.Text;
                if(CopyFromSolutionRootRadioButton.Checked)
                {
                    mCopyManager.Settings.RelativeSourceFolder = null;
                }
                else
                {
                    mCopyManager.Settings.EffectiveSourceFolder = CopyFromTextBox.Text;
                }

                TryCreateDirectiory(mCopyManager.Settings.DestinationFolder);

                string whyCantCopy = mCopyManager.GetWhyCantCopy();
                if(!string.IsNullOrEmpty(whyCantCopy))
                {
                    MessageBox.Show(whyCantCopy);
                }
                else
                {
                    mCopyManager.BeginCopying(shouldSave:true);

                    this.CopyItButton.Text = "Cancel Copy";

                    if (CopyClick != null)
                    {
                        CopyClick(this, null);
                    }
                }
            }
        }

        private void TryCreateDirectiory(string destinationFolder)
        {
            if(!System.IO.Directory.Exists(destinationFolder))
            {
                // If the directory doesn't exist, but one directory above does, we'll
                // try to create a directory:
                var parentDirectory = FileManager.GetDirectory(destinationFolder);

                if(System.IO.Directory.Exists(parentDirectory))
                {
                    // parent exists, but the desired directory doesn't, so let's try to make it:
                    System.IO.Directory.CreateDirectory(destinationFolder);
                }
            }
        }

        private void CopyBackButton_Click(object sender, EventArgs e)
        {
            bool shouldContinue = true;

            if(mCopyManager.IsCopying)
            {
                shouldContinue = false;
            }

            string from = null;
            string to = null;

            if (shouldContinue)
            {
                // We're copying back, so use the "opposite" text boxes:
                from = CopyToTextBox.Text;
                to = this.CopyFromTextBox.Text;

                if(string.IsNullOrEmpty(to) && CopyFromSolutionRootRadioButton.Checked)
                {
                    // copy to the root (where the .sln is located)
                    to = ProjectManager.ProjectRootDirectory;
                }

                var response = MessageBox.Show(
                    "Are you sure you want to copy the project back from\n" +
                    from + "\nto\n" + to + "\n\nThis will overwrite files in your Glue project.",
                    "Copy back?",
                    MessageBoxButtons.YesNo
                    );

                shouldContinue = response == DialogResult.Yes;
            }

            if(shouldContinue)
            {
                mCopyManager.Settings.DestinationFolder = to;
                mCopyManager.Settings.EffectiveSourceFolder = from;

                string whyCantCopy = mCopyManager.GetWhyCantCopy();

                if(!string.IsNullOrEmpty(whyCantCopy))
                {
                    MessageBox.Show(whyCantCopy);
                    shouldContinue = false;
                }
            }

            if(shouldContinue)
            {
                mCopyManager.BeginCopying(shouldSave:false);

                this.CopyItButton.Text = "Cancel Copy";

                if (CopyClick != null)
                {
                    CopyClick(this, null);
                }
            }


        }

        private void CopyFromSolutionRootRadioChecked(object sender, EventArgs e)
        {
            UpdateCopyFromUiEnabledState();
        }

        private void CopyFromCustomFolderRadioChecked(object sender, EventArgs e)
        {
            UpdateCopyFromUiEnabledState();
        }

        private void UpdateCopyFromUiEnabledState()
        {
            CopyFromBrowseButton.Enabled = CopyFromSolutionRootRadioButton.Checked == false;
            CopyFromTextBox.Enabled = CopyFromSolutionRootRadioButton.Checked == false;
        }

        private void CopyFromBrowserButtonClick(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                this.CopyFromTextBox.Text = dialog.SelectedPath + "\\";
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            mCopyManager.Settings.CopyingDetermination = CopyingDetermination.SourceVsDestinationDates;
        }

        private void AllSinceLastCopyRadio_CheckedChanged(object sender, EventArgs e)
        {
            mCopyManager.Settings.CopyingDetermination = CopyingDetermination.SinceLastCopy;
        }


    }
}
