using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Graphics;

using FlatRedBall.Math.Geometry;
using FlatRedBall.Math;
using FlatRedBall.ManagedSpriteGroups;

using FlatRedBall.Input;

#if FRB_MDX
using Microsoft.DirectX;

using Keys = Microsoft.DirectX.DirectInput.Key;
#else
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Keys = Microsoft.Xna.Framework.Input.Keys;

#endif

namespace EditorObjects.Hud
{
    public class EditorHandles
    {
		#region Fields

		public Sprite origin;
		public Sprite yAxis;
		public Sprite xAxis;
		public Sprite zAxis;

		public Sprite xScale;
		public Sprite yScale;

		public Sprite xRot;
		public Sprite yRot;

		public Vector3 changeVector;

		bool mCursorPushedOnAxis;
        bool mCursorOverAxis;

		Sprite axisGrabbed;
        SpriteList editAxisSpriteArray = new SpriteList();

        float mScale = -1;

        bool mVisible = false;

        PositionedObject mGrabbedPositionedObject;


        public bool AxesClicked
        {
            get;
            set;
        }

        #region method vars for reducing garbage collection
        PositionedObjectList<PositionedObject> spritesAlreadyChanged;
        #endregion

        #endregion

        #region Properties

        public PositionedObject CurrentObject
        {
            get
            {
                return mGrabbedPositionedObject;
            }
            set
            {
                bool valueChanged = mGrabbedPositionedObject != value;

                mGrabbedPositionedObject = value;
                Visible = mGrabbedPositionedObject != null;
                
                if (mGrabbedPositionedObject != null && origin.Parent != mGrabbedPositionedObject)
                {

                    origin.AttachTo(mGrabbedPositionedObject, false);
                }
                else if(mGrabbedPositionedObject == null)
                {
                    origin.Detach();
                }
            }
        }

        public bool CursorOverAxis
        {
            get { return mCursorOverAxis; }
        }

        public bool CursorPushedOnAxis
        {
            get { return mCursorPushedOnAxis; }
        }

        public float DistanceFromCamera
        {
            get
            {
                return (origin.Position - SpriteManager.Camera.Position).Length();
            }
        }

        public bool HierarchyControl
        {
            get;
            set;
        }

        public bool UpdatePositionsImmediately
        {
            get;
            set;
        }

        public bool Visible
        {
            get
            {
                return mVisible;
            }
            set
            {
                if (value != mVisible)
                {
                    mVisible = value;
                    editAxisSpriteArray.Visible = mVisible;
                    if (mVisible == false)
                    {
                        mCursorOverAxis = false;
                    }
                }
            }
        }

        public float Scale
        {
            set
            {
                if (value != mScale)
                {
                    mScale = value;
                    yAxis.RelativeRotationZ = 1.570796f;
                    zAxis.RelativeRotationY = 1.570796f;

                    origin.RelativeX = 0 * mScale;
                    xAxis.RelativeX = 1.6f * mScale;
                    yAxis.RelativeX = 0 * mScale;
                    yAxis.RelativeY = 1.6f * mScale;
                    zAxis.RelativeX = 0 * mScale;
                    zAxis.RelativeY = 0 * mScale;
                    zAxis.RelativeZ = -1.6f * mScale;
                    xScale.RelativeX = 3.4f * mScale;
                    yScale.RelativeY = 3.4f * mScale;
                    xRot.RelativeX = 4 * mScale;
                    yRot.RelativeY = 4 * mScale;

                    origin.ScaleX = 0.4f * mScale;
                    origin.ScaleY = 0.4f * mScale;
                    xAxis.ScaleX = 1.28f * mScale;
                    xAxis.ScaleY = 0.32f * mScale;
                    yAxis.ScaleX = 1.28f * mScale;
                    yAxis.ScaleY = 0.32f * mScale;
                    zAxis.ScaleX = 1.28f * mScale;
                    zAxis.ScaleY = .32f * mScale;
                    xScale.ScaleX = 0.32f * mScale;
                    xScale.ScaleY = 0.32f * mScale;
                    yScale.ScaleX = 0.32f * mScale;
                    yScale.ScaleY = 0.32f * mScale;
                    xRot.ScaleX = .32f * mScale;
                    xRot.ScaleY = .32f * mScale;
                    yRot.ScaleX = .32f * mScale;
                    yRot.ScaleY = .32f * mScale;

                }
            }
        }

        #endregion

        #region Methods

        public EditorHandles()
        {
            spritesAlreadyChanged = new PositionedObjectList<PositionedObject>();

			Layer axisLayer = SpriteManager.AddLayer();

			origin = SpriteManager.AddSprite(
                FlatRedBallServices.Load<Texture2D>("Content/EditorHandles/Origin.png", GuiManager.InternalGuiContentManagerName), axisLayer);
			origin.Name = "OriginOfEditAxes";
			editAxisSpriteArray.Add(origin);
			
			xAxis = SpriteManager.AddSprite(
                FlatRedBallServices.Load<Texture2D>("Content/EditorHandles/MoveX.png", GuiManager.InternalGuiContentManagerName), axisLayer);

			editAxisSpriteArray.Add(xAxis);

			yAxis = SpriteManager.AddSprite(
                FlatRedBallServices.Load<Texture2D>("Content/EditorHandles/MoveY.png", GuiManager.InternalGuiContentManagerName), axisLayer);
			editAxisSpriteArray.Add(yAxis);

			zAxis = SpriteManager.AddSprite(
                FlatRedBallServices.Load<Texture2D>("Content/EditorHandles/MoveZ.png", GuiManager.InternalGuiContentManagerName), axisLayer);
			editAxisSpriteArray.Add(zAxis);

			xScale = SpriteManager.AddSprite(
                FlatRedBallServices.Load<Texture2D>("Content/EditorHandles/ScaleX.png", GuiManager.InternalGuiContentManagerName), axisLayer);
			editAxisSpriteArray.Add(xScale);

			yScale = SpriteManager.AddSprite(
                FlatRedBallServices.Load<Texture2D>("Content/EditorHandles/ScaleY.png", GuiManager.InternalGuiContentManagerName), axisLayer);
			editAxisSpriteArray.Add(yScale);

			xRot = SpriteManager.AddSprite(
                FlatRedBallServices.Load<Texture2D>("Content/EditorHandles/RotateX.png", GuiManager.InternalGuiContentManagerName), axisLayer);

			editAxisSpriteArray.Add(xRot);

			yRot = SpriteManager.AddSprite(
                FlatRedBallServices.Load<Texture2D>("Content/EditorHandles/RotateY.png", GuiManager.InternalGuiContentManagerName), axisLayer);
			

			
			editAxisSpriteArray.Add(yRot);

			xAxis.AttachTo(origin, true);
			yAxis.AttachTo(origin, true);
			zAxis.AttachTo(origin, true);
			xScale.AttachTo(origin, true);
			yScale.AttachTo(origin, true);
			xRot.AttachTo(origin, true);
			yRot.AttachTo(origin, true);

            Scale = 1;

            editAxisSpriteArray.Visible = false;
            mCursorOverAxis = false;
            mVisible = false;



        }

        public void Control(Cursor cursor, Camera camera, float xToY)
        {
            // Nick Spacek got an exception in this method.  Not sure why - I can't get it.
            // So I'll chop it up into try catch blocks to narrow it down.
            try
            {
                if (CurrentObject == null)
                    Visible = false;

                if (!cursor.PrimaryDown)
                {
                    AxesClicked = false;
                    changeVector.X = changeVector.Y = changeVector.Z = 0;
                    mCursorPushedOnAxis = false;
                }

                if (origin.Visible == false) return;
            }
            catch (NullReferenceException)
            {
                throw new NullReferenceException("Exception in Control method setup");
            }

            Sprite spriteHighlighted = cursor.GetSpriteOver(this.editAxisSpriteArray);
            try
            {

                #region Fades for the cursor being over or not over certain parts

                editAxisSpriteArray.Alpha = .5f * GraphicalEnumerations.MaxColorComponentValue ;
                mCursorOverAxis = false;

                if (spriteHighlighted != null)
                {
                    spriteHighlighted.Alpha = GraphicalEnumerations.MaxColorComponentValue;
                    mCursorOverAxis = true;
                }

                #endregion
            }
            catch (NullReferenceException)
            {
                throw new NullReferenceException("Exception in setting fades.");
            }

            try
            {
                #region Pushing button
                if (cursor.PrimaryPush)
                {
                    if (spriteHighlighted != null)
                    {
                        mCursorPushedOnAxis = true;
                        axisGrabbed = spriteHighlighted;
                        cursor.SetObjectRelativePosition(origin);

                        if (origin.Parent is Sprite)
                        {
                            UndoManager.AddToWatch(origin.Parent as Sprite);
                        }
                    }
                }
                #endregion
            }
            catch (NullReferenceException)
            {
                throw new NullReferenceException("Exception in Pushing button");
            }

            DraggingActivity(cursor, xToY);

            try
            {
                #region Clicking button
                if (cursor.PrimaryClick && mCursorPushedOnAxis)
                {
                    mCursorPushedOnAxis = false;
                    axisGrabbed = null;
                    cursor.StaticPosition = false;
                    AxesClicked = true;

                    UndoManager.RecordUndos<Sprite>();
                    UndoManager.ClearObjectsWatching<Sprite>();
                }

                #endregion
            }
            catch
            {
                throw new NullReferenceException("Exception in Clicking button");
            }
        }

        private void DraggingActivity(Cursor cursor, float xToY)
        {
            try
            {
                changeVector = new Vector3(0, 0, 0);

                #region Button down
                if (cursor.PrimaryDown && mCursorPushedOnAxis && origin.Parent != null && (cursor.XVelocity != 0 || cursor.YVelocity != 0))
                {
                    // At the time of this writing it is possible to
                    // select a Sprite in the SpriteGrid and shrink the
                    // grid which removes the Sprite but does not remove the
                    // EditAxes.  If the user tries to perform operations on the
                    // Sprite it's possible to get an exception.  Therefore, add a
                    // test to make sure the Sprite isn't null in the above if statement.

                    spritesAlreadyChanged.Clear();

                    float movementMultiplier = DistanceFromCamera / 40.0f;

#if FRB_MDX
                    Vector3 cursorVector = cursor.WorldXChangeAt(origin.Parent.Z) * SpriteManager.Camera.RotationMatrix.Right() +
                        cursor.WorldYChangeAt(origin.Parent.Z) * SpriteManager.Camera.RotationMatrix.Up();
                    
                    //Plane screenPlane = Plane.FromPointNormal(SpriteManager.Camera.Position, SpriteManager.Camera.RotationMatrix.Forward());

                    Vector3 cameraRight = SpriteManager.Camera.RotationMatrix.Right();
                    Vector3 cameraUp = SpriteManager.Camera.RotationMatrix.Up();
#else
                    Vector3 cursorVector = cursor.XVelocity * SpriteManager.Camera.RotationMatrix.Right +
                        cursor.YVelocity * SpriteManager.Camera.RotationMatrix.Up;

                    //Plane screenPlane = Plane.FromPointNormal(SpriteManager.Camera.Position, SpriteManager.Camera.RotationMatrix.Forward());

                    Vector3 cameraRight = SpriteManager.Camera.RotationMatrix.Right;
                    Vector3 cameraUp = SpriteManager.Camera.RotationMatrix.Up;
#endif

                    Vector3 vectorToMoveAlong = GetVectorToMoveAlong();
                    ApplyMovement(ref cursorVector, ref cameraRight, ref cameraUp, ref vectorToMoveAlong);


                    #region xScale grabbed
                    if (axisGrabbed == xScale)
                    {
                        cursor.StaticPosition = true;

                        float change;
                        if (origin.Parent.RotationZ == (float)System.Math.PI * .5f || origin.Parent.RotationZ == (float)System.Math.PI * 1.5f)
                            change = 1 + cursor.YVelocity / 100.0f;
                        else
                            change = 1 + cursor.XVelocity / 100.0f;

                        if (origin.Parent as IScalable != null)
                            ((IScalable)(origin.Parent)).ScaleX *= change;
                        else
                        {
                            ((IScalable)origin.Parent).ScaleX *= change;
                            //                            if (GameData.sfMan.CurrentSpriteFrames.Count != 0)
                            //                              GuiData.propertiesWindow.sfSpriteBorderWidth.MaxValue =
                            //                                 System.Math.Min(GameData.sfMan.CurrentSpriteFrames[0].ScaleX, GameData.sfMan.CurrentSpriteFrames[0].ScaleY);

                        }

                        if (InputManager.Keyboard.KeyDown(Keys.LeftShift) || InputManager.Keyboard.KeyDown(Keys.RightShift))
                        {
                            if (origin.Parent is IScalable)
                            {
                                ((IScalable)(origin.Parent)).ScaleY = ((IScalable)(origin.Parent)).ScaleX / xToY;
                            }
                            else
                            {
                                ((IScalable)origin.Parent).ScaleY = ((IScalable)origin.Parent).ScaleX / xToY;
                            }
                        }
                    }
                    #endregion
                    #region xRot grabbed
                    else if (axisGrabbed == xRot)
                    {
                        cursor.StaticPosition = true;

#if FRB_MDX
                        Vector3 xAxisVector =
                            origin.Parent.RotationMatrix.Right();
#else
                        Vector3 xAxisVector =
                            origin.Parent.RotationMatrix.Right;

#endif
                        RotatePrivateSpritesAboutAxis(xAxisVector, cursor);
                    }
                    #endregion
                    #region yScale grabbed
                    else if (axisGrabbed == yScale)
                    {
                        cursor.StaticPosition = true;

                        float change;
                        if (origin.Parent.RotationZ == (float)System.Math.PI * .5f || origin.Parent.RotationZ == (float)System.Math.PI * 1.5f)
                            change = 1 + cursor.XVelocity / 100.0f;
                        else
                            change = 1 + cursor.YVelocity / 100.0f;


                        ((IScalable)origin.Parent).ScaleY *= change;

                        if (origin.Parent is SpriteFrame)
                        {
                            //                            GuiData.propertiesWindow.sfSpriteBorderWidth.MaxValue =
                            //                               System.Math.Min(GameData.sfMan.CurrentSpriteFrames[0].ScaleX, GameData.sfMan.CurrentSpriteFrames[0].ScaleY);
                        }


                        if (InputManager.Keyboard.KeyDown(Keys.LeftShift) || InputManager.Keyboard.KeyDown(Keys.RightShift))
                        {

                            ((Sprite)origin.Parent).ScaleX = ((Sprite)origin.Parent).ScaleY * xToY;
                        }
                    }
                    #endregion
                    #region yRot grabbed
                    else if (axisGrabbed == yRot)
                    {
                        cursor.StaticPosition = true;
#if FRB_MDX
                        Vector3 yAxisVector = origin.Parent.RotationMatrix.Up();
#else
                        Vector3 yAxisVector = origin.Parent.RotationMatrix.Up;
#endif

                        RotatePrivateSpritesAboutAxis(yAxisVector, cursor);
                    }
                    #endregion
                    #region origin grabbed
                    else if (axisGrabbed == origin)
                    {
                        cursor.StaticPosition = true;

#if FRB_MDX
                        Vector3 zAxisVector = origin.Parent.RotationMatrix.Forward();
#else
                        Vector3 zAxisVector = origin.Parent.RotationMatrix.Forward;
#endif
                        RotatePrivateSpritesAboutAxis(zAxisVector, cursor);
                    }
                    #endregion
                }
                #endregion
            }
            catch (NullReferenceException)
            {
                throw new NullReferenceException("Exception in Button down");
            }
        }

        private void ApplyMovement(ref Vector3 cursorVector, ref Vector3 cameraRight, ref Vector3 cameraUp, ref Vector3 vectorToMoveAlong)
        {

            if (vectorToMoveAlong != new Vector3(0, 0, 0))
            {
                // This is old code that I wrote LONG ago---------------------------------------------------
                //int baseScreenX = 0;
                //int baseScreenY = 0;
                //MathFunctions.AbsoluteToWindow(origin.Parent.X, origin.Parent.Y, origin.Parent.Z, ref baseScreenX, ref baseScreenY, SpriteManager.Camera, true);

                //int endScreenX = 0;
                //int endScreenY = 0;
                //MathFunctions.AbsoluteToWindow(
                //    origin.Parent.X + vectorToMoveAlong.X,
                //    origin.Parent.Y + vectorToMoveAlong.Y,
                //    origin.Parent.Z + vectorToMoveAlong.Z,
                //    ref endScreenX, ref endScreenY, SpriteManager.Camera, true);


                //// invert the Y
                //Vector3 moveAlongProjected = new Vector3(endScreenX - baseScreenX, baseScreenY - endScreenY, 0);

                //float worldXChange;
                //float worldYChange;

                //MathFunctions.ScreenToAbsoluteDistance(GuiManager.Cursor.LastScreenX - GuiManager.Cursor.ScreenX,
                //    GuiManager.Cursor.LastScreenY - GuiManager.Cursor.ScreenY,
                //    out worldXChange, out worldYChange, origin.Parent.Z, SpriteManager.Camera);


                //Vector3 rightProjected = worldXChange * moveAlongProjected.X * cameraRight;
                //Vector3 upProjected = worldYChange * moveAlongProjected.Y * cameraUp;

                //Vector3 normalizedProjected = Vector3.Normalize(rightProjected + upProjected);
                // end of old code-----------------------------------------------------------------------------
                

                changeVector = Vector3.Dot(cursorVector, vectorToMoveAlong) * vectorToMoveAlong;

                if (UpdatePositionsImmediately)
                {
                    origin.Parent.Position += changeVector;
                }
            }
        }

        private Vector3 GetVectorToMoveAlong()
        {
            Vector3 vectorToMoveAlong = new Vector3(0, 0, 0);

            if (axisGrabbed == xAxis)
            {
#if FRB_MDX
                vectorToMoveAlong = origin.Parent.RotationMatrix.Right();
#else
                        vectorToMoveAlong = origin.Parent.RotationMatrix.Right;
#endif
            }
            else if (axisGrabbed == yAxis)
            {
#if FRB_MDX
                vectorToMoveAlong = origin.Parent.RotationMatrix.Up();
#else
                        vectorToMoveAlong = origin.Parent.RotationMatrix.Up;
#endif

            }
            else if (axisGrabbed == zAxis)
            {
#if FRB_MDX
                vectorToMoveAlong = origin.Parent.RotationMatrix.Backward();
#else
                        vectorToMoveAlong = origin.Parent.RotationMatrix.Backward;
#endif
            }
            return vectorToMoveAlong;
        }

        void RotatePrivateSpritesAboutAxis(Vector3 axisVector, Cursor cursor)
        {
            if(CurrentObject != null)
            {

                PositionedObject s = CurrentObject;

                #region no parent
                if (s.Parent == null && !spritesAlreadyChanged.Contains(s))
                {
                    
                    spritesAlreadyChanged.AddOneWay(s);

#if FRB_MDX
                    s.RotationMatrix *= Matrix.RotationAxis(axisVector, cursor.YVelocity / 16.0f);
#else
                    s.RotationMatrix *= Matrix.CreateFromAxisAngle(axisVector, cursor.YVelocity / 16.0f);
#endif
                    // Fixes accumulation error:
                    s.RotationZ = s.RotationZ;
                    //if (s is ISpriteEditorObject)
                    //{
                    //    float temporaryRotX = 0;
                    //    float temporaryRotY = 0;
                    //    float temporaryRotationZ = 0;

                    //    MathFunctions.ExtractRotationValuesFromMatrix(tempMatrix,
                    //        ref temporaryRotX,
                    //        ref temporaryRotY,
                    //        ref temporaryRotationZ);

                    //    (s).RotationX = temporaryRotX;
                    //    (s).RotationY = temporaryRotY;
                    //    (s).RotationZ = temporaryRotationZ;
                    //}
                    //else
                    //{
                    //}
                }
                #endregion

                #region has parent, group hierarchy is pressed (so in hierarchy (NOT GROUP) mode )
                else if (HierarchyControl)
                {

                    spritesAlreadyChanged.AddOneWay(s);

#if FRB_MDX
                    s.RotationMatrix *= Matrix.RotationAxis(axisVector, cursor.YVelocity / 16.0f);
#else
                    s.RotationMatrix *= Matrix.CreateFromAxisAngle(axisVector, cursor.YVelocity / 16.0f);
#endif
                    s.SetRelativeFromAbsolute();
                }
                #endregion

                #region rotation logic for the entire group - ONLY do on topparents
                else
                {
                    Sprite parentSprite = (Sprite)(s.TopParent);

                    spritesAlreadyChanged.AddOneWay(parentSprite);

                    Vector3 positionFromOrigin = new Vector3((float)(parentSprite.X - origin.X),
                        (float)(parentSprite.Y - origin.Y), (float)(parentSprite.Z - origin.Z));

#if FRB_MDX
                    Matrix rotationMatrixToUse = Matrix.RotationAxis(axisVector, cursor.YVelocity / 16.0f);
#else
                    Matrix rotationMatrixToUse = Matrix.CreateFromAxisAngle(axisVector, cursor.YVelocity / 16.0f);
#endif


                    parentSprite.RotationMatrix *= rotationMatrixToUse;

                    MathFunctions.TransformVector(ref positionFromOrigin, ref rotationMatrixToUse);

                    (parentSprite).X = (float)(positionFromOrigin.X + origin.X);
                    (parentSprite).Y = (float)(positionFromOrigin.Y + origin.Y);
                    (parentSprite).Z = (float)(positionFromOrigin.Z + origin.Z);


                    float temporaryRotX = 0;
                    float temporaryRotY = 0;
                    float temporaryRotationZ = 0;

                    MathFunctions.ExtractRotationValuesFromMatrix(parentSprite.RotationMatrix,
                        ref temporaryRotX,
                        ref temporaryRotY,
                        ref temporaryRotationZ);

                    (s).RotationX = temporaryRotX;
                    (s).RotationY = temporaryRotY;
                    (s).RotationZ = temporaryRotationZ;

                }
                #endregion
            }

            //foreach (SpriteFrame sf in GameData.EditorLogic.CurrentSpriteFrames)
            //{
            //    Matrix tempMatrix = sf.RotationMatrix;
            //    tempMatrix *= Matrix.RotationAxis(axisVector, cursor.YVelocity / 16.0f);

            //    float RotationX = 0;
            //    float RotationY = 0;
            //    float RotationZ = 0;

            //    MathFunctions.ExtractRotationValuesFromMatrix(tempMatrix, ref RotationX,
            //        ref RotationY,
            //        ref RotationZ);

            //    sf.RotationX = RotationX;
            //    sf.RotationY = RotationY;
            //    sf.RotationZ = RotationZ;


            //}

        }

        public void Update()
        {
            if (SpriteManager.Camera.Orthogonal)
            {
                Scale = 24.2f / SpriteManager.Camera.PixelsPerUnitAt(this.origin.Z);
            }
            else
            {
                float desiredScale = DistanceFromCamera / 30.0f;

                Scale = desiredScale;
            }
        }

        #endregion
    }
}
