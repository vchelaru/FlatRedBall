using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Math;
using FlatRedBall.Graphics;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Utilities;

namespace FlatRedBall
{
    public partial class Sprite : PositionedObject, IAnimationChainAnimatable, IScalable, IVisible
    {
        #region Fields

        #region Scale

        float mScaleX;
        float mScaleY;
        float mScaleXVelocity;
        float mScaleYVelocity;

        #endregion

        #region Animation

        // September 21, 2011 (Victor Chelaru)
        // This used to default to false but I think
        // it's best for it to default to true.  The reason
        // is that if it defaults to false, someone might assign
        // an AnimationChain and not know why...and wonder what is
        // going on.  But if we default it to true then it'll make sense
        // why the Sprite is animating when its AnimationChain is assigned.
        bool mAnimate = true;

        // December 1, 2011 (Victor Chelaru)
        // This used to be false but now the typical
        // setup is to have a Sprite attached to an Entity.
        // Users expect that relative values will apply.
        bool mUseAnimationRelativePosition = true;
        AnimationChainList mAnimationChains;
        int mCurrentChainIndex;

        float mAnimationSpeed;

        internal bool mJustCycled;
        internal bool mJustChangedFrame;

        int mCurrentFrameIndex;
        double mTimeIntoAnimation;

        bool mIgnoreAnimationChainTextureFlip;

        #endregion

        #region Particle Fields
        #region XML Docs
        /// <summary>
        /// Used by particles to flag a particular Sprite as empty.
        /// </summary>
        /// <remarks>
        /// The SpriteManager is responsible for particle recycling and uses this value to indicate
        /// whether a Sprite is currently used.  This should not be manually modified.
        /// </remarks>
        #endregion
        internal bool mEmpty;
        internal int mParticleIndex;
        double mTimeCreated;
        #endregion


        bool mVisible;

        #endregion

        #region Properties


        #region Animation

        public bool Animate
        {
            get { return mAnimate; }
            set { mAnimate = value; }
        }

        [ExportOrder(4)]
        public AnimationChainList AnimationChains
        {
            get { return mAnimationChains; }
            set
            {
#if DEBUG
                if(value == null)
                {
                    throw new InvalidOperationException("AnimationChains cannot be set to null. To clear the animations, assign the value to a new AnimationChainList.");
                }
#endif

                mAnimationChains = value;
                UpdateTimeBasedOffOfAnimationFrame();
                UpdateToCurrentAnimationFrame();
            }
        }

        public float AnimationSpeed
        {
            get { return mAnimationSpeed; }
            set { mAnimationSpeed = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the current animationChain - retrieved through the CurrentChainIndex property.
        /// </summary>
        #endregion
        public AnimationChain CurrentChain
        {
            get
            {
                if (mCurrentChainIndex != -1 && mAnimationChains.Count > 0 && mCurrentChainIndex < mAnimationChains.Count)
                {
                    return mAnimationChains[mCurrentChainIndex];
                }
                else
                    return null;
            }
        }

        public int CurrentChainIndex
        {
            get { return mCurrentChainIndex; }
            set
            {
                bool wasChangd = mCurrentChainIndex != value;

                mCurrentChainIndex = value;

                if (wasChangd)
                {
                    // June 20, 2017
                    // Not sure why we
                    // reset the mTimeIntoAnimation
                    // here. Instead, we should be setting
                    // it to preserve the frame. That is, we
                    // should either reset both the frame and
                    // mTimeIntoAnimation to 0, or we should preserve
                    // both. I'm going to preserve both because that seems
                    // to be the intended behavior of the engine, historically:
                    //mTimeIntoAnimation = 0;
                    UpdateToCurrentAnimationFrame();
                    UpdateTimeBasedOffOfAnimationFrame();
                }
            }
        }

        [ExportOrder(5)]

        public string CurrentChainName
        {
            get
            {
                if (mAnimationChains != null && mAnimationChains.Count != 0 && mCurrentChainIndex != -1 && mAnimationChains != null)
                {
                    return mAnimationChains[mCurrentChainIndex].Name;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (mCurrentChainIndex == -1 ||
                    mCurrentChainIndex >= mAnimationChains.Count ||
                    mAnimationChains[mCurrentChainIndex].Name != value)
                {
                    bool wasAnimationSet = false;

                    // If the animation is null, let's not do anything.  
                    // This could get set to null through Glue.
                    if (string.IsNullOrEmpty(value))
                    {
                        wasAnimationSet = true;
                        mTimeIntoAnimation = 0;
                        mCurrentChainIndex = -1;
                    }
                    else
                    {
#if DEBUG
                        if (mAnimationChains == null)
                        {
                            throw new NullReferenceException("AnimationChains haven't been set yet.  Do this first before setting CurrentChainName");
                        }
#endif


                        for (int i = mAnimationChains.Count - 1; i > -1; i--)
                        {
                            if (mAnimationChains[i].Name == value)
                            {
                                mTimeIntoAnimation = 0;
                                mCurrentFrameIndex = 0;
                                wasAnimationSet = true;
                                mCurrentChainIndex = i;

                                UpdateToCurrentAnimationFrame();

                                break;
                            }
                        }
                    }

                    if (!wasAnimationSet)
                    {
                        string error = "There is no animation named " + value;

                        if(mAnimationChains.Count == 0)
                        {
                            error += "\nThis sprite has no animations";
                        }
                        else
                        {
                            error += "\nAvailable animations are:";
                            for (int i = 0; i < mAnimationChains.Count; i++)
                            {
                                error += "\n" + mAnimationChains[i].Name;
                            }
                        }
                        throw new InvalidOperationException(error);
                    }
                }
            }

        }

        public int CurrentFrameIndex
        {
            get { return mCurrentFrameIndex; }
            set
            {
                if (mCurrentFrameIndex != value)
                {
                    mCurrentFrameIndex = value;
                    UpdateTimeBasedOffOfAnimationFrame();
                    UpdateToCurrentAnimationFrame();
                }
            }
        }

        public bool JustChangedFrame
        {
            get { return mJustChangedFrame; }
        }

        public bool JustCycled
        {
            get { return mJustCycled; }
        }

        public bool UseAnimationRelativePosition
        {
            get { return mUseAnimationRelativePosition; }
            set { mUseAnimationRelativePosition = value; }
        }

        public bool IgnoreAnimationChainTextureFlip
        {
            get { return mIgnoreAnimationChainTextureFlip; }
            set { mIgnoreAnimationChainTextureFlip = value; }
        }

        public double TimeIntoAnimation
        {
            get
            {
                return mTimeIntoAnimation;
            }
            set
            {
                mTimeIntoAnimation = value;

                UpdateFrameBasedOffOfTimeIntoAnimation();

                UpdateToCurrentAnimationFrame();

            }
        }
        //public double TimeUntilNextFrame
        //{
        //    get { return mTimeUntilNextFrame; }
        //    set { mTimeUntilNextFrame = value; }
        //}

        #endregion

        #region IScalable

        /// <summary>
        /// The distance from the center of the Sprite to its edge, which is equal to Width / 2.
        /// </summary>
        public float ScaleX
        {
            get { return mScaleX; }
            set { mScaleX = value; }
        }

        /// <summary>
        /// The distance from the center of the Sprite to its edge, which is equal to Height / 2. 
        /// </summary>
        public float ScaleY
        {
            get { return mScaleY; }
            set { mScaleY = value; }
        }

        public float ScaleXVelocity
        {
            get { return mScaleXVelocity; }
            set { mScaleXVelocity = value; }
        }

        public float ScaleYVelocity
        {
            get { return mScaleYVelocity; }
            set { mScaleYVelocity = value; }
        }

        public float Left
        {
            get { return Position.X - mScaleX; }
            set { Position.X = value + mScaleX; }
        }

        public float Right
        {
            get { return Position.X + mScaleX; }
            set { Position.X = value - mScaleX; }
        }

        public float Top
        {
            get { return Position.Y + mScaleY; }
            set { Position.Y = value - mScaleY; }
        }

        public float Bottom
        {
            get { return Position.Y - mScaleY; }
            set { Position.Y = value + mScaleY; }
        }

        /// <summary>
        /// Returns the left edge of the Sprite relative its Parent.  This value uses RelativeX and Width/2 to calculate this value.
        /// </summary>
        public float RelativeLeft
        {
            get { return RelativePosition.X - mScaleX; }
            set { RelativePosition.X = value + mScaleX; }
        }

        /// <summary>
        /// Returns the right edge of the Sprite relative its Parent.  This value uses RelativeX and Width/2 to calculate this value.
        /// </summary>
        public float RelativeRight
        {
            get { return RelativePosition.X + mScaleX; }
            set { RelativePosition.X = value - mScaleX; }
        }

        /// <summary>
        /// Returns the top edge of the Sprite relative its Parent.  This value uses RelativeY and Height/2 to calculate this value.
        /// </summary>
        public float RelativeTop
        {
            get { return RelativePosition.Y + mScaleY; }
            set { RelativePosition.Y = value - mScaleY; }
        }

        /// <summary>
        /// Returns the bottom edge of the Sprite relative its Parent.  This value uses RelativeY and Height/2 to calculate this value.
        /// </summary>
        public float RelativeBottom
        {
            get { return RelativePosition.Y - mScaleY; }
            set { RelativePosition.Y = value + mScaleY; }
        }

        /// <summary>
        /// The Sprite's Width in absolute world coordinates. This value may be set according
        /// to the currently displayed texture and texture coordinates if TextureScale is greater than 0.
        /// </summary>
        public float Width
        {
            get
            {
                return ScaleX * 2;
            }
            set
            {
                ScaleX = value / 2.0f;
            }
        }

        /// <summary>
        /// The Sprite's Height in absolute world coordinates. This value may be set according
        /// to the currently displayed texture and texture coordinates if TextureScale is greater than 0.
        /// </summary>
        public float Height
        {
            get
            {
                return ScaleY * 2;
            }
            set
            {
                ScaleY = value / 2.0f;
            }
        }

        #endregion

        #region XML Docs
        /// <summary>
        /// The time returned by the TimeManager when the Sprite was created.
        /// </summary>
        /// <remarks>
        /// This value is automatically set when the Sprite
        /// is added through the SpriteManager.  If a Sprite is created manually (either as a
        /// Sprite or a class inheriting from the Sprite class) this value should be set manually
        /// if it is to be used later.
        /// </remarks>
        #endregion
        public double TimeCreated
        {
            get { return mTimeCreated; }
            set { mTimeCreated = value; }
        }

        #region XML Docs
        /// <summary>
        /// Controls the visibility of the Sprite
        /// </summary>
        /// <remarks>
        /// This variable controls the visiblity of the Sprite.  Sprites are visible
        /// by default.  Setting Visible to false will make the sprite invisible, but
        /// the Sprite will continue to behave regularly; custom behavior, movement, attachment,
        /// and animation are still executed, and collision is possible.
        /// </remarks>
        #endregion
        public virtual bool Visible
        {
            get { return mVisible; }
            set 
            { 
                mVisible = value; 
            }
        }

        #region ICursorSelectable
        public bool CursorSelectable
        {
            get { return mCursorSelectable; }
            set { mCursorSelectable = value; }
        }
        #endregion


        #endregion

        #region Methods

        public override void Initialize()
        {
            Initialize(true);
        }



        public override void Initialize(bool initializeListsBelongingTo)
        {
            base.Initialize(initializeListsBelongingTo);

            mTexture = null;
            mScaleX = 1;
            mScaleY = 1;
            mScaleXVelocity = 0;
            mScaleYVelocity = 0;

            mVisible = true;
            mEmpty = true;


            Remove = null;

            mAnimate = false;

            Alpha = 1;
            Red = 0;
            Green = 0;
            Blue = 0;

            AlphaRate = 0;
            RedRate = 0;
            GreenRate = 0;
            BlueRate = 0;

            mCurrentChainIndex = 0;
            mCurrentFrameIndex = 0;

            mAnimationSpeed = 1;
            mJustChangedFrame = false;
            mJustCycled = false;
            mInstructions.Clear();


            PlatformSpecificInitialization();
            //           constantPixelSize = -1;
        }

        #region Animation

        #region XML Docs
        /// <summary>
        /// Performs the every-frame logic for updating the current AnimationFrame index.  If the
        /// Sprite is part of the SpriteManager then this is automatically called.
        /// </summary>
        /// <param name="currentTime">The number of seconds that have passed since the game has started running.</param>
        #endregion
        public void AnimateSelf(double currentTime)
        {
            mJustChangedFrame = false;
            mJustCycled = false;
            if (mAnimate == false || mCurrentChainIndex == -1 || mAnimationChains.Count == 0 || mAnimationChains[mCurrentChainIndex].Count == 0) return;

            int frameBefore = mCurrentFrameIndex;

            // June 10, 2011
            // A negative animation speed should cause the animation to play in reverse
            //Removed the System.Math.Abs on the mAnimationSpeed variable to restore the correct behaviour.
            //double modifiedTimePassed = TimeManager.SecondDifference * System.Math.Abs(mAnimationSpeed);
            double modifiedTimePassed = TimeManager.SecondDifference * mAnimationSpeed;

            mTimeIntoAnimation += modifiedTimePassed;

            AnimationChain animationChain = mAnimationChains[mCurrentChainIndex];

            mTimeIntoAnimation = MathFunctions.Loop(mTimeIntoAnimation, animationChain.TotalLength, out mJustCycled);

            UpdateFrameBasedOffOfTimeIntoAnimation();

            if (mCurrentFrameIndex != frameBefore)
            {
                UpdateToCurrentAnimationFrame();
                mJustChangedFrame = true;
            }

            if (mJustCycled)
            {
                mJustCycled = true;
            }
        }

        #region XML Docs
        /// <summary>
        /// Clears all references to AnimationChains and sets the Animate property to false.
        /// </summary>
        #endregion
        public void ClearAnimationChains()
        {
            mAnimate = false;
            mCurrentChainIndex = -1;

            mAnimationChains.Clear();
        }

        #region XML Docs
        /// <summary>
        /// Removes the AnimationChain from the Sprite's internal AnimationChain List.
        /// </summary>
        /// <remarks>
        /// If the chainToRemove is also the CurrentChain, the animate field 
        /// is set to false.
        /// </remarks>
        /// <param name="chainToRemove">The AnimationChain to remove.</param>
        #endregion
        public void RemoveAnimationChain(AnimationChain chainToRemove)
        {
            int index = mAnimationChains.IndexOf(chainToRemove);

            if (mAnimationChains.Contains(chainToRemove))
            {
                mAnimationChains.Remove(chainToRemove);
            }
            if (index == mCurrentChainIndex)
            {
                mCurrentChainIndex = -1;
                mAnimate = false;
            }
        }

        #region XML Docs
        /// <summary>
        /// Sets the argument chainToSet as the animationChain. If the argument chainToSet is not
        /// part of the Sprite's internal list of AnimationChains, it is added.
        /// </summary>
        /// <remarks>
        /// This differs from FlatRedBall MDX - this method on FlatRedBall MDX does not add the argument
        /// AnimationChain to the Sprite's internal list.
        /// <para>
        /// This does not set any animation-related properties, but it does set the current
        /// texture to the current frame's texture.  Therefore, it is still necessary to set Animate to true.
        /// </para>
        /// </remarks>
        /// <param name="chainToSet">The AnimationChain to set as the current AnimationChain.  This is
        /// added to the internal AnimationChains property if it is not already there.</param>
        #endregion
        public void SetAnimationChain(AnimationChain chainToSet)
        {
            if (chainToSet != null)
            {
                int index = mAnimationChains.IndexOf(chainToSet);
                if (index != -1)
                    mCurrentChainIndex = index;
                else
                {
                    mAnimationChains.Add(chainToSet);
                    mCurrentChainIndex = mAnimationChains.Count - 1;
                }

                mTimeIntoAnimation = 0;
                mCurrentFrameIndex = 0;
                UpdateToCurrentAnimationFrame();
            }
        }


        public void SetAnimationChain(AnimationChain chainToSet, double timeIntoAnimation)
        {
            if (chainToSet != null)
            {
                mCurrentFrameIndex = 0;
                SetAnimationChain(chainToSet);

                mTimeIntoAnimation = timeIntoAnimation;

                UpdateFrameBasedOffOfTimeIntoAnimation();
            }
        }

        #region XML Docs
        /// <summary>
        /// Sets the current AnimationChain by name and keeps the CurrentFrame the same.
        /// </summary>
        /// <remarks>
        /// This method assumes that the Sprite contains a reference to an AnimationChain with the name matching chainToSet.  Passing a
        /// name that is not found in the Sprite's AnimationChainArray will not cause any changes.
        /// 
        /// <para>This method will keep the CurrentFrame property the same (unless it exceeds the bounds of the new AnimationChain).  In the 
        /// case that the CurrentFrame is greater than the bounds of the new AnimationChain, the animation will cycle back to the beginning.
        /// The animate field is not changed to true if it is false.</para>
        /// <seealso cref="FRB.Sprite.AnimationChains"/>
        /// </remarks>
        /// <param name="chainToSet">The name of the AnimationChain to set as current.</param>
        #endregion
        [Obsolete("Use the CurrentChainName Property instead of this method")]
        public void SetAnimationChain(string chainToSet)
        {
            CurrentChainName = chainToSet;
        }


        public string GetAnimationInformation()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Animate: ").AppendLine(mAnimate.ToString());
            if (CurrentChain != null)
                stringBuilder.Append("CurrentChain: ").AppendLine(CurrentChain.Name);
            else
                stringBuilder.Append("CurrentChain: <null>");

            stringBuilder.Append("CurrentFrameIndex: ").AppendLine(CurrentFrameIndex.ToString());

            stringBuilder.Append("Time Into Animation: ").AppendLine(mTimeIntoAnimation.ToString());

            return stringBuilder.ToString();


        }


        void UpdateFrameBasedOffOfTimeIntoAnimation()
        {
            double timeIntoAnimation = mTimeIntoAnimation;

            if (timeIntoAnimation < 0)
            {
                throw new ArgumentException("The timeIntoAnimation argument must be 0 or positive");
            }
            else if (CurrentChain != null && CurrentChain.Count > 1)
            {
                int frameIndex = 0;
                while (timeIntoAnimation >= 0)
                {
                    double frameTime = CurrentChain[frameIndex].FrameLength;

                    if (timeIntoAnimation < frameTime)
                    {
                        mCurrentFrameIndex = frameIndex;

                        break;
                    }
                    else
                    {
                        timeIntoAnimation -= frameTime;

                        frameIndex = (frameIndex + 1) % CurrentChain.Count;
                    }
                }
            }
        }

        void UpdateTimeBasedOffOfAnimationFrame()
        {
            int animationFrame = mCurrentFrameIndex;

            if (animationFrame < 0)
            {
                throw new ArgumentException("The animationFrame argument must be 0 or positive");
            }
            else if (CurrentChain != null && CurrentChain.Count > 1)
            {
                mTimeIntoAnimation = 0.0f;
                //update to the correct time for this frame
                for (int x = 0; x < mCurrentFrameIndex && x < CurrentChain.Count; x++)
                {
                    mTimeIntoAnimation += CurrentChain[x].FrameLength;
                }
            }
        }




        #endregion


        #region XML Docs
        /// <summary>
        /// Sets the ScaleY so that the ScaleX/ScaleY ratio is the same as the source image used for the Sprite's texture.
        /// </summary>
        #endregion
        public void SetScaleYRatioToX()
        {
            float widthToUse = mTexture.Width * (RightTextureCoordinate - LeftTextureCoordinate);
            float heightToUse = mTexture.Height * (BottomTextureCoordinate - TopTextureCoordinate);

            if (widthToUse != 0)
            {
                ScaleY = ScaleX * heightToUse / widthToUse;
            }
        }

        #region XML Docs
        /// <summary>
        /// Sets the ScaleY so that the ScaleX/ScaleY ratio is the same as the source image used for the Sprite's texture.
        /// </summary>
        #endregion
        public virtual void SetScaleXRatioToY()
        {
            float widthToUse = mTexture.Width * (RightTextureCoordinate - LeftTextureCoordinate);
            float heightToUse = mTexture.Height * (BottomTextureCoordinate - TopTextureCoordinate);

            if (heightToUse != 0)
            {
                ScaleX = ScaleY * widthToUse / (float)heightToUse;
            }
        }

        public override void Pause(FlatRedBall.Instructions.InstructionList instructions)
        {
            FlatRedBall.Instructions.Pause.SpriteUnpauseInstruction instruction =
                new FlatRedBall.Instructions.Pause.SpriteUnpauseInstruction(this);

            instruction.Stop(this);

            instructions.Add(instruction);
        }

        #region XML Docs
        /// <summary>
        /// Applies all velocities, rates, accelerations for real and relative values.
        /// If the Sprite is part of the SpriteManager (which is common) then this is automatically
        /// called.
        /// </summary>
        /// <param name="secondDifference">The number of seocnds that have passed since last frame.</param>
        /// <param name="secondDifferenceSquaredDividedByTwo">Precalculated (secondDifference * secondDifference)/2.0f for applying acceleration.</param>
        /// <param name="secondsPassedLastFrame">The number of seconds that passed last frame for calculating "real" values.</param>
        #endregion
        public override void TimedActivity(float secondDifference, double secondDifferenceSquaredDividedByTwo, float secondsPassedLastFrame)
        {
            base.TimedActivity(secondDifference, secondDifferenceSquaredDividedByTwo, secondsPassedLastFrame);

            if (mAlphaRate != 0.0f)
                Alpha += mAlphaRate * secondDifference;
            if (mRedRate != 0.0f)
                Red += mRedRate * secondDifference;
            if (mGreenRate != 0.0f)
                Green += mGreenRate * secondDifference;
            if (mBlueRate != 0.0f)
                Blue += mBlueRate * secondDifference;

            mScaleX += mScaleXVelocity * secondDifference;
            mScaleY += mScaleYVelocity * secondDifference;

        }

        #endregion


        #region IVisible implementation



        IVisible IVisible.Parent
        {
            get
            {
                return mParent as IVisible;
            }
        }

        public bool AbsoluteVisible
        {
            get
            {
                IVisible iVisibleParent = ((IVisible)this).Parent;
                return mVisible && (iVisibleParent == null || IgnoresParentVisibility || iVisibleParent.AbsoluteVisible);
            }
        }

        public bool IgnoresParentVisibility
        {
            get;
            set;
        }

        #endregion
    }
}
