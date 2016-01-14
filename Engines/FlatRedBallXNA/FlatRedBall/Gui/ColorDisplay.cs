using System;
using System.Collections.Generic;
using System.Text;
#if FRB_XNA || WINDOWS_PHONE || SILVERLIGHT || MONODROID
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
#elif FRB_MDX
using System.Drawing;
using Microsoft.DirectX;
#endif

namespace FlatRedBall.Gui
{
    public class ColorDisplay : Window
    {
        #region Fields

        TextDisplay mADisplay;
        TextDisplay mRDisplay;
        TextDisplay mGDisplay;
        TextDisplay mBDisplay;

        TimeLine mASlider;
        TimeLine mRSlider;
        TimeLine mGSlider;
        TimeLine mBSlider;

        TextBox mATextBox;
        TextBox mRTextBox;
        TextBox mGTextBox;
        TextBox mBTextBox;

        #endregion

        #region Properties

        public Color BeforeChangeColorValue
        {
            get
            {
#if FRB_XNA || SILVERLIGHT
                return new Color(
                    new Vector4(
                        (float)mRSlider.BeforeChangeValue,
                        (float)mGSlider.BeforeChangeValue,
                        (float)mBSlider.BeforeChangeValue,
                        (float)mASlider.BeforeChangeValue)
                );
#elif FRB_MDX
                return Color.FromArgb(
                        (int)mASlider.BeforeChangeValue,
                        (int)mRSlider.BeforeChangeValue,
                        (int)mGSlider.BeforeChangeValue,
                        (int)mBSlider.BeforeChangeValue
                        );
#endif
            }
        }

        public Color ColorValue
        {
            get
            {
#if FRB_XNA || SILVERLIGHT
                return new Color(
                    new Vector4(
                        (float)mRSlider.CurrentValue,
                        (float)mGSlider.CurrentValue,
                        (float)mBSlider.CurrentValue,
                        (float)mASlider.CurrentValue)
                );
#elif FRB_MDX
                return Color.FromArgb(
                        (int)mASlider.CurrentValue,
                        (int)mRSlider.CurrentValue,
                        (int)mGSlider.CurrentValue,
                        (int)mBSlider.CurrentValue);

#endif
            }

            set
            {
#if FRB_XNA
                mASlider.CurrentValue = value.A / 255.0f;
                mRSlider.CurrentValue = value.R / 255.0f;
                mGSlider.CurrentValue = value.G / 255.0f;
                mBSlider.CurrentValue = value.B / 255.0f;
#elif FRB_MDX
                mASlider.CurrentValue = value.A;
                mRSlider.CurrentValue = value.R;
                mGSlider.CurrentValue = value.G;
                mBSlider.CurrentValue = value.B;

#endif
                SetTextValuesToSliders();
            }
        }



        #endregion

        #region Events

        public event GuiMessage ValueChanged;

        #endregion

        #region Event and Delegate Methods

        void OnTextBoxValueChange(Window callingWindow)
        {
#if FRB_MDX
            ColorValue =  Color.FromArgb(
                (int)mASlider.BeforeChangeValue,
                (int)mRSlider.BeforeChangeValue,
                (int)mGSlider.BeforeChangeValue,
                (int)mBSlider.BeforeChangeValue
                );
#else
            ColorValue = new Color(
                new Vector4(
                        float.Parse(mRTextBox.Text),
                        float.Parse(mGTextBox.Text),
                        float.Parse(mBTextBox.Text),
                        float.Parse(mATextBox.Text))
                );
#endif
            if (ValueChanged != null)
            {
                ValueChanged(this);
            }
        }


        private void OnSliderValueChanged(Window callingWindow)
        {
            if (ValueChanged != null)
            {
                ValueChanged(this);
            }


            SetTextValuesToSliders();
        }


        #endregion

        #region Methods

        public ColorDisplay(Cursor cursor)
            : base(cursor)
        {
            float border = .5f;

            this.ScaleX = 7f;
            this.ScaleY = 6f;

            float spaceForDisplay = 1;


            #region Create the sliders

            mASlider = new TimeLine(cursor);
            mRSlider = new TimeLine(cursor);
            mGSlider = new TimeLine(cursor);
            mBSlider = new TimeLine(cursor);

            AddWindow(mASlider);
            AddWindow(mRSlider);
            AddWindow(mGSlider);
            AddWindow(mBSlider);

            mASlider.Y = border + 3.2f;
            mRSlider.Y = border + 5.5f;
            mGSlider.Y = border + 7.8f;
            mBSlider.Y = border + 10.1f;

            mASlider.GuiChange += OnSliderValueChanged;
            mRSlider.GuiChange += OnSliderValueChanged;
            mGSlider.GuiChange += OnSliderValueChanged;
            mBSlider.GuiChange += OnSliderValueChanged;

            // Since the ranges are identical don't show them for
            // all sliders, just the top.
            mASlider.ShowValues = true;
            mRSlider.ShowValues = false;
            mGSlider.ShowValues = false;
            mBSlider.ShowValues = false;

            mASlider.VerticalBarIncrement = .5f * FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;
            mRSlider.VerticalBarIncrement = .5f * FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;
            mGSlider.VerticalBarIncrement = .5f * FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;
            mBSlider.VerticalBarIncrement = .5f * FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;

            mASlider.SmallVerticalBarIncrement = mASlider.VerticalBarIncrement / 2.0f;
            mRSlider.SmallVerticalBarIncrement = mRSlider.VerticalBarIncrement / 2.0f;
            mGSlider.SmallVerticalBarIncrement = mGSlider.VerticalBarIncrement / 2.0f;
            mBSlider.SmallVerticalBarIncrement = mBSlider.VerticalBarIncrement / 2.0f;


            mASlider.ValueWidth = FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;
            mRSlider.ValueWidth = FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;
            mGSlider.ValueWidth = FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;
            mBSlider.ValueWidth = FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;

            mASlider.ScaleX = this.ScaleX - border - spaceForDisplay;
            mRSlider.ScaleX = this.ScaleX - border - spaceForDisplay;
            mGSlider.ScaleX = this.ScaleX - border - spaceForDisplay;
            mBSlider.ScaleX = this.ScaleX - border - spaceForDisplay;

            mASlider.X = border + spaceForDisplay * 2 + mASlider.ScaleX;
            mRSlider.X = border + spaceForDisplay * 2 + mRSlider.ScaleX;
            mGSlider.X = border + spaceForDisplay * 2 + mGSlider.ScaleX;
            mBSlider.X = border + spaceForDisplay * 2 + mBSlider.ScaleX;




            #endregion

            #region Create the TextDisplays
            mADisplay = new TextDisplay(mCursor);
            AddWindow(mADisplay);

            mRDisplay = new TextDisplay(mCursor);
            AddWindow(mRDisplay);

            mGDisplay = new TextDisplay(mCursor);
            AddWindow(mGDisplay);

            mBDisplay = new TextDisplay(mCursor);
            AddWindow(mBDisplay);

            mADisplay.X = .2f;
            mRDisplay.X = .2f;
            mGDisplay.X = .2f;
            mBDisplay.X = .2f;

            mADisplay.Text = "A";
            mRDisplay.Text = "R";
            mGDisplay.Text = "G";
            mBDisplay.Text = "B";

            mADisplay.Y = mASlider.Y;
            mRDisplay.Y = mRSlider.Y;
            mGDisplay.Y = mGSlider.Y;
            mBDisplay.Y = mBSlider.Y;
            #endregion

        }


        public void CreateTextBoxes()
        {
            this.SetScaleTL(10.5f, 6);

            mATextBox = new TextBox(mCursor);
            AddWindow(mATextBox);

            mRTextBox = new TextBox(mCursor);
            AddWindow(mRTextBox);

            mGTextBox = new TextBox(mCursor);
            AddWindow(mGTextBox);

            mBTextBox = new TextBox(mCursor);
            AddWindow(mBTextBox);

            mATextBox.Format = TextBox.FormatTypes.Decimal;
            mRTextBox.Format = TextBox.FormatTypes.Decimal;
            mGTextBox.Format = TextBox.FormatTypes.Decimal;
            mBTextBox.Format = TextBox.FormatTypes.Decimal;

            mATextBox.LosingFocus += new GuiMessage(OnTextBoxValueChange);
            mRTextBox.LosingFocus += new GuiMessage(OnTextBoxValueChange);
            mGTextBox.LosingFocus += new GuiMessage(OnTextBoxValueChange);
            mBTextBox.LosingFocus += new GuiMessage(OnTextBoxValueChange);

            float textBoxX = 17f;
            mATextBox.X = textBoxX;
            mRTextBox.X = textBoxX;
            mGTextBox.X = textBoxX;
            mBTextBox.X = textBoxX;

            float textBoxScaleX = 3;
            mATextBox.ScaleX = textBoxScaleX;
            mRTextBox.ScaleX = textBoxScaleX;
            mGTextBox.ScaleX = textBoxScaleX;
            mBTextBox.ScaleX = textBoxScaleX;

            mATextBox.Y = mASlider.Y;
            mRTextBox.Y = mRSlider.Y;
            mGTextBox.Y = mGSlider.Y;
            mBTextBox.Y = mBSlider.Y; 
        }



        private void SetTextValuesToSliders()
        {
            if (mATextBox != null)
            {
                mATextBox.SetCompleteText(mASlider.CurrentValue.ToString(), false);
                mRTextBox.SetCompleteText(mRSlider.CurrentValue.ToString(), false);
                mGTextBox.SetCompleteText(mGSlider.CurrentValue.ToString(), false);
                mBTextBox.SetCompleteText(mBSlider.CurrentValue.ToString(), false);
            }
        }

        #endregion
    }
}
