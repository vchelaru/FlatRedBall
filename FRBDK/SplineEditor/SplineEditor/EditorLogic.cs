using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Input;

using FlatRedBall.Gui;
using FlatRedBall.Math.Splines;
using Microsoft.Xna.Framework;
using ToolTemplate.Entities;
using ToolTemplate.Gui;
using Microsoft.Xna.Framework.Graphics;

using Keys = Microsoft.Xna.Framework.Input.Keys;
using EditorObjects;
using EditorObjects.Undo.PropertyComparers;
using SplineEditor.States;
using SplineEditor.Commands;
using SplineEditor.ViewModels;

namespace ToolTemplate
{
    public class EditorLogic
    {
        #region Fields

        private ReactiveHud mReactiveHud = new ReactiveHud();

        AllObjectsToolbarViewModel allObjectsToolbarViewModel;

        Spline mCurrentSpline;
        SplinePoint mCurrentSplinePoint;


        Spline mSplineOver;
        SplinePoint mSplinePointOver;
        SplinePoint mGrabbedSplinePoint;
        int mHandleIndexOver0Base = -1;

        float mXGrabOffset;
        float mYGrabOffset;

        bool mSwitchedCurrentOnLastPush;

        #endregion

        #region Properties

        public Spline CurrentSpline
        {
            get
            {
                Spline toReturn = null;

                bool found = false;
                if (mCurrentSplinePoint != null)
                {
                    foreach (var spline in EditorData.SplineList)
                    {
                        if (spline.Contains(mCurrentSplinePoint))
                        {
                            toReturn = spline;
                            found = true;
                            break;
                        }
                    }

                }

                if (!found)
                {
                    toReturn = mCurrentSpline;
                }

                return toReturn;
            }
            set
            {
                if (mCurrentSpline != null)
                {
                    mCurrentSpline.PointColor = Color.DarkGray;
                    mCurrentSpline.PathColor = Color.DarkMagenta;

                    mCurrentSplinePoint = null;
                }

                mCurrentSpline = value;

                if (mCurrentSpline != null)
                {
                    // I don't know why we forced visible here, it should
                    // just stay whatever it was before:
                    //mCurrentSpline.Visible = true;
                    mCurrentSpline.PointColor = Color.White;
                    mCurrentSpline.PathColor = Color.Red;

                    mCurrentSpline.CalculateVelocities();
                    mCurrentSpline.CalculateAccelerations();
                    mCurrentSpline.CalculateDistanceTimeRelationships(.05f);

                    // Before highlighting the object let's make sure that the list is showing it
                    // Update: Doing this actually refreshes the list and this can screw selection and
                    // collapse state.  It's annoying, and I don't think we need it:
                    //GuiData.SplineListDisplay.UpdateToList();

                    mCurrentSpline.Visible = true;
                }

                GuiData.UpdateToSpline(mCurrentSpline);


                UpdateDeselectedSplineVisibility();


                if (mCurrentSpline == null ||
                    (CurrentSplinePoint != null && !mCurrentSpline.Contains(mCurrentSplinePoint)))
                {
                    CurrentSplinePoint = null;
                }
            }
        }

        public SplinePoint CurrentSplinePoint
        {
            get
            {
                return mCurrentSplinePoint;

            }
            set
            {
                mCurrentSplinePoint = value;

                GuiData.UpdateToSplinePoint(mCurrentSplinePoint);
            }
        }

        bool showDeselectedSplines;
        public bool ShowDeselectedSplines
        {
            get
            {
                return showDeselectedSplines;
            }
            set
            {
                showDeselectedSplines = value;
                UpdateDeselectedSplineVisibility();
            }
        }

        #endregion

        #region Methods

        #region Constructor

        public EditorLogic()
        {

        }

        #endregion

        #region Public Methods

        public void Update()
        {
            UpdateSplines();

            MouseSplineActivity();

            mReactiveHud.Update();

            AppCommands.Self.Preview.SplineCrawlerActivity();

            KeyboardShortcutActivity();
        }

        internal AllObjectsToolbarViewModel GetAllObjectsToolbarViewModel()
        {
            if(allObjectsToolbarViewModel == null)
            {
                CreateAllObjectsToolbarViewModel();
            }
            return allObjectsToolbarViewModel;
        }

        private void CreateAllObjectsToolbarViewModel()
        {
            allObjectsToolbarViewModel = new AllObjectsToolbarViewModel();
            this.ShowDeselectedSplines = allObjectsToolbarViewModel.ShowDeselectedSplines;
            allObjectsToolbarViewModel.PropertyChanged += AllObjectsToolbarViewModel_PropertyChanged;
        }

        private void AllObjectsToolbarViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(allObjectsToolbarViewModel.ShowDeselectedSplines))
            {
                ShowDeselectedSplines = allObjectsToolbarViewModel.ShowDeselectedSplines;
            }
        }

        #endregion

        #region Private Methods

        private void GetSplineAndSplinePointOver()
        {
            mSplinePointOver = null;
            mSplineOver = null;
            mHandleIndexOver0Base = -1;


            #region First see if the cursor is over the current Spline

            if (mCurrentSpline != null)
            {
                if (mReactiveHud.SplinePointSelectionMarker.HasCursorOver(GuiManager.Cursor, out mHandleIndexOver0Base))
                {
                    mSplineOver = AppState.Self.CurrentSpline;
                    mSplinePointOver = AppState.Self.CurrentSplinePoint;
                }
                else
                {
                    SplinePoint splinePointOver = GetSplinePointOver(mCurrentSpline);

                    if (splinePointOver != null)
                    {
                        mSplinePointOver = splinePointOver;
                        mSplineOver = mCurrentSpline;
                        return;
                    }
                }
            }

            #endregion

            #region If not, try the other Splines

            for (int i = 0; i < EditorData.SplineList.Count; i++)
            {
                Spline spline = EditorData.SplineList[i];
                if (spline.Visible)
                {
                    SplinePoint splinePointOver = GetSplinePointOver(spline);

                    if (splinePointOver != null)
                    {
                        mSplineOver = spline;
                        mSplinePointOver = splinePointOver;
                        break;
                    }
                }
            }

            #endregion
        }

        private SplinePoint GetSplinePointOver(Spline spline)
        {


            foreach (SplinePoint splinePoint in spline)
            {
                float splinePointRadius = AppState.Self.Preview.SplinePointRadius / Camera.Main.PixelsPerUnitAt(splinePoint.Position.Z);
                float cursorWorldX = GuiManager.Cursor.WorldXAt(splinePoint.Position.Z);
                float cursorWorldY = GuiManager.Cursor.WorldYAt(splinePoint.Position.Z);

                if (
                    (splinePoint.Position -
                     new Vector3(cursorWorldX, cursorWorldY, splinePoint.Position.Z)).Length() < splinePointRadius)
                {
                    return splinePoint;
                }
            }

            return null;
        }


        private void UpdateDeselectedSplineVisibility()
        {
            foreach(var spline in EditorData.SplineList)
            {
                if(spline != mCurrentSpline)
                {
                    spline.Visible = showDeselectedSplines;
                }
            }
        }


        private void KeyboardShortcutActivity()
        {

            if (InputManager.ReceivingInput == null)
            {
                EditorObjects.CameraMethods.KeyboardCameraControl(SpriteManager.Camera);
            }

            #region CTRL+C copy current Spline

            if (InputManager.InputReceiver == null && InputManager.Keyboard.ControlCPushed() && CurrentSpline != null)
            {
                Spline newSpline = CurrentSpline.Clone() as Spline;
                EditorData.SplineList.Add(newSpline);

                EditorData.InitializeSplineAfterCreation(newSpline);

                AppCommands.Self.Gui.RefreshTreeView();
            }

            #endregion

            #region SPACE - create spline crawler

            if (InputManager.Keyboard.KeyPushed(Keys.Space) &&
                !InputManager.IsKeyConsumedByInputReceiver(Keys.Space))
            {
                AppCommands.Self.Preview.CreateSplineCrawler();

            }
            #endregion

        }

        private void MouseGrabbingActivity()
        {
            Cursor cursor = GuiManager.Cursor;

            if (cursor.PrimaryPush && GuiManager.Cursor.WindowOver == null)
            {
                mCurrentSplinePoint = null;

                if (mReactiveHud.SplineMover.IsMouseOver == false)
                {
                    mSwitchedCurrentOnLastPush = mCurrentSpline != mSplineOver;

                    CurrentSpline = mSplineOver;
                    // Make sure we set the CurrentSplinePoint after setting the CurrentSpline so that
                    // the GUI for the CurrentSpline is already set.
                    CurrentSplinePoint = mSplinePointOver;
                    mGrabbedSplinePoint = mSplinePointOver;

                    if (mGrabbedSplinePoint != null)
                    {
                        float cursorX = cursor.WorldXAt(mGrabbedSplinePoint.Position.Z);
                        float cursorY = cursor.WorldYAt(mGrabbedSplinePoint.Position.Z);

                        mXGrabOffset = mGrabbedSplinePoint.Position.X - cursorX;
                        mYGrabOffset = mGrabbedSplinePoint.Position.Y - cursorY;
                    }

                }
                // else if over the spline mover, let the current spline point stay at null
            }

            if (cursor.PrimaryDown && mGrabbedSplinePoint != null)
            {
                if (mHandleIndexOver0Base != -1)
                {
                    float cursorX = cursor.WorldXAt(mGrabbedSplinePoint.Position.Z);
                    float cursorY = cursor.WorldYAt(mGrabbedSplinePoint.Position.Z);

                    float differenceX = cursorX - mGrabbedSplinePoint.Position.X;
                    float differenceY = cursorY - mGrabbedSplinePoint.Position.Y;

                    differenceX *= 2;
                    differenceY *= 2;

                    if (mHandleIndexOver0Base == 0)
                    {
                        // Index 0 goes negative, index 1 goes positive
                        differenceX *= -1;
                        differenceY *= -1;
                    }

                    mGrabbedSplinePoint.Velocity.X = differenceX;
                    mGrabbedSplinePoint.Velocity.Y = differenceY;
                }
                else if (!mSwitchedCurrentOnLastPush)
                {
                    mGrabbedSplinePoint.Position = new Vector3(
                        cursor.WorldXAt(mGrabbedSplinePoint.Position.Z) + mXGrabOffset,
                        cursor.WorldYAt(mGrabbedSplinePoint.Position.Z) + mYGrabOffset,
                        mGrabbedSplinePoint.Position.Z);
                }
            }
            if (cursor.PrimaryClick)
            {
                mGrabbedSplinePoint = null;

                GuiData.PropertyGrid.Refresh();
            }


        }

        private void MouseSplineActivity()
        {
            if (GuiManager.Cursor.IsInWindow())
            {
                if (GuiManager.Cursor.PrimaryDown == false)
                {
                    GetSplineAndSplinePointOver();
                }

                MouseGrabbingActivity();
            }
        }



        private void UpdateSplines()
        {
            if (mCurrentSpline != null)
            {
                mCurrentSpline.CalculateVelocities();
                mCurrentSpline.CalculateAccelerations();

                mCurrentSpline.Sort();
            }


            foreach (Spline spline in EditorData.SplineList)
            {
                spline.PointFrequency = .15f;
                spline.UpdateShapes();
            }
        }

        #endregion

        #endregion


    }
}
