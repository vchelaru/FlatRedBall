using FlatRedBall.Math.Splines;
using SplineEditor.Commands;
using SplineEditorXna4.Gui.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using ToolTemplate;
using ToolTemplate.Gui;

namespace SplineEditor.Gui.Forms
{
    public partial class SplineListDisplayForm : Form
    {
        private Controls.SplineListDisplayControl SplineListDisplayControl;

        #region Properties

        public List<Spline> Splines
        {
            get { return SplineListDisplayControl.Splines; }
            set { SplineListDisplayControl.Splines = value; }
        }

        public Spline SelectedSpline
        {
            get { return SplineListDisplayControl.SelectedSpline; }
            set { SplineListDisplayControl.SelectedSpline = value; }
        }

        public SplinePoint SelectedSplinePoint
        {
            get { return SplineListDisplayControl.SelectedSplinePoint; }
            set { SplineListDisplayControl.SelectedSplinePoint = value; }
        }

        public PropertyGrid PropertyGrid
        {
            get
            {
                return this.SplineListDisplayControl.PropertyGrid;
            }
        }
        #endregion

        #region Events

        public event EventHandler SplineSelect;
        public event EventHandler SplinePointSelect;

        #endregion

        public SplineListDisplayForm()
        {
            InitializeComponent();


            SplineListDisplayControl = new Controls.SplineListDisplayControl();
            SplineListDisplayControl.Dock = DockStyle.Fill;
            this.Controls.Add(SplineListDisplayControl);
            SplineListDisplayControl.BringToFront();


            this.SplineListDisplayControl.SplineSelect += HandleSplineSelect;
            this.SplineListDisplayControl.SplinePointSelect += HandleSplinePointSelect;
        }

        private void HandleSplinePointSelect(object sender, EventArgs e)
        {
            if (SplinePointSelect != null)
            {
                SplinePointSelect(this, null);
            }
        }

        private void HandleSplineSelect(object sender, EventArgs e)
        {

            if (SplineSelect != null)
            {
                SplineSelect(this, null);
            } 
            
        }

        internal void UpdateToList()
        {
            this.SplineListDisplayControl.UpdateListDisplay();
        }

        internal void UpdateToList(Spline spline)
        {
            this.SplineListDisplayControl.UpdateListDisplay(spline);
        }

        internal void UpdateToList(SplinePoint splinePoint)
        {
            this.SplineListDisplayControl.UpdateListDisplay(splinePoint);
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AppCommands.Self.File.Load();
        }

        private void addSplineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AppCommands.Self.Edit.AddSpline();
        }

        private void addSplinePointToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AppCommands.Self.Edit.AddSplinePoint();
        }

        private void deleteSplinePointToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AppCommands.Self.Edit.DeleteCurrentSplinePoint();
        }

        private void deleteSplineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AppCommands.Self.Edit.DeleteCurrentSpline();
        }

        private void saveToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            AppCommands.Self.File.Save();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AppCommands.Self.File.SaveAs();
        }

        private void loadScenescnxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AppCommands.Self.File.LoadScene();
        }

        private void cameraPropertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CameraPropertiesWindow cameraPropertiesWindow = new CameraPropertiesWindow(ReactiveHud.Self);

            ElementHost.EnableModelessKeyboardInterop(cameraPropertiesWindow);
            cameraPropertiesWindow.Show();
        }
    }
}
