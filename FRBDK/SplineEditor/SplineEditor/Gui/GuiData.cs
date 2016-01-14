using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Math.Splines;
using EditorObjects;
using SplineEditor.Gui.Forms;
using SplineEditor.Gui.Displayers;
using SplineEditor.Commands;
using SplineEditor.States;
using System.Windows.Forms;

namespace ToolTemplate.Gui
{
    public static class GuiData
    {
        #region Fields

        static SplineListDisplayForm mSplineListDisplay;

        static System.Windows.Forms.PropertyGrid mPropertyGrid;
        //static SplinePointPropertyGrid mPropertyGrid;
        static Menu mMenu;


        #endregion

        #region Properties



        public static SplineListDisplayForm SplineListDisplay
        {
            get { return mSplineListDisplay; }
        }

        public static System.Windows.Forms.PropertyGrid PropertyGrid
        {
            get { return mPropertyGrid; }
        }

        #endregion

        #region Event Methods
        
        private static void AfterRemoveSpline()
        {
            EditorData.EditorLogic.CurrentSpline.Visible = false;
            EditorData.EditorLogic.CurrentSpline = null;
        }

        
        #endregion

        #region Methods

        #region Initialize

        public static void Initialize()
        {
            CreateSplineListDisplayWindow();

            CreatedDisplayers();

            CreateMenuStrip();

        }


        #endregion

        #region Public Methods

        public static void UpdateToSpline(Spline spline)
        {
            GuiData.SplineListDisplay.SelectedSpline = spline;

        }


        public static void UpdateToSplinePoint(SplinePoint splinePoint)
        {
            GuiData.SplineListDisplay.SelectedSplinePoint = splinePoint;



        }

        #endregion

        #region Private Methods

        private static void CreateMenuStrip()
        {
            
        }

        private static void CreateSplineListDisplayWindow()
        {
            mSplineListDisplay = new SplineListDisplayForm();
            Form form = Form.FromHandle(FlatRedBallServices.WindowHandle) as Form;
            mSplineListDisplay.Owner = form;
            mSplineListDisplay.Show(form);

            mSplineListDisplay.Splines = EditorData.SplineList;

            mSplineListDisplay.SplineSelect += HandleSplineSelect;
            mSplineListDisplay.SplinePointSelect += HandleSplinePointSelect;
            mPropertyGrid = mSplineListDisplay.PropertyGrid;
        }

        private static void HandleSplinePointSelect(object sender, EventArgs e)
        {
            AppState.Self.CurrentSplinePoint = mSplineListDisplay.SelectedSplinePoint;
        }

        static void HandleSplineSelect(object sender, EventArgs e)
        {
            AppState.Self.CurrentSpline = mSplineListDisplay.SelectedSpline;
        }


        private static void CreatedDisplayers()
        {

        }

        #endregion

        #endregion
    }
}
