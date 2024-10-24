using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall.Graphics.Animation;
using FlatRedBall.Instructions;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.IO;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Graphics.Particle
{
    #region XML Docs
    /// <summary>
    /// Save class for the EmissionSettings class.  This class is used
    /// in the .emix (Emitter XML) file.
    /// </summary>
    #endregion
    public class EmissionSettingsSave
    {
        #region Fields

        #region Velocity
        RangeType mVelocityRangeType = RangeType.Component;
        float mRadialVelocity;
        float mRadialVelocityRange;

        float mXVelocity;
        float mYVelocity;
        float mZVelocity;

        float mXVelocityRange;
        float mYVelocityRange;
        float mZVelocityRange;

        float mWedgeAngle;
        float mWedgeSpread;
        #endregion

        #region Rotation

        bool mBillboarded;

        float mRotationX;
        float mRotationXVelocity;

        float mRotationXRange;
        float mRotationXVelocityRange;

        float mRotationY;
        float mRotationYVelocity;

        float mRotationYRange;
        float mRotationYVelocityRange;

        float mRotationZ;
        float mRotationZVelocity;

        float mRotationZRange;
        float mRotationZVelocityRange;
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

        bool mMatchScaleXToY;
        #endregion

        float mFade;
        float mTintRed;
        float mTintGreen;
        float mTintBlue;

        float mFadeRate;
        float mTintRedRate;
        float mTintGreenRate;
        float mTintBlueRate;

        string mBlendOperation;
        string mColorOperation;

        bool mAnimate;
        InstructionBlueprintListSave mInstructions;
        

        float mDrag = 0;
        #endregion

        #region Properties

        #region Velocity

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

        public bool Billboarded
        {
            get { return mBillboarded; }
            set { mBillboarded = value; }
        }

        public float RotationX
        {
            get { return mRotationX; }
            set { mRotationX = value; }
        }

        public float RotationXVelocity
        {
            get { return mRotationXVelocity; }
            set { mRotationXVelocity = value; }
        }

        public float RotationXRange
        {
            get { return mRotationXRange; }
            set { mRotationXRange = value; }
        }

        public float RotationXVelocityRange
        {
            get { return mRotationXVelocityRange; }
            set { mRotationXVelocityRange = value; }
        }

        public float RotationY
        {
            get { return mRotationY; }
            set { mRotationY = value; }
        }

        public float RotationYVelocity
        {
            get { return mRotationYVelocity; }
            set { mRotationYVelocity = value; }
        }

        public float RotationYRange
        {
            get { return mRotationYRange; }
            set { mRotationYRange = value; }
        }

        public float RotationYVelocityRange
        {
            get { return mRotationYVelocityRange; }
            set { mRotationYVelocityRange = value; }
        }


        public float RotationZ
        {
            get { return mRotationZ; }
            set { mRotationZ = value; }
        }
        public float RotationZVelocity
        {
            get { return mRotationZVelocity; }
            set { mRotationZVelocity = value; }
        }

        public float RotationZRange
        {
            get { return mRotationZRange; }
            set { mRotationZRange = value; }
        }
        public float RotationZVelocityRange
        {
            get { return mRotationZVelocityRange; }
            set { mRotationZVelocityRange = value; }
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

		// Compiling with C# 6.0 still causes problems on Android
		float textureScale = 1;
        public float TextureScale
        {
			get { return textureScale; }
			set {
				textureScale = value;
			}
        }

        #endregion

        #region Tint/Fade/Blend/Color

        public float Fade
        {
            get { return mFade; }
            set { mFade = value; }
        }
        public float TintRed
        {
            get { return mTintRed; }
            set { mTintRed = value; }
        }
        public float TintGreen
        {
            get { return mTintGreen; }
            set { mTintGreen = value; }
        }
        public float TintBlue
        {
            get { return mTintBlue; }
            set { mTintBlue = value; }
        }

        public float FadeRate
        {
            get { return mFadeRate; }
            set { mFadeRate = value; }
        }
        public float TintRedRate
        {
            get { return mTintRedRate; }
            set { mTintRedRate = value; }
        }
        public float TintGreenRate
        {
            get { return mTintGreenRate; }
            set { mTintGreenRate = value; }
        }
        public float TintBlueRate
        {
            get { return mTintBlueRate; }
            set { mTintBlueRate = value; }
        }

        public string BlendOperation
        {
            get { return mBlendOperation; }
            set { mBlendOperation = value; }
        }
        public string ColorOperation
        {
            get { return mColorOperation; }
            set { mColorOperation = value; }
        }

        #endregion

        #region Texture/Animation Settings
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
        public string AnimationChains
        {
            get;
            set;
        }

        /// <summary>
        /// The chain that is currently animating
        /// </summary>
        public string CurrentChainName
        {
            get;
            set;
        }

        /// <summary>
        /// The particle texture.
        /// If animation chains are set, they should override this.
        /// </summary>
        public string Texture
        {
            get;
            set;
        }

        #endregion

        public InstructionBlueprintListSave Instructions
        {
            get { return mInstructions; }
            set { mInstructions = value; }
        }

        #endregion

        #region Methods

        #region Constructor

        public EmissionSettingsSave()
        {

            mVelocityRangeType = RangeType.Radial;
            mRadialVelocity = 1;
            mScaleX = 1;
            mScaleY = 1;
            mFade = 1;

            mColorOperation = "";
            mBlendOperation = "";
        }

        #endregion

        public static EmissionSettingsSave FromEmissionSettings(EmissionSettings emissionSettings)
        {
            EmissionSettingsSave emissionSettingsSave = new EmissionSettingsSave();

            emissionSettingsSave.mVelocityRangeType = emissionSettings.VelocityRangeType;
            emissionSettingsSave.mRadialVelocity = emissionSettings.RadialVelocity;
            emissionSettingsSave.mRadialVelocityRange = emissionSettings.RadialVelocityRange;

            emissionSettingsSave.mXVelocity = emissionSettings.XVelocity;
            emissionSettingsSave.mYVelocity = emissionSettings.YVelocity;
            emissionSettingsSave.mZVelocity = emissionSettings.ZVelocity;

            emissionSettingsSave.mXVelocityRange = emissionSettings.XVelocityRange;
            emissionSettingsSave.mYVelocityRange = emissionSettings.YVelocityRange;
            emissionSettingsSave.mZVelocityRange = emissionSettings.ZVelocityRange;

            emissionSettingsSave.mWedgeAngle = emissionSettings.WedgeAngle;
            emissionSettingsSave.mWedgeSpread = emissionSettings.WedgeSpread;

            emissionSettingsSave.Billboarded = emissionSettings.Billboarded;

            emissionSettingsSave.mRotationX = emissionSettings.RotationX;
            emissionSettingsSave.mRotationXVelocity = emissionSettings.RotationXVelocity;
            emissionSettingsSave.mRotationXRange = emissionSettings.RotationXRange;
            emissionSettingsSave.mRotationXVelocityRange = emissionSettings.RotationXVelocityRange;

            emissionSettingsSave.mRotationY = emissionSettings.RotationY;
            emissionSettingsSave.mRotationYVelocity = emissionSettings.RotationYVelocity;
            emissionSettingsSave.mRotationYRange = emissionSettings.RotationYRange;
            emissionSettingsSave.mRotationYVelocityRange = emissionSettings.RotationYVelocityRange;
            

            emissionSettingsSave.mRotationZ = emissionSettings.RotationZ;
            emissionSettingsSave.mRotationZVelocity = emissionSettings.RotationZVelocity;
            emissionSettingsSave.mRotationZRange = emissionSettings.RotationZRange;
            emissionSettingsSave.mRotationZVelocityRange = emissionSettings.RotationZVelocityRange;

            emissionSettingsSave.mXAcceleration = emissionSettings.XAcceleration;
            emissionSettingsSave.mYAcceleration = emissionSettings.YAcceleration;
            emissionSettingsSave.mZAcceleration = emissionSettings.ZAcceleration;

            emissionSettingsSave.mXAccelerationRange = emissionSettings.XAccelerationRange;
            emissionSettingsSave.mYAccelerationRange = emissionSettings.YAccelerationRange;
            emissionSettingsSave.mZAccelerationRange = emissionSettings.ZAccelerationRange;

            emissionSettingsSave.mScaleX = emissionSettings.ScaleX;
            emissionSettingsSave.mScaleY = emissionSettings.ScaleY;
            emissionSettingsSave.mScaleXRange = emissionSettings.ScaleXRange;
            emissionSettingsSave.mScaleYRange = emissionSettings.ScaleYRange;

            emissionSettingsSave.mMatchScaleXToY = emissionSettings.MatchScaleXToY;

            emissionSettingsSave.mScaleXVelocity = emissionSettings.ScaleXVelocity;
            emissionSettingsSave.mScaleYVelocity = emissionSettings.ScaleYVelocity;

            emissionSettingsSave.mScaleXVelocityRange = emissionSettings.ScaleXVelocityRange;
            emissionSettingsSave.mScaleYVelocityRange = emissionSettings.ScaleYVelocityRange;

            if (emissionSettings.TextureScale > 0)
            {
                emissionSettingsSave.TextureScale = emissionSettings.TextureScale;
            }
            else
            {
                emissionSettingsSave.TextureScale = -1;
            }

            int multiple = 255;
            emissionSettingsSave.mFade = (1 - emissionSettings.Alpha) * 255;
            emissionSettingsSave.mTintRed = emissionSettings.Red * 255;
            emissionSettingsSave.mTintGreen = emissionSettings.Green * 255;
            emissionSettingsSave.mTintBlue = emissionSettings.Blue * 255;

            emissionSettingsSave.mFadeRate = -emissionSettings.AlphaRate*255;
            emissionSettingsSave.mTintRedRate = emissionSettings.RedRate*255;
            emissionSettingsSave.mTintGreenRate = emissionSettings.GreenRate*255;
            emissionSettingsSave.mTintBlueRate = emissionSettings.BlueRate*255;

            emissionSettingsSave.mBlendOperation =
                GraphicalEnumerations.BlendOperationToFlatRedBallMdxString(emissionSettings.BlendOperation);

            emissionSettingsSave.mColorOperation = GraphicalEnumerations.ColorOperationToFlatRedBallMdxString(
                emissionSettings.ColorOperation);

            // preserve animation  and texture settings
            emissionSettingsSave.mAnimate = emissionSettings.Animate;

            // TODO: Justin - not sure if we need to force relative here?
            emissionSettingsSave.AnimationChains = emissionSettings.AnimationChains.Name;
            emissionSettingsSave.CurrentChainName = emissionSettings.CurrentChainName;
            
            // TODO: Justin - not sure if we neet to force relative here?
            emissionSettingsSave.Texture = emissionSettings.Texture.Name;

            emissionSettingsSave.Instructions = InstructionBlueprintListSave.FromInstructionBlueprintList(emissionSettings.Instructions);
            foreach (InstructionSave blueprint in emissionSettingsSave.Instructions.Instructions)
            {
                if (GraphicalEnumerations.ConvertableSpriteProperties.Contains(blueprint.Member))
                {
                    blueprint.Value = (float)blueprint.Value * multiple;
                }
            }
            emissionSettingsSave.mDrag = emissionSettings.Drag;

            return emissionSettingsSave;

        }

        public void InvertHandedness()
        {
            mZVelocity = -mZVelocity;
            mZVelocityRange = -mZVelocityRange;

            mRotationY = -mRotationY;
            mRotationYVelocity = -mRotationYVelocity;

            mRotationYRange = -mRotationYRange;
            mRotationYVelocityRange = -mRotationYVelocityRange;

            mZAcceleration = -mZAcceleration;
            mZAccelerationRange = -mZAccelerationRange;

        }

        public EmissionSettings ToEmissionSettings(string contentManagerName)
        {
            EmissionSettings emissionSettings = new EmissionSettings();
            emissionSettings.VelocityRangeType = VelocityRangeType;

            emissionSettings.RadialVelocity = RadialVelocity;
            emissionSettings.RadialVelocityRange = RadialVelocityRange;

            emissionSettings.Billboarded = Billboarded;

            emissionSettings.XVelocity = XVelocity;
            emissionSettings.YVelocity = YVelocity;
            emissionSettings.ZVelocity = ZVelocity;
            emissionSettings.XVelocityRange = XVelocityRange;
            emissionSettings.YVelocityRange = YVelocityRange;
            emissionSettings.ZVelocityRange = ZVelocityRange;

            emissionSettings.WedgeAngle = WedgeAngle;
            emissionSettings.WedgeSpread = WedgeSpread;

            emissionSettings.RotationX = RotationX;
            emissionSettings.RotationXVelocity = RotationXVelocity;
            emissionSettings.RotationXRange = RotationXRange;
            emissionSettings.RotationXVelocityRange = RotationXVelocityRange;

            emissionSettings.RotationY = RotationY;
            emissionSettings.RotationYVelocity = RotationYVelocity;
            emissionSettings.RotationYRange = RotationYRange;
            emissionSettings.RotationYVelocityRange = RotationYVelocityRange;

            emissionSettings.RotationZ = RotationZ;
            emissionSettings.RotationZVelocity = RotationZVelocity;
            emissionSettings.RotationZRange = RotationZRange;
            emissionSettings.RotationZVelocityRange = RotationZVelocityRange;

            emissionSettings.XAcceleration = XAcceleration;
            emissionSettings.YAcceleration = YAcceleration;
            emissionSettings.ZAcceleration = ZAcceleration;

            emissionSettings.XAccelerationRange = XAccelerationRange;
            emissionSettings.YAccelerationRange = YAccelerationRange;
            emissionSettings.ZAccelerationRange = ZAccelerationRange;

            emissionSettings.Drag = Drag;

            emissionSettings.ScaleX = ScaleX;
            emissionSettings.ScaleY = ScaleY;
            emissionSettings.ScaleXRange = ScaleXRange;
            emissionSettings.ScaleYRange = ScaleYRange;

            emissionSettings.ScaleXVelocity = ScaleXVelocity;
            emissionSettings.ScaleYVelocity = ScaleYVelocity;
            emissionSettings.ScaleXVelocityRange = ScaleXVelocityRange;
            emissionSettings.ScaleYVelocityRange = ScaleYVelocityRange;

            emissionSettings.MatchScaleXToY = MatchScaleXToY;

            if (TextureScale > 0)
            {
                emissionSettings.TextureScale = TextureScale;
            }
            else
            {
                emissionSettings.TextureScale = -1;
            }


            const float divisionValue = 255;
            emissionSettings.Alpha = (255 - Fade) / divisionValue;
            emissionSettings.Red = TintRed / divisionValue;
            emissionSettings.Green = TintGreen / divisionValue;
            emissionSettings.Blue = TintBlue / divisionValue;

            emissionSettings.AlphaRate = -FadeRate / divisionValue;
            emissionSettings.RedRate = TintRedRate / divisionValue;
            emissionSettings.BlueRate = TintBlueRate / divisionValue;
            emissionSettings.GreenRate = TintGreenRate / divisionValue;

            emissionSettings.BlendOperation = 
                GraphicalEnumerations.TranslateBlendOperation(BlendOperation);
            emissionSettings.ColorOperation =
                GraphicalEnumerations.TranslateColorOperation(ColorOperation);

            emissionSettings.Animate = Animate;
            emissionSettings.CurrentChainName = CurrentChainName;

            // load the animation chains or the texture but not both
            // chains take priority over texture?
            if (!string.IsNullOrEmpty(AnimationChains))
            {
                // load the animation chain, note that this could throw an exception if file names are bad or chain doesn't exist!
                emissionSettings.AnimationChains = FlatRedBallServices.Load<AnimationChainList>(AnimationChains, contentManagerName);
                if (!string.IsNullOrEmpty(CurrentChainName))
                {
                    emissionSettings.AnimationChain = emissionSettings.AnimationChains[emissionSettings.CurrentChainName];
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(Texture))
                {
                    // load the texture, 
                    emissionSettings.Texture = FlatRedBallServices.Load<Texture2D>(Texture, contentManagerName);
                }
            }

            if (Instructions != null)
            {
                emissionSettings.Instructions = Instructions.ToInstructionBlueprintList();

                foreach (InstructionBlueprint blueprint in emissionSettings.Instructions)
                {

                    if (GraphicalEnumerations.ConvertableSpriteProperties.Contains(blueprint.MemberName))
                    {
                        blueprint.MemberValue = (float)blueprint.MemberValue / divisionValue;
                    }

                    //if(blueprint.MemberName.Equals("Alpha")){

                    //}

                    //else if(blueprint.MemberName.Equals("AlphaRate")){

                    //}
                    //else if(blueprint.MemberName.Equals("Blue")){

                    //}
                    //else if(blueprint.MemberName.Equals("BlueRate")){

                    //}
                    //else if(blueprint.MemberName.Equals("Green")){

                    //}
                    //else if(blueprint.MemberName.Equals("GreenRate")){

                    //}
                    //else if(blueprint.MemberName.Equals("Red")){

                    //}
                    //else if(blueprint.MemberName.Equals("RedRate")){

                    //}
                }

            }

            // TODO:  Add support for saving AnimationChains.  Probably need to change the mAnimation
            // field from an AnimationChain to an AnimationChainSave

            return emissionSettings;
        }

        #endregion
    }
}
