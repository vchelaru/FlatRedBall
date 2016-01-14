using System;

#if FRB_MDX
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D=Microsoft.DirectX.Direct3D;
using Texture2D = FlatRedBall.Texture2D;
#elif FRB_XNA || SILVERLIGHT || WINDOWS_PHONE
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

#endif
using FlatRedBall.Math.Geometry;

using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Utilities;


using Point = FlatRedBall.Math.Geometry.Point;

namespace FlatRedBall.Gui
{
    #region Enums

    public enum ButtonPushedState
    {
        Down,
        Up
    }

    #endregion

    #region XML docs
    /// <summary>
	/// A UI element which displays text and visually responds to pushes.
    /// </summary>
    #endregion
    public class Button : Window
    {

        #region Fields

		bool mShowsToolTip = true;

        internal string mText;

		internal Point overlayTL;
		internal Point overlayTR;
		internal Point overlayBL;
		internal Point overlayBR;

		internal Point downOverlayTL;
		internal Point downOverlayTR;
		internal Point downOverlayBL;
		internal Point downOverlayBR;

        #region XML Docs
        /// <summary>
        /// Whether the base (center and borders) are drawn
        /// </summary>
        #endregion
        bool mDrawBase;

		protected Texture2D mOverlayTexture;

		private bool mFlipVertical;
		private bool mFlipHorizontal;

		private bool mHighlightOnDown;

        ButtonPushedState mButtonPushedState = ButtonPushedState.Up;


        #region SpriteFrame values

        Text mTextObject;

        ButtonSkin mUpSkin;
        ButtonSkin mDownSkin;

        #endregion
        #endregion



        #region Properties

        #region XML Docs
        /// <summary>
        /// Gets the current ButtonPushedState.
        /// </summary>
        #endregion
        public ButtonPushedState ButtonPushedState
        {
            get { return mButtonPushedState; }
            internal set 
            { 
                mButtonPushedState = value;
                if (GuiManagerDrawn == false)
                {
                    if (mButtonPushedState == ButtonPushedState.Up)
                    {
                        SetTexturePropertiesFromSkin(mUpSkin);
                    }
                    else
                    {
                        SetTexturePropertiesFromSkin(mDownSkin);
                    }
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// Whether the Window draws its base and borders.  Setting to false will only draw the Window's texture.
        /// </summary>
        /// <remarks>
        /// Set this to false if the Button is used to draw a texture.
        /// </remarks>
        #endregion
        public bool DrawBase
        {
            get { return mDrawBase; }
            set { mDrawBase = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets and sets whether the Button can be interacted with.
        /// </summary>
        #endregion
        public override bool Enabled
        {
            get
            {
                return base.Enabled;
            }
            set
            {
                base.Enabled = value;
#if FRB_MDX
                if(value == true)
                    this.mColor = 0xFF000000;
                else
                    this.mColor = 0xAA000000;
#else
                if(value == true)
                    this.mColor.PackedValue = 0xFF000000;
                else
                    this.mColor.PackedValue = 0xAA000000;
#endif

            }
        }

        #region XML Docs
        /// <summary>
        /// Gets and sets whether to horizontally flip the overlayed texture.
        /// </summary>
        /// <remarks>
        /// This property only affects the appearance of the Button if SetOverlayTexture is called to change the
        /// overlay texture.
        /// </remarks>
        #endregion
        public bool FlipHorizontal
        {
            get { return mFlipHorizontal; }
            set { mFlipHorizontal = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets and sets whether to horizontally flip the overlayed texture.
        /// </summary>
        /// <remarks>
        /// This property only affects the appearance of the Button if SetOverlayTexture is called to change the
        /// overlay texture.
        /// </remarks>
        #endregion
        public bool FlipVertical
        {
            get { return mFlipVertical; }
            set { mFlipVertical = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets and sets whether the button should draw itself lighter when pressed if it
        /// is referencing an overly texture.  Default value is true.
        /// </summary>
        /// <remarks>
        /// This property has no impact if the button is not referencing an overlay texture.
        /// </remarks>
        #endregion
        public bool HighlightOnDown
        {
            get { return mHighlightOnDown; }
            set { mHighlightOnDown = value; }
        }

		public bool ShowsToolTip
		{
			get { return mShowsToolTip; }
			set { mShowsToolTip = value; }
		}

        #region XML Docs
        /// <summary>
        /// The string to display and the tool tip text to display when the cursor
        /// moves over the button.
        /// </summary>
        /// <remarks>
        /// This is only on the Button if the overlay texture is null.  The tool tip will show
        /// regardless of whether the button is showing a texture or not.
        /// </remarks>
        #endregion
        public virtual string Text
        {
            get { return mText; }
            set 
            { 
                mText = value;
                if (mTextObject != null)
                {
                    mTextObject.DisplayText = value;
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// The overlay texture the button is displaying.
        /// </summary>
        #endregion
        public Texture2D UpOverlayTexture
        {
            get { return mOverlayTexture; }
        }

        public override bool Visible
        {
            get
            {
                return base.Visible;
            }
            set
            {
                base.Visible = value;

                if (mTextObject != null)
                {
                    mTextObject.Visible = value;
                }
            }
        }

        #endregion



        #region Methods

        #region Constructor

        public Button(Cursor cursor) : 
            base(cursor)
		{
			//overlayTexture = null; automatic

            ScaleX = 1;
            ScaleY = 1;

			DrawBase = true;

			overlayTL.X = overlayTL.Y = -1;
			overlayTR.X = overlayTR.Y = -1;
			overlayBL.X = overlayBL.Y = -1;
			overlayBR.X = overlayBR.Y = -1;

			downOverlayTL.X = downOverlayTL.Y = -1;
			downOverlayTR.X = downOverlayTR.Y = -1;
			downOverlayBL.X = downOverlayBL.Y = -1;
			downOverlayBR.X = downOverlayBR.Y = -1;

			HighlightOnDown = true;

//			flipOnXAxis = false;
//			flipOnYAxis = false;

			mNumberOfVertices = 54; // 9 quads, 6 vertices per quad

		}

        public Button(string buttonTexture, Cursor cursor, string contentManagerName)
            :
            base(buttonTexture, cursor, contentManagerName)
        {
            ScaleX = 1;
            ScaleY = 1;
        }

        public Button(GuiSkin guiSkin, Cursor cursor)
            : base(guiSkin, cursor)
        {
            mUpSkin = guiSkin.ButtonSkin;
            mDownSkin = guiSkin.ButtonDownSkin;

            mTextObject = TextManager.AddText(this.Text, guiSkin.ButtonSkin.Font);
            mTextObject.HorizontalAlignment = HorizontalAlignment.Center;
            mTextObject.VerticalAlignment = VerticalAlignment.Center;
            mTextObject.AttachTo(SpriteFrame, false);
            mTextObject.RelativeZ = -.001f * FlatRedBall.Math.MathFunctions.ForwardVector3.Z;

            SetTexturePropertiesFromSkin(mUpSkin);

            ScaleX = 1;
            ScaleY = 1;
        }
        #endregion


        #region Public Methods

        public virtual void Press()
        {
            OnClick();
        }


        public virtual void SetOverlayTextures(Texture2D upTexture, Texture2D downTexture)
		{
			mOverlayTexture = upTexture;

            SetAnimationChain(null);
		}


		public void SetOverlayTextures(int col, int row)
		{
            overlayTL.X = col * 16 / 256.0f;
            overlayTL.Y = .75f + row * 16 / 256.0f;

            overlayTR.X = (1 + col) * 16 / 256.0f;
            overlayTR.Y = .75f + row * 16 / 256.0f;

            overlayBL.X = col * 16 / 256.0f;
            overlayBL.Y = .75f + (1 + row) * 16 / 256.0f;

            overlayBR.X = (1 + col) * 16 / 256.0f;
            overlayBR.Y = .75f + (1 + row) * 16 / 256.0f;

            mNumberOfVertices = 54 + 6; // 9 quads, 6 vertices per quad plus the new overlaying quad		

        
        
        }


		public void SetOverlayTextures(int col, int row, int downCol, int downRow)
		{
			SetOverlayTextures(col, row);


			downOverlayTL.X = downCol*16/256.0f;
			downOverlayTL.Y = .75f + downRow*16/256.0f;

			downOverlayTR.X = (1 + downCol)*16/256.0f;
			downOverlayTR.Y = .75f + downRow*16/256.0f;

			downOverlayBL.X = downCol*16/256.0f;
			downOverlayBL.Y = .75f + (1 + downRow)*16/256.0f;

			downOverlayBR.X = (1+downCol)*16/256.0f;
			downOverlayBR.Y = .75f + (1 + downRow)*16/256.0f;

			mNumberOfVertices = 54 + 6; // 9 quads, 6 vertices per quad plus the new overlaying quad

		}	


		public void SetOverlayAnimationChain(AnimationChain achToSet)
		{
            throw new NotImplementedException();
			//mSprite.SetAnimationChain(achToSet, TimeManager.CurrentTime);
        }

        #endregion

        #region Protected Methods

        public override void SetSkin(GuiSkin guiSkin)
        {
            SetSkin(guiSkin.ButtonSkin, guiSkin.ButtonDownSkin);
        }

        public void SetSkin(ButtonSkin upSkin, ButtonSkin downSkin)
        {
            mUpSkin = upSkin;
            mDownSkin = downSkin;

            switch (mButtonPushedState)
            {
                case ButtonPushedState.Up:
                    SetTexturePropertiesFromSkin(mUpSkin);
                    break;
                case ButtonPushedState.Down:
                    SetTexturePropertiesFromSkin(mDownSkin);
                    break;
            }
        }

        private void SetTexturePropertiesFromSkin(ButtonSkin buttonSkin)
        {
            SetFromWindowSkin(buttonSkin);

            if (mTextObject != null)
            {
                mTextObject.Font = buttonSkin.Font;
                mTextObject.Scale = buttonSkin.TextScale;
                mTextObject.Spacing = buttonSkin.TextSpacing;
            }

        }

        #endregion

        #region Internal Methods

        internal override void Destroy()
        {
            Destroy(false);
        }

        internal protected override void Destroy(bool keepEvents)
        {
            base.Destroy(keepEvents);
            if (mTextObject != null)
            {
                TextManager.RemoveText(mTextObject);
            }        
        }
#if !SILVERLIGHT
        internal override void DrawSelfAndChildren(Camera camera)
		{

            if (Visible == false)
				return;

			float xToUse = mWorldUnitX;
            float yToUse = mWorldUnitY;
                float edgeWidth = .2f;

			#region draw the basic button

			StaticVertices[0].Position.Z = StaticVertices[1].Position.Z = StaticVertices[2].Position.Z = 
                StaticVertices[3].Position.Z = StaticVertices[4].Position.Z = StaticVertices[5].Position.Z = 
                camera.Z + FlatRedBall.Math.MathFunctions.ForwardVector3.Z * 100;

			StaticVertices[0].Color = StaticVertices[1].Color = StaticVertices[2].Color = StaticVertices[3].Color = StaticVertices[4].Color = StaticVertices[5].Color = mColor;
			if(mDrawBase)
			{

				#region state == "down"
				if(ButtonPushedState == ButtonPushedState.Down)
				{
					#region Top Left of the Window

					StaticVertices[0].Position.X = xToUse - ScaleX;
                    StaticVertices[0].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[0].TextureCoordinate.X = .179688f;
					StaticVertices[0].TextureCoordinate.Y = .64453125f;


					StaticVertices[1].Position.X = xToUse - ScaleX;
					StaticVertices[1].Position.Y = yToUse + ScaleY;
					StaticVertices[1].TextureCoordinate.X = .179688f;
					StaticVertices[1].TextureCoordinate.Y = .640625f;

                    StaticVertices[2].Position.X = xToUse - ScaleX + edgeWidth;
					StaticVertices[2].Position.Y = yToUse + ScaleY;
					StaticVertices[2].TextureCoordinate.X = .18359375f;
					StaticVertices[2].TextureCoordinate.Y = .640625f;


					StaticVertices[3] = StaticVertices[0];

					StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xToUse - ScaleX + edgeWidth;
                    StaticVertices[5].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[5].TextureCoordinate.X = .18359375f;
					StaticVertices[5].TextureCoordinate.Y = .64453125f;

                    GuiManager.WriteVerts(StaticVertices);

					#endregion
			
					#region Top Border

                    StaticVertices[0].Position.X = xToUse - ScaleX + edgeWidth;
                    StaticVertices[0].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[0].TextureCoordinate.X = .1796875f;
					StaticVertices[0].TextureCoordinate.Y = .64453125f;


                    StaticVertices[1].Position.X = xToUse - ScaleX + edgeWidth;
					StaticVertices[1].Position.Y = yToUse + ScaleY;
					StaticVertices[1].TextureCoordinate.X = .1796875f;
					StaticVertices[1].TextureCoordinate.Y = .640625f;


                    StaticVertices[2].Position.X = xToUse + ScaleX - edgeWidth;
					StaticVertices[2].Position.Y = yToUse + ScaleY;
					StaticVertices[2].TextureCoordinate.X = .18359375f;
					StaticVertices[2].TextureCoordinate.Y = .640625f;


					StaticVertices[3] = StaticVertices[0];

					StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xToUse + ScaleX - edgeWidth;
                    StaticVertices[5].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[5].TextureCoordinate.X = .18359375f;
					StaticVertices[5].TextureCoordinate.Y = .64453125f;

                    GuiManager.WriteVerts(StaticVertices);

					#endregion

					#region Top Right of the Window
                    StaticVertices[0].Position.X = xToUse + ScaleX - edgeWidth;
                    StaticVertices[0].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[0].TextureCoordinate.X = .17578125f;
					StaticVertices[0].TextureCoordinate.Y = .640625f;

                    StaticVertices[1].Position.X = xToUse + ScaleX - edgeWidth;
					StaticVertices[1].Position.Y = yToUse + ScaleY;
					StaticVertices[1].TextureCoordinate.X = .17578125f;
					StaticVertices[1].TextureCoordinate.Y = .63671875f;

					StaticVertices[2].Position.X = xToUse + ScaleX;
					StaticVertices[2].Position.Y = yToUse + ScaleY;
					StaticVertices[2].TextureCoordinate.X = .1796875f;
					StaticVertices[2].TextureCoordinate.Y = .63671875f;

					StaticVertices[3] = StaticVertices[0];

					StaticVertices[4] = StaticVertices[2];

					StaticVertices[5].Position.X = xToUse + ScaleX;
                    StaticVertices[5].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[5].TextureCoordinate.X = .1796875f;
					StaticVertices[5].TextureCoordinate.Y = .640625f;

                    GuiManager.WriteVerts(StaticVertices);
					#endregion

			
					#region RightBorder
                    StaticVertices[0].Position.X = xToUse + ScaleX - edgeWidth;
                    StaticVertices[0].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[0].TextureCoordinate.X = .17578125f;
					StaticVertices[0].TextureCoordinate.Y = .63671875f;

                    StaticVertices[1].Position.X = xToUse + ScaleX - edgeWidth;
                    StaticVertices[1].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[1].TextureCoordinate.X = .17578125f;
					StaticVertices[1].TextureCoordinate.Y = .6328125f;

					StaticVertices[2].Position.X = xToUse + ScaleX;
                    StaticVertices[2].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[2].TextureCoordinate.X = .1796875f;
					StaticVertices[2].TextureCoordinate.Y = .6328125f;

					StaticVertices[3] = StaticVertices[0];

					StaticVertices[4] = StaticVertices[2];

					StaticVertices[5].Position.X = xToUse + ScaleX;
                    StaticVertices[5].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[5].TextureCoordinate.X = .1796875f;
					StaticVertices[5].TextureCoordinate.Y = .63671875f;

                    GuiManager.WriteVerts(StaticVertices);

					#endregion			

			
					#region Bottom Border

                    StaticVertices[0].Position.X = xToUse - ScaleX + edgeWidth;
					StaticVertices[0].Position.Y = yToUse - ScaleY;
					StaticVertices[0].TextureCoordinate.X = .1796875f;
					StaticVertices[0].TextureCoordinate.Y = .63671875f;

                    StaticVertices[1].Position.X = xToUse - ScaleX + edgeWidth;
                    StaticVertices[1].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[1].TextureCoordinate.X = .1796875f;
					StaticVertices[1].TextureCoordinate.Y = .6328125f;

                    StaticVertices[2].Position.X = xToUse + ScaleX - edgeWidth;
                    StaticVertices[2].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[2].TextureCoordinate.X = .18359375f;
					StaticVertices[2].TextureCoordinate.Y = .6328125f;

					StaticVertices[3] = StaticVertices[0];

					StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xToUse + ScaleX - edgeWidth;
					StaticVertices[5].Position.Y = yToUse - ScaleY;
					StaticVertices[5].TextureCoordinate.X = .18359375f;
					StaticVertices[5].TextureCoordinate.Y = .63671875f;

                    GuiManager.WriteVerts(StaticVertices);

					#endregion

			
					#region LeftBorder
					StaticVertices[0].Position.X = xToUse - ScaleX;
                    StaticVertices[0].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[0].TextureCoordinate.X = .18359375f;
					StaticVertices[0].TextureCoordinate.Y = .640625f;

					StaticVertices[1].Position.X = xToUse - ScaleX;
                    StaticVertices[1].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[1].TextureCoordinate.X = .18359375f;
					StaticVertices[1].TextureCoordinate.Y = .63671875f;

                    StaticVertices[2].Position.X = xToUse - ScaleX + edgeWidth;
                    StaticVertices[2].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[2].TextureCoordinate.X = .1875f;
					StaticVertices[2].TextureCoordinate.Y = .63671875f;

					StaticVertices[3] = StaticVertices[0];

					StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xToUse - ScaleX + edgeWidth;
                    StaticVertices[5].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[5].TextureCoordinate.X = .1875f;
					StaticVertices[5].TextureCoordinate.Y = .640625f;

                    GuiManager.WriteVerts(StaticVertices);

					#endregion			
			
					#region Bottom Left of the Window
					StaticVertices[0].Position.X = xToUse - ScaleX;
					StaticVertices[0].Position.Y = yToUse - ScaleY;
					StaticVertices[0].TextureCoordinate.X = .179688f;
					StaticVertices[0].TextureCoordinate.Y = .63671875f;

					StaticVertices[1].Position.X = xToUse - ScaleX;
                    StaticVertices[1].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[1].TextureCoordinate.X = .179688f;
					StaticVertices[1].TextureCoordinate.Y = .6328125f;

                    StaticVertices[2].Position.X = xToUse - ScaleX + edgeWidth;
                    StaticVertices[2].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[2].TextureCoordinate.X = .18359375f;
					StaticVertices[2].TextureCoordinate.Y = .6328125f;

					StaticVertices[3] = StaticVertices[0];				

					StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xToUse - ScaleX + edgeWidth;
					StaticVertices[5].Position.Y = yToUse - ScaleY;
					StaticVertices[5].TextureCoordinate.X = .18359375f;
					StaticVertices[5].TextureCoordinate.Y = .63671875f;

                    GuiManager.WriteVerts(StaticVertices);

					#endregion

					#region Center
                    StaticVertices[0].Position.X = xToUse - ScaleX + edgeWidth;
                    StaticVertices[0].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[0].TextureCoordinate.X = .10938f;
					StaticVertices[0].TextureCoordinate.Y = .63719f;

                    StaticVertices[1].Position.X = xToUse - ScaleX + edgeWidth;
                    StaticVertices[1].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[1].TextureCoordinate.X = .10938f;
					StaticVertices[1].TextureCoordinate.Y = .6953125f;


                    StaticVertices[2].Position.X = xToUse + ScaleX - edgeWidth;
                    StaticVertices[2].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[2].TextureCoordinate.X = .10998f;
					StaticVertices[2].TextureCoordinate.Y = .6953125f;
					

					StaticVertices[3] = StaticVertices[0];
					

					StaticVertices[4] = StaticVertices[2];

                    StaticVertices[5].Position.X = xToUse + ScaleX - edgeWidth;
                    StaticVertices[5].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[5].TextureCoordinate.X = .10998f;
					StaticVertices[5].TextureCoordinate.Y = .63719f;

                    GuiManager.WriteVerts(StaticVertices);
			
					#endregion
			
					#region Bottom Right of the Window

                    StaticVertices[0].Position.X = xToUse + ScaleX - edgeWidth;
					StaticVertices[0].Position.Y = yToUse - ScaleY;
					StaticVertices[0].TextureCoordinate.X = .17578125f;
					StaticVertices[0].TextureCoordinate.Y = .63671875f;

                    StaticVertices[1].Position.X = xToUse + ScaleX - edgeWidth;
                    StaticVertices[1].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[1].TextureCoordinate.X = .17578125f;
					StaticVertices[1].TextureCoordinate.Y = .6328125f;

					StaticVertices[2].Position.X = xToUse + ScaleX;
                    StaticVertices[2].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[2].TextureCoordinate.X = .1796875f;
					StaticVertices[2].TextureCoordinate.Y = .6328125f;

					StaticVertices[3] = StaticVertices[0];

					StaticVertices[4] = StaticVertices[2];

					StaticVertices[5].Position.X = xToUse + ScaleX;
					StaticVertices[5].Position.Y = yToUse - ScaleY;
					StaticVertices[5].TextureCoordinate.X = .1796875f;
					StaticVertices[5].TextureCoordinate.Y = .63671875f;

                    GuiManager.WriteVerts(StaticVertices);

					#endregion
				}
					#endregion
		
				#region state == "up"
				else
				{

					#region Top Left of the Window
					StaticVertices[0].Position.X = xToUse - ScaleX;
					StaticVertices[0].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[0].TextureCoordinate.X = .17578125f;
					StaticVertices[0].TextureCoordinate.Y = .63671875f;

					StaticVertices[1].Position.X = xToUse - ScaleX;
					StaticVertices[1].Position.Y = yToUse + ScaleY;
					StaticVertices[1].TextureCoordinate.X = .17578125f;
					StaticVertices[1].TextureCoordinate.Y = .6328125f;

					StaticVertices[2].Position.X = xToUse - ScaleX + edgeWidth;
					StaticVertices[2].Position.Y = yToUse + ScaleY;
					StaticVertices[2].TextureCoordinate.X = .1796875f;
					StaticVertices[2].TextureCoordinate.Y = .6328125f;

					StaticVertices[3] = StaticVertices[0];

					StaticVertices[4] = StaticVertices[2];


					StaticVertices[5].Position.X = xToUse - ScaleX + edgeWidth;
					StaticVertices[5].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[5].TextureCoordinate.X = .1796875f;
					StaticVertices[5].TextureCoordinate.Y = .63671875f;

                    GuiManager.WriteVerts(StaticVertices);

					#endregion
			
					#region Top Border

					StaticVertices[0].Position.X = xToUse - ScaleX + edgeWidth;
					StaticVertices[0].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[0].TextureCoordinate.X = .1796875f;
					StaticVertices[0].TextureCoordinate.Y = .63671875f;

					StaticVertices[1].Position.X = xToUse - ScaleX + edgeWidth;
					StaticVertices[1].Position.Y = yToUse + ScaleY;
					StaticVertices[1].TextureCoordinate.X = .1796875f;
					StaticVertices[1].TextureCoordinate.Y = .6328125f;

					StaticVertices[2].Position.X = xToUse + ScaleX - edgeWidth;
					StaticVertices[2].Position.Y = yToUse + ScaleY;
					StaticVertices[2].TextureCoordinate.X = .18359375f;
					StaticVertices[2].TextureCoordinate.Y = .6328125f;

					StaticVertices[3] = StaticVertices[0];

					StaticVertices[4] = StaticVertices[2];

					StaticVertices[5].Position.X = xToUse + ScaleX - edgeWidth;
					StaticVertices[5].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[5].TextureCoordinate.X = .18359375f;
					StaticVertices[5].TextureCoordinate.Y = .63671875f;

                    GuiManager.WriteVerts(StaticVertices);

					#endregion

					#region Top Right of the Window
					StaticVertices[0].Position.X = xToUse + ScaleX - edgeWidth;
					StaticVertices[0].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[0].TextureCoordinate.X = .179688f;
					StaticVertices[0].TextureCoordinate.Y = .63671875f;

					StaticVertices[1].Position.X = xToUse + ScaleX - edgeWidth;
					StaticVertices[1].Position.Y = yToUse + ScaleY;
					StaticVertices[1].TextureCoordinate.X = .179688f;
					StaticVertices[1].TextureCoordinate.Y = .6328125f;

					StaticVertices[2].Position.X = xToUse + ScaleX;
					StaticVertices[2].Position.Y = yToUse + ScaleY;
					StaticVertices[2].TextureCoordinate.X = .18359375f;
					StaticVertices[2].TextureCoordinate.Y = .6328125f;

					StaticVertices[3] = StaticVertices[0];

					StaticVertices[4] = StaticVertices[2];

					StaticVertices[5].Position.X = xToUse + ScaleX;
					StaticVertices[5].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[5].TextureCoordinate.X = .18359375f;
					StaticVertices[5].TextureCoordinate.Y = .63671875f;

                    GuiManager.WriteVerts(StaticVertices);
					#endregion

					#region RightBorder
					StaticVertices[0].Position.X = xToUse + ScaleX - edgeWidth;
					StaticVertices[0].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[0].TextureCoordinate.X = .18359375f;
					StaticVertices[0].TextureCoordinate.Y = .640625f;
					

					StaticVertices[1].Position.X = xToUse + ScaleX - edgeWidth;
					StaticVertices[1].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[1].TextureCoordinate.X = .18359375f;
					StaticVertices[1].TextureCoordinate.Y = .63671875f;
					

					StaticVertices[2].Position.X = xToUse + ScaleX;
					StaticVertices[2].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[2].TextureCoordinate.X = .1875f;
					StaticVertices[2].TextureCoordinate.Y = .63671875f;
					


					StaticVertices[3] = StaticVertices[0];

					StaticVertices[4] = StaticVertices[2];

					
					StaticVertices[5].Position.X = xToUse + ScaleX;
					StaticVertices[5].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[5].TextureCoordinate.X = .1875f;
					StaticVertices[5].TextureCoordinate.Y = .640625f;

                    GuiManager.WriteVerts(StaticVertices);

					#endregion			
			
					#region Bottom Right of the Window

					StaticVertices[0].Position.X = xToUse + ScaleX - edgeWidth;
					StaticVertices[0].Position.Y = yToUse - ScaleY;
					StaticVertices[0].TextureCoordinate.X = .179688f;
					StaticVertices[0].TextureCoordinate.Y = .64453125f;

					StaticVertices[1].Position.X = xToUse + ScaleX - edgeWidth;
					StaticVertices[1].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[1].TextureCoordinate.X = .179688f;
					StaticVertices[1].TextureCoordinate.Y = .640625f;
					

					StaticVertices[2].Position.X = xToUse + ScaleX;
					StaticVertices[2].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[2].TextureCoordinate.X = .18359375f;
					StaticVertices[2].TextureCoordinate.Y = .640625f;
					
					StaticVertices[3] = StaticVertices[0];
					StaticVertices[4] = StaticVertices[2];
					
					
					StaticVertices[5].Position.X = xToUse + ScaleX;
					StaticVertices[5].Position.Y = yToUse - ScaleY;
					StaticVertices[5].TextureCoordinate.X = .18359375f;
					StaticVertices[5].TextureCoordinate.Y = .64453125f;

                    GuiManager.WriteVerts(StaticVertices);
			
					#endregion

					#region Bottom Border

					StaticVertices[0].Position.X = xToUse - ScaleX + edgeWidth;
					StaticVertices[0].Position.Y = yToUse - ScaleY;
					StaticVertices[0].TextureCoordinate.X = .1796875f;
					StaticVertices[0].TextureCoordinate.Y = .64453125f;

					StaticVertices[1].Position.X = xToUse - ScaleX + edgeWidth;
					StaticVertices[1].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[1].TextureCoordinate.X = .1796875f;
					StaticVertices[1].TextureCoordinate.Y = .640625f;

					StaticVertices[2].Position.X = xToUse + ScaleX - edgeWidth;
					StaticVertices[2].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[2].TextureCoordinate.X = .18359375f;
					StaticVertices[2].TextureCoordinate.Y = .640625f;

					StaticVertices[3] = StaticVertices[0];
					StaticVertices[4] = StaticVertices[2];

					StaticVertices[5].Position.X = xToUse + ScaleX - edgeWidth;
					StaticVertices[5].Position.Y = yToUse - ScaleY;
					StaticVertices[5].TextureCoordinate.X = .18359375f;
					StaticVertices[5].TextureCoordinate.Y = .64453125f;

                    GuiManager.WriteVerts(StaticVertices);

					#endregion
			
					#region LeftBorder
					StaticVertices[0].Position.X = xToUse - ScaleX;
					StaticVertices[0].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[0].TextureCoordinate.X = .17578125f;
					StaticVertices[0].TextureCoordinate.Y = .63671875f;

					StaticVertices[1].Position.X = xToUse - ScaleX;
					StaticVertices[1].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[1].TextureCoordinate.X = .17578125f;
					StaticVertices[1].TextureCoordinate.Y = .6328125f;

					StaticVertices[2].Position.X = xToUse - ScaleX + edgeWidth;
					StaticVertices[2].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[2].TextureCoordinate.X = .1796875f;
					StaticVertices[2].TextureCoordinate.Y = .6328125f;
					

					StaticVertices[3] = StaticVertices[0];
					StaticVertices[4] = StaticVertices[2];


					StaticVertices[5].Position.X = xToUse - ScaleX + edgeWidth;
					StaticVertices[5].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[5].TextureCoordinate.X = .1796875f;
					StaticVertices[5].TextureCoordinate.Y = .63671875f;

                    GuiManager.WriteVerts(StaticVertices);

					#endregion			
			
					#region Bottom Left of the Window
			
					StaticVertices[0].Position.X = xToUse - ScaleX;
					StaticVertices[0].Position.Y = yToUse - ScaleY;
					StaticVertices[0].TextureCoordinate.X = .17578125f;
					StaticVertices[0].TextureCoordinate.Y = .640625f;

					StaticVertices[1].Position.X = xToUse - ScaleX;
					StaticVertices[1].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[1].TextureCoordinate.X = .17578125f;
					StaticVertices[1].TextureCoordinate.Y = .63671875f;


					StaticVertices[2].Position.X = xToUse - ScaleX + edgeWidth;
					StaticVertices[2].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[2].TextureCoordinate.X = .1796875f;
					StaticVertices[2].TextureCoordinate.Y = .63671875f;
					

					StaticVertices[3] = StaticVertices[0];
					StaticVertices[4] = StaticVertices[2];
					
					StaticVertices[5].Position.X = xToUse - ScaleX + edgeWidth;
					StaticVertices[5].Position.Y = yToUse - ScaleY;
					StaticVertices[5].TextureCoordinate.X = .1796875f;
					StaticVertices[5].TextureCoordinate.Y = .640625f;

                    GuiManager.WriteVerts(StaticVertices);

					#endregion

					#region Center
					StaticVertices[0].Position.X = xToUse - ScaleX + edgeWidth;
					StaticVertices[0].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[0].TextureCoordinate.X = .10938f;
					StaticVertices[0].TextureCoordinate.Y = .6953125f;
					

					StaticVertices[1].Position.X = xToUse - ScaleX + edgeWidth;
					StaticVertices[1].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[1].TextureCoordinate.X = .10938f;
					StaticVertices[1].TextureCoordinate.Y = .63719f;
					
					StaticVertices[2].Position.X = xToUse + ScaleX - edgeWidth;
					StaticVertices[2].Position.Y = yToUse + ScaleY - edgeWidth;
					StaticVertices[2].TextureCoordinate.X = .10998f;
					StaticVertices[2].TextureCoordinate.Y = .63719f;
					
					StaticVertices[3] = StaticVertices[0];
					StaticVertices[4] = StaticVertices[2];
					
					StaticVertices[5].Position.X = xToUse + ScaleX - edgeWidth;
					StaticVertices[5].Position.Y = yToUse - ScaleY + edgeWidth;
					StaticVertices[5].TextureCoordinate.X = .10998f;
					StaticVertices[5].TextureCoordinate.Y = .6953125f;

                    GuiManager.WriteVerts(StaticVertices);
			
					#endregion
				}
				#endregion
			}
			#endregion

			#region draw overlay texture if it is an icon in our Gui texture

#if FRB_MDX
			uint colorBefore = mColor;
#else
            Color colorBefore = mColor;
#endif

			if(ButtonPushedState == ButtonPushedState.Down && mHighlightOnDown)
			{
                // This will only have an impact on the button if it is using an overlay texture.
#if FRB_MDX
				StaticVertices[0].Color = StaticVertices[1].Color = StaticVertices[2].Color = StaticVertices[3].Color = StaticVertices[4].Color = StaticVertices[5].Color = 0xff333333;
#else
                StaticVertices[0].Color.PackedValue = StaticVertices[1].Color.PackedValue = StaticVertices[2].Color.PackedValue =
                    StaticVertices[3].Color.PackedValue = StaticVertices[4].Color.PackedValue = StaticVertices[5].Color.PackedValue = 0xff333333;

#endif

			}

            if (ButtonPushedState == ButtonPushedState.Down && downOverlayTL.X != -1)
			{

				StaticVertices[0].Position.X = xToUse - ScaleX + edgeWidth;
				StaticVertices[0].Position.Y = yToUse - ScaleY + edgeWidth;
				StaticVertices[0].TextureCoordinate.X = (float)downOverlayBL.X;
				StaticVertices[0].TextureCoordinate.Y = (float)downOverlayBL.Y;


				StaticVertices[1].Position.X = xToUse - ScaleX + edgeWidth;	
				StaticVertices[1].Position.Y = yToUse + ScaleY - edgeWidth;
				StaticVertices[1].TextureCoordinate.X = (float)downOverlayTL.X;
				StaticVertices[1].TextureCoordinate.Y = (float)downOverlayTL.Y;

				StaticVertices[2].Position.X = xToUse + ScaleX - edgeWidth;
				StaticVertices[2].Position.Y = yToUse + ScaleY - edgeWidth;
				StaticVertices[2].TextureCoordinate.X = (float)downOverlayTR.X;
				StaticVertices[2].TextureCoordinate.Y = (float)downOverlayTR.Y;


				StaticVertices[3] = StaticVertices[0];
				StaticVertices[4] = StaticVertices[2];

				StaticVertices[5].Position.X = xToUse + ScaleX - edgeWidth;
				StaticVertices[5].Position.Y = yToUse - ScaleY + edgeWidth;
				StaticVertices[5].TextureCoordinate.X = (float)downOverlayBR.X;	
				StaticVertices[5].TextureCoordinate.Y = (float)downOverlayBR.Y;

                GuiManager.WriteVerts(StaticVertices);	
				
#if FRB_MDX
				mColor = 0xff000000;
#else
                mColor.PackedValue = 0xff000000;
#endif

			}
			else if(overlayTL.X != -1)
			{

				StaticVertices[0].Position.X = xToUse - ScaleX + edgeWidth;
				StaticVertices[0].Position.Y = yToUse - ScaleY + edgeWidth;
				StaticVertices[0].TextureCoordinate.X = (float)overlayBL.X;
				StaticVertices[0].TextureCoordinate.Y = (float)overlayBL.Y;

				StaticVertices[1].Position.X = xToUse - ScaleX + edgeWidth;	
				StaticVertices[1].Position.Y = yToUse + ScaleY - edgeWidth;
				StaticVertices[1].TextureCoordinate.X = (float)overlayTL.X;	
				StaticVertices[1].TextureCoordinate.Y = (float)overlayTL.Y;

				StaticVertices[2].Position.X = xToUse + ScaleX - edgeWidth;
				StaticVertices[2].Position.Y = yToUse + ScaleY - edgeWidth;
				StaticVertices[2].TextureCoordinate.X = (float)overlayTR.X;
				StaticVertices[2].TextureCoordinate.Y = (float)overlayTR.Y;


				StaticVertices[3] = StaticVertices[0];
				StaticVertices[4] = StaticVertices[2];

				StaticVertices[5].Position.X = xToUse + ScaleX - edgeWidth;
				StaticVertices[5].Position.Y = yToUse - ScaleY + edgeWidth;
				StaticVertices[5].TextureCoordinate.X = (float)overlayBR.X;	
				StaticVertices[5].TextureCoordinate.Y = (float)overlayBR.Y;

                GuiManager.WriteVerts(StaticVertices);
			}
			
			mColor = colorBefore;

			#endregion

			#region draw the overlay texture if it is in a different texture

			if(this.mOverlayTexture != null)
			{
				float left = mTextureLeft;
				float right = mTextureRight;
				float top = mTextureTop;
				float bottom = mTextureBottom;
				

				if(mFlipHorizontal)
				{
                    float temporary = left;
                    left = right;
                    right = temporary;
				}

				if(mFlipVertical)
				{
                    float temporary = top;
                    top = bottom;
                    bottom = temporary;
				}
				GuiManager.AddTextureSwitch(mOverlayTexture);

				StaticVertices[0].Position.X = xToUse - ScaleX + edgeWidth;
				StaticVertices[0].Position.Y = yToUse - ScaleY + edgeWidth;
				StaticVertices[0].TextureCoordinate.X = left;
				StaticVertices[0].TextureCoordinate.Y = bottom;

				StaticVertices[1].Position.X = xToUse - ScaleX + edgeWidth;
				StaticVertices[1].Position.Y = yToUse + ScaleY - edgeWidth;
				StaticVertices[1].TextureCoordinate.X = left;
				StaticVertices[1].TextureCoordinate.Y = top;

				StaticVertices[2].Position.X = xToUse + ScaleX - edgeWidth;
				StaticVertices[2].Position.Y = yToUse + ScaleY - edgeWidth;
				StaticVertices[2].TextureCoordinate.X = right;
				StaticVertices[2].TextureCoordinate.Y = top;

				StaticVertices[3] = StaticVertices[0];
				StaticVertices[4] = StaticVertices[2];

				StaticVertices[5].Position.X = xToUse + ScaleX - edgeWidth;
				StaticVertices[5].Position.Y = yToUse - ScaleY + edgeWidth;
				StaticVertices[5].TextureCoordinate.X = right;
				StaticVertices[5].TextureCoordinate.Y = bottom;

                GuiManager.WriteVerts(StaticVertices);			
			}

			#endregion



			#region Draw the text

#if FRB_MDX
            TextManager.mZForVertexBuffer = camera.Position.Z + 100;
#else
            TextManager.mZForVertexBuffer = camera.Position.Z - 100;

#endif
            TextManager.mScaleForVertexBuffer = GuiManager.TextHeight / 2.0f;
            TextManager.mSpacingForVertexBuffer = GuiManager.TextHeight / 2.0f;
            TextManager.mAlignmentForVertexBuffer = HorizontalAlignment.Center;
            TextManager.mMaxWidthForVertexBuffer = 1000;

            if (this.overlayTL.X == -1 && this.mOverlayTexture == null && Text != null && Text != "")
			{
                TextManager.mXForVertexBuffer = xToUse;
                TextManager.mYForVertexBuffer = yToUse;

                int numberOfLines = StringFunctions.GetLineCount(mText);

                if (numberOfLines > 1)
                {
                    TextManager.mYForVertexBuffer += TextManager.mSpacingForVertexBuffer * (numberOfLines - 1);

                    
                }

                TextManager.mRedForVertexBuffer = TextManager.mGreenForVertexBuffer = TextManager.mBlueForVertexBuffer = 20;

				if(!Enabled)
                    TextManager.mAlphaForVertexBuffer = FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue/2.0f;
				else
                    TextManager.mAlphaForVertexBuffer = FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;

                string textToDraw = this.Text;

                TextManager.Draw(ref textToDraw);
			}
			#endregion

        }

        internal override int GetNumberOfVerticesToDraw()
        {
            int i = 0;

            if (mDrawBase) i += 54;

            if (ButtonPushedState == ButtonPushedState.Down && downOverlayTL.X != -1)
            {
                i += 6;
            }
            else if (overlayTL.X != -1)
                i += 6;

            if (this.mOverlayTexture != null)
                i += 6;

            // This was using mText before in the CharacterCountWithoutWhitespace call.  It should
            // use Text instead so that if the instance is a ToggleButton then the correct
            // text is used.
            if (this.overlayTL.X == -1 && this.mOverlayTexture == null && Text != null)
                i += FlatRedBall.Utilities.StringFunctions.CharacterCountWithoutWhitespace(Text) * 6;

            return i;

        }
#endif
        public override void TestCollision(Cursor cursor)
        {

            base.TestCollision(cursor);

			if (cursor.WindowOver == this && ShowsToolTip)
			{
				GuiManager.ToolTipText = this.Text;
			}

            if (cursor.PrimaryDown == false)
            {
                ButtonPushedState = ButtonPushedState.Up;          
            }
            if (cursor.WindowOver == this && cursor.PrimaryDown && cursor.WindowPushed == this)
            {
                ButtonPushedState = ButtonPushedState.Down;

            }

        }


        #endregion

		#endregion
	}
}
