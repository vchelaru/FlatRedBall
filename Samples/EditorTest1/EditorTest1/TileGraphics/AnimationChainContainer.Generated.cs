using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Graphics.Animation;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.Math;

namespace FlatRedBall.TileGraphics
{
    /// <summary>
    /// An AnimationChain container that self animates.  This can be used
    /// as the container for IAnimationChainAnimatables.
    /// </summary>
    public class AnimationChainContainer
    {

        double mTimeIntoAnimation = 0;

        public float AnimationSpeed
        {
            get;
            set;
        }

        int _currentFrameIndex;
        public int CurrentFrameIndex
        {
            get => _currentFrameIndex;
            set
            {
                _currentFrameIndex = value;
                UpdateTimeBasedOffOfAnimationFrame();
            }
        }

        public AnimationFrame CurrentFrame
        {
            get
            {
                return AnimationChain[CurrentFrameIndex];
            }
        }

        public AnimationChain AnimationChain
        {
            get;
            set;
        }

        public Texture2D CurrentTexture
        {
            get
            {
                return AnimationChain[CurrentFrameIndex].Texture;
            }
        }

        public AnimationChainContainer(AnimationChain animationChain)
        {
            this.AnimationChain = animationChain;
            AnimationSpeed = 1;
            CurrentFrameIndex = 0;
        }

        /// <summary>
        /// Moves the animation forward by the argument time;
        /// </summary>
        /// <param name="secondDifference">The amount of time that has passed since last update.</param>
        public void Activity(float secondDifference)
        {
            if (AnimationChain != null)
            {
                double modifiedTimePassed = secondDifference * AnimationSpeed;

                mTimeIntoAnimation += modifiedTimePassed;

                mTimeIntoAnimation = MathFunctions.Loop(mTimeIntoAnimation, AnimationChain.TotalLength);

                UpdateFrameBasedOffOfTimeIntoAnimation();
            }
        }


        void UpdateTimeBasedOffOfAnimationFrame()
        {
            int animationFrame = _currentFrameIndex;

            if (animationFrame < 0)
            {
                throw new ArgumentException("The animationFrame argument must be 0 or positive");
            }
            else if (AnimationChain?.Count > 1)
            {
                mTimeIntoAnimation = 0.0f;
                //update to the correct time for this frame
                for (int x = 0; x < _currentFrameIndex && x < AnimationChain.Count; x++)
                {
                    mTimeIntoAnimation += AnimationChain[x].FrameLength;
                }
            }
        }

        void UpdateFrameBasedOffOfTimeIntoAnimation()
        {
            double timeIntoAnimation = mTimeIntoAnimation;

            if (timeIntoAnimation < 0)
            {
                throw new ArgumentException("The timeIntoAnimation argument must be 0 or positive");
            }
            else if (AnimationChain != null && AnimationChain.Count > 1)
            {
                int frameIndex = 0;

                // Don't start the while loop if the animation
                // has a length of 0, will result in an infinite loop
                if (AnimationChain.TotalLength > 0)
                {
                    while (timeIntoAnimation >= 0)
                    {
                        double frameTime = AnimationChain[frameIndex].FrameLength;
                        if (timeIntoAnimation < frameTime)
                        {
                            _currentFrameIndex = frameIndex;

                            break;
                        }
                        else
                        {
                            timeIntoAnimation -= frameTime;

                            frameIndex = (frameIndex + 1) % AnimationChain.Count;
                        }
                    }
                }
            }
        }

    }
}
