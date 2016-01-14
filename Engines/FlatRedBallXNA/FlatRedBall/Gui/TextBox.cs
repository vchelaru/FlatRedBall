using System;
using System.Globalization;

#if FRB_MDX

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D=Microsoft.DirectX.Direct3D;
using Microsoft.DirectX.DirectInput;
using Texture2D = FlatRedBall.Texture2D;

using Keys = Microsoft.DirectX.DirectInput.Key;
#elif FRB_XNA || SILVERLIGHT || WINDOWS_PHONE
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
#elif SILVERLIGHT

#endif
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics;
using FlatRedBall.Input;
using System.Collections.Generic;
namespace FlatRedBall.Gui
{
	/// <summary>
	/// Summary description for TextBox.
	/// </summary>
	/// 
	public class TextBox : Window, IInputReceiver
    {
        #region Enums

        public enum FormatTypes{ NoFormat, Integer, Decimal }

        #endregion

        #region Fields


        // the complete text may be longer than the window to display the text.  Therefore, the mDisplayedText
        // holds only what is visible
		private string mCompleteText;
        private string mDisplayedText;

        private string mCompleteTextOnLastGainFocus;

		public FormatTypes Format;

		int mAbsoluteCursorPosition;

		int mFirstLetterShowing;
		int mLastLetterShowing;
		int mHighlightStartPosition;
		int mHighlightEndPosition;
//		int decimalPrecision;

		public bool fixedLength;
//		public bool losingInput;
		int maxLength;

		bool mTakingInput = true;

        IInputReceiver mNextInTabSequence;

        HorizontalAlignment mHorizontalAlignment;
        float mSpacing;
        float mScale;
        //float mTextRed;
        //float mTextGreen;
        //float mTextBlue;

        #region if not drawn by the Gui man, these are necessary

        Sprite mCursorSprite;

        Text mTextObject;

        #endregion

        List<string> mAllOptions = new List<string>();

#if !SILVERLIGHT
        ListBox mOptionsListBox;
#endif

        int mIndexPushed = -1;

        List<Keys> mIgnoredKeys = new List<Keys>();

        #endregion

        #region Properties

        public HorizontalAlignment Alignment
        {
            set {mHorizontalAlignment = value; }
            get {return mHorizontalAlignment;}

        }


        public Sprite CursorSprite
        {
            get { return mCursorSprite; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the position of the visible text cursor given the mAbsoluteCursorPosition.
        /// </summary>
        #endregion
        private float CursorXPosition
        {
            get
            {
                if (mAbsoluteCursorPosition == -1)
                {
                    throw new System.IndexOutOfRangeException("The TextBox's cursor position is -1.  This is an invalid value");
                }

                if (SpriteFrame != null)
                {
                    mSpacing = mTextObject.Spacing;
                    mScale = mTextObject.Scale;

#if SILVERLIGHT
                    float leftBorder = SpriteFrame.X - SpriteFrame.ScaleX + mSpacing/2.0f;

#else

                    float leftBorder = SpriteFrame.X - SpriteFrame.ScaleX + mSpacing;
#endif

                    if (CompleteText == "")
                        return leftBorder;
                    else
                    {
#if SILVERLIGHT
						string substring = CompleteText.Substring(
                                    mFirstLetterShowing,
                                    System.Math.Min(
										mAbsoluteCursorPosition - mFirstLetterShowing, 
										CompleteText.Length - mFirstLetterShowing));
                        return leftBorder +
                            TextManager.GetWidth(
                                substring,
                                mSpacing,
                                TextManager.DefaultSpriteFont);
#else
                        return leftBorder +
                            TextManager.GetWidth(
                                CompleteText.Substring(
                                    mFirstLetterShowing,

                                    System.Math.Min(mAbsoluteCursorPosition - mFirstLetterShowing, CompleteText.Length - mFirstLetterShowing)),
                                mSpacing,
                                Font);
#endif
                    }
                }
                else
                {
                    try
                    {

                        float leftBorder = mWorldUnitX;

                        switch (Alignment)
                        {
                            case HorizontalAlignment.Left:
                                leftBorder = mWorldUnitX - mScaleX + mSpacing;
                                break;
                            case HorizontalAlignment.Center:
                                leftBorder -= .5f * TextManager.GetWidth(
                                    DisplayedText,
                                    mSpacing,
                                    Font);
                                break;
                            case HorizontalAlignment.Right:
                                leftBorder = mWorldUnitX + mScaleX - mSpacing;
                                break;
                        }
                        
                        
                        if (string.IsNullOrEmpty(DisplayedText) == false && Alignment == HorizontalAlignment.Left)
                        {
#if SILVERLIGHT
                            leftBorder -= TextManager.DefaultFont.GetCharacterWidth(DisplayedText[0]) * 
								mSpacing / 2.0f;
#else
                            leftBorder -= TextManager.DefaultFont.GetCharacterWidth(DisplayedText[0]) * 
								GuiManager.TextSpacing / 2.0f;
#endif
                        }
                        if (CompleteText == "")
                        {
                            return leftBorder;
                        }
                        else if (HideCharacters)
                        {
                            if (Alignment == HorizontalAlignment.Left)
                            {
                                return leftBorder + TextManager.GetWidth(DisplayedText, mSpacing, Font);
                            }
                            else if (Alignment == HorizontalAlignment.Center)
                            {
                                return leftBorder + TextManager.GetWidth(DisplayedText, mSpacing, Font);
                            }
                            else
                            {
                                return leftBorder;
                            }
                        }
                        else
                        {
                            float textWidth = TextManager.GetWidth(
                                    CompleteText.Substring(
                                        mFirstLetterShowing,

                                        System.Math.Min(mAbsoluteCursorPosition - mFirstLetterShowing, CompleteText.Length - mFirstLetterShowing)),
                                    mSpacing,
                                    Font);

                            if (Alignment == HorizontalAlignment.Left)
                            {
                                return leftBorder + textWidth;
                            }
                            else if (Alignment == HorizontalAlignment.Center)
                            {
                                return leftBorder + textWidth;
                            }
                            else
                            {
                                return leftBorder;
                            }

                        }
                    }
                    catch(Exception)
                    {
                        //int m = 3;

                        return 0;
                    }
                }
            }
        }


        private string CompleteText
        {
            get { return mCompleteText; }
            set
            {
                SetCompleteText(value, true);

            }
        }




        private string DisplayedText
        {
            get 
            {
                if (HideCharacters)
                {
                    return new string('*', mDisplayedText.Length);
                }
                else
                {
                    return mDisplayedText;
                }
            }
            set
            {
                mDisplayedText = value;

                if (mTextObject != null)
                    mTextObject.DisplayText = DisplayedText;

            }
        }

#if SILVERLIGHT
		public SpriteFont Font
		{
			get
			{
				return TextManager.DefaultSpriteFont;
			}
		}
#else

        public BitmapFont Font
        {
            get 
            {
                if (mTextObject != null)
                    return mTextObject.Font;
                else
                    return null;
            }
            set 
            {
                if (mTextObject != null)
                    mTextObject.Font = value;
                else
                {
                    throw new InvalidOperationException("Custom Fonts can only be set when usint GuiSkins.  The default rendering does not support custom fonts.");
                }
            }
        }
#endif

        public bool HideCharacters
        {
            get;
            set;
        }


        public List<Keys> IgnoredKeys
        {
            get { return mIgnoredKeys; }
        }


        public float Spacing
        {
            get
            {
                return mSpacing;
            }
            set
            {
                mSpacing = value;
            }
        }


        public float Scale
        {
            get { return mScale;  }
            set { mScale = value; }
        }


        public override float ScaleY
        {
            get { return base.ScaleY; }
            set
            {
                base.ScaleY = value;

                if (mCursorSprite != null)
                {
                    mCursorSprite.ScaleX = ScaleY * .1f;
                    mCursorSprite.ScaleY = ScaleY - .1f;
                }
            }
        }


        public string Text
		{
			get{	return mCompleteText;	}
			set
			{

				if(value == null)
				{
                    Text = "";
				}
				else
				{
                    // reset the highlight start and end
					mHighlightStartPosition = -1;
					mHighlightEndPosition = -1;

                    mFirstLetterShowing = 0;
#if SILVERLIGHT
                    mLastLetterShowing = mFirstLetterShowing - 1 + 
                        TextManager.GetNumberOfCharsIn(TextAreaWidth, value, mSpacing, 
                        mFirstLetterShowing, Font, mHorizontalAlignment);
#else
                    mLastLetterShowing = mFirstLetterShowing - 1 + 
                        TextManager.GetNumberOfCharsIn(TextAreaWidth, value, GuiManager.TextSpacing, 
                        mFirstLetterShowing, Font, mHorizontalAlignment);
     
#endif
                    if (fixedLength == true && value.Length > maxLength)
                    {
                        CompleteText = value.Remove(maxLength, value.Length - maxLength);
                        DisplayedText = CompleteText;
                    }
                    else
                    {
                        CompleteText = value;
                        RegulateCursorPosition();

                    }
				}
			}
		}


        private float TextAreaWidth
        {
#if SILVERLIGHT
            get { return (ScaleX) * 2 - mSpacing; }

#else
            get { return (ScaleX) * 2 - GuiManager.TextHeight/2.0f; }
#endif
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

        #region XML Docs
        /// <summary>
        /// The position of the cursor on the text in the TextBox.  This is always greater than 0 and less than
        /// the length of the text.
        /// </summary>
        #endregion
        public int AbsoluteCursorPosition
        {
            get { return mAbsoluteCursorPosition; }
        }


        public bool HasTextChangedSinceLastGainFocus
        {
            get { return mCompleteText != mCompleteTextOnLastGainFocus; }
        }

        public IInputReceiver NextInTabSequence
        {
            get { return mNextInTabSequence; }
            set { mNextInTabSequence = value; }
        }

		#endregion

		#region Events

        public event GuiMessage GainFocus;

        public event FocusUpdateDelegate FocusUpdate;

		public event GuiMessage EnterPressed = null;

		public event GuiMessage EscapeRelease = null;

        #region XML Docs
        /// <summary>
        /// Raised when the displayed text changes.  This will be called for every
        /// letter typed when the user is typing - it does not wait for an Enter or
        /// loss of focus.
        /// </summary>
        #endregion
        public event GuiMessage TextChange;

		#endregion

        #region Event Methods

        public void ShowCursor(Window callingWindow)
        {
            if (mCursorSprite != null && TakingInput)
                mCursorSprite.Visible = true;

        }

        public void HideCursor(Window callingWindow)
        {
            if (this.mCursorSprite != null)
                mCursorSprite.Visible = false;
        }

#if !SILVERLIGHT
        private void OptionsHighlight(Window callingWindow)
        {
            CollapseItem collapseItem = ((ListBox)callingWindow).GetFirstHighlightedItem();

            if (collapseItem == null)
            {
                return;
            }


            else if (collapseItem.ReferenceObject != null)
            {
                double valueHighlighted = (double)collapseItem.ReferenceObject;

                this.Text = valueHighlighted.ToString();
            }
            else
            {
                this.Text = collapseItem.Text;
            }

            // This simulates that we lost focus of the text box - like pushing enter
            OnLosingFocus();
        }
#endif 

        public void OnResize(Window callingWindow)
        {
            // This refreshes how much of the text is seen.
            Text = Text;
        }

        public void OnFocusUpdate()
        {
            if (FocusUpdate != null)
                FocusUpdate(this);
        }

        private void TextBoxClick(Window callingWindow)
        {
            if (TakingInput)
            {
                InputManager.ReceivingInput = this;
                // here we find where to put the cursor

                mAbsoluteCursorPosition = GetAbsoluteIndexAtCursorPosition();

                if (mAbsoluteCursorPosition < mFirstLetterShowing)
                {
                    // Throw an exception?
                }

                if (!mCursor.PrimaryDoubleClick && mIndexPushed == -1)
                {
                    mHighlightStartPosition = -1;
                    mHighlightEndPosition = -1;
                }

                mIndexPushed = -1;
            }
        }

        private void TextBoxDoubleClick(Window callingWindow)
        {
            if (TakingInput)
            {

                mHighlightStartPosition = 0;
                mHighlightEndPosition = CompleteText.Length;

                mAbsoluteCursorPosition = mHighlightEndPosition;

            }
        }

        private void TextBoxDrag(Window callingWindow)
        {
            if (mIndexPushed != -1)
            {
                int indexOver = GetAbsoluteIndexAtCursorPosition();

                mHighlightStartPosition = System.Math.Min(mIndexPushed, indexOver);
                mHighlightEndPosition = System.Math.Max(mIndexPushed, indexOver);
            }
        }

        private void TextBoxPush(Window callingWindow)
        {
			TextBoxClick(callingWindow);
            mHighlightStartPosition = mHighlightEndPosition = -1;
            mIndexPushed = GetAbsoluteIndexAtCursorPosition();
        }

        #endregion

        #region Methods

        #region Constructors
        public TextBox(Cursor cursor) : 
            base(cursor)
		{
            mFirstLetterShowing = 0;
            mLastLetterShowing = -1;
			CompleteText = "";

            fixedLength = false;
			mHighlightStartPosition = mHighlightEndPosition = -1;

            mSpacing = GuiManager.TextSpacing;
            mScale = GuiManager.TextHeight / 2.0f;
		
			// leave this next line in or else text will be white
            // Update June 25, 2011
            // Why do we need this in?  It doesn't seem to do anything.
			//mTextRed = mTextGreen = mTextBlue = 0;

			this.LosingFocus += new GuiMessage(RemoveHighlight);
//			decimalPosition = 9;

            ScaleY = 1;

            Push += TextBoxPush;
            Dragging += TextBoxDrag;
            Click += TextBoxClick;
            DoubleClick += TextBoxDoubleClick;
            this.Resizing += OnResize;
            
		}

        // Vic says:  This method may no longer be needed now that we are using GuiSkins
        // to customize the UI.

        //public TextBox(string baseTexture, string cursorTexture,
        //    Camera camera, Cursor cursor, string contentManagerName)
        //    : base(baseTexture, cursor, contentManagerName)
        //{
        //    mFirstLetterShowing = 0;
        //    mLastLetterShowing = -1;

        //    CompleteText = "";

        //    mTextObject = TextManager.AddText("", SpriteFrame.LayerBelongingTo);
        //    mCursorSprite = SpriteManager.AddSprite(
        //        FlatRedBallServices.Load<Texture2D>(cursorTexture, contentManagerName)
                
                
        //        );

        //    mCursorSprite.ScaleX = .1f;
        //    mCursorSprite.ScaleY = 1;
        //    mCursorSprite.Z = -.01f;
        //    mCursorSprite.Visible = false;

        //    fixedLength = false;
        //    mHighlightStartPosition = mHighlightEndPosition = -1;

        //    mTextObject.AttachTo(SpriteFrame, false);
        //    mTextRed = mTextGreen = mTextBlue = 0;
        //    mTextObject.RelativeY = .1f;


        //    LosingFocus += new GuiMessage(RemoveHighlight);
        //    LosingFocus += new GuiMessage(HideCursor);
        //    Click += new GuiMessage(ShowCursor);
        //    this.Resizing += OnResize;
        //}


        public TextBox(GuiSkin guiSkin, Cursor cursor)
            : base(guiSkin, cursor)
        {
            

            mFirstLetterShowing = 0;
            mLastLetterShowing = -1;

            CompleteText = "";

            mTextObject = TextManager.AddText("", SpriteFrame.LayerBelongingTo);

            mCursorSprite = SpriteManager.AddSprite( 
                guiSkin.TextBoxSkin.Texture,
                SpriteFrame.LayerBelongingTo);
//            cursorSprite = SpriteManager.AddSprite("redball.bmp");


            mCursorSprite.ScaleX = .1f;
            mCursorSprite.ScaleY = SpriteFrame.ScaleY - .1f ;
            mCursorSprite.Z = SpriteFrame.Z -.1f * FlatRedBall.Math.MathFunctions.ForwardVector3.Z;
            mCursorSprite.Visible = false;

            fixedLength = false;
            mHighlightStartPosition = mHighlightEndPosition = -1;

            mTextObject.AttachTo(SpriteFrame, false);
            mTextObject.RelativeY = .1f;
            mTextObject.RelativeZ = -.01f * FlatRedBall.Math.MathFunctions.ForwardVector3.Z;
            //mTextRed = mTextGreen = mTextBlue = 0;

			// Now that the text object is created, let's call SetTexturePropertiesFromSkin
			SetTexturePropertiesFromSkin(guiSkin);

            LosingFocus += new GuiMessage(RemoveHighlight);
            LosingFocus += new GuiMessage(HideCursor);
            Click += new GuiMessage(ShowCursor);

            Push += TextBoxPush;
            Dragging += TextBoxDrag;
            Click += TextBoxClick;
            DoubleClick += TextBoxDoubleClick;
            this.Resizing += OnResize;
        }


        #endregion

        #region Public Methods
        public override void Activity(Camera camera)
        {
            base.Activity(camera);

            if (GuiManagerDrawn == false)
            {
                if (mHorizontalAlignment == HorizontalAlignment.Left)
                {
#if SILVERLIGHT
                    mTextObject.RelativeX = -ScaleX + mTextObject.Scale/2.0f;

#else
                    mTextObject.RelativeX = -ScaleX + mTextObject.Scale;
#endif

                }
                else
                {
                    mTextObject.RelativeX = 0;
                }


                if (InputManager.ReceivingInput == this)
                {
                    mCursorSprite.X = CursorXPosition;
                    mCursorSprite.Y = mWorldUnitY;
                }
            }
        }


        public override void ClearEvents()
        {
            base.ClearEvents();

            GainFocus = null;

            EnterPressed = null;
		    EscapeRelease = null;
            TextChange = null;
            FocusUpdate = null;
        }


        public bool CurrentlyReceivingInput()
        {
            return InputManager.ReceivingInput == this;
        }


        public void OnGainFocus()
        {
            mCompleteTextOnLastGainFocus = mCompleteText;

            if (GainFocus != null)
                GainFocus(this);
        }


        public void SetOptions(IList<string> options)
        {
            mAllOptions.Clear();
            mAllOptions.AddRange(options);
        }


        public void SetCompleteText(string value, bool raiseTextChangeEvent)
        {
            if (mCompleteText != value)
            {
                mCompleteText = value;

                // Make sure it's not beyond the end of the word
                // Update:  Vic says - I'm not sure why this is here,
                // but when setting the Text in code, the mLastLetterShowing
                // will never increase.  
                // I'm going to comment it out and hopefully we figure out why this
                // was needed and we can come up with logic that will both fix the original
                // bug this was solving as well as the situation where the user sets the CompleteText
                // through code instead of typing
                //mLastLetterShowing = System.Math.Min(value.Length - 1, mLastLetterShowing);
                mLastLetterShowing = value.Length - 1;

                // Make sure it's not beyond the visible boundaries

#if SILVERLIGHT
					int numberOfCharactersInArea = 
						TextManager.GetNumberOfCharsIn(TextAreaWidth, value, mSpacing,
                        mFirstLetterShowing, TextManager.DefaultSpriteFont, mHorizontalAlignment);

                    mLastLetterShowing = System.Math.Min(mLastLetterShowing, mFirstLetterShowing - 1 +
                        numberOfCharactersInArea);
#else
                mLastLetterShowing = System.Math.Min(mLastLetterShowing, mFirstLetterShowing - 1 +
                    TextManager.GetNumberOfCharsIn(TextAreaWidth, value, GuiManager.TextSpacing,
                    mFirstLetterShowing, Font, mHorizontalAlignment));
#endif

                mFirstLetterShowing = System.Math.Min(mFirstLetterShowing, mLastLetterShowing);
                mFirstLetterShowing = System.Math.Max(0, mFirstLetterShowing);

                if (raiseTextChangeEvent && TextChange != null)
                {
                    TextChange(this);
                }
            }

            if (mLastLetterShowing == -1)
                DisplayedText = "";
            else
                DisplayedText = CompleteText.Substring(mFirstLetterShowing, 1 + mLastLetterShowing - mFirstLetterShowing);
        }


		public int TruncateText()
		{
            int difference = 0;
            if (fixedLength)
            {
#if SILVERLIGHT
                maxLength = TextManager.GetNumberOfCharsIn(TextAreaWidth, Text, mSpacing, mFirstLetterShowing);

#else
                maxLength = TextManager.GetNumberOfCharsIn(TextAreaWidth, Text, GuiManager.TextSpacing, mFirstLetterShowing);
#endif
                difference = Text.Length - maxLength;

                Text = Text.Substring(0, maxLength);
            }
            else
            {
                int lastFirstLetterShowing = mFirstLetterShowing;

                Text = Text; // the property refreshes the start and end so the text fits in the window

                difference = mFirstLetterShowing - lastFirstLetterShowing;
            }

			return difference;



		}

		
		public void UpdateFormat()
		{
			switch(Format)
			{	
				case FormatTypes.NoFormat:

					break;

				case FormatTypes.Integer:
					break;

				case FormatTypes.Decimal:
					break;
			}

		}


		public override string ToString()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.Append("Text: ").Append(Text).Append("\n");
            sb.Append("Absolute Cursor Position: ").Append(mAbsoluteCursorPosition).Append("\n");
			sb.Append("First Letter Showing: ").Append(mFirstLetterShowing).Append("\n");
			sb.Append("Last Letter Showing:  ").Append(mLastLetterShowing).Append("\n");

			return sb.ToString();
			




		}

        #endregion

        #region Internal Methods (called by the GuiManager)
        internal override void Destroy()
        {
            base.Destroy();
            if (mCursorSprite != null)
            {
                SpriteManager.RemoveSprite(mCursorSprite);
            }
            if (mTextObject != null)
            {
                TextManager.RemoveText(this.mTextObject);
            }
        }


        internal protected override void Destroy(bool keepEvents)
        {
            base.Destroy(keepEvents);
            if (mCursorSprite != null)
            {
                SpriteManager.RemoveSprite(mCursorSprite);
            }
            if (mTextObject != null)
            {
                TextManager.RemoveText(this.mTextObject);
            }
        }

#if !SILVERLIGHT
        internal override void DrawSelfAndChildren(Camera camera)
		{

            if (Visible == false) return;

			float xToUse = mWorldUnitX;
            float yToUse = mWorldUnitY;

            mScale = GuiManager.TextHeight / 2.0f;
            mSpacing = GuiManager.TextSpacing;

            DrawBase(camera);

			#region next we draw the highlight box if text is highlighted

			if(mHighlightStartPosition != -1)
			{
				float startXPos = (float)(xToUse -mScaleX+.5f)+(mScale * TextManager.GetRelativeOffset(mHighlightStartPosition, mFirstLetterShowing, CompleteText));
				
                float endXPos = (float)
                    (mWorldUnitX - mScaleX+.5f) +
                    (mScale * 
                        TextManager.GetRelativeOffset( System.Math.Min(mHighlightEndPosition, mLastLetterShowing +1), mFirstLetterShowing, CompleteText));
				float highlightScaleX = (endXPos - startXPos)/2.0f;
				float highlightXPos = (startXPos + endXPos)/2.0f;



				StaticVertices[0].Position.X = highlightXPos - highlightScaleX; 
				StaticVertices[0].Position.Y = yToUse - 1;
				StaticVertices[0].TextureCoordinate.X = .0234375f;	
				StaticVertices[0].TextureCoordinate.Y = .65f;

				StaticVertices[1].Position.X = highlightXPos - highlightScaleX; 
				StaticVertices[1].Position.Y = yToUse + 1;
				StaticVertices[1].TextureCoordinate.X = .0234375f;	
				StaticVertices[1].TextureCoordinate.Y = .65f;

				StaticVertices[2].Position.X = highlightXPos + highlightScaleX; 
				StaticVertices[2].Position.Y = yToUse + 1;
				StaticVertices[2].TextureCoordinate.X = .02734375f;	
				StaticVertices[2].TextureCoordinate.Y = .66f;

				StaticVertices[3] = StaticVertices[0];
				StaticVertices[4] = StaticVertices[2];

				StaticVertices[5].Position.X = highlightXPos + highlightScaleX; 
				StaticVertices[5].Position.Y = yToUse - 1;
				StaticVertices[5].TextureCoordinate.X = .02734375f;	
				StaticVertices[5].TextureCoordinate.Y = .65f;

                GuiManager.WriteVerts(StaticVertices);
			}

			#endregion

			#region next we draw the text

            if (Text.Length != 0 && 1 + mLastLetterShowing - mFirstLetterShowing > 0 &&
                mFirstLetterShowing < Text.Length)
            {
                string stringToWrite = DisplayedText;

                DrawTextLine(ref stringToWrite, mWorldUnitY);
            }

			#endregion

			#region finally we draw the cursor
            if (InputManager.ReceivingInput == this)
			{// we draw a text cursor

                // on FRB MDX the camera.X part throws off the cursor when the camera is at a non-zero position.  What's it do in FRB XNA?
                float xPos =  CursorXPosition;// (float)(si.X + camera.X - si.ScaleX + .57f) + (mScale * TextManager.GetRelX(cursorPosition, firstLetterShowing, Text));

                float cursorScaleY = this.ScaleY - .1f;
                float cursorScaleX = .1f * ScaleY;

				#region cursor vertices
                StaticVertices[0].Position.X = xPos - cursorScaleX;
                StaticVertices[0].Position.Y = yToUse - cursorScaleY;
				StaticVertices[0].TextureCoordinate.X = .219f;	
				StaticVertices[0].TextureCoordinate.Y = .649f;

                StaticVertices[1].Position.X = xPos - cursorScaleX;
                StaticVertices[1].Position.Y = yToUse + cursorScaleY;
				StaticVertices[1].TextureCoordinate.X = .219f;	
				StaticVertices[1].TextureCoordinate.Y = .648f;

                StaticVertices[2].Position.X = xPos + cursorScaleX;
                StaticVertices[2].Position.Y = yToUse + cursorScaleY;
				StaticVertices[2].TextureCoordinate.X = .22f;	
				StaticVertices[2].TextureCoordinate.Y = .648f;

				StaticVertices[3] = StaticVertices[0];
				StaticVertices[4] = StaticVertices[2];

                StaticVertices[5].Position.X = xPos + cursorScaleX;
                StaticVertices[5].Position.Y = yToUse - cursorScaleY;
				StaticVertices[5].TextureCoordinate.X = .22f;	
				StaticVertices[5].TextureCoordinate.Y = .649f;

                GuiManager.WriteVerts(StaticVertices);

				#endregion			
			}
			#endregion


        }

        internal void DrawBase(Camera camera)
        {
            float xToUse = mWorldUnitX;
            float yToUse = mWorldUnitY;


            #region first we draw the base window

#if FRB_MDX
            if (Enabled)
            {
                StaticVertices[0].Color = StaticVertices[1].Color = StaticVertices[2].Color = StaticVertices[3].Color = StaticVertices[4].Color = StaticVertices[5].Color = 0xff000000;
            }
            else
            {
                StaticVertices[0].Color = StaticVertices[1].Color = StaticVertices[2].Color = StaticVertices[3].Color = StaticVertices[4].Color = StaticVertices[5].Color = 0x88000000;
            }
#else
            if (Enabled)
            {
                StaticVertices[0].Color.PackedValue = StaticVertices[1].Color.PackedValue = StaticVertices[2].Color.PackedValue =
                    StaticVertices[3].Color.PackedValue = StaticVertices[4].Color.PackedValue = StaticVertices[5].Color.PackedValue = 0xff000000;
            }
            else
            {
                StaticVertices[0].Color.PackedValue = StaticVertices[1].Color.PackedValue = StaticVertices[2].Color.PackedValue =
                    StaticVertices[3].Color.PackedValue = StaticVertices[4].Color.PackedValue = StaticVertices[5].Color.PackedValue = 0x88000000;
            }
#endif
            StaticVertices[0].Position.Z = StaticVertices[1].Position.Z = StaticVertices[2].Position.Z =
                StaticVertices[3].Position.Z = StaticVertices[4].Position.Z = StaticVertices[5].Position.Z =
                camera.Z + FlatRedBall.Math.MathFunctions.ForwardVector3.Z * 100;


            #region LeftBorder
            StaticVertices[0].Position.X = xToUse - ScaleX;
            StaticVertices[0].Position.Y = yToUse - ScaleY;
            StaticVertices[0].TextureCoordinate.X = .5273438f;
            StaticVertices[0].TextureCoordinate.Y = .6445313f;

            StaticVertices[1].Position.X = xToUse - ScaleX;
            StaticVertices[1].Position.Y = yToUse + ScaleY;
            StaticVertices[1].TextureCoordinate.X = .5273438f;
            StaticVertices[1].TextureCoordinate.Y = .5703125f;

            StaticVertices[2].Position.X = xToUse - ScaleX + .2f;
            StaticVertices[2].Position.Y = yToUse + ScaleY;
            StaticVertices[2].TextureCoordinate.X = .5351563f;
            StaticVertices[2].TextureCoordinate.Y = .5703125f;

            StaticVertices[3] = StaticVertices[0];
            StaticVertices[4] = StaticVertices[2];

            StaticVertices[5].Position.X = xToUse - ScaleX + .2f;
            StaticVertices[5].Position.Y = yToUse - ScaleY;
            StaticVertices[5].TextureCoordinate.X = .5351563f;
            StaticVertices[5].TextureCoordinate.Y = .6445313f;

            GuiManager.WriteVerts(StaticVertices);

            #endregion

            #region Center
            StaticVertices[0].Position.X = xToUse - ScaleX + .2f;
            StaticVertices[0].Position.Y = yToUse - ScaleY;
            StaticVertices[0].TextureCoordinate.X = .5351563f;
            StaticVertices[0].TextureCoordinate.Y = .6445313f;

            StaticVertices[1].Position.X = xToUse - ScaleX + .2f;
            StaticVertices[1].Position.Y = yToUse + ScaleY;
            StaticVertices[1].TextureCoordinate.X = .5351563f;
            StaticVertices[1].TextureCoordinate.Y = .5703125f;

            StaticVertices[2].Position.X = xToUse + ScaleX - .2f;
            StaticVertices[2].Position.Y = yToUse + ScaleY;
            StaticVertices[2].TextureCoordinate.X = .5390625f;
            StaticVertices[2].TextureCoordinate.Y = .5703125f;

            StaticVertices[3] = StaticVertices[0];
            StaticVertices[4] = StaticVertices[2];

            StaticVertices[5].Position.X = xToUse + ScaleX - .2f;
            StaticVertices[5].Position.Y = yToUse - ScaleY;
            StaticVertices[5].TextureCoordinate.X = .5390625f;
            StaticVertices[5].TextureCoordinate.Y = .6445313f;

            GuiManager.WriteVerts(StaticVertices);

            #endregion

            #region RightBorder
            StaticVertices[0].Position.X = xToUse + ScaleX - .2f;
            StaticVertices[0].Position.Y = yToUse - ScaleY;
            StaticVertices[0].TextureCoordinate.X = .5390625f;
            StaticVertices[0].TextureCoordinate.Y = .6445313f;

            StaticVertices[1].Position.X = xToUse + ScaleX - .2f;
            StaticVertices[1].Position.Y = yToUse + ScaleY;
            StaticVertices[1].TextureCoordinate.X = .5390625f;
            StaticVertices[1].TextureCoordinate.Y = .5703125f;

            StaticVertices[2].Position.X = xToUse + ScaleX;
            StaticVertices[2].Position.Y = yToUse + ScaleY;
            StaticVertices[2].TextureCoordinate.X = .546875f;
            StaticVertices[2].TextureCoordinate.Y = .5703125f;

            StaticVertices[3] = StaticVertices[0];
            StaticVertices[4] = StaticVertices[2];

            StaticVertices[5].Position.X = xToUse + ScaleX;
            StaticVertices[5].Position.Y = yToUse - ScaleY;
            StaticVertices[5].TextureCoordinate.X = .546875f;
            StaticVertices[5].TextureCoordinate.Y = .6445313f;

            GuiManager.WriteVerts(StaticVertices);

            #endregion

            #endregion
        }
#endif


        internal void DrawTextLine(ref string textToDraw, float worldYPosition)
        {
            TextManager.mRedForVertexBuffer = .1f * GraphicalEnumerations.MaxColorComponentValue;
            TextManager.mGreenForVertexBuffer = .1f * GraphicalEnumerations.MaxColorComponentValue;
            TextManager.mBlueForVertexBuffer = .1f * GraphicalEnumerations.MaxColorComponentValue;
            TextManager.mAlphaForVertexBuffer = 1f * GraphicalEnumerations.MaxColorComponentValue;

            TextManager.mAlignmentForVertexBuffer = this.Alignment;// HorizontalAlignment.Left;

            switch (Alignment)
            {
                case HorizontalAlignment.Left:

                    TextManager.mXForVertexBuffer = (mWorldUnitX - mScaleX + mSpacing);
                    break;
                case HorizontalAlignment.Center:
                    TextManager.mXForVertexBuffer = mWorldUnitX;
                    break;
                case HorizontalAlignment.Right:
                    TextManager.mXForVertexBuffer = (mWorldUnitX + mScaleX - mSpacing);
                    break;
            }
            TextManager.mYForVertexBuffer = (worldYPosition);
            TextManager.mZForVertexBuffer = AbsoluteWorldUnitZ;

            TextManager.mScaleForVertexBuffer = GuiManager.TextHeight / 2.0f;
            TextManager.mSpacingForVertexBuffer = GuiManager.TextSpacing;

            // The TextBox will worry about truncating strings
            TextManager.mMaxWidthForVertexBuffer = float.PositiveInfinity;

            TextManager.Draw(ref textToDraw);
        }

#if !SILVERLIGHT
        internal override int GetNumberOfVerticesToDraw()
        {
            int numVertices = 3 * 6; // base window
            if (this.mHighlightStartPosition != -1)
                numVertices += 6;

            if (this.DisplayedText.Length != 0)
            {
                numVertices += DisplayedText.Replace(" ", "").Length * 6;
            }

            if (InputManager.ReceivingInput == this)
            {
                numVertices += 6;
            }

            return numVertices;
        }
#endif

        internal void RegulateCursorPosition()
        {
            mAbsoluteCursorPosition = System.Math.Min(mAbsoluteCursorPosition, this.CompleteText.Length);

            if (mAbsoluteCursorPosition < mFirstLetterShowing)
            {
                //int m = 3;
                // 
            }
        }


        internal void RemoveHighlight(Window callingWindow)
        {
            mHighlightStartPosition = -1;
            mHighlightEndPosition = -1;
        }
		
		

        #endregion

        #region Private Methods
        /// <summary>
        /// Makes sure the current text can be successfully parsed, and if not, changes it to 0.
        /// </summary>
        private void FixNumericalInput()
        {
            if ((this.Format == TextBox.FormatTypes.Integer || this.Format == TextBox.FormatTypes.Decimal))
            {

                if((this.Text == "" || this.Text == "-" || this.Text == "." || this.Text == ","))
                    this.Text = "0";

                // If the text starts with a period and the text box is an integer, make the displayed value a 0.
                if (this.Format == FormatTypes.Integer && this.Text.StartsWith("."))
                    this.Text = "0";
            }

            

        }


        private int GetAbsoluteIndexAtCursorPosition()
        {
            float cursorrelX = 0;
            float width = 0;
            switch (Alignment)
            {
                case HorizontalAlignment.Left:

                    cursorrelX = -mScale / 2.0f + mCursor.XForUI - (mWorldUnitX - mScaleX);
                    break;
                case HorizontalAlignment.Center:

                    width = TextManager.GetWidth(
                                DisplayedText,
                                mSpacing,
                                Font);

                    cursorrelX = -mScale / 2.0f + mCursor.XForUI - (mWorldUnitX - width/2.0f);
                    break;
                case HorizontalAlignment.Right:
                    width = TextManager.GetWidth(
                        DisplayedText,
                        mSpacing,
                        Font);

                    cursorrelX = -mScale / 2.0f + mCursor.XForUI - (mWorldUnitX + mScale - width);
                    break;
            }

            // if this TextBox is not GuiManagerDrawn, then it is probably not at 100 units away from the camera.
            if (!GuiManagerDrawn)
            {
                float temporaryY = 0;
                float temporaryX = 0;
                mCursor.GetCursorPosition(out temporaryX, out temporaryY, SpriteFrame.Z);

                cursorrelX = -mScale / 2.0f + temporaryX - (mWorldUnitX - mScaleX);
            }


            return mFirstLetterShowing +
                TextManager.GetCursorPosition(cursorrelX, CompleteText, mScale, mFirstLetterShowing);

        }

        #endregion

        #region Protected Methods

        public override void SetSkin(GuiSkin guiSkin)
        {
            SetFromWindowSkin(guiSkin.TextBoxSkin);

            SetTexturePropertiesFromSkin(guiSkin);
        }

        private void SetTexturePropertiesFromSkin(GuiSkin guiSkin)
        {
            if (mTextObject != null)
            {
                mTextObject.Font = guiSkin.TextBoxSkin.Font;
                mTextObject.Scale = guiSkin.TextBoxSkin.TextScale;
                mTextObject.Spacing = guiSkin.TextBoxSkin.TextSpacing;

				mTextObject.Red = guiSkin.TextBoxSkin.TextRed;
				mTextObject.Green = guiSkin.TextBoxSkin.TextGreen;
				mTextObject.Blue = guiSkin.TextBoxSkin.TextBlue;
            }

        }

        #endregion

        #region IInputReceiver Methods


        public void LoseFocus()
        {
            if (mCursorSprite != null)
                mCursorSprite.Visible = false;

            base.OnLosingFocus();
        }

		public void ReceiveInput()
		{
            HorizontalAlignment alignment = HorizontalAlignment.Center;

            bool wasSomethingTyped = false;

            #region Copy: CTRL + C

            if (InputManager.Keyboard.ControlCPushed())
            {
#if !XBOX360 && !SILVERLIGHT && !WINDOWS_PHONE && !MONODROID

                bool isSTAThreadUsed =
                    System.Threading.Thread.CurrentThread.GetApartmentState() == System.Threading.ApartmentState.STA;

                
#if DEBUG
                if (!isSTAThreadUsed)
                {
                    GuiManager.ShowMessageBox("Need to set [STAThread] on Main to support copy/paste", "Error Copying");
                }

#endif


                if (isSTAThreadUsed &&
                    this.mHighlightStartPosition != -1 && this.mHighlightEndPosition != -1 &&
                    mHighlightStartPosition != mHighlightEndPosition)
                {


                    System.Windows.Forms.Clipboard.SetText(CompleteText.Substring(
                        mHighlightStartPosition, mHighlightEndPosition - mHighlightStartPosition));
                }
#endif
            }

            #endregion

            #region Select All: CTRL + A

            if (InputManager.Keyboard.KeyPushed(Keys.A) &&
                (InputManager.Keyboard.KeyDown(Keys.LeftControl) || InputManager.Keyboard.KeyDown(Keys.RightControl)))
            {
                // Highlight the entire thing
                HighlightCompleteText();
            }

            #endregion

            #region backspace
#if FRB_MDX
            if (InputManager.Keyboard.KeyTyped(Keys.BackSpace))
#else
            if (InputManager.Keyboard.KeyTyped(Keys.Back))
#endif
			{
                wasSomethingTyped = true;

				if(mHighlightStartPosition != -1)
				{
                    // Text is highlighted so delete everything.

					CompleteText = CompleteText.Remove(mHighlightStartPosition, mHighlightEndPosition - mHighlightStartPosition);
                    //mAbsoluteCursorPosition = 0;
                    mAbsoluteCursorPosition = mHighlightStartPosition;
                    
                    mHighlightStartPosition = mHighlightEndPosition= -1;

                    
                    // for now assume the highlight removed everything
                    //mFirstLetterShowing = 0;
                    //mLastLetterShowing = -1;

				}
				else if(mAbsoluteCursorPosition > 0)
				{
                    // Not at the very beginning where backspace is not possible.

                    if (mAbsoluteCursorPosition == mLastLetterShowing + 1 && mFirstLetterShowing != 0)
                    {
                        int absoluteCursorPosition = mAbsoluteCursorPosition;

                        // the cursor is on the right side of the text box and the text can "scroll" to the left

                        CompleteText = CompleteText.Remove(mAbsoluteCursorPosition - 1, 1);

#if SILVERLIGHT
                        mFirstLetterShowing = System.Math.Max(0, 1 + mLastLetterShowing - TextManager.GetNumberOfCharsIn(
                            TextAreaWidth, CompleteText, mSpacing, mLastLetterShowing, Font, HorizontalAlignment.Right));

#else
                        mFirstLetterShowing = System.Math.Max(0, 1 + mLastLetterShowing - TextManager.GetNumberOfCharsIn(
                            TextAreaWidth, CompleteText, GuiManager.TextSpacing, mLastLetterShowing, Font, HorizontalAlignment.Right));
#endif                       
                        //refresh the complete text

                        CompleteText = CompleteText;

                        if (Text == "")
                            mLastLetterShowing = -1;
                        // make sure that the last letter showing is not greater than the last letter in the Text object.
                        mLastLetterShowing =  System.Math.Min(mLastLetterShowing, CompleteText.Length - 1);

                        mAbsoluteCursorPosition--;
                    }
                    else
                    {
                        int oldFirstLetterShowing = mFirstLetterShowing;
                        int oldAbsoluteCursorPosition = mAbsoluteCursorPosition;

                        CompleteText = CompleteText.Remove(mAbsoluteCursorPosition - 1, 1);
                        // make sure that the last letter showing is not greater than the last letter in the Text object.
                        mLastLetterShowing = System.Math.Min(mLastLetterShowing, CompleteText.Length - 1);

                        if (mLastLetterShowing == CompleteText.Length - 1)
                        {
#if SILVERLIGHT
                            // this means that the text box is showing the end of the text, so we need to adjust the firstLetterShowing
                            mFirstLetterShowing = System.Math.Max(0, 1 + mLastLetterShowing - TextManager.GetNumberOfCharsIn(
                                TextAreaWidth, CompleteText, mSpacing, mLastLetterShowing, Font, HorizontalAlignment.Right));

#else
                            // this means that the text box is showing the end of the text, so we need to adjust the firstLetterShowing
                            mFirstLetterShowing = System.Math.Max(0, 1 + mLastLetterShowing - TextManager.GetNumberOfCharsIn(
                                TextAreaWidth, CompleteText, GuiManager.TextSpacing, mLastLetterShowing, Font, HorizontalAlignment.Right));
#endif
                        }

                        CompleteText = CompleteText;

                        if (oldAbsoluteCursorPosition == mAbsoluteCursorPosition)
                        {
                            // When the CompleteText is set, this can raise an event, which could
                            // regulate the cursor.  This automatically moves the cursor back.
                            // Calling this code if the cursor position has changed will make it move
                            // back two.
                            mAbsoluteCursorPosition--;
                        }
                    }
				}
            }
			#endregion

			#region delete
            if (InputManager.Keyboard.KeyTyped(Keys.Delete))
			{
                wasSomethingTyped = true;

                if (mHighlightStartPosition != -1)
                {
                    CompleteText = CompleteText.Remove(mHighlightStartPosition, mHighlightEndPosition - mHighlightStartPosition);
                    mAbsoluteCursorPosition = mHighlightStartPosition;

                    mHighlightStartPosition = mHighlightEndPosition = -1;
                }
                else if (mAbsoluteCursorPosition < CompleteText.Length)
                {
                    CompleteText = CompleteText.Remove(mAbsoluteCursorPosition, 1);

                    if (mAbsoluteCursorPosition + mFirstLetterShowing == mLastLetterShowing)
                    {
                        int oldFirstLetterShowing = mFirstLetterShowing;

#if SILVERLIGHT
                        mFirstLetterShowing = System.Math.Max(0, 1 + mLastLetterShowing - TextManager.GetNumberOfCharsIn(
                            TextAreaWidth, CompleteText, mSpacing, mLastLetterShowing, Font, HorizontalAlignment.Right));

#else
                        mFirstLetterShowing = System.Math.Max(0, 1 + mLastLetterShowing - TextManager.GetNumberOfCharsIn(
                            TextAreaWidth, CompleteText, GuiManager.TextSpacing, mLastLetterShowing, Font, HorizontalAlignment.Right));
#endif
                    }

                    CompleteText = CompleteText;
                    
                }

			}
			#endregion
			
            #region right arrow
#if FRB_MDX
            if (InputManager.Keyboard.KeyTyped(Keys.RightArrow))
#else
            if (InputManager.Keyboard.KeyTyped(Keys.Right))
#endif
			{
                mHighlightStartPosition = -1;
                mHighlightEndPosition = -1;

                if (mAbsoluteCursorPosition < this.CompleteText.Length)
                {
                    mAbsoluteCursorPosition++;
                }

                if (mAbsoluteCursorPosition > mLastLetterShowing+1)
                {
                    alignment = HorizontalAlignment.Right;
                    mLastLetterShowing++;
                }
			}
			#endregion
            
            #region End

            if (InputManager.Keyboard.KeyTyped(Keys.End))
            {
                mAbsoluteCursorPosition = this.CompleteText.Length;

                mHighlightStartPosition = -1;
                mHighlightEndPosition = -1;

                if (mAbsoluteCursorPosition > mLastLetterShowing + 1)
                {
                    alignment = HorizontalAlignment.Right;
                    mLastLetterShowing = mAbsoluteCursorPosition - 1;
                }
            }

            #endregion
            
            #region left arrow

#if FRB_MDX
            if (InputManager.Keyboard.KeyTyped(Keys.LeftArrow))
#else
            if (InputManager.Keyboard.KeyTyped(Keys.Left))
#endif
			{
                mHighlightStartPosition = -1;
                mHighlightEndPosition = -1;

                if (mAbsoluteCursorPosition > 0)
                {
                    mAbsoluteCursorPosition--;
                }
                if(mAbsoluteCursorPosition < mFirstLetterShowing)
                {
                    mFirstLetterShowing--;
                    alignment = HorizontalAlignment.Left;

                }
			}
			#endregion

            #region Home

            if (InputManager.Keyboard.KeyTyped(Keys.Home))
            {
                mHighlightStartPosition = -1;
                mHighlightEndPosition = -1;

                mAbsoluteCursorPosition = 0;

                if(mAbsoluteCursorPosition < mFirstLetterShowing)
                {
                    mFirstLetterShowing = 0;
                    alignment = HorizontalAlignment.Left;

                }
            }

            #endregion

            #region return/enter
#if FRB_MDX
            if (InputManager.Keyboard.KeyReleased(Keys.Return) || InputManager.Keyboard.KeyReleased(Key.NumPadEnter))
#else
            if(InputManager.Keyboard.KeyReleased(Keys.Enter))
#endif
			{
				GuiManager.mLastWindowWithFocus = null;
                FixNumericalInput();
                
                InputManager.ReceivingInput = null;

				if(EnterPressed != null)
					EnterPressed(this);
			}
			#endregion

            #region tab
            if (InputManager.Keyboard.KeyPushed(Keys.Tab))
            {
                InputManager.ReceivingInput = this.NextInTabSequence;

				while(InputManager.ReceivingInput != null && InputManager.ReceivingInput is Window &&
					!((Window)InputManager.ReceivingInput).Enabled)
				{
					InputManager.ReceivingInput = InputManager.ReceivingInput.NextInTabSequence;
				}

                FixNumericalInput();

                OnLosingFocus();

                if (InputManager.ReceivingInput != null && InputManager.ReceivingInput is TextBox)
                {
                    ((TextBox)InputManager.ReceivingInput).mHighlightStartPosition = 0;
                    ((TextBox)InputManager.ReceivingInput).mHighlightEndPosition =
                        ((TextBox)InputManager.ReceivingInput).CompleteText.Length;
                }
            }
            #endregion
            
            #region escape release
            if (InputManager.Keyboard.KeyReleased(Keys.Escape) && this.EscapeRelease != null)
			{
				EscapeRelease(this);
			}
				#endregion

            else
			{

				string typed = InputManager.Keyboard.GetStringTyped();
				foreach(char c in typed)
                {
                    wasSomethingTyped = true;


                    #region we have text highlighted, so we need to get rid of it before typing something
                    if (mHighlightStartPosition != -1)
					{
                        CompleteText = CompleteText.Remove(mHighlightStartPosition, mHighlightEndPosition - mHighlightStartPosition);
                        mAbsoluteCursorPosition = mHighlightStartPosition;
						mHighlightStartPosition = mHighlightEndPosition = -1;

                        if (mAbsoluteCursorPosition < 0)
                        {
                            mAbsoluteCursorPosition = 0;
                        }
					}
					#endregion

					#region if shift is not down
					
					if(Format == FormatTypes.NoFormat)
					{
                        // The CompletedText property uses mLastLetterShowing to limit the displayed text.
                        // I'm not sure I understand completely what's going on with mLastLetterShowing, but
                        // incrementing it fixes the problem.  If more problems arise, see if mLastLetterShowing
                        // can be completely eliminated.
                        mLastLetterShowing++;
                        CompleteText = CompleteText.Insert(mAbsoluteCursorPosition, c.ToString());
                        mAbsoluteCursorPosition++;
					}
					else if(Format == FormatTypes.Integer)
					{
						if (c > 47 && c < 58)
						{
                            CompleteText = CompleteText.Insert(mAbsoluteCursorPosition, c.ToString());
                            mAbsoluteCursorPosition++;
						}
                        else if (c == '-' && mAbsoluteCursorPosition == 0)
						{
                            CompleteText = CompleteText.Insert(mAbsoluteCursorPosition, c.ToString());

                            mAbsoluteCursorPosition++;
						}
					}
					else if(Format == FormatTypes.Decimal)
					{
						NumberFormatInfo LocalFormat = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();

						char decimalSeparator = LocalFormat.NumberDecimalSeparator[0];


						if(  (c > 47 && c < 58))
						{
                            CompleteText = CompleteText.Insert(mAbsoluteCursorPosition, c.ToString());
                            mAbsoluteCursorPosition++;
						}
						else if(c == decimalSeparator)
						{
							int position = -1;
                            position = CompleteText.IndexOf(decimalSeparator);
                            #region if there is no decimal separator
                            if (position == -1)
							{
                                CompleteText = CompleteText.Insert(mAbsoluteCursorPosition, c.ToString());
                                mAbsoluteCursorPosition++;
                            }
                            #endregion

                            #region else, there is, so remove it then re-add it
                            else
							{
                                mCompleteText = CompleteText.Remove(position, 1);
                                CompleteText = CompleteText.Insert(mAbsoluteCursorPosition - 1, c.ToString());
                            }
                            #endregion
                        }
                        else if (c == (int)'-' && 
                            (mAbsoluteCursorPosition == 0 || 
                              (mAbsoluteCursorPosition == 1 && mDisplayedText[0] == '0') ))
                        {
                            CompleteText = CompleteText.Insert(0, c.ToString());

                            mAbsoluteCursorPosition++;
                        }
                        else
                        {
#if !SILVERLIGHT
                            mOptionsListBox = GuiManager.AddPerishableListBox();

                            mOptionsListBox.ScaleX = System.Math.Max(this.ScaleX, 4);
                            mOptionsListBox.ScaleY = 5;
                            mOptionsListBox.X = this.ScreenRelativeX;
                            mOptionsListBox.Y = this.ScreenRelativeY + this.ScaleY + mOptionsListBox.ScaleY;

                            mOptionsListBox.AddItem("PI", System.Math.PI);
                            mOptionsListBox.AddItem("-PI", -System.Math.PI);
                            mOptionsListBox.AddItem("PI/2", System.Math.PI/2.0);
                            mOptionsListBox.AddItem("-PI/2", -System.Math.PI/2.0);

                            mOptionsListBox.Highlight += OptionsHighlight;
                            mOptionsListBox.Click += GuiManager.RemoveWindow;
#endif
                        }
					}
					#endregion

//					cursorPosition -= TruncateText();
                    if (mAbsoluteCursorPosition > mLastLetterShowing + 1)
                    {
                        alignment = HorizontalAlignment.Right;
                        mLastLetterShowing++;
                    }

                }
			}
#if !SILVERLIGHT
            #region Show the options list box if appropraite
            if ( mAllOptions.Count != 0)
            {
                if (wasSomethingTyped || !GuiManager.PerishableWindows.Contains(mOptionsListBox))
                {

                    if (!GuiManager.PerishableWindows.Contains(mOptionsListBox))
                    {
                        mOptionsListBox = GuiManager.AddPerishableListBox();
                        mOptionsListBox.CurrentToolTipOption = ListBoxBase.ToolTipOption.CursorOver;

                        mOptionsListBox.ScaleX = System.Math.Max(this.ScaleX, 4);
                        mOptionsListBox.ScaleY = 10;
                        mOptionsListBox.X = this.ScreenRelativeX;
                        mOptionsListBox.Y = this.ScreenRelativeY + this.ScaleY + mOptionsListBox.ScaleY;

                        mOptionsListBox.HighlightOnRollOver = true;

                        mOptionsListBox.Click += OptionsHighlight;
                        mOptionsListBox.Click += GuiManager.RemoveWindow;
                    }

                    mOptionsListBox.Clear();

                    bool isNullOrEmpty = string.IsNullOrEmpty(Text);

                    string textAsLower = Text.ToLower();

                    foreach (string s in mAllOptions)
                    {
                        bool matches = isNullOrEmpty || s.ToLower().Contains(textAsLower);

                        if (matches)
                        {
                            mOptionsListBox.AddItem(s);
                        }
                    }
                }
                
            }
            #endregion
#endif
            if (alignment == HorizontalAlignment.Right)
            {
#if SILVERLIGHT
                mFirstLetterShowing = mLastLetterShowing + 1 -
                    TextManager.GetNumberOfCharsIn(TextAreaWidth, CompleteText, mSpacing,
                    mLastLetterShowing, this.Font, alignment);
#else
                mFirstLetterShowing = mLastLetterShowing + 1 -
                    TextManager.GetNumberOfCharsIn(TextAreaWidth, CompleteText, GuiManager.TextSpacing,
                    mLastLetterShowing, null, alignment);
#endif
                DisplayedText = CompleteText.Substring(mFirstLetterShowing, mLastLetterShowing - mFirstLetterShowing + 1);
            }
            else if (alignment == HorizontalAlignment.Left)
            {
#if SILVERLIGHT
                mLastLetterShowing = mFirstLetterShowing - 1 +
                    TextManager.GetNumberOfCharsIn(TextAreaWidth, CompleteText, GuiManager.TextSpacing,
                    mFirstLetterShowing, null, alignment);
#else
                mLastLetterShowing = mFirstLetterShowing - 1 +
                    TextManager.GetNumberOfCharsIn(TextAreaWidth, CompleteText, GuiManager.TextSpacing,
                    mFirstLetterShowing, null, alignment);
#endif
                DisplayedText = CompleteText.Substring(mFirstLetterShowing, mLastLetterShowing - mFirstLetterShowing + 1);

            }

        }

        public void HighlightCompleteText()
        {
            mHighlightStartPosition = 0;
            mHighlightEndPosition = CompleteText.Length;
        }

        #region XML Docs
        /// <summary>
        /// Whether the user can type in this Text Box.  Setting this to false makes the TextBox "read only".
        /// </summary>
        #endregion
        public bool TakingInput
		{
			get
			{	
				return mTakingInput;
			}
			set
			{
				mTakingInput = value;
			}
		}
		
		#endregion



		#endregion
	}
}