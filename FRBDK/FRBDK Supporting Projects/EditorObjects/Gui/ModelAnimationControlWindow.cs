using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Graphics.Model;
using Microsoft.Xna.Framework;
using FlatRedBall.Graphics.Model.Animation;

namespace EditorObjects.Gui
{
    public class ModelAnimationControlWindow : Window
    {
        #region Fields

        private PositionedModel mModel;

        private List<String> mAnimations;

        private ComboBox mCurrentAnimation;
        private ComboBox mBlendAnimation;

        private TextDisplay mCurrentAnimationText;
        private TextDisplay mBlendAnimationText;

        private TimeLine mBlendController;

        private TimeLine mAnimationTime;
        private TimeLine mBlendTime;

        private TimeLine mAnimationSpeed;
        private TimeLine mBlendSpeed;

        private AnimationController mAnimationController;
        private AnimationController mBlendAnimationController;

        private Button mAnimationStopStart;
        private double mStoppedAnimationSpeed = 1.0;
        private double mStoppedBlendAnimationSpeed = 1.0;
        private bool mAnimationPlaying = true;

        private float mBlendFactor = 0f;

        #endregion

        #region Properties

        public PositionedModel PositionedModel
        {
            get { return mModel; }
            set
            { 
                mModel = value;

                mCurrentAnimation.Clear();
                mBlendAnimation.Clear();

                if (mModel != null)
                {
                    // Set window name
                    this.Name = mModel.Name + " Animations";

                    // Get the animation names
                    mAnimations = null;// mModel.GetAnimationNameList();

                    // Populate combo boxes
                    mCurrentAnimation.AddItem("null", "null");
                    mBlendAnimation.AddItem("null", "null");
                    foreach (String animName in mAnimations)
                    {
                        mCurrentAnimation.AddItem(animName, animName);
                        mBlendAnimation.AddItem(animName, animName);
                    }
                }
            }
        }

        #endregion

        #region Event Methods

        void mAnimationTime_onGUIChange(Window callingWindow)
        {
            if (mAnimationController != null)
            {
                mAnimationController.ElapsedTime =
                    ((TimeLine)callingWindow).CurrentValue;
            }
        }

        void mBlendTime_onGUIChange(Window callingWindow)
        {
            if (mBlendAnimationController != null)
            {
                mBlendAnimationController.ElapsedTime =
                    ((TimeLine)callingWindow).CurrentValue;
            }
        }

        void mAnimationSpeed_onGUIChange(Window callingWindow)
        {
            if (mAnimationController != null)
            {
                mAnimationController.SpeedFactor = ((TimeLine)callingWindow).CurrentValue;

                if (!mAnimationPlaying)
                {
                    mAnimationStopStart.Text = "Stop";
                    mAnimationPlaying = true;
                }
            }
        }

        void mBlendSpeed_onGUIChange(Window callingWindow)
        {
            if (mBlendAnimationController != null)
            {
                mBlendAnimationController.SpeedFactor = ((TimeLine)callingWindow).CurrentValue;

                if (!mAnimationPlaying)
                {
                    mAnimationStopStart.Text = "Stop";
                    mAnimationPlaying = true;
                }
            }
        }

        void mAnimationStopStart_Click(Window callingWindow)
        {
            if (mAnimationPlaying)
            {
                mStoppedAnimationSpeed = (mAnimationController != null) ?
                    mAnimationController.SpeedFactor : 1.0;
                mStoppedBlendAnimationSpeed = (mBlendAnimationController != null) ?
                    mBlendAnimationController.SpeedFactor : 1.0;

                if (mAnimationController != null) mAnimationController.SpeedFactor = 0.0;
                if (mBlendAnimationController != null) mBlendAnimationController.SpeedFactor = 0.0;

                mAnimationStopStart.Text = "Play";

                mAnimationPlaying = false;
            }
            else
            {
                if (mAnimationController != null) mAnimationController.SpeedFactor = mStoppedAnimationSpeed;
                if (mBlendAnimationController != null) mBlendAnimationController.SpeedFactor = mStoppedBlendAnimationSpeed;

                mAnimationStopStart.Text = "Stop";

                mAnimationPlaying = true;
            }
        }

        void mBlendAnimation_ItemClick(Window callingWindow)
        {
            if (mModel == null)
            {
                return;
            }

            ComboBox animSelect = callingWindow as ComboBox;
            String selectedAnim = animSelect.SelectedObject as String;

            if (selectedAnim == "null")
            {
                mModel.ClearBlendAnimation();
                mBlendAnimationController = null;
            }
            else
            {
                mBlendAnimationController = mModel.SetBlendAnimation(selectedAnim);
                mBlendTime.MinimumValue = 0.0;
                mBlendTime.MaximumValue = mBlendAnimationController.Duration;
                mBlendTime.Start = 0.0;
                mBlendTime.ValueWidth = mBlendAnimationController.Duration;

                mBlendTime.VerticalBarIncrement = .2f * (1 + (int)mBlendTime.ValueWidth);
                mBlendTime.SmallVerticalBarIncrement = mBlendTime.VerticalBarIncrement / 4.0f;

                mBlendTime.CurrentValue = 0.0;
                mBlendSpeed.CurrentValue = mBlendAnimationController.SpeedFactor;
            }
        }

        void mCurrentAnimation_ItemClick(Window callingWindow)
        {
            if (mModel == null)
            {
                return;
            }

            ComboBox animSelect = callingWindow as ComboBox;
            String selectedAnim = animSelect.SelectedObject as String;

            if (selectedAnim == "null")
            {
                mModel.ClearAnimation();
                mAnimationController = null;
            }
            else
            {
                mModel.CurrentAnimation = selectedAnim;
                mAnimationController = mModel.GetAnimation(selectedAnim);
                mBlendAnimation.SelectItemByText("null");
                mAnimationTime.MinimumValue = 0.0;
                mAnimationTime.MaximumValue = mAnimationController.Duration;
                mAnimationTime.Start = 0.0;
                mAnimationTime.ValueWidth = mAnimationController.Duration;

                mAnimationTime.VerticalBarIncrement = .2f * (1 + (int)mAnimationTime.ValueWidth);
                mAnimationTime.SmallVerticalBarIncrement = mAnimationTime.VerticalBarIncrement / 4.0f;

                mAnimationTime.CurrentValue = 0.0;
                mAnimationSpeed.CurrentValue = mAnimationController.SpeedFactor;
            }
        }

        void mBlendController_onGUIChange(Window callingWindow)
        {
            mBlendFactor = (float)((TimeLine)callingWindow).CurrentValue;

            mModel.SetBlendAmount(mBlendFactor);
        }

        void AnimationEditor_Resizing(Window callingWindow)
        {
            PositionUIElements();
        }

        #endregion

        #region Methods

        #region Constructor / Initialize

        public ModelAnimationControlWindow(PositionedModel model)
            : base(GuiManager.Cursor)
        {
            // Set Window properties
            HasMoveBar = true;
            Resizable = true;
            HasCloseButton = true;
            
            // Set scaling
            ScaleX = 20f;

            // Add combo boxes
            mCurrentAnimation = new ComboBox(mCursor);
            AddWindow(mCurrentAnimation);

            mBlendAnimation = new ComboBox(mCursor);
            AddWindow(mBlendAnimation);

            mCurrentAnimation.ItemClick += new GuiMessage(mCurrentAnimation_ItemClick);
            mBlendAnimation.ItemClick += new GuiMessage(mBlendAnimation_ItemClick);

            // Add text
            mCurrentAnimationText = new TextDisplay(mCursor);
            AddWindow(mCurrentAnimationText);

            mBlendAnimationText = new TextDisplay(mCursor);
            AddWindow(mBlendAnimationText);

            mCurrentAnimationText.Text = "Current Animation";
            mBlendAnimationText.Text = "Blend Animation";

            // Blend controller
            mBlendController = new TimeLine(GuiManager.Cursor);
            this.AddWindow(mBlendController);
            mBlendController.ScaleX = 18f;
            mBlendController.MinimumValue = 0.0;
            mBlendController.MaximumValue = 1.0;
            mBlendController.Start = 0.0;
            mBlendController.ValueWidth = 1.0;
            mBlendController.VerticalBarIncrement = 0.1;
            mBlendController.CurrentValue = 0.0;
            mBlendController.GuiChange += new GuiMessage(mBlendController_onGUIChange);

            // Animation timings
            mAnimationTime = new TimeLine(GuiManager.Cursor);
            this.AddWindow(mAnimationTime);
            mAnimationTime.ScaleX = 9f;
            mAnimationTime.VerticalBarIncrement = 0.2;
            mAnimationTime.SmallVerticalBarIncrement = 0.1;
            mAnimationTime.ValueWidth = 1;
            mAnimationTime.GuiChange += new GuiMessage(mAnimationTime_onGUIChange);

            mBlendTime = new TimeLine(GuiManager.Cursor);
            this.AddWindow(mBlendTime);
            mBlendTime.ScaleX = 9f;
            mBlendTime.VerticalBarIncrement = 0.2;
            mBlendTime.SmallVerticalBarIncrement = 0.1;
            mBlendTime.ValueWidth = 1;
            mBlendTime.GuiChange += new GuiMessage(mBlendTime_onGUIChange);

            // Animation speeds
            mAnimationSpeed = new TimeLine(GuiManager.Cursor);
            this.AddWindow(mAnimationSpeed);
            mAnimationSpeed.ScaleX = 9f;
            mAnimationSpeed.MinimumValue = 0.0;
            mAnimationSpeed.MaximumValue = 2.0;
            mAnimationSpeed.Start = 0.0;
            mAnimationSpeed.ValueWidth = 2.0;
            mAnimationSpeed.VerticalBarIncrement = 0.5;
            mAnimationSpeed.SmallVerticalBarIncrement = 0.1;
            mAnimationSpeed.CurrentValue = 1.0;
            mAnimationSpeed.GuiChange += new GuiMessage(mAnimationSpeed_onGUIChange);

            mBlendSpeed = new TimeLine(GuiManager.Cursor);
            this.AddWindow(mBlendSpeed);
            mBlendSpeed.ScaleX = 9f;
            mBlendSpeed.MinimumValue = 0.0;
            mBlendSpeed.MaximumValue = 2.0;
            mBlendSpeed.Start = 0.0;
            mBlendSpeed.ValueWidth = 2.0;
            mBlendSpeed.VerticalBarIncrement = 0.5;
            mBlendSpeed.SmallVerticalBarIncrement = 0.1;
            mBlendSpeed.CurrentValue = 1.0;
            mBlendSpeed.GuiChange += new GuiMessage(mBlendSpeed_onGUIChange);

            mAnimationStopStart = new Button(GuiManager.Cursor);
            this.AddWindow(mAnimationStopStart);
            mAnimationStopStart.ScaleX = 5f;
            mAnimationStopStart.Text = "Stop";
            mAnimationStopStart.Click += new GuiMessage(mAnimationStopStart_Click);

            // Set scaling
            mCurrentAnimation.ScaleX = 9f;
            mBlendAnimation.ScaleX = 9f;
            mCurrentAnimationText.ScaleY = 1f;// mCurrentAnimation.ScaleY;
            mCurrentAnimationText.ScaleX = mCurrentAnimation.ScaleX;
            mBlendAnimationText.ScaleY = 1f;// mBlendAnimation.ScaleY;
            mBlendAnimationText.ScaleX = mBlendAnimation.ScaleX;

            ScaleY = mCurrentAnimationText.ScaleY + mCurrentAnimation.ScaleY +
                2f * mBlendController.ScaleY + 2f * mAnimationTime.ScaleY +
                2f * mAnimationSpeed.ScaleY + 2f * mAnimationStopStart.ScaleY +
                .5f;
            ScaleX = (mCurrentAnimation.ScaleX + mBlendAnimation.ScaleX) + 4.5f / 2f;

            PositionUIElements();

            Resizing += new GuiMessage(AnimationEditor_Resizing);

            // Add model
            PositionedModel = model;
        }



        #endregion

        private void PositionUIElements()
        {
            #region Scaling

            float columnWidth = (ScaleX - (4.5f / 2f)) / 2f;

            mCurrentAnimation.ScaleX = columnWidth;
            mBlendAnimation.ScaleX = columnWidth;
            mCurrentAnimationText.ScaleY = 1f;// mCurrentAnimation.ScaleY;
            mCurrentAnimationText.ScaleX = columnWidth;
            mBlendAnimationText.ScaleY = 1f;// mBlendAnimation.ScaleY;
            mBlendAnimationText.ScaleX = columnWidth;
            mCurrentAnimation.ScaleX = columnWidth;
            mBlendAnimation.ScaleX = columnWidth;
            mAnimationTime.ScaleX = columnWidth;
            mBlendTime.ScaleX = columnWidth;
            mAnimationSpeed.ScaleX = columnWidth;
            mBlendSpeed.ScaleX = columnWidth;
            mBlendController.ScaleX = columnWidth * 2f;

            #endregion
            
            Vector2 posCursor;
            float maxY = 0f;

            posCursor = new Vector2(1.5f, 0.5f);
            posCursor.Y += mCurrentAnimationText.ScaleY;
            mCurrentAnimationText.X = posCursor.X;
            mCurrentAnimationText.Y = posCursor.Y;
            posCursor.Y += mCurrentAnimationText.ScaleY + mCurrentAnimation.ScaleY;
            mCurrentAnimation.X = posCursor.X + mCurrentAnimation.ScaleX;
            mCurrentAnimation.Y = posCursor.Y;
            posCursor.Y += mCurrentAnimation.ScaleY + mAnimationTime.ScaleY * 3f;
            mAnimationTime.X = posCursor.X + mAnimationTime.ScaleX;
            mAnimationTime.Y = posCursor.Y;
            posCursor.Y += mAnimationTime.ScaleY + mAnimationSpeed.ScaleY * 3f;
            mAnimationSpeed.X = posCursor.X + mAnimationSpeed.ScaleX;
            mAnimationSpeed.Y = posCursor.Y;
            posCursor.Y += mAnimationSpeed.ScaleY;


            if (posCursor.Y > maxY) maxY = posCursor.Y;

            posCursor = new Vector2(2f + 2f * mCurrentAnimation.ScaleX, 0.5f);
            posCursor.Y += mBlendAnimationText.ScaleY;
            mBlendAnimationText.X = posCursor.X;
            mBlendAnimationText.Y = posCursor.Y;
            posCursor.Y += mBlendAnimationText.ScaleY + mBlendAnimation.ScaleY;
            mBlendAnimation.X = posCursor.X + mBlendAnimation.ScaleX;
            mBlendAnimation.Y = posCursor.Y;
            posCursor.Y += mBlendAnimation.ScaleY + mBlendTime.ScaleY * 3f;
            mBlendTime.X = posCursor.X + mBlendTime.ScaleX;
            mBlendTime.Y = posCursor.Y;
            posCursor.Y += mBlendTime.ScaleY + mBlendSpeed.ScaleY * 3f;
            mBlendSpeed.X = posCursor.X + mBlendSpeed.ScaleX;
            mBlendSpeed.Y = posCursor.Y;
            posCursor.Y += mBlendSpeed.ScaleY;

            if (posCursor.Y > maxY) maxY = posCursor.Y;

            posCursor = new Vector2(1.5f, maxY);
            posCursor.Y += mBlendController.ScaleY * 3f;
            mBlendController.X = posCursor.X + mBlendController.ScaleX;
            mBlendController.Y = posCursor.Y;
            posCursor.Y += mBlendController.ScaleY;

            if (posCursor.Y > maxY) maxY = posCursor.Y;

            posCursor.Y += mAnimationStopStart.ScaleY + 0.5f;
            mAnimationStopStart.Y = posCursor.Y;
            posCursor.Y += mAnimationStopStart.ScaleY;
        }

        public void Update()
        {
            if (mAnimationController != null)
            {
                mAnimationTime.CurrentValue = mAnimationController.ElapsedTime;
                mAnimationSpeed.CurrentValue = mAnimationController.SpeedFactor;
            }

            if (mBlendAnimationController != null)
            {
                mBlendTime.CurrentValue = mBlendAnimationController.ElapsedTime;
                mBlendSpeed.CurrentValue = mBlendAnimationController.SpeedFactor;
            }
        }

        #endregion
    }
}
