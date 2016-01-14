using System;

#if FRB_MDX
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

#else

#endif

using FlatRedBall.Graphics;


namespace FlatRedBall.Gui
{
	/// <summary>
	/// Summary description for CollapseWindow.
	/// </summary>
	public class CollapseWindow : Window
	{
		#region Fields
		private bool mCollapsed;
		private float fullY;
		private float fullScaleY;
		#endregion

        #region properties
        public bool isCollapsed
        {
            get { return mCollapsed; }
            set
            {
                if (value != mCollapsed)
                {
                    mCollapsed = value;


                    UpdateStateToCollapsed();
                }
            }
        }

        public float FullY
        {
            get { return fullY; }
            set { fullY = value; }
        }

        public float FullScaleY
        {
            get { return fullScaleY; }
            set { fullScaleY = value; }
        }

        #endregion

        #region Methods

        public CollapseWindow(Cursor cursor) : base(cursor)
		{
			this.HasMoveBar = true;
	//		Collapsed = false;



		}

		public void Collapse()
		{
			if(!mCollapsed)
			{
				fullY = mWorldUnitY;
				fullScaleY = mScaleY;

                SetPositionTL(GuiManager.Camera.XEdge + mWorldUnitX, -mWorldUnitY + GuiManager.Camera.YEdge - mScaleY + 1.1f);
				ScaleY = 1.1f;

				mCollapsed = true;

			}
		}
				
		private void DoubleClickCollapse()
		{
			mCollapsed = !mCollapsed;
            UpdateStateToCollapsed();
		}

        private void UpdateStateToCollapsed()
        {
            if (mCollapsed)
            {
                fullY = mWorldUnitY;
                fullScaleY = mScaleY;

                SetPositionTL(GuiManager.Camera.XEdge + mWorldUnitX, -mWorldUnitY + GuiManager.Camera.YEdge - mScaleY + 1.1f);
                ScaleY = 1.1f;

            }
            else
            {
                mWorldUnitY -= fullScaleY - 1.1f;

                ScaleY = (float)fullScaleY;

                this.UpdateDependencies();

            }
        }

        internal override void DrawSelfAndChildren(Camera camera)
		{

            if (Visible == false)
				return;

			if(!mCollapsed)
			{
                base.DrawSelfAndChildren(camera);
			}
			else
			{
				#region collapsed

                float xToUse = (float)(mWorldUnitX);
                float yToUse = (float)(mWorldUnitY);


				StaticVertices[0].Color = StaticVertices[1].Color = StaticVertices[2].Color = StaticVertices[3].Color = StaticVertices[4].Color = StaticVertices[5].Color = mColor;

				StaticVertices[0].Position.Z = StaticVertices[1].Position.Z = StaticVertices[2].Position.Z = 
                    StaticVertices[3].Position.Z = StaticVertices[4].Position.Z = StaticVertices[5].Position.Z = 
                    camera.Z + FlatRedBall.Math.MathFunctions.ForwardVector3.Z * 100;


				#region Left Side
				StaticVertices[0].Position.X = xToUse - ScaleX;
				StaticVertices[0].Position.Y = yToUse + ScaleY - .4f;
				StaticVertices[0].TextureCoordinate.X = 0;
				StaticVertices[0].TextureCoordinate.Y = .6953125f;

				StaticVertices[1].Position.X = xToUse - ScaleX;
				StaticVertices[1].Position.Y = yToUse + ScaleY + 2.8f;
				StaticVertices[1].TextureCoordinate.X = 0;
				StaticVertices[1].TextureCoordinate.Y = .570313f;

				StaticVertices[2].Position.X = xToUse - ScaleX + .6f;
				StaticVertices[2].Position.Y = yToUse + ScaleY + 2.8f;
				StaticVertices[2].TextureCoordinate.X = .0234375f;
				StaticVertices[2].TextureCoordinate.Y = .570313f;

				StaticVertices[3] = StaticVertices[0];
				StaticVertices[4] = StaticVertices[2];

				StaticVertices[5].Position.X = xToUse - ScaleX + .6f;
				StaticVertices[5].Position.Y = yToUse + ScaleY -.4f;
				StaticVertices[5].TextureCoordinate.X = .0234375f;
				StaticVertices[5].TextureCoordinate.Y = .6953125f;

                GuiManager.WriteVerts(StaticVertices);

				#endregion


				#region Title Bar Center
				StaticVertices[0].Position.X = xToUse - ScaleX + .59f;
				StaticVertices[0].Position.Y = yToUse + ScaleY -.4f;
				StaticVertices[0].TextureCoordinate.X = .01953125f;
				StaticVertices[0].TextureCoordinate.Y = .6953125f;

				StaticVertices[1].Position.X = xToUse - ScaleX + .59f;
				StaticVertices[1].Position.Y = yToUse + ScaleY + 2.8f;
				StaticVertices[1].TextureCoordinate.X = .01953125f;
				StaticVertices[1].TextureCoordinate.Y = .570313f;

				StaticVertices[2].Position.X = xToUse + ScaleX - .59f;
				StaticVertices[2].Position.Y = yToUse + ScaleY + 2.8f;
				StaticVertices[2].TextureCoordinate.X = .0234375f;
				StaticVertices[2].TextureCoordinate.Y = .570313f;

				StaticVertices[3] = StaticVertices[0];
				StaticVertices[4] = StaticVertices[2];

				StaticVertices[5].Position.X = xToUse + ScaleX - .59f;
				StaticVertices[5].Position.Y = yToUse + ScaleY - .4f;
				StaticVertices[5].TextureCoordinate.X = .0234375f;
				StaticVertices[5].TextureCoordinate.Y = .6953125f;

                GuiManager.WriteVerts(StaticVertices);

				#endregion

				#region Right Side
				StaticVertices[0].Position.X = xToUse + ScaleX - .6f;
				StaticVertices[0].Position.Y = yToUse + ScaleY - .4f;
				StaticVertices[0].TextureCoordinate.X = .0234375f;
				StaticVertices[0].TextureCoordinate.Y = .6953125f;

				StaticVertices[1].Position.X = xToUse + ScaleX - .6f;
				StaticVertices[1].Position.Y = yToUse + ScaleY+ 2.8f;
				StaticVertices[1].TextureCoordinate.X = .0234375f;
				StaticVertices[1].TextureCoordinate.Y = .570313f;

				StaticVertices[2].Position.X = xToUse + ScaleX;
				StaticVertices[2].Position.Y = yToUse + ScaleY+ 2.8f;
				StaticVertices[2].TextureCoordinate.X = 0;
				StaticVertices[2].TextureCoordinate.Y = .570313f;

				StaticVertices[3] = StaticVertices[0];
				StaticVertices[4] = StaticVertices[2];
				
				StaticVertices[5].Position.X = xToUse + ScaleX;
				StaticVertices[5].Position.Y = yToUse + ScaleY - .4f;
				StaticVertices[5].TextureCoordinate.X = 0;
				StaticVertices[5].TextureCoordinate.Y = .6953125f;

                GuiManager.WriteVerts(StaticVertices);

				#endregion

				#region title bar text
				if(mName != "")
				{

                    TextManager.mXForVertexBuffer = xToUse - ScaleX + 1.0f;
					TextManager.mYForVertexBuffer = yToUse + ScaleY + 1.1f;
#if FRB_MDX
					TextManager.mZForVertexBuffer = (float)camera.Z + 100;
#else
                    TextManager.mZForVertexBuffer = (float)camera.Z - 100;
#endif

					TextManager.mRedForVertexBuffer = 255;
					TextManager.mGreenForVertexBuffer = 255;
					TextManager.mBlueForVertexBuffer = 255;

					TextManager.mScaleForVertexBuffer = GuiManager.TextHeight/2.0f;
					TextManager.mSpacingForVertexBuffer = GuiManager.TextSpacing;


					TextManager.Draw(ref mName);
				}
				#endregion


				#endregion
			}

		}
				
		public void Expand()
		{
			if(mCollapsed)
			{
                mWorldUnitY -= fullScaleY - 1.1f;

				ScaleY = (float)fullScaleY;

				this.UpdateDependencies();

				mCollapsed = false;
			}
		}


        internal override int GetNumberOfVerticesToDraw()
		{
			if(!mCollapsed)
			{
				return base.GetNumberOfVerticesToDraw();
			}
			else
			{
				return 18 + this.mName.Replace(" ", "").Length * 6;
			}
		}


        public override bool IsPointOnWindow(float screenRelativeX, float screenRelativeY)
        {
            if (isCollapsed)
            {
                return
                    screenRelativeX < mWorldUnitX + mScaleX &&
                    screenRelativeX > mWorldUnitX - mScaleX &&
                    screenRelativeY < mWorldUnitY + mScaleY + 2.8f &&
                    screenRelativeY > mWorldUnitY + mScaleY;
            }
            else
            {
                return base.IsPointOnWindow(screenRelativeX, screenRelativeY);
            }
        }


        public override void TestCollision(Cursor cursor)
		{
			if(!mCollapsed)
			{
				base.TestCollision(cursor);


				if(cursor.WindowOver == this && cursor.PrimaryDoubleClick &&
                    cursor.YForUI > mWorldUnitY + mScaleY - 2.1f)
				{
					DoubleClickCollapse();
				}
			}
			else
			{
                if (cursor.YForUI > mWorldUnitY + mScaleY - 2.1f)
				{ 
					if(cursor.PrimaryPush)
					{// drag the window
						cursor.WindowPushed = this;
						cursor.GrabWindow(this);
					}
					if(cursor.PrimaryDoubleClick)
					{
						DoubleClickCollapse();
					}
				}
			}
		}

	
		#endregion
	}
}
