using FlatRedBall;
using FlatRedBall.Math.Splines;
using FlatRedBall.Utilities;
using Microsoft.Xna.Framework;
using SplineEditor.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ToolTemplate;
using ToolTemplate.Gui;

namespace SplineEditor.Commands
{
    public class EditCommands
    {
        internal void AddSpline()
        {
            Spline spline = new Spline();

            EditorData.SplineList.Add(spline);

            EditorData.InitializeSplineAfterCreation(spline);
            // Refresh tree view after InitializeSplineAfterCreation 
            // so that the new Spline has a name
            AppCommands.Self.Gui.RefreshTreeView();

            AppState.Self.CurrentSpline = spline;

        }

        internal void AddSplinePoint()
        {
            Spline spline = EditorData.EditorLogic.CurrentSpline;
            if (spline == null)
            {
                MessageBox.Show("Select a spline to add points to");
            }
            else
            {
                SplinePoint newSplinePoint = new SplinePoint();
                if (spline.Count > 1)
                {
                    SplinePoint pointBefore = spline[spline.Count - 1];

                    newSplinePoint.Time = pointBefore.Time + 1;

                    if (spline.Count == 1)
                    {
                        newSplinePoint.Position = pointBefore.Position;
                        newSplinePoint.Position.X += 30 / SpriteManager.Camera.PixelsPerUnitAt(newSplinePoint.Position.Z);
                    }
                    else
                    {
                        SplinePoint pointBeforePointBefore = spline[spline.Count - 2];

                        Vector3 difference = pointBefore.Position - pointBeforePointBefore.Position;

                        if (difference == Vector3.Zero)
                        {
                            newSplinePoint.Position = pointBefore.Position;
                            newSplinePoint.Position.X += 30 / Camera.Main.PixelsPerUnitAt(0);
                        }
                        else
                        {
                            newSplinePoint.Position = pointBefore.Position + difference;
                        }

                    }

                }
                else
                {
                    newSplinePoint.Position.X = SpriteManager.Camera.X;
                    newSplinePoint.Position.Y = SpriteManager.Camera.Y;

                    if (spline.Count > 0)
                    {
                        newSplinePoint.Time = spline[0].Time + 1;

                        newSplinePoint.Position = spline[0].Position;
                        newSplinePoint.Position.X += 30 / Camera.Main.PixelsPerUnitAt(0);
                    }
                }
                spline.Add(newSplinePoint);
                spline.CalculateVelocities();
                spline.CalculateAccelerations();
                spline.CalculateDistanceTimeRelationships(.1f);

                GuiData.PropertyGrid.Refresh();
                GuiData.SplineListDisplay.UpdateToList();
            }
            //if (AfterNewPointAdded != null)
            //{
            //    AfterNewPointAdded(this);
            //}
        }

        internal void DeleteCurrentSplinePoint()
        {
            if (AppState.Self.CurrentSplinePoint != null)
            {
                AppState.Self.CurrentSpline.Remove(AppState.Self.CurrentSplinePoint);
                AppState.Self.CurrentSplinePoint = null;

                AppCommands.Self.Gui.RefreshTreeView();
                AppCommands.Self.Gui.RefreshPropertyGrid();
            }


        }

        internal void DuplicateSpline()
        {
            var whatToCopy = AppState.Self.CurrentSpline;

            if (whatToCopy != null)
            {
                var newSpline = AppState.Self.CurrentSpline.Clone();

                var casted = EditorData.SplineList.Cast<INameable>();

                EditorData.SplineList.Add(newSpline);

                EditorData.InitializeSplineAfterCreation(newSpline);
                // Refresh tree view after InitializeSplineAfterCreation 
                // so that the new Spline has a name
                AppCommands.Self.Gui.RefreshTreeView();

                AppState.Self.CurrentSpline = newSpline;
            }
        }

        internal void DeleteCurrentSpline()
        {
            if (AppState.Self.CurrentSpline != null)
            {
                AppState.Self.CurrentSpline.Visible = false;
                EditorData.SplineList.Remove(AppState.Self.CurrentSpline);

                AppState.Self.CurrentSpline = null;


                AppCommands.Self.Gui.RefreshTreeView();
                AppCommands.Self.Gui.RefreshPropertyGrid();
            }
        }

        internal void FlipX()
        {
            var splineToFlip = AppState.Self.CurrentSpline;

            if(splineToFlip != null)
            {
                foreach(var point in splineToFlip)
                {
                    point.Position.X *= -1f;
                }


                splineToFlip.CalculateVelocities();
                splineToFlip.CalculateAccelerations();
                splineToFlip.CalculateDistanceTimeRelationships(.1f);

                GuiData.PropertyGrid.Refresh();

            }
        }
    }
}
