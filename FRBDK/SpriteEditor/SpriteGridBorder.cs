using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.ManagedSpriteGroups;

using FlatRedBall.Gui;

using FlatRedBall.Math.Geometry;

using Vector3 = Microsoft.DirectX.Vector3;
using SpriteGrid = FlatRedBall.ManagedSpriteGroups.SpriteGrid;
using EditorObjects;

namespace SpriteEditor
{
    class SpriteGridBorder
    {
        #region Fields

        Cursor cursor;

        
        Polygon mMainRectangle;
        Polygon[] mCornerHandles;
        Polygon[] mSideHandles;
        

        Polygon mHandleGrabbed;

        FlatRedBall.ManagedSpriteGroups.SpriteGrid mParentSpriteGrid;

        Camera camera;
        #endregion

        #region Properties


        public bool IsHandleGrabbed
        {
            get { return mHandleGrabbed != null; }
        }
        
        public bool Visible
        {
            get { return mMainRectangle.Visible; }
            set
            {
                
                mMainRectangle.Visible = value;
                mCornerHandles[0].Visible = value;
                mCornerHandles[1].Visible = value;
                mCornerHandles[2].Visible = value;
                mCornerHandles[3].Visible = value;

                mSideHandles[0].Visible = value;
                mSideHandles[1].Visible = value;
                mSideHandles[2].Visible = value;
                mSideHandles[3].Visible = value;
                 
            }

        }
        

        #endregion

        #region Methods
        
        public SpriteGridBorder(Cursor cursor, Camera camera)
        {
            this.cursor = cursor;
            this.camera = camera;

            mMainRectangle = Polygon.CreateRectangle(1,1);
            ShapeManager.AddPolygon(mMainRectangle);



            mCornerHandles = new Polygon[4];
            mSideHandles = new Polygon[4];
            for (int i = 0; i < 4; i++)
            {
                mCornerHandles[i] = Polygon.CreateRectangle(.5f, .5f);
                mCornerHandles[i].Color = System.Drawing.Color.Yellow;
                ShapeManager.AddPolygon(mCornerHandles[i]);

                mSideHandles[i] = Polygon.CreateRectangle(.5f, .5f);
                mSideHandles[i].Color = System.Drawing.Color.Yellow;
                ShapeManager.AddPolygon(mSideHandles[i]);
            }
             
            Visible = false;
        }

        public void Activity()
        {
            
            #region if visible, SetCursorAndHandleColors according to what the cursor is over
            if (Visible)
            {
                SetCursorAndHandleDisplay();

            }
            #endregion

            #region if cursor.primaryPush, see if we are grabbing a handle
            if (cursor.PrimaryPush && Visible)
            {
                // see if the cursor is over any of the polys

                // Vic says: Talk about a hack - we check the color?  How lame is that.
                // I think we need to figure a different way to do this.


                foreach (Polygon lp in mSideHandles)
                {
                    if (lp.Color.ToArgb() != -256 && mHandleGrabbed != lp)
                    {
                        UndoManager.AddToWatch<SpriteGrid>(this.mParentSpriteGrid);
                        mHandleGrabbed = lp;
                    }
                }

                foreach (Polygon lp in mCornerHandles)
                {
                    if (lp.Color.ToArgb() != -256 && mHandleGrabbed != lp)
                    {
                        UndoManager.AddToWatch<SpriteGrid>(this.mParentSpriteGrid);
                        mHandleGrabbed = lp;
                    }
                }
            }
            #endregion

            #region if cursor.primaryDown and mHandleGrabbed, adjust the SpriteGrid bounds
            if (cursor.PrimaryDown && mHandleGrabbed != null && 
                (cursor.XVelocity != 0 || cursor.YVelocity != 0 ||
                camera.XVelocity != 0 || camera.YVelocity != 0 || camera.ZVelocity != 0))
            {
                if (mHandleGrabbed == mSideHandles[1])
                {
                    mParentSpriteGrid.XRightBound = cursor.WorldXAt(mHandleGrabbed.Z);
                }
                else if (mHandleGrabbed == mSideHandles[3])
                {
                    mParentSpriteGrid.XLeftBound = cursor.WorldXAt(mHandleGrabbed.Z);
                }

                else if (mParentSpriteGrid.GridPlane == SpriteGrid.Plane.XY)
                {
                    if (mHandleGrabbed == mSideHandles[0])
                    {
                        mParentSpriteGrid.YTopBound = cursor.WorldYAt(mHandleGrabbed.Z);
                    }
                    else if (mHandleGrabbed == mSideHandles[2])
                    {
                        mParentSpriteGrid.YBottomBound = cursor.WorldYAt(mHandleGrabbed.Z);
                    }
                    else if (mHandleGrabbed == mCornerHandles[0])
                    {
                        mParentSpriteGrid.YTopBound = cursor.WorldYAt(mHandleGrabbed.Z);
                        mParentSpriteGrid.XLeftBound = cursor.WorldXAt(mHandleGrabbed.Z);
                    }
                    else if (mHandleGrabbed == mCornerHandles[1])
                    {
                        mParentSpriteGrid.YTopBound = cursor.WorldYAt(mHandleGrabbed.Z);
                        mParentSpriteGrid.XRightBound = cursor.WorldXAt(mHandleGrabbed.Z);
                    }
                    else if (mHandleGrabbed == mCornerHandles[2])
                    {
                        mParentSpriteGrid.YBottomBound = cursor.WorldYAt(mHandleGrabbed.Z);
                        mParentSpriteGrid.XRightBound = cursor.WorldXAt(mHandleGrabbed.Z);
                    }
                    else if (mHandleGrabbed == mCornerHandles[3])
                    {
                        mParentSpriteGrid.YBottomBound = cursor.WorldYAt(mHandleGrabbed.Z);
                        mParentSpriteGrid.XLeftBound = cursor.WorldXAt(mHandleGrabbed.Z);
                    }
                }
                else
                {
                    if (mHandleGrabbed == mSideHandles[0])
                    {
                        mParentSpriteGrid.ZFarBound += cursor.YVelocity;
                    }
                    else if (mHandleGrabbed == mSideHandles[2])
                    {
                        mParentSpriteGrid.ZCloseBound += cursor.YVelocity;
                    }
                    else if (mHandleGrabbed == mCornerHandles[0])
                    {
                        mParentSpriteGrid.ZFarBound += cursor.YVelocity;
                        mParentSpriteGrid.XLeftBound = cursor.WorldXAt(mHandleGrabbed.Z);
                    }
                    else if (mHandleGrabbed == mCornerHandles[1])
                    {
                        mParentSpriteGrid.ZFarBound += cursor.YVelocity;
                        mParentSpriteGrid.XRightBound = cursor.WorldXAt(mHandleGrabbed.Z);
                    }
                    else if (mHandleGrabbed == mCornerHandles[2])
                    {
                        mParentSpriteGrid.ZCloseBound += cursor.YVelocity;
                        mParentSpriteGrid.XRightBound = cursor.WorldXAt(mHandleGrabbed.Z);
                    }
                    else if (mHandleGrabbed == mCornerHandles[3])
                    {
                        mParentSpriteGrid.ZCloseBound += cursor.YVelocity;
                        mParentSpriteGrid.XLeftBound = cursor.WorldXAt(mHandleGrabbed.Z);
                    }
                }

                if(mHandleGrabbed != null)
                    SetLinePolygonPositions();
            }

            #endregion

            if (cursor.PrimaryClick)
            {
                if (mHandleGrabbed != null)
                {
                    UndoManager.RecordUndos<SpriteGrid>();
                    UndoManager.ClearObjectsWatching<SpriteGrid>();
                }
                mHandleGrabbed = null;
            }
             
        }

        public void AttachTo(SpriteGrid parentSpriteGrid)
        {
            
            mParentSpriteGrid = parentSpriteGrid;

            if (mParentSpriteGrid != null)
            {
                SetLinePolygonPositions();
                /*
                for (int i = 0; i < 4; i++)
                {
                    mCornerHandles[i].ScaleBy(
                        (mCornerHandles[i].Z - camera.Z) /
                        (200*(mCornerHandles[i].X - mCornerHandles[i].Points[0].X))                
                        );

                    mSideHandles[i].ScaleBy(
                        (mSideHandles[i].Z - camera.Z) /
                        (200 * (mSideHandles[i].X - mSideHandles[i].Points[0].X))
                        );   
                }*/
                Visible = true;
            }
            else
            {
                Visible = false;
            }
             
        }

        private void SetCursorAndHandleDisplay()
        {

            #region Set the handle scales
            float pixelsPerUnit = 0;
            float desiredScale = 0;

            for (int i = 0; i < 4; i++)
            {

                pixelsPerUnit = SpriteManager.Camera.PixelsPerUnitAt(mCornerHandles[i].Z);
                desiredScale = 5 * (1 / pixelsPerUnit);
                mCornerHandles[i].ScaleBy(desiredScale / mCornerHandles[i].Points[0].X);

                pixelsPerUnit = SpriteManager.Camera.PixelsPerUnitAt(mSideHandles[i].Z);
                desiredScale = 5 * (1 / pixelsPerUnit);
                mSideHandles[i].ScaleBy(desiredScale / mSideHandles[i].Points[0].X);
            }            

            #endregion

            #region Reset all of the colors to yellow - will set the appropriate corners later

            for (int i = 0; i < 4; i++)
            {
                mCornerHandles[i].Color = System.Drawing.Color.Yellow;
                mSideHandles[i].Color = System.Drawing.Color.Yellow;
            }

            #endregion

            #region See if the Cursor is on any of the handles.  If so, set the cursor's current display and change the color of the handle
            if (cursor.IsOn(mCornerHandles[0]))
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeNWSE;
                mCornerHandles[0].Color = System.Drawing.Color.Magenta;
            }
            else if (cursor.IsOn(mCornerHandles[1]))
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeNESW;
                mCornerHandles[1].Color = System.Drawing.Color.Magenta;
            }
            if (cursor.IsOn(mCornerHandles[2]))
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeNWSE;
                mCornerHandles[2].Color = System.Drawing.Color.Magenta;
            }
            else if (cursor.IsOn(mCornerHandles[3]))
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeNESW;
                mCornerHandles[3].Color = System.Drawing.Color.Magenta;
            }

            if (cursor.IsOn(mSideHandles[0]))
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeNS;
                mSideHandles[0].Color = System.Drawing.Color.Magenta;
            }
            else if (cursor.IsOn(mSideHandles[1]))
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeWE;
                mSideHandles[1].Color = System.Drawing.Color.Magenta;
            }
            if (cursor.IsOn(mSideHandles[2]))
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeNS;
                mSideHandles[2].Color = System.Drawing.Color.Magenta;
            }
            else if (cursor.IsOn(mSideHandles[3]))
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeWE;
                mSideHandles[3].Color = System.Drawing.Color.Magenta;
            }
            #endregion
        
        
            
        }

        internal void SetLinePolygonPositions()
        {
            if (mParentSpriteGrid == null)
                return;

            #region Find the center/top/bottom positions

            float xCenter =
                (mParentSpriteGrid.XLeftBound + mParentSpriteGrid.XRightBound) / 2.0f;
            float yTop;
            float yCenter;
            float yBottom;

            float zTop;
            float zCenter;
            float zBottom;

            if (mParentSpriteGrid.GridPlane == SpriteGrid.Plane.XY)
            {
                yTop = mParentSpriteGrid.YTopBound;
                yCenter =
                    (mParentSpriteGrid.YTopBound + mParentSpriteGrid.YBottomBound) / 2.0f;
                yBottom = mParentSpriteGrid.YBottomBound;

                zTop = zCenter = zBottom = mParentSpriteGrid.Blueprint.Z;
            }
            else
            {
                yTop = yCenter = yBottom = mParentSpriteGrid.Blueprint.Y;

                zTop = mParentSpriteGrid.ZFarBound;
                zCenter =
                    (mParentSpriteGrid.ZCloseBound + mParentSpriteGrid.ZFarBound) / 2.0f;
                zBottom = mParentSpriteGrid.ZCloseBound;
            }
            #endregion

            #region Set the handle positions

            // top side
            mSideHandles[0].X = xCenter;
            mSideHandles[0].Y = yTop;
            mSideHandles[0].Z = zTop;

            // right side
            mSideHandles[1].X = mParentSpriteGrid.XRightBound;
            mSideHandles[1].Y = yCenter;
            mSideHandles[1].Z = zCenter;

            // bottom side
            mSideHandles[2].X = xCenter;
            mSideHandles[2].Y = yBottom;
            mSideHandles[2].Z = zBottom;

            // left side
            mSideHandles[3].X = mParentSpriteGrid.XLeftBound;
            mSideHandles[3].Y = yCenter;
            mSideHandles[3].Z = zCenter;

            // top left corner
            mCornerHandles[0].X = mParentSpriteGrid.XLeftBound;
            mCornerHandles[0].Y = yTop;
            mCornerHandles[0].Z = zTop;

            // top right corner
            mCornerHandles[1].X = mParentSpriteGrid.XRightBound;
            mCornerHandles[1].Y = yTop;
            mCornerHandles[1].Z = zTop;

            // bottom right corner
            mCornerHandles[2].X = mParentSpriteGrid.XRightBound;
            mCornerHandles[2].Y = yBottom;
            mCornerHandles[2].Z = zBottom;

            // bottom left corner
            mCornerHandles[3].X = mParentSpriteGrid.XLeftBound;
            mCornerHandles[3].Y = yBottom;
            mCornerHandles[3].Z = zBottom;
            #endregion

            #region Set the main rectangle position

            mMainRectangle.X = xCenter;
            mMainRectangle.Y = yCenter;
            mMainRectangle.Z = zCenter;

            #endregion

            #region Set the main rectangle scale

            float desiredScaleX = (mParentSpriteGrid.XRightBound - mParentSpriteGrid.XLeftBound) / 2.0f;
            float desiredScaleY = 0;

            if (mParentSpriteGrid.GridPlane == SpriteGrid.Plane.XY)
            {
                desiredScaleY = (yTop - yBottom) / 2.0f;
            }
            else
            {
                desiredScaleY = (zTop - zBottom) / 2.0f;
            }

            mMainRectangle.ScaleBy(
                desiredScaleX / mMainRectangle.Points[0].X,
                desiredScaleY / mMainRectangle.Points[0].Y);
            #endregion

            if (mParentSpriteGrid.GridPlane == SpriteGrid.Plane.XZ)
            {
                mMainRectangle.RotationX = (float)Math.PI / 2;
            }
            else
            {
                mMainRectangle.RotationX = 0;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Handle Grabbed: ").Append(this.mHandleGrabbed);
            return sb.ToString();
        }

        #endregion
    }
}
