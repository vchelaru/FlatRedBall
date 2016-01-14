using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Content.Math.Splines;
using EditorObjects;
using FlatRedBall.Math.Splines;
using Microsoft.Xna.Framework;
using FlatRedBall;
using FlatRedBall.IO.Remote;
using FlatRedBall.IO;
using SplineEditor.Data;
using FlatRedBall.Content.Scene;

namespace ToolTemplate.Gui
{
    public class Menu : MenuStrip
    {
        #region Event Methods

        #region File

        private void LoadSceneClick(Window callingWindow)
        {
            FileWindow fileWindow = GuiManager.AddFileWindow();
            fileWindow.SetFileType("scnx");
            fileWindow.SetToLoad();
            fileWindow.OkClick += LoadSceneOk;

            if (EditorData.SplineList != null && !string.IsNullOrEmpty(EditorData.SplineList.Name))
            {
                fileWindow.SetDirectory(FileManager.GetDirectory(EditorData.SplineList.Name));

            }

        }

        private void LoadSceneOk(Window callingWindow)
        {
            FileWindow asFileWindow = callingWindow as FileWindow;

            string fileName = asFileWindow.Results[0];

            EditorData.LoadScene(fileName);

        }

    
        #endregion

        private void ScaleAllSplinePositions(Window callingWindow)
        {
            TextInputWindow tiw = 
                GuiManager.ShowTextInputWindow(
                "Enter amount to scale all positions by.  Spline will scale relative to the origin (0,0).", "Enter Scale");
            tiw.Format = TextBox.FormatTypes.Decimal;
            tiw.X += .07f;
            tiw.Y += .01f;

            tiw.Text = "1";

            tiw.OkClick += ScaleAllSplinePositionsOk;
        }

        private void ScaleAllSplinePositionsOk(Window callingWindow)
        {
            float value = float.Parse(((TextInputWindow)callingWindow).Text);

            if (value == 0)
            {
                GuiManager.ShowMessageBox("Scaling by 0 will destroy all Spline positions.", "Error");
            }
            else
            {

                foreach (Spline spline in EditorData.SplineList)
                {
                    for (int i = 0; i < spline.Count; i++)
                    {
                        SplinePoint splinePoint = spline[i];

                        splinePoint.Position = splinePoint.Position * value;
                    }
                }
            }
        }


        private void SplineMovementClick(Window callingWindow)
        {
            EditorData.EditorLogic.CreateSplineCrawler();
        }

        private void TileWindowsClick(Window callingWindow)
        {
            GuiManager.TileWindows();
        }

        private void View3D(Window callingWindow)
        {
            SpriteManager.Camera.Orthogonal = false;
        }

        private void ViewPixelPerfect(Window callingWindow)
        {
            SpriteManager.Camera.UsePixelCoordinates(false);
        }

        #endregion

        #region Methods

        public Menu()
            : base(GuiManager.Cursor)
        {

        }

        #endregion
    }
}
