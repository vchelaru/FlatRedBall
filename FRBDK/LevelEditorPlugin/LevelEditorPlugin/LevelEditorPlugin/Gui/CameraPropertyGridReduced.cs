using System;
using FlatRedBall.Gui;
using FlatRedBall;
using FlatRedBall.Math;

namespace LevelEditor.Gui
{
    public class CameraPropertyGridReduced : PropertyGrid<Camera>
    {
        TextBox mResolutionWidthTextBox;
        TextBox mResolutionHeightTextBox;

        void OnResolutionTextBoxLosingFocus(Window callingWindow)
        {
            int width = GetIntegerFromTextBox(mResolutionWidthTextBox);
            int height = GetIntegerFromTextBox(mResolutionHeightTextBox);

            if (width == -1)
            {
                width = FlatRedBallServices.GraphicsOptions.ResolutionWidth;
            }
            if (height == -1)
            {
                height = FlatRedBallServices.GraphicsOptions.ResolutionHeight;
            }

            const int minimumResolutionValue = 20;
            width = Math.Max(minimumResolutionValue, width);
            height = Math.Max(minimumResolutionValue, height);

            if (width != FlatRedBallServices.GraphicsOptions.ResolutionWidth ||
                height != FlatRedBallServices.GraphicsOptions.ResolutionHeight)
            {
                FlatRedBallServices.GraphicsOptions.SetResolution(
                    width, height);
            }
        }

        private int GetIntegerFromTextBox(TextBox textBox)
        {
            if (textBox.Text == "" || textBox.Text == "-." || textBox.Text == "-")
                textBox.Text = "0";

            try
            {

                float valueAsFloat = float.Parse(textBox.Text);
                int valueAsInt = MathFunctions.RoundToInt(valueAsFloat);

                return valueAsInt;
            }
            catch (Exception e)
            {
                return -1;

            }
        }


        public CameraPropertyGridReduced(Cursor cursor)
            : base(cursor)
        {
            ExcludeAllMembers();
            IncludeMember("X");
            IncludeMember("Y");

            IncludeMember("Orthogonal");

            mResolutionWidthTextBox = new TextBox(cursor);
            mResolutionWidthTextBox.ScaleX = 4;
            mResolutionWidthTextBox.Format = TextBox.FormatTypes.Integer;
            mResolutionWidthTextBox.LosingFocus += new GuiMessage(OnResolutionTextBoxLosingFocus);
            AddWindow(mResolutionWidthTextBox);
            SetLabelForWindow(mResolutionWidthTextBox, "Resolution Width");

            mResolutionHeightTextBox = new TextBox(cursor);
            mResolutionHeightTextBox.ScaleX = 4;
            mResolutionHeightTextBox.Format = TextBox.FormatTypes.Integer;
            mResolutionHeightTextBox.LosingFocus += new GuiMessage(OnResolutionTextBoxLosingFocus);
            AddWindow(mResolutionHeightTextBox);
            SetLabelForWindow(mResolutionHeightTextBox, "Resolution Height");

            IncludeMember("BackgroundColor");
        }


        public override void UpdateDisplayedProperties()
        {
            base.UpdateDisplayedProperties();

            if (mResolutionWidthTextBox != null && !this.IsWindowOrChildrenReceivingInput)
            {
                mResolutionHeightTextBox.Text = FlatRedBallServices.GraphicsOptions.ResolutionHeight.ToString();
                mResolutionWidthTextBox.Text = FlatRedBallServices.GraphicsOptions.ResolutionWidth.ToString();
            }
        }

    }
}
