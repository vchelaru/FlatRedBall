using System;

#if FRB_MDX
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

using Keys = Microsoft.DirectX.DirectInput.Key;
#else
using Microsoft.Xna.Framework.Input;
#endif
using FlatRedBall.Graphics;
using FlatRedBall.Input;
using System.Collections.Generic;

namespace FlatRedBall.Gui
{
	/// <summary>
	/// Summary description for OkCancelWindow.
	/// </summary>
	public class OkCancelWindow : Window, FlatRedBall.Gui.IInputReceiver
	{

		#region Fields
		Button mOkButton;
		Button cancelButton;
		TextField textField = new TextField();

		public Window callingWindow;

        IInputReceiver mNextInTabSequence;
        List<Keys> mIgnoredKeys = new List<Keys>();
		#endregion


		#region Properties
		public string Message
		{
			set{	textField.DisplayText = value;			}
		}
		public string OkText
		{
			set{	mOkButton.Text = value;	}
			get{	return mOkButton.Text;	}
		}										 

		public string CancelText
		{
			set{	cancelButton.Text = value;	}
            get { return cancelButton.Text; }
		}
		public new float ScaleX
		{
			get{	return (float)mScaleX;	}
			set
			{	
				mScaleX = value; 
				if(mCloseButton != null)
					mCloseButton.SetPositionTL(2*mScaleX - 1.1f, 1.1f);
				Update();

			}
		}
		public new float ScaleY
		{
			get{	return (float)mScaleY;	}
			set
			{	
				mScaleY = value; 
				if(mCloseButton != null)
					mCloseButton.SetPositionTL(2*mScaleX - 1.1f, 1.1f);
				Update();
			}
		}

        public bool TakingInput
        {
            get { return true; }
        }

        public IInputReceiver NextInTabSequence
        {
            get { return mNextInTabSequence; }
            set { mNextInTabSequence = value; }
        }

        public List<Keys> IgnoredKeys
        {
            get { return mIgnoredKeys; }
        }

		#endregion


		#region Events

        #region XML Docs
        /// <summary>
        /// Event raised when the OkCancelWindow gets keyboard input.
        /// </summary>
        #endregion
        public event GuiMessage GainFocus;

        public event FocusUpdateDelegate FocusUpdate;

		public event GuiMessage OkClick = null;
		public event GuiMessage CancelClick = null;
		#endregion


		#region Delegate and delegate calling methods

		public static void OkButtonClicked(Window callingWindow)
		{
			Button okButtonClicked = callingWindow as Button;
			OkCancelWindow okCancelWindow = callingWindow.Parent as OkCancelWindow;

			okCancelWindow.Visible = false;

			if(okCancelWindow.OkClick != null)
			{
				if(okCancelWindow.callingWindow != null)
					okCancelWindow.OkClick(okCancelWindow.callingWindow);
				else
					okCancelWindow.OkClick(okCancelWindow);
			}

            InputManager.ReceivingInput = null;
		}

		
		public static void CancelButtonClicked(Window callingWindow)
		{
			Button cancelButtonClicked = callingWindow as Button;
			OkCancelWindow okCancelWindow = callingWindow.Parent as OkCancelWindow;



			okCancelWindow.Visible = false;

			if(okCancelWindow.CancelClick != null)
			{
				if(okCancelWindow.callingWindow != null)
                    okCancelWindow.CancelClick(okCancelWindow.callingWindow);
				else
					okCancelWindow.CancelClick(okCancelWindow);
			}

            InputManager.ReceivingInput = null;


		}

        public void OnFocusUpdate()
        {
            if (FocusUpdate != null)
                FocusUpdate(this);
        }

		#endregion


		#region Methods


        #region Constructor

        public OkCancelWindow(Cursor cursor) : base(cursor)
		{


            mOkButton = new Button(mCursor);
            AddWindow(mOkButton);
			mOkButton.ScaleY = 1.2f;
            mOkButton.Text = "Ok";
			mOkButton.Click += new GuiMessage(OkButtonClicked);

            cancelButton = new Button(mCursor);
            AddWindow(cancelButton);
			cancelButton.ScaleY = 1.2f;
            cancelButton.Text = "Cancel";
			cancelButton.Click += new GuiMessage(CancelButtonClicked);
        }

        #endregion


        #region Public Methods


        public void Activate(string textToDisplay, string Name)
		{
//			TextField textField = new TextField();

            Visible = true;
			mName = Name;
			ScaleX = 13;
			ScaleY = 13;

//			textDisplay.SetDimensions(ScaleY - 4, -ScaleY + 4, -ScaleX + 1, ScaleX - 1, 0);
			textField.DisplayText = textToDisplay;
		}


        public override void ClearEvents()
        {
            base.ClearEvents();
            OkClick = null;
		    CancelClick = null;
        }

        public void LoseFocus()
        {

        }


        public void OnGainFocus()
        {
            if (GainFocus != null)
                GainFocus(this);
        }

        public void ReceiveInput()
        {
#if FRB_MDX
            if (InputManager.Keyboard.KeyReleased(Keys.Return))
#else
            if (InputManager.Keyboard.KeyReleased(Keys.Enter))

#endif
            {
                mOkButton.OnClick();
            }
            if (InputManager.Keyboard.KeyReleased(Keys.Escape))
            {
                cancelButton.OnClick();
            }

        }


        public void Update()
        {
            float top = this.ScaleY;
            float right = this.ScaleX;


            double tempTop, tempBottom, tempLeft, tempRight;

            for (int i = 0; i < base.mChildren.Count; i++)
            {
                if (mChildren[i] != mOkButton && mChildren[i] != cancelButton)
                {
                    tempTop = mChildren[i].WorldUnitRelativeY + mChildren[i].ScaleY + 2.4f;
                    tempBottom = mChildren[i].WorldUnitRelativeY - mChildren[i].ScaleY - 2.5f;
                    tempLeft = mChildren[i].WorldUnitRelativeX - mChildren[i].ScaleX - .3f;
                    tempRight = mChildren[i].WorldUnitRelativeX + mChildren[i].ScaleX + .3f;

                    if (tempTop > top)
                        top = (float)tempTop;
                    if (tempBottom < -top)
                        top = -(float)tempBottom;
                    if (tempRight > right)
                        right = (float)tempRight;
                    if (tempLeft < -right)
                        right = -(float)tempLeft;
                }
            }

            if (top < 3)
                top = 3;
            if (right < 6)
                right = 6;

            this.mScaleX = right;
            this.mScaleY = top;


            textField.SetDimensions(
                (float)ScaleY - 2,
                (float)-ScaleY + 4,
                (float)(mWorldUnitX - ScaleX + 1),
                (float)(mWorldUnitX + ScaleX - 1), 0);
            textField.mZ = 100;
            textField.RelativeToCamera = true;

            mOkButton.SetPositionTL(right / 2.0f, 2 * top - 1.5f);
            mOkButton.ScaleX = right / 2.0f - .3f;

            cancelButton.SetPositionTL(3 * right / 2.0f, 2 * top - 1.5f);
            cancelButton.ScaleX = right / 2.0f - .3f;


        }

        #endregion


        #region Internal Methods

        internal override void DrawSelfAndChildren(Camera camera)
		{
#if !SILVERLIGHT
            base.DrawSelfAndChildren(camera);

			textField.SetDimensions(
				(float)(mWorldUnitY + ScaleY - .5f),
                (float)(mWorldUnitY - ScaleY + 4),
                (float)(mWorldUnitX - ScaleX + 1),
                (float)(mWorldUnitX + ScaleX - 1), 100);
            textField.TextHeight = GuiManager.TextHeight;


            textField.RelativeToCamera = false;

            TextManager.mRedForVertexBuffer = TextManager.mGreenForVertexBuffer = TextManager.mBlueForVertexBuffer = 20;
            TextManager.mScaleForVertexBuffer = GuiManager.TextHeight / 2.0f;
            TextManager.mSpacingForVertexBuffer = GuiManager.TextSpacing;
            TextManager.mNewLineDistanceForVertexBuffer = 2.0f;
            textField.mZ = camera.Z + 100 * FlatRedBall.Math.MathFunctions.ForwardVector3.Z;
		
			TextManager.Draw(textField);
#endif
		}


        internal override int GetNumberOfVerticesToDraw()
		{
            return base.GetNumberOfVerticesToDraw() + textField.DisplayText.Replace("\n", "").Replace(" ", "").Length * 6;
        }

        #endregion


		
		#endregion
	}
}
