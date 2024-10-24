using System;
using System.Collections.Generic;
using System.Text;



using FlatRedBall.Graphics.Animation;
using FlatRedBall.Instructions;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Graphics.Particle
{
    #region XML Docs
    /// <summary>
    /// Defines the state of Sprites immediately after being created by the containing Emitter.
    /// </summary>
    #endregion
    public class EmissionSettings
    {
        #region Fields

        #region Velocity
        RangeType mVelocityRangeType;
        float mRadialVelocity = 1;
        float mRadialVelocityRange = 0;

        float mXVelocity = -1;
        float mYVelocity = -1;
        float mZVelocity = -1;
        float mXVelocityRange = 2;
        float mYVelocityRange = 2;
        float mZVelocityRange = 2;

        float mWedgeAngle = 0;
        float mWedgeSpread = (float)System.Math.PI/4.0f;
        #endregion

        #region Rotation
        float mRotationX;
        float mRotationY;
        float mRotationZ;
        float mRotationXVelocity;
        float mRotationYVelocity;
        float mRotationZVelocity;

        float mRotationXRange;
        float mRotationYRange;
        float mRotationZRange;
        float mRotationXVelocityRange;
        float mRotationYVelocityRange;
        float mRotationZVelocityRange;

        bool mBillboarded;
        #endregion

        #region Acceleration
        float mXAcceleration;
        float mYAcceleration;
        float mZAcceleration;
        float mXAccelerationRange;
        float mYAccelerationRange;
        float mZAccelerationRange;
        #endregion

        #region Scale
        float mScaleX;
        float mScaleY;
        float mScaleXRange;
        float mScaleYRange;

        float mScaleXVelocity;
        float mScaleYVelocity;
        float mScaleXVelocityRange;
        float mScaleYVelocityRange;

        bool mMatchScaleXToY = false;


        #endregion

        float mAlpha;
        float mRed;
        float mGreen;
        float mBlue;

        float mAlphaRate;
        float mRedRate;
        float mGreenRate;
        float mBlueRate;

        InstructionBlueprintList mInstructions;
        BlendOperation mBlendOperation;

        ColorOperation mColorOperation;

        bool mAnimate;
        AnimationChain mAnimationChain;
        string mCurrentChainName;

        float mDrag = 0;
        #endregion

        #region Properties

        #region Velocity

        /// <summary>
        /// Sets the type of velocity to use.  This impacts which Velocity values are
        /// applied to emitted Sprites.
        /// </summary>
        public RangeType VelocityRangeType
        {
            get { return mVelocityRangeType; }
            set { mVelocityRangeType = value; }
        }

        public float RadialVelocity
        {
            get { return mRadialVelocity; }
            set { mRadialVelocity = value; }
        }
        public float RadialVelocityRange
        {
            get { return mRadialVelocityRange; }
            set { mRadialVelocityRange = value; }
        }

        public float XVelocity
        {
            get { return mXVelocity; }
            set { mXVelocity = value; }
        }
        public float YVelocity
        {
            get { return mYVelocity; }
            set { mYVelocity = value; }
        }
        public float ZVelocity
        {
            get { return mZVelocity; }
            set { mZVelocity = value; }
        }
        public float XVelocityRange
        {
            get { return mXVelocityRange; }
            set { mXVelocityRange = value; }
        }
        public float YVelocityRange
        {
            get { return mYVelocityRange; }
            set { mYVelocityRange = value; }
        }
        public float ZVelocityRange
        {
            get { return mZVelocityRange; }
            set { mZVelocityRange = value; }
        }

        public float WedgeAngle
        {
            get { return mWedgeAngle; }
            set { mWedgeAngle = value; }
        }
        public float WedgeSpread
        {
            get { return mWedgeSpread; }
            set { mWedgeSpread = value; }
        }

        #endregion

        #region Rotation

        public float RotationX
        {
            get { return mRotationX; }
            set { mRotationX = value; }
        }

        public float RotationY
        {
            get { return mRotationY; }
            set { mRotationY = value; }
        }

        public float RotationZ
        {
            get { return mRotationZ; }
            set { mRotationZ = value; }
        }

        public float RotationXVelocity
        {
            get { return mRotationXVelocity; }
            set { mRotationXVelocity = value; }
        }

        public float RotationYVelocity
        {
            get { return mRotationYVelocity; }
            set { mRotationYVelocity = value; }
        }

        public float RotationZVelocity
        {
            get { return mRotationZVelocity; }
            set
            {
#if DEBUG
                if(float.IsInfinity(value) || float.IsNegativeInfinity(value))
                {
                    throw new Exception("Invalid RotationZVelocity value");
                }
#endif
                mRotationZVelocity = value;
            }
        }

        public float RotationXRange
        {
            get { return mRotationXRange; }
            set { mRotationXRange = value; }
        }

        public float RotationYRange
        {
            get { return mRotationYRange; }
            set { mRotationYRange = value; }
        }

        public float RotationZRange
        {
            get { return mRotationZRange; }
            set { mRotationZRange = value; }
        }

        public float RotationXVelocityRange
        {
            get { return mRotationXVelocityRange; }
            set { mRotationXVelocityRange = value; }
        }

        public float RotationYVelocityRange
        {
            get { return mRotationYVelocityRange; }
            set { mRotationYVelocityRange = value; }
        }

        public float RotationZVelocityRange
        {
            get { return mRotationZVelocityRange; }
            set { mRotationZVelocityRange = value; }
        }

        public bool Billboarded
        {
            get { return mBillboarded; }
            set { mBillboarded = value; }
        }

        #endregion

        #region Acceleration / Drag

        public float XAcceleration
        {
            get { return mXAcceleration; }
            set { mXAcceleration = value; }
        }
        public float YAcceleration
        {
            get { return mYAcceleration; }
            set { mYAcceleration = value; }
        }
        public float ZAcceleration
        {
            get { return mZAcceleration; }
            set { mZAcceleration = value; }
        }
        public float XAccelerationRange
        {
            get { return mXAccelerationRange; }
            set { mXAccelerationRange = value; }
        }
        public float YAccelerationRange
        {
            get { return mYAccelerationRange; }
            set { mYAccelerationRange = value; }
        }
        public float ZAccelerationRange
        {
            get { return mZAccelerationRange; }
            set { mZAccelerationRange = value; }
        }

        public float Drag
        {
            get { return mDrag; }
            set { mDrag = value; }
        }

        #endregion

        #region Scale
        public float ScaleX
        {
            get { return mScaleX; }
            set { mScaleX = value; }
        }
        public float ScaleY
        {
            get { return mScaleY; }
            set { mScaleY = value; }
        }
        public float ScaleXRange
        {
            get { return mScaleXRange; }
            set { mScaleXRange = value; }
        }
        public float ScaleYRange
        {
            get { return mScaleYRange; }
            set { mScaleYRange = value; }
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
        public float ScaleXVelocityRange
        {
            get { return mScaleXVelocityRange; }
            set { mScaleXVelocityRange = value; }
        }
        public float ScaleYVelocityRange
        {
            get { return mScaleYVelocityRange; }
            set { mScaleYVelocityRange = value; }
        }

        public bool MatchScaleXToY
        {
            get { return mMatchScaleXToY; }
            set { mMatchScaleXToY = value; }
        }

        [Obsolete("Use TextureScale instead.  TextureScale = PixelSize * 2")]
        public float PixelSize { get; set; }

        public float TextureScale
        {
            get { return PixelSize * 2.0f; }
            set { PixelSize = value / 2.0f; }
        }

        #endregion

        #region Tint/Fade/Blend/Color

        public float Alpha
        {
            get { return mAlpha; }
            set { mAlpha = value; }
        }
        public float Red
        {
            get { return mRed; }
            set { mRed = value; }
        }
        public float Green
        {
            get { return mGreen; }
            set { mGreen = value; }
        }
        public float Blue
        {
            get { return mBlue; }
            set { mBlue = value; }
        }

        public float AlphaRate
        {
            get { return mAlphaRate; }
            set { mAlphaRate = value; }
        }
        public float RedRate
        {
            get { return mRedRate; }
            set { mRedRate = value; }
        }
        public float GreenRate
        {
            get { return mGreenRate; }
            set { mGreenRate = value; }
        }
        public float BlueRate
        {
            get { return mBlueRate; }
            set { mBlueRate = value; }
        }

        public BlendOperation BlendOperation
        {
            get { return mBlendOperation; }
            set { mBlendOperation = value; }
        }

        public ColorOperation ColorOperation
        {
            get { return mColorOperation; }
            set { mColorOperation = value; }
        }

        #endregion

        #region Texture/Animation

        /// <summary>
        /// Whether or not the emitted particle should automatically animate
        /// </summary>
        public bool Animate
        {
            get { return mAnimate; }
            set { mAnimate = value; }
        }

        /// <summary>
        /// The animation chains to use for the particle animation
        /// </summary>
        public AnimationChainList AnimationChains
        {
            get;
            set;
        }

        /// <summary>
        /// The currently set animation chain
        /// </summary>
        public AnimationChain AnimationChain
        {
            get { return mAnimationChain; }
            set { mAnimationChain = value; }
        }

        /// <summary>
        /// The chain that is currently animating
        /// </summary>
        public string CurrentChainName
        {
            get
            {
                return mCurrentChainName;
            }
            set
            {
                mCurrentChainName = value;
                if (AnimationChains != null && AnimationChains[mCurrentChainName] != null)
                {
                    AnimationChain = AnimationChains[mCurrentChainName];
                }
            }
        }

        /// <summary>
        /// The particle texture.
        /// If animation chains are set, they should override this.
        /// </summary>
        public Texture2D Texture
        {
            get;
            set;
        }

        #endregion

        public InstructionBlueprintList Instructions
        {
            get
            {
                return mInstructions;
            }

            set
            {
                mInstructions = value;
            }
        }

        #endregion

        #region Methods

        public EmissionSettings()
        {
            mInstructions = new InstructionBlueprintList();

            mVelocityRangeType = RangeType.Radial;
            mScaleX = 1;
            mScaleY = 1;
            PixelSize = -1;
            mAlpha = GraphicalEnumerations.MaxColorComponentValue;

            mColorOperation = ColorOperation.Texture;
            mBlendOperation = BlendOperation.Regular;
        }

        public EmissionSettings Clone()
        {
            return (EmissionSettings)this.MemberwiseClone();
        }
        
        #endregion
    }
}
