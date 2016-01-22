using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBallProfiler.Managers;
using FlatRedBall.Performance.Measurement;
using System.Reflection;
using FlatRedBallProfiler.Controls;

namespace FlatRedBallProfiler
{
    public partial class MainControl : UserControl
    {
        #region Fields

        public EventHandler AfterLoad;
        public EventHandler AfterLoadRenderBreaks;
        #endregion

        #region Constructor

        public MainControl()
        {

            InitializeComponent();

            // Layout is difficult in winforms land so we're going to do it in code:
            CurrentRenderBreakControl.Dock = DockStyle.Fill;
            RenderBreakHistoryControlHost.Dock = DockStyle.Fill;

            ApplicationState.Self.Initialize(SectionTreeView);
            RenderBreakManager.Self.Initialize(tabControl1, RenderBreakTab, RenderBreakTreeView, pictureBox1);
            RenderBreakPropertyGridManager.Self.Initialize(propertyGrid1);

            ProfilerCommands.Self.RegisterTabControl(this.tabControl1);
        }

        #endregion

        private void loadSectionTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = openFileDialog.FileName;

                ProjectManager.Self.Load(fileName);

                if (AfterLoad != null) AfterLoad(null, null);

            }
        }

        private void MainControl_Load(object sender, EventArgs e)
        {
            TreeViewManager.Self.Initialize(this.SectionTreeView);
        }

        private void expanedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeViewManager.Self.ViewMode = ViewMode.Expaned;
        }

        private void collapsedTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeViewManager.Self.ViewMode = ViewMode.Collapsed;
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            Section currentSection = ApplicationState.Self.CurrentSection;

            if (currentSection == null)
            {
                this.DetailsTextBox.Text = null;
            }
            else
            {
                float percentage = 1;
                if (currentSection.TopParent.Time != 0)
                {
                    percentage = currentSection.Time / currentSection.TopParent.Time;
                }

                percentage *= 100;
                // Let's add another few decimal points here for extra accuracy since it's going
                // to potentially be a really small number
                this.DetailsTextBox.Text = "Of total: " + percentage.ToString("0.0000") + "%" ;
            }
        }

        private void loadFromClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string text = System.Windows.Forms.Clipboard.GetText();

            ProjectManager.Self.LoadFromString(text);

            if (AfterLoad != null) AfterLoad(null, null);

        }

        private void loadRenderBreaksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = openFileDialog.FileName;

                RenderBreakManager.Self.LoadRenderBreaks(fileName);


                if (AfterLoadRenderBreaks != null) AfterLoadRenderBreaks(null, null);

            }
        }

        private void RenderBreakTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            RenderBreakManager.Self.ReactToTreeViewItemSelect();
        }

        private void runGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void PullFromScreenButton_Click(object sender, EventArgs e)
        {
            var screen = FlatRedBall.Screens.ScreenManager.CurrentScreen;
            FieldInfo fieldInfo = null;

            string message = null;

            if (screen == null)
            {
                message = "No screen is available";
            }
            else
            {
                var screenType = screen.GetType();
                fieldInfo = screenType.GetField("mSection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            }

            if(fieldInfo == null)
            {
                message = "Screen has no \"mSection\" field. You need to turn this on in Glue to see load sections";
            }
            else
            { 
                Section section = fieldInfo.GetValue(screen) as Section;

                ProjectManager.Self.Section = section;
            }

            if(!string.IsNullOrEmpty(message))
            {
                MessageBox.Show(message);
            }
        }

        private void FromEngineButton_Click(object sender, EventArgs e)
        {
            RenderBreakManager.Self.GetRenderBreaksFromEngine();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            RenderBreakManager.Self.ShowEntireTexture = checkBox1.Checked;

            RenderBreakManager.Self.ReactToTreeViewItemSelect();
        }

        private void CurrentRenderBreakRadio_CheckedChanged(object sender, EventArgs e)
        {
            UpdateRenderBreakControlVisibility();
        }

        private void HistoryRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            UpdateRenderBreakControlVisibility();
        }

        private void UpdateRenderBreakControlVisibility()
        {
            CurrentRenderBreakControl.Visible = CurrentRenderBreakRadio.Checked;
            RenderBreakHistoryControlHost.Visible = HistoryRadioButton.Checked;
        }
    }
}
