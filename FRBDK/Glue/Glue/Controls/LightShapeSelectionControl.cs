using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace FlatRedBall.Glue.Controls
{
    public enum MarqueeLightShape
    {
        Square,
        Circle
    }


    public partial class LightShapeSelectionControl : UserControl
    {
        private MarqueeLightShape lightShapeValue = MarqueeLightShape.Square;
        private IWindowsFormsEditorService editorService = null;
        private System.Windows.Forms.Panel squarePanel;
        private System.Windows.Forms.Panel circlePanel;


        // This constructor takes a MarqueeLightShape value from the
        // design-time environment, which will be used to display
        // the initial state.
        public LightShapeSelectionControl(
            MarqueeLightShape lightShape,
            IWindowsFormsEditorService editorService)
        {
            // This call is required by the designer.
            InitializeComponent();
            InitializeComponentCustom();

            // Cache the light shape value provided by the 
            // design-time environment.
            this.lightShapeValue = lightShape;

            // Cache the reference to the editor service.
            this.editorService = editorService;

            // Handle the Click event for the two panels. 
            this.squarePanel.Click += new EventHandler(squarePanel_Click);
            this.circlePanel.Click += new EventHandler(circlePanel_Click);
        }


        // LightShape is the property for which this control provides
        // a custom user interface in the Properties window.
        public MarqueeLightShape LightShape
        {
            get
            {
                return this.lightShapeValue;
            }

            set
            {
                if (this.lightShapeValue != value)
                {
                    this.lightShapeValue = value;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using (
                System.Drawing.Graphics gSquare = this.squarePanel.CreateGraphics(),
                gCircle = this.circlePanel.CreateGraphics())
            {
                // Draw a filled square in the client area of
                // the squarePanel control.
                gSquare.FillRectangle(
                    Brushes.Red,
                    0,
                    0,
                    this.squarePanel.Width,
                    this.squarePanel.Height
                    );

                // If the Square option has been selected, draw a 
                // border inside the squarePanel.
                if (this.lightShapeValue == MarqueeLightShape.Square)
                {
                    gSquare.DrawRectangle(
                        Pens.Black,
                        0,
                        0,
                        this.squarePanel.Width - 1,
                        this.squarePanel.Height - 1);
                }

                // Draw a filled circle in the client area of
                // the circlePanel control.
                gCircle.Clear(this.circlePanel.BackColor);
                gCircle.FillEllipse(
                    Brushes.Blue,
                    0,
                    0,
                    this.circlePanel.Width,
                    this.circlePanel.Height
                    );

                // If the Circle option has been selected, draw a 
                // border inside the circlePanel.
                if (this.lightShapeValue == MarqueeLightShape.Circle)
                {
                    gCircle.DrawRectangle(
                        Pens.Black,
                        0,
                        0,
                        this.circlePanel.Width - 1,
                        this.circlePanel.Height - 1);
                }
            }
        }

        private void squarePanel_Click(object sender, EventArgs e)
        {
            this.lightShapeValue = MarqueeLightShape.Square;

            this.Invalidate(false);

            this.editorService.CloseDropDown();
        }

        private void circlePanel_Click(object sender, EventArgs e)
        {
            this.lightShapeValue = MarqueeLightShape.Circle;

            this.Invalidate(false);

            this.editorService.CloseDropDown();
        }

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponentCustom()
        {
            this.squarePanel = new System.Windows.Forms.Panel();
            this.circlePanel = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // squarePanel
            // 
            this.squarePanel.Location = new System.Drawing.Point(8, 10);
            this.squarePanel.Name = "squarePanel";
            this.squarePanel.Size = new System.Drawing.Size(60, 60);
            this.squarePanel.TabIndex = 2;
            // 
            // circlePanel
            // 
            this.circlePanel.Location = new System.Drawing.Point(80, 10);
            this.circlePanel.Name = "circlePanel";
            this.circlePanel.Size = new System.Drawing.Size(60, 60);
            this.circlePanel.TabIndex = 3;
            // 
            // LightShapeSelectionControl
            // 
            this.Controls.Add(this.squarePanel);
            this.Controls.Add(this.circlePanel);
            this.Name = "LightShapeSelectionControl";
            this.Size = new System.Drawing.Size(150, 80);
            this.ResumeLayout(false);

        }
        #endregion

    }
}
