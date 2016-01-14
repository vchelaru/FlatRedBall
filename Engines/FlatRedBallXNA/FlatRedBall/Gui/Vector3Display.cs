using System;
using System.Collections.Generic;
using System.Text;
#if FRB_XNA || WINDOWS_PHONE || SILVERLIGHT || MONODROID
using Microsoft.Xna.Framework;
//using System.Windows.Forms;
using Keys = Microsoft.Xna.Framework.Input.Keys;
#elif FRB_MDX
using Microsoft.DirectX;
using Keys = Microsoft.DirectX.DirectInput.Key;
#endif

namespace FlatRedBall.Gui
{
    public class Vector3Display : Window, IInputReceiver
    {
        #region Fields

        TextDisplay mXDisplay;
        TextDisplay mYDisplay;
        TextDisplay mZDisplay;
        TextDisplay mWDisplay;

        UpDown mXUpDown;
        UpDown mYUpDown;
        UpDown mZUpDown;
        UpDown mWUpDown;

        int mNumberOfComponents = 3;

        List<Keys> mIgnoredKeys = new List<Keys>();

        #endregion

        #region Properties

        public Vector2 BeforeChangeVector2Value
        {
            get { return new Vector2(mXUpDown.BeforeChangeValue, mYUpDown.BeforeChangeValue); }
        }
        
        public Vector3 BeforeChangeVector3Value
        {
            get { return new Vector3(mXUpDown.BeforeChangeValue, mYUpDown.BeforeChangeValue, mZUpDown.BeforeChangeValue); }
        }

        public List<Keys> IgnoredKeys
        {
            get { return mIgnoredKeys; }
        }

        public bool IsUserEditingWindow
        {
            get
            {
                return mCursor.WindowPushed == mXUpDown.UpDownButton ||
                    mCursor.WindowPushed == mYUpDown.UpDownButton ||
                    mCursor.WindowPushed == mZUpDown.UpDownButton ||
                    mCursor.WindowPushed == mWUpDown.UpDownButton;
            }
        }

        private UpDown LastVisibleUpDown
        {
            get
            {
                if (mWUpDown.Visible)
                {
                    return mWUpDown;
                }
                else if (mZUpDown.Visible)
                {
                    return mZUpDown;
                }
                else if (mYUpDown.Visible)
                {
                    return mYUpDown;
                }
                else if (mXUpDown.Visible)
                {
                    return mXUpDown;
                }
                else
                {
                    return null;
                }
            }
        }
        public int NumberOfComponents
        {
            get { return mNumberOfComponents; }
            set 
            {
 
                mNumberOfComponents = value;

                #region Set the visibility of the components

                mXDisplay.Visible = mNumberOfComponents > 0;
                mXUpDown.Visible = mXDisplay.Visible;

                mYDisplay.Visible = mNumberOfComponents > 1;
                mYUpDown.Visible = mYDisplay.Visible;

                mZDisplay.Visible = mNumberOfComponents > 2;
                mZUpDown.Visible = mZDisplay.Visible;

                mWDisplay.Visible = mNumberOfComponents > 3;
                mWUpDown.Visible = mWDisplay.Visible;
                #endregion

                ScaleY = .5f + mNumberOfComponents;


                float border = .5f;                
                
                mXUpDown.Y = border + 1;
                mYUpDown.Y = border + 3;
                mZUpDown.Y = border + 5;
                mWUpDown.Y = border + 7;

                mXDisplay.Y = mXUpDown.Y;
                mYDisplay.Y = mYUpDown.Y;
                mZDisplay.Y = mZUpDown.Y;
                mWDisplay.Y = mWUpDown.Y;

                if (mWUpDown.Visible)
                {
                    mZUpDown.NextInTabSequence = mWUpDown;
                }

                if (mZUpDown.Visible)
                {
                    mYUpDown.NextInTabSequence = mZUpDown;
                }

                if (mYUpDown.Visible)
                {
                    mXUpDown.NextInTabSequence = mYUpDown;
                }

            }
        }

        public float Sensitivity
        {
            get { return mXUpDown.Sensitivity; }
            set
            {
                if (mXUpDown != null)
                    mXUpDown.Sensitivity = value;
                if (mYUpDown != null)
                    mYUpDown.Sensitivity = value;
                if (mZUpDown != null)
                    mZUpDown.Sensitivity = value;
                if (mWUpDown != null)
                    mWUpDown.Sensitivity = value;
            }
        }

        public Vector2 Vector2Value
        {
            get { return new Vector2(mXUpDown.CurrentValue, mYUpDown.CurrentValue); }
            set
            {
                mXUpDown.CurrentValue = value.X;
                mYUpDown.CurrentValue = value.Y;

                if (!mCursor.IsOn(mXUpDown as IWindow))
                {
                    mXUpDown.ForceUpdateBeforeChangedValue();
                }
                if (!mCursor.IsOn(mYUpDown as IWindow))
                {
                    mYUpDown.ForceUpdateBeforeChangedValue();
                }
                if (!mCursor.IsOn(mZUpDown as IWindow))
                {
                    mZUpDown.ForceUpdateBeforeChangedValue();
                }
                if (!mCursor.IsOn(mWUpDown as IWindow))
                {
                    mWUpDown.ForceUpdateBeforeChangedValue();
                }            
            
            }
        }


        public Vector3 Vector3Value
        {
            get { return new Vector3(mXUpDown.CurrentValue, mYUpDown.CurrentValue, mZUpDown.CurrentValue); }
            set
            {
                mXUpDown.CurrentValue = value.X;
                mYUpDown.CurrentValue = value.Y;
                mZUpDown.CurrentValue = value.Z;

                if (!mCursor.IsOn(mXUpDown as IWindow))
                {
                    mXUpDown.ForceUpdateBeforeChangedValue();
                }
                if (!mCursor.IsOn(mYUpDown as IWindow))
                {
                    mYUpDown.ForceUpdateBeforeChangedValue();
                }
                if (!mCursor.IsOn(mZUpDown as IWindow))
                {
                    mZUpDown.ForceUpdateBeforeChangedValue();
                }
                if (!mCursor.IsOn(mWUpDown as IWindow))
                {
                    mWUpDown.ForceUpdateBeforeChangedValue();
                }            
            
            }
        }



        #endregion

        #region Events

        public GuiMessage ValueChanged;
        public GuiMessage AfterValueChanged;

        #endregion

        #region Event and Delegate Methods

        // July 10, 2011
        // These two methods
        // were previously not
        // implemented - the FocusUpdate
        // and GainFocus events were not being
        // used.  I implemented them.  Hopefully
        // this doesn't cause issues if the engine
        // raises these events...but I think it should
        // be okay.
        void OnFocusUpdateInternal(IInputReceiver inputReceiver)
        {
            if (this.FocusUpdate != null)
            {
                this.FocusUpdate(this);
            }
        }
        void OnGainFocusInternal(Window callingWindow)
        {
            if (this.GainFocus != null)
            {
                this.GainFocus(this);
            }
        }

        private void OnValueChanged(Window callingWindow)
        {
            if (ValueChanged != null)
            {
                ValueChanged(this);
            }
        }

        private void OnAfterValueChanged(Window callingWindow)
        {
            if(AfterValueChanged != null)
            {
                AfterValueChanged(this);
            }

            // The UpDown's BeforeChangeValue is only
            // set when the user pushes on the UpDown.
            // That means that if the user changes the X,
            // then Y, then Z, the BeforeChange value on all
            // three will remain the exact same.  This forces
            // the BeforeChangeValue to be accurate so it can be
            // used in the BeforeChangeVector3Value property.
            //mXUpDown.ForceUpdateBeforeChangedValue();
            //mYUpDown.ForceUpdateBeforeChangedValue();
            //mZUpDown.ForceUpdateBeforeChangedValue();
            //mWUpDown.ForceUpdateBeforeChangedValue();
        }

        private void ComponentLosingFocus(Window callingWindow)
        {
            base.OnLosingFocus();
        }

        #endregion

        #region Methods

        public Vector3Display(Cursor cursor)
            : base(cursor)
        {
            float border = .5f;
            this.ScaleX = 6f;
            this.ScaleY = 3.5f;

            #region Create the UpDowns and set their static properties

            mXUpDown = new UpDown(mCursor);
            AddWindow(mXUpDown);

            mYUpDown = new UpDown(mCursor);
            AddWindow(mYUpDown);

            mZUpDown = new UpDown(mCursor);
            AddWindow(mZUpDown);

            mWUpDown = new UpDown(mCursor);
            AddWindow(mWUpDown);
            
            mXUpDown.Y = border + 1;
            mYUpDown.Y = border + 3;
            mZUpDown.Y = border + 5;
            mWUpDown.Y = border + 7;


            mXUpDown.ValueChanged += OnValueChanged;
            mYUpDown.ValueChanged += OnValueChanged;
            mZUpDown.ValueChanged += OnValueChanged;
            mWUpDown.ValueChanged += OnValueChanged;

            mXUpDown.LosingFocus += ComponentLosingFocus;
            mYUpDown.LosingFocus += ComponentLosingFocus;
            mZUpDown.LosingFocus += ComponentLosingFocus;
            mWUpDown.LosingFocus += ComponentLosingFocus;

            mXUpDown.AfterValueChanged += OnAfterValueChanged;
            mYUpDown.AfterValueChanged += OnAfterValueChanged;
            mZUpDown.AfterValueChanged += OnAfterValueChanged;
            mWUpDown.AfterValueChanged += OnAfterValueChanged;

            mXUpDown.FocusUpdate += OnFocusUpdateInternal;
            mYUpDown.FocusUpdate += OnFocusUpdateInternal;
            mZUpDown.FocusUpdate += OnFocusUpdateInternal;
            mWUpDown.FocusUpdate += OnFocusUpdateInternal;

            mXUpDown.GainFocus += OnGainFocusInternal;
            mYUpDown.GainFocus += OnGainFocusInternal;
            mZUpDown.GainFocus += OnGainFocusInternal;
            mWUpDown.GainFocus += OnGainFocusInternal;

            #endregion

            #region Create the TextDisplays and set their static properties
            mXDisplay = new TextDisplay(mCursor);
            AddWindow(mXDisplay);

            mYDisplay = new TextDisplay(mCursor);
            AddWindow(mYDisplay);

            mZDisplay = new TextDisplay(mCursor);
            AddWindow(mZDisplay);

            mWDisplay = new TextDisplay(mCursor);
            AddWindow(mWDisplay);

            mXDisplay.Text = "X:";
            mYDisplay.Text = "Y:";
            mZDisplay.Text = "Z:";
            mWDisplay.Text = "W:";

            mXDisplay.X = 0f;
            mYDisplay.X = 0f;
            mZDisplay.X = 0f;
            mWDisplay.X = 0f;

            mXDisplay.Y = mXUpDown.Y;
            mYDisplay.Y = mYUpDown.Y;
            mZDisplay.Y = mZUpDown.Y;
            mWDisplay.Y = mWUpDown.Y;

            #endregion

            float spaceForDisplay = 1;

            #region Set the UpDown ScaleX values

            mXUpDown.ScaleX = this.ScaleX - border - spaceForDisplay;
            mYUpDown.ScaleX = this.ScaleX - border - spaceForDisplay;
            mZUpDown.ScaleX = this.ScaleX - border - spaceForDisplay;
            mWUpDown.ScaleX = this.ScaleX - border - spaceForDisplay;

            #endregion

            mXUpDown.X = border + spaceForDisplay * 2 + mXUpDown.ScaleX;
            mYUpDown.X = border + spaceForDisplay * 2 + mXUpDown.ScaleX;
            mZUpDown.X = border + spaceForDisplay * 2 + mXUpDown.ScaleX;
            mWUpDown.X = border + spaceForDisplay * 2 + mXUpDown.ScaleX;

            NumberOfComponents = 3;
        }



        #endregion

        #region IInputReceiver Members

        public bool TakingInput
        {
            get { return mXUpDown.TakingInput; }
        }

        public IInputReceiver NextInTabSequence
        {
            get
            {
                UpDown lastUpDown = LastVisibleUpDown;

                if (lastUpDown != null)
                {
                    return lastUpDown.NextInTabSequence;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                UpDown lastUpDown = LastVisibleUpDown;

                if (lastUpDown != null)
                {
                    lastUpDown.NextInTabSequence = value;
                }
            }
        }

        public event GuiMessage GainFocus;

        public event FocusUpdateDelegate FocusUpdate;

        public void OnFocusUpdate()
        {
            throw new InvalidOperationException("Vector3 should never have its OnFocusUpdate called");
        }

        public void OnGainFocus()
        {
            FlatRedBall.Input.InputManager.ReceivingInput = mXUpDown;
        }

        public void LoseFocus()
        {
            // do nothing
        }

        public void ReceiveInput()
        {
            throw new InvalidOperationException("Vector3 should never have its ReceiveInput called");
        }

        #endregion
    }
}
