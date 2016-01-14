using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math.Splines;
using FlatRedBall.Gui;
using FlatRedBall;
using Microsoft.Xna.Framework;
using FlatRedBall.Instructions;

namespace EditorObjects.Gui
{
    public class SplinePropertyGrid : PropertyGrid<Spline>
    {
        #region Fields

        ListDisplayWindow mListDisplayWindow;

        #endregion

        #region Properties


        public ListDisplayWindow SplinePointListDisplayWindow
        {
            get { return mListDisplayWindow; }
        }


        public SplinePoint SelectedSplinePoint
        {
            get { return mListDisplayWindow.GetFirstHighlightedObject() as SplinePoint; }
            set 
            {
                mListDisplayWindow.HighlightObject(value, false); 
            }
        }


        public override Spline SelectedObject
        {
            get
            {
                return base.SelectedObject;
            }
            set
            {
                base.SelectedObject = value;

                if (mListDisplayWindow != null)
                {
                    mListDisplayWindow.ObjectDisplaying = value;
                }
            }
        }


        public override List<InstructionList> UndoInstructions
        {
            set
            {
                base.UndoInstructions = value;
                if (mListDisplayWindow != null)
                {
                    mListDisplayWindow.UndoInstructions = value;
                }
            }
        }

        #endregion

        #region Event

        public event GuiMessage AfterNewPointAdded;
        public event GuiMessage HighlightPoint;

        #endregion

        #region Event Methods

        void AdjustNewSplinePoint(Window callingWindow)
        {
            SplinePoint newSplinePoint = mListDisplayWindow.LastItemAdded as SplinePoint;

            if (SelectedObject.Count > 1)
            {
                // Remove the new point out because we're not sure what the sorting has done.
                // We'll add it back in after giving it its time.
                SelectedObject.Remove(newSplinePoint);

                SplinePoint pointBefore = SelectedObject[SelectedObject.Count - 1];

                newSplinePoint.Time = pointBefore.Time + 1;

                if (SelectedObject.Count == 1)
                {
                    newSplinePoint.Position = pointBefore.Position;
                    newSplinePoint.Position.X += 25 / SpriteManager.Camera.PixelsPerUnitAt(newSplinePoint.Position.Z);
                }
                else
                {
                    SplinePoint pointBeforePointBefore = SelectedObject[SelectedObject.Count - 2];

                    Vector3 difference = pointBefore.Position - pointBeforePointBefore.Position;

                    if (difference == Vector3.Zero)
                    {
                        newSplinePoint.Position = pointBefore.Position;
                        newSplinePoint.Position.X += 1;
                    }
                    else
                    {
                        newSplinePoint.Position = pointBefore.Position + difference;
                    }

                }

                SelectedObject.Add(newSplinePoint);
            }
            else
            {
                newSplinePoint.Position.X = SpriteManager.Camera.X;
                newSplinePoint.Position.Y = SpriteManager.Camera.Y;
            }

            if (AfterNewPointAdded != null)
            {
                AfterNewPointAdded(this);
            }
        }

        void NewPointHighlighted(Window callingWindow)
        {
            if (HighlightPoint != null)
            {
                HighlightPoint(this);
            }
        }

        void ScalePointPositionClick(Window callingWindow)
        {
            TextInputWindow tiw = GuiManager.ShowTextInputWindow("Enter amount to scale position by.  Spline will scale relative to its first point.", "Enter Scale");
            tiw.Format = TextBox.FormatTypes.Decimal;
            tiw.Text = "1";

            tiw.OkClick += ScalePointPositionOk;
        }

        void ScalePointPositionOk(Window callingWindow)
        {
            float value = float.Parse(((TextInputWindow)callingWindow).Text);
            if (SelectedObject != null && SelectedObject.Count > 1)
            {
                SplinePoint pointToScaleRelativeTo = SelectedObject[0];

                for(int i = 1; i < SelectedObject.Count; i++)
                {
                    SplinePoint splinePoint = SelectedObject[i];

                    Vector3 difference = splinePoint.Position - pointToScaleRelativeTo.Position;

                    difference *= value;

                    splinePoint.Position = pointToScaleRelativeTo.Position + difference;

                }
            }
        }

        void ScalePointTimeClick(Window callingWindow)
        {
            TextInputWindow tiw = GuiManager.ShowTextInputWindow("Enter amount to scale time by.  A value of 2 will double the length.", "Enter Scale");
            tiw.Format = TextBox.FormatTypes.Decimal;
            tiw.Text = "1";
            tiw.OkClick += ScalePointTimeOk;
        }

        void ScalePointTimeOk(Window callingWindow)
        {
            float value = float.Parse(((TextInputWindow)callingWindow).Text);

            if (SelectedObject != null)
            {
                foreach (SplinePoint splinePoint in SelectedObject)
                {
                    splinePoint.Time *= value;
                }
            }
        }

        void SetSplineStartTo0Click(Window callingWindow)
        {
            if (SelectedObject == null)
                return;

            double start = SelectedObject.StartTime;

            for (int i = 0; i < SelectedObject.Count; i++)
            {
                SelectedObject[i].Time -= start;
            }

        }

        void UpdateListDisplayWindow(Window callingWindow)
        {
            mListDisplayWindow.UpdateToList();
        }

        #endregion

        #region Methods

        #region Constructor

        public SplinePropertyGrid(Cursor cursor)
            : base(cursor)
        {
            #region Exclude Members

            ExcludeMember("Visible");
            ExcludeMember("IsReadOnly");
            ExcludeMember("SplinePointVisibleRadius");
            ExcludeMember("PathColor");
            ExcludeMember("PointColor");
            ExcludeMember("Length");

            #endregion

            #region Create the ListDisplayWindow for this

            mListDisplayWindow = new ListDisplayWindow(cursor);
            mListDisplayWindow.ScaleX = 10;
            mListDisplayWindow.ScaleY = 20;

            Button button = mListDisplayWindow.EnableAddingToList(typeof(SplinePoint));
            button.Text = "Add Point";
            button.X += 1;
            button.ScaleX += 1;

            button = mListDisplayWindow.EnableRemovingFromList();
            button.Text = "Remove Point";
            button.X += 1;
            button.ScaleX += 1;

            mListDisplayWindow.ShowPropertyGridOnStrongSelect = true;
            mListDisplayWindow.AfterAddItem += AdjustNewSplinePoint;
            mListDisplayWindow.ListBox.StrongSelectOnHighlight = true;
            mListDisplayWindow.ListBox.Highlight += NewPointHighlighted;

            if (mSelectedObject != null)
            {
                mListDisplayWindow.ObjectDisplaying = this.ObjectDisplaying;
            }

            if (this.mUndoInstructions != null)
            {
                mListDisplayWindow.UndoInstructions = mUndoInstructions;
            }

            this.AddWindow(mListDisplayWindow, "Points");

            this.AfterUpdateDisplayedProperties += UpdateListDisplayWindow;

            #endregion

            #region Create the Scale Point Time Button

            Button scalePointTime = new Button(mCursor);
            scalePointTime.Text = "Scale Point Time";
            scalePointTime.ScaleX = 10;
            scalePointTime.ScaleY = 1.3f;
            this.AddWindow(scalePointTime, "Points");

            scalePointTime.Click += ScalePointTimeClick;

            #endregion

            #region Create the Scale Point Position Button

            Button scalePointPosition = new Button(mCursor);
            scalePointPosition.Text = "Scale Point Position";
            scalePointPosition.ScaleX = 10;
            scalePointPosition.ScaleY = 1.3f;
            this.AddWindow(scalePointPosition, "Points");

            scalePointPosition.Click += ScalePointPositionClick;

            #endregion

            #region Create the "Set Spline Start to 0" Button

            Button setSplineStartTo0 = new Button(mCursor);
            setSplineStartTo0.Text = "Set Spline Start to 0";
            setSplineStartTo0.ScaleX = 10;
            setSplineStartTo0.ScaleY = 1.3f;
            this.AddWindow(setSplineStartTo0, "Points");
            setSplineStartTo0.Click += SetSplineStartTo0Click;

            #endregion

            SelectCategory("Uncategorized");
        }

        #endregion

        #region Public Methods

        public void HighlightNoCall(SplinePoint splinePoint)
        {
            mListDisplayWindow.HighlightObjectNoCall(splinePoint, false);
        }

        public override void UpdateDisplayedProperties()
        {
            base.UpdateDisplayedProperties();

            if (mListDisplayWindow != null)
            {
                //mListDisplayWindow.UpdateToObject();
            }
        }

        #endregion

        #endregion
    }
}
