using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Instructions;
#if FRB_MDX
using Texture2D = FlatRedBall.Texture2D;
using Microsoft.DirectX;

#else//if FRB_XNA || SILVERLIGHT
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#endif

namespace FlatRedBall.Graphics.Particle
{
    #region Enums

    public enum RangeType
    {
        #region XML Docs
        /// <summary>
        /// Each individual component (X, Y, Z) had an independent range.
        /// </summary>
        #endregion
        Component,

        #region XML Docs
        /// <summary>
        /// The X and Y components are set according to a random angle spanning a full circle and a radial velocity or rate value.
        /// </summary>
        #endregion
        Radial,

        #region XML Docs
        /// <summary>
        /// The X,Y,and Z components are set according to a random point on a full sphere and a radial velocity or rate value.
        /// </summary>
        #endregion
        Spherical,

        #region XML Docs
        /// <summary>
        /// The X and Y components are set according to a random angle within wedge values using a radial velocity or rate value.
        /// </summary>
        #endregion
        Wedge,

        #region XML Docs
        /// <summary>
        /// The X, Y, and Z components are set according to a random point on a 
        /// </summary>
        #endregion
        Cone
    }

    #endregion

    #region XML Docs
    /// <summary>
    /// An emitter is an invisible object which can create one or more Sprites at a specific 
    /// rate or on a method call.
    /// </summary>
    #endregion
    public class Emitter : PositionedObject, IEquatable<Emitter>, IReadOnlyScalable
    {
        #region Enums

        public enum RemovalEventType
        {
            /// <summary>
            /// No removal event specified.
            /// </summary>
            None,
            /// <summary>
            /// Particles will be removed when out of the screen.
            /// </summary>
            /// <remarks>
            /// This uses the camera's IsSpriteInView method.
            /// </remarks>
            OutOfScreen,
            /// <summary>
            /// Particles will be removed when Alpha is 0
            /// </summary>
            /// <remarks>
            /// Setting the Alpha to 0 manually on a particle created by an emitter with this removal event will
            /// also remove the Sprite.
            /// </remarks>
            Alpha0,
            /// <summary>
            /// Particles will be removed after a certain amount of time has passed after emission.  This value is set through the
            /// SecondsLasting property.
            /// </summary>
            Timed
        }

        public enum AreaEmissionType
        {
            Point,
            Rectangle,
            Cube
        }

        #endregion

        #region Fields

        private const float frameInterval = 1f / 30;
        
        // Justin Johnson 4/2/2015: retired particle blueprints and textures are now part of emission settings
        //Sprite mParticleBlueprint;

        //private Texture2D mTexture;
        
        FlatRedBall.Utilities.GameRandom mRandom;

        bool mEmitsAutomaticallyUpdatedSprites = true;

        EmissionSettings mEmissionSettings;

        #region Emission Bounding Control
        bool mBoundedEmission;
        Polygon mEmissionBoundary;
        #endregion

        #region Parent changing booleans

        bool mParentVelocityChangesEmissionVelocity;

        #endregion

        // modifications to emitters
        bool mRotationChangesParticleRotation = true;
        bool mRotationChangesParticleAcceleration = true;

        #region Area Emission
        AreaEmissionType mAreaEmission;
        float mScaleX;
        float mScaleY;
        float mScaleZ;
        #endregion

        bool mTimedEmission = false;

        internal float mSecondsLasting;
        internal float mSecondsModifier;

        Layer mLayerToEmitOn;

        #endregion

        #region Properties

        // Justin Johnson 04/2015: Textures are now part of the settings object
        //public Texture2D Texture
        //{
        //    get { return mTexture; }
        //    set { mTexture = value; }
        //}


        public bool EmitsAutomaticallyUpdatedSprites
        {
            get { return mEmitsAutomaticallyUpdatedSprites; }
            set { mEmitsAutomaticallyUpdatedSprites = value; }
        }

        public EmissionSettings EmissionSettings
        {
            get { return mEmissionSettings; }
            set { mEmissionSettings = value; }
        }

        // Justin Johnson 04/2015: Retired particle blueprint system
        // Animation chains are now part of the emission settings object
        //public FlatRedBall.Graphics.Animation.AnimationChainList AnimationChains
        //{
        //    get { return mParticleBlueprint.AnimationChains; }
        //    set { mParticleBlueprint.AnimationChains = value;}
        //}

        public string CurrentChainName
        {
            get { return mEmissionSettings.CurrentChainName ; }
            set { mEmissionSettings.CurrentChainName = value; }
        }

        private bool IsReadyForTimedEmission
        {
            get
            {
                return this.TimedEmission == true && // it is emitted by time.
                                TimeManager.CurrentTime - mLastTimeEmitted > mSecondFrequency && // it's time to emit
                                Parent is Emitter == false;
            }
        }

        /// <summary>
        /// If true, restricts this emitter to only emit when its EmissionBoundary is within the camera's view. This defaults to false, which means the emitter
        /// will ignore camera bounds when attempting to emit.
        /// </summary>
        /// <remarks>
        /// BoundedEmission can be used to improve the performance of your game, by reducing the number of off-screen particles which the engine has to manage.
        /// </remarks>
        /// <seealso cref="EmissionBoundary"/>
        public bool BoundedEmission
        {
            get { return mBoundedEmission; }
            set { mBoundedEmission = value; }
        }

        public Polygon EmissionBoundary
        {
            get { return mEmissionBoundary; }
            set { mEmissionBoundary = value; }
        }

        // Justin Johnson 04/2015: Retired particle blueprint system
        //public Sprite ParticleBlueprint
        //{
        //    get { return mParticleBlueprint; }
        //    set { mParticleBlueprint = value; }
        //}


        //public FlatRedBall.Audio.SoundEffectGroup EmissionSound
        //{
        //    get { return mSound; }
        //    set { mSound = value; }
        //}


        #region Scale values and areaEmissionType

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

        public float ScaleZ
        {
            get { return mScaleZ; }
            set { mScaleZ = value; }
        }

        #endregion

        public bool ParentVelocityChangesEmissionVelocity
        {
            get { return mParentVelocityChangesEmissionVelocity; }
            set { mParentVelocityChangesEmissionVelocity = value; }
        }


        public bool RotationChangesParticleRotation
        {
            get { return mRotationChangesParticleRotation; }
            set { mRotationChangesParticleRotation = value; }
        }

        public bool RotationChangesParticleAcceleration
        {
            get { return mRotationChangesParticleAcceleration; }
            set { mRotationChangesParticleAcceleration = value; }
        }


        public void SimulateEmit(double currentTime)
        {
            mLastTimeEmitted = currentTime;
        }


        public Layer LayerToEmitOn
        {
            get { return mLayerToEmitOn; }
            set { mLayerToEmitOn = value; }
        }

        #region emission event/time value

        /// <summary>
        /// Sets the area which defines where new particles can appear. 
        /// By default this is set to Point, which means
        /// emitted particles will appear at the Emitter's Position.
        /// </summary>
        /// <remarks>
        /// If the AreaEmission is set to Rectangle or Cube, then the ScaleX, ScaleY, and ScaleZ values are used.
        /// </remarks>
        public AreaEmissionType AreaEmission
        {
            get { return mAreaEmission; }
            set { mAreaEmission = value; }
        }

        double mLastTimeEmitted = 0;
        float mSecondFrequency = 1;
        public float SecondFrequency
        {
            get { return mSecondFrequency; }
            set { mSecondFrequency = value; }
        }

        private int mNumberPerEmission = 1;
        public int NumberPerEmission
        {
            get { return mNumberPerEmission; }
            set { mNumberPerEmission = value; }

        }

        /// <summary>
        /// Controls whether emission occurs on a timer when calling TimedEmit.
        /// </summary>
        /// <remarks>
        /// This variable is only useful when calling TimedEmit. It enables code to call TimedEmit
        /// in one area and to control the emission variable in another.
        /// </remarks>
        public bool TimedEmission
        {
            get { return mTimedEmission; }
            set { mTimedEmission = value; }
        }

        #endregion

        #region removal event

        // not sure why this was used, but we'll just take it out for now
        //		public customFunction removalFunction;

        /// <summary>
        /// Specifies the number of seconds that particles will remain on the screen and in memory.  This is only considered
        /// if the RemovalEvent is set to RemovalEventType.TIMED;
        /// </summary>
        public float SecondsLasting
        {
            get { return mSecondsLasting; }
            set { mSecondsLasting = value; }
        }

        public float SecondsModifier
        {
            get { return mSecondsModifier; }
            set { mSecondsModifier = value; }
        }


        internal RemovalEventType mRemovalEvent = RemovalEventType.None;
        /// <summary>
        /// Specifies the type of logic to perform for removing the particle.
        /// </summary>
        /// <seealso cref="SecondsLasting"/>
        public RemovalEventType RemovalEvent
        {
            get { return mRemovalEvent; }
            set
            {
                mRemovalEvent = value;
            }
        }


        #endregion

        #endregion

        #region Methods

        #region Constructor and Destructor

        public Emitter()
            : base()
        {
            mEmissionSettings = new EmissionSettings();
            mRandom = FlatRedBallServices.Random;

            // Justin Johnson, 04/2015 - retiring particle blueprints
            //ParticleBlueprint = new Sprite();

            mEmissionBoundary = Polygon.CreateRectangle(32, 32);
            mEmissionBoundary.AttachTo(this, false);

            mAreaEmission = AreaEmissionType.Point;

            this.ParentRotationChangesPosition = true;
            this.ParentRotationChangesRotation = true;
            mParentVelocityChangesEmissionVelocity = true;

            mSecondsModifier = 1.0f;

        }


        #endregion

        #region Public Methods

        public Emitter Clone()
        {
            Emitter tempEmitter = base.Clone<Emitter>();

            // Justin Johnson, 04/2015 - retiring particle blueprints
            //tempEmitter.ParticleBlueprint = this.ParticleBlueprint.Clone();

            tempEmitter.mInstructions = new FlatRedBall.Instructions.InstructionList();

            tempEmitter.EmissionSettings = EmissionSettings.Clone();

            tempEmitter.EmissionBoundary = EmissionBoundary.Clone<Polygon>();

            tempEmitter.BoundedEmission = BoundedEmission;

            return tempEmitter;
        }

        public void Emit()
        {
            Emit(null);
        }

        MethodInfo sSpriteManagerRemoveSprite = typeof(SpriteManager).GetMethod("RemoveSprite", new Type[] { typeof(Sprite) });

        // This is not thread safe:
        static AxisAlignedRectangle cameraRectangle = new AxisAlignedRectangle();

        /// <summary>
        /// Emits particles as specified by the Emitter class
        /// </summary>
        /// <remarks>
        /// This method initiates one emission of particles.  The number of particles emitted
        /// depends on the numberPerEmission variable.  This method does not consider emission timing, and the time
        /// is not recorded for emission timing.  
        /// 
        /// The argument SpriteList stores all Sprites which were emitted
        /// during the call.  The Sprites are added regularly, rather than in a one way relationship.  This enables
        /// modification of emitted particles after the method ends.  null can be passed as an argument
        /// if specific action is not needed for emitted particles.  Particles are automatically created
        /// through the SpriteManager as Particle Sprites.
        /// <seealso cref="FlatRedBall.Graphics.Particle.Emitter.TimedEmit()"/>
        /// <seealso cref="FlatRedBall.SpriteManager.AddParticleSprite"/>
        /// 
        /// </remarks>
        /// <param name="spriteList">The list of Sprites (which can be null) to add all Sprites created
        /// by this call.</param>
        public void Emit(SpriteList spriteList)
        {

            UpdateDependencies(TimeManager.CurrentTime);
            if (mBoundedEmission && mEmissionBoundary != null)
            {
                float cameraDistance;
#if FRB_MDX
                cameraDistance = -SpriteManager.Camera.Z;
#else
                cameraDistance = SpriteManager.Camera.Z;
#endif
                if (cameraDistance < 0) return; // skip check bounding if camera is behind z = 0 plain
                cameraRectangle.Position = new Vector3(SpriteManager.Camera.X, SpriteManager.Camera.Y, 0);
                cameraRectangle.ScaleY = SpriteManager.Camera.YEdge * cameraDistance / 100;
                cameraRectangle.ScaleX = cameraRectangle.ScaleY * SpriteManager.Camera.AspectRatio;
                if (!mEmissionBoundary.CollideAgainst(cameraRectangle))
                {
                    return;
                }
            }

            #region loop through creating NumberPerEmission particles

            for (int i = 0; i < NumberPerEmission; i++)
            {
                #region Create the tempParticle (the Sprite instance)

                Sprite tempParticle = null;

                if (mEmitsAutomaticallyUpdatedSprites)
                {
                    tempParticle = SpriteManager.AddParticleSprite(EmissionSettings.Texture);
                }
                else
                {
                    tempParticle = SpriteManager.AddManualParticleSprite(EmissionSettings.Texture);
                }

                #endregion

                #region Removal events
                switch ((int)this.RemovalEvent)
                {
                    case (int)RemovalEventType.Alpha0:
                        SpriteManager.mRemoveWhenAlphaIs0.Add(tempParticle);
                        break;
                    case (int)RemovalEventType.OutOfScreen:
                        tempParticle.CustomBehavior += FlatRedBall.Utilities.CustomBehaviorFunctions.RemoveWhenOutsideOfScreen;

                        break;
                    case (int)RemovalEventType.Timed:
                        // August 28, 2012
                        // The following code
                        // was added during Sentient
                        // to fix some issue with particles
                        // not being removed when paused.  Not
                        // sure what the problem was, but this bug
                        // causes particles to be immediately removed
                        // if they are spawned when the game is paused -
                        // at least most of the time.  The reason is because
                        // when the game is paused, then the CurrentTime is 0
                        // which means the particle is told to be removed at a 
                        // time very close to 0.  Usually this is in the past so
                        // any particles spawned when the game is paused will immediately
                        // be removed.  Not sure what the intent was here...
                        //double isEnginePaused = 1.0;
                        //if( InstructionManager.IsEnginePaused )
                        //{
                        //    isEnginePaused = 0.0;
                        //}                       
                        SpriteManager.mTimedRemovalList.InsertSorted(tempParticle,
                            TimeManager.CurrentTime + (mSecondsLasting * mSecondsModifier));
                        //                        SpriteManager.RemoveSpriteInstruction(tempParticle, TimeManager.CurrentTimeAfterXSeconds(SecondsLasting));
                        break;
                }
                #endregion

                if (tempParticle == null)
                {
                    // do something here with the SpriteManager.  Probably call some event
                }

                #region Sprite-specific stuff (not stuff that other types can do, which are handled below)
                // manually inlined for speed:

                //tempParticle.LeftTextureCoordinate = 0;
                tempParticle.mVertices[0].TextureCoordinate.X = 0;
                tempParticle.mVertices[3].TextureCoordinate.X = 0;

                //tempParticle.RightTextureCoordinate = 1;
                tempParticle.mVertices[1].TextureCoordinate.X = 1;
                tempParticle.mVertices[2].TextureCoordinate.X = 1;

                //tempParticle.TopTextureCoordinate = 0;
                tempParticle.mVertices[0].TextureCoordinate.Y = 0;
                tempParticle.mVertices[1].TextureCoordinate.Y = 0;

                // Use the setter here to call UpdateScale
                tempParticle.BottomTextureCoordinate = 1;

                // Justin Johnson, 04/2015 - retiring particle blueprints
                // Use the animation chain from the 
                // emission settings instead of setting
                // the animation based on chains and chain name
                if (EmissionSettings.AnimationChains != null)
                {
                    tempParticle.AnimationChains = EmissionSettings.AnimationChains;
                }
                tempParticle.SetAnimationChain(EmissionSettings.AnimationChain);
                tempParticle.CurrentFrameIndex = 0;
                tempParticle.Animate = EmissionSettings.Animate;

                //tempParticle.AnimationChains = mParticleBlueprint.AnimationChains;
                //if (!string.IsNullOrEmpty(mParticleBlueprint.CurrentChainName ))
                //{

                //    tempParticle.CurrentChainName = mParticleBlueprint.CurrentChainName;
                //    tempParticle.Animate = mParticleBlueprint.Animate;
                //    tempParticle.CurrentFrameIndex = 0;
                //    //                return;
                //}

                if (EmissionSettings.Billboarded)
                {
                    SpriteManager.Camera.AddSpriteToBillboard(tempParticle);
                }


                if (spriteList != null) spriteList.Add(tempParticle);

                if (mLayerToEmitOn != null)
                {
                    SpriteManager.AddToLayer(tempParticle, mLayerToEmitOn);
                }
                #endregion


                SetPositionedObjectProperties(tempParticle);
                //SetScalableProperties(tempParticle);
                SetSpriteScaleProperties(tempParticle);


                SetColorableProperties(tempParticle);


                // this sets the CustomBehavior to null so be sure not to call this after
                // setting any CustomBehavior.
                // Update on March 7, 2011:  CustomBehaviors are falling
                // out of style.  Particles no longer support them.
                // tempParticle.CopyCustomBehaviorFrom(mParticleBlueprint);

            }

            #endregion

        }

        public void SetColorableProperties(IColorable tempParticle)
        {
            #region Alpha, Color, and Operations

            tempParticle.Alpha = mEmissionSettings.Alpha;
            tempParticle.AlphaRate = mEmissionSettings.AlphaRate;
            tempParticle.Red = mEmissionSettings.Red;
            tempParticle.Green = mEmissionSettings.Green;
            tempParticle.Blue = mEmissionSettings.Blue;

            tempParticle.RedRate = mEmissionSettings.RedRate;
            tempParticle.GreenRate = mEmissionSettings.GreenRate;
            tempParticle.BlueRate = mEmissionSettings.BlueRate;

            tempParticle.ColorOperation = mEmissionSettings.ColorOperation;
            tempParticle.BlendOperation = mEmissionSettings.BlendOperation;

            #endregion
        }

        public void SetScalableProperties(IScalable scalable)
        {
            #region Scale and ScaleVelocity

            scalable.ScaleY = mEmissionSettings.ScaleY + (float)(mRandom.NextDouble() * mEmissionSettings.ScaleYRange);
            scalable.ScaleYVelocity = mEmissionSettings.ScaleYVelocity + (float)(mRandom.NextDouble() * mEmissionSettings.ScaleYVelocityRange); ;

            if (mEmissionSettings.MatchScaleXToY)
            {
                scalable.ScaleX = scalable.ScaleY;
                scalable.ScaleXVelocity = scalable.ScaleYVelocity;

            }
            else
            {
                scalable.ScaleX = mEmissionSettings.ScaleX + (float)(mRandom.NextDouble() * mEmissionSettings.ScaleXRange);
                scalable.ScaleXVelocity = mEmissionSettings.ScaleXVelocity + (float)(mRandom.NextDouble() * mEmissionSettings.ScaleXVelocityRange);
            }

            #endregion
        }

        public void SetSpriteScaleProperties(Sprite sprite)
        {
            SetScalableProperties(sprite);
            sprite.PixelSize = mEmissionSettings.PixelSize;
        }

        public void SetPositionedObjectProperties(PositionedObject positionedObject)
        {
            #region position

            if (mAreaEmission == AreaEmissionType.Rectangle)
            {
                positionedObject.X = (float)(-mScaleX + mRandom.NextDouble() * mScaleX * 2);
                positionedObject.Y = (float)(-mScaleY + mRandom.NextDouble() * mScaleY * 2);
                positionedObject.Z = 0;
            }
            else if (mAreaEmission == AreaEmissionType.Cube)
            {

                positionedObject.X = (float)(-mScaleX + mRandom.NextDouble() * mScaleX * 2);
                positionedObject.Y = (float)(-mScaleY + mRandom.NextDouble() * mScaleY * 2);
                positionedObject.Z = (float)(-mScaleZ + mRandom.NextDouble() * mScaleZ * 2);


            }
            else
            {
                positionedObject.Position.X = 0;
                positionedObject.Position.Y = 0;
                positionedObject.Position.Z = 0;
            }

            //positionedObject.Position -= Position;

            MathFunctions.TransformVector(ref positionedObject.Position, ref mRotationMatrix);

            positionedObject.Position.X += Position.X;
            positionedObject.Position.Y += Position.Y;
            positionedObject.Position.Z += Position.Z;

            #endregion

            #region Velocity

            //                tempParticle.drag = mEmissionSettings.Drag;

            switch (mEmissionSettings.VelocityRangeType)
            {
                case RangeType.Component:
                    positionedObject.Velocity.X = mEmissionSettings.XVelocity + (float)(mRandom.NextDouble() * mEmissionSettings.XVelocityRange);
                    positionedObject.Velocity.Y = mEmissionSettings.YVelocity + (float)(mRandom.NextDouble() * mEmissionSettings.YVelocityRange);
                    positionedObject.Velocity.Z = mEmissionSettings.ZVelocity + (float)(mRandom.NextDouble() * mEmissionSettings.ZVelocityRange);

                    break;
                case RangeType.Cone:
                    {
                        // currently there is a stronger concentration at the center
                        // The same code as the wedge, only rotate the velocity about the wedge vector by a random amount

                        float wedgeVelocityDirection = mEmissionSettings.WedgeAngle +
                            (float)(mRandom.NextDouble() * mEmissionSettings.WedgeSpread - mEmissionSettings.WedgeSpread / 2.0f);
                        float wedgeRadialVelocity = mEmissionSettings.RadialVelocity + (float)(mRandom.NextDouble() * mEmissionSettings.RadialVelocityRange);

                        positionedObject.Velocity.X = (float)(System.Math.Cos(wedgeVelocityDirection) * wedgeRadialVelocity);
                        positionedObject.Velocity.Y = (float)(System.Math.Sin(wedgeVelocityDirection) * wedgeRadialVelocity);

                        Vector3 vectorToRotateAbout = new Vector3(
                            (float)(System.Math.Cos(mEmissionSettings.WedgeAngle)),
                            (float)(System.Math.Sin(mEmissionSettings.WedgeAngle)),
                            0);

                        float angleToRotate = (float)(2 * System.Math.PI * mRandom.NextDouble());

                        Matrix rotationMatrix;


#if FRB_MDX
                            rotationMatrix = Matrix.RotationAxis(
                                vectorToRotateAbout, angleToRotate);
#else
                        Matrix.CreateFromAxisAngle(
                            ref vectorToRotateAbout,
                            angleToRotate,
                            out rotationMatrix);
#endif
                        FlatRedBall.Math.MathFunctions.TransformVector(
                            ref positionedObject.Velocity, ref rotationMatrix);
                        break;
                    }
                case RangeType.Radial:
                    float velocityDirection = (float)(2 * System.Math.PI * mRandom.NextDouble());
                    float radialVelocity = mEmissionSettings.RadialVelocity + (float)(mRandom.NextDouble() * mEmissionSettings.RadialVelocityRange);
                    positionedObject.Velocity.X = (float)(System.Math.Cos(velocityDirection) * radialVelocity);
                    positionedObject.Velocity.Y = (float)(System.Math.Sin(velocityDirection) * radialVelocity);

                    break;
                case RangeType.Spherical:
                    float xVelocity;
                    float yVelocity;
                    float zVelocity;

                    FlatRedBall.Math.MathFunctions.GetPointOnUnitSphere(mRandom, out xVelocity, out yVelocity,
                        out zVelocity);

                    float sphericalVelocity = mEmissionSettings.RadialVelocity +
                        (float)(mRandom.NextDouble() * mEmissionSettings.RadialVelocityRange);

                    positionedObject.Velocity.X = xVelocity * sphericalVelocity;
                    positionedObject.Velocity.Y = yVelocity * sphericalVelocity;
                    positionedObject.Velocity.Z = zVelocity * sphericalVelocity;

                    break;
                case RangeType.Wedge:
                    {
                        float wedgeVelocityDirection = mEmissionSettings.WedgeAngle +
                            (float)(mRandom.NextDouble() * mEmissionSettings.WedgeSpread - mEmissionSettings.WedgeSpread / 2.0f);
                        float wedgeRadialVelocity = mEmissionSettings.RadialVelocity + (float)(mRandom.NextDouble() * mEmissionSettings.RadialVelocityRange);

                        positionedObject.Velocity.X = (float)(System.Math.Cos(wedgeVelocityDirection) * wedgeRadialVelocity);
                        positionedObject.Velocity.Y = (float)(System.Math.Sin(wedgeVelocityDirection) * wedgeRadialVelocity);


                        break;
                    }
            }

            MathFunctions.TransformVector(ref positionedObject.Velocity, ref mRotationMatrix);

            #region consider parent velocity and rotation
            if (mParent != null)
            {
                if (ParentVelocityChangesEmissionVelocity)
                {
                    if (mParent.KeepTrackOfReal)
                    {
                        positionedObject.Velocity += mParent.RealVelocity;
                    }
                    else
                    {
                        positionedObject.Velocity.X += mParent.Velocity.X;
                        positionedObject.Velocity.Y += mParent.Velocity.Y;
                        positionedObject.Velocity.Z += mParent.Velocity.Z;
                    }
                }

            }
            #endregion


            #endregion

            #region Acceleration and Drag

            positionedObject.Acceleration.X = mEmissionSettings.XAcceleration + (float)(mRandom.NextDouble() * mEmissionSettings.XAccelerationRange);
            positionedObject.Acceleration.Y = mEmissionSettings.YAcceleration + (float)(mRandom.NextDouble() * mEmissionSettings.YAccelerationRange);
            positionedObject.Acceleration.Z = mEmissionSettings.ZAcceleration + (float)(mRandom.NextDouble() * mEmissionSettings.ZAccelerationRange);

            if (mRotationChangesParticleAcceleration)
            {
                MathFunctions.TransformVector(
                    ref positionedObject.Acceleration, ref mRotationMatrix);
            }

            positionedObject.Drag = mEmissionSettings.Drag;

            #endregion

            #region Rotation and rotational velocity

            positionedObject.RotationX = mEmissionSettings.RotationX + (float)(mRandom.NextDouble() * mEmissionSettings.RotationXRange);
            positionedObject.RotationXVelocity = mEmissionSettings.RotationXVelocity + (float)(mRandom.NextDouble() * mEmissionSettings.RotationXVelocityRange);

            positionedObject.RotationY = mEmissionSettings.RotationY + (float)(mRandom.NextDouble() * mEmissionSettings.RotationYRange);
            positionedObject.RotationYVelocity = mEmissionSettings.RotationYVelocity + (float)(mRandom.NextDouble() * mEmissionSettings.RotationYVelocityRange);

            positionedObject.RotationZ = mEmissionSettings.RotationZ + (float)(mRandom.NextDouble() * mEmissionSettings.RotationZRange);
            positionedObject.RotationZVelocity = mEmissionSettings.RotationZVelocity + (float)(mRandom.NextDouble() * mEmissionSettings.RotationZVelocityRange);

            if (mRotationChangesParticleRotation)
            {
                positionedObject.RotationMatrix *= RotationMatrix;
            }
            #endregion

            if (EmissionSettings.Instructions.Count != 0)
            {
                EmissionSettings.Instructions.FillInstructionList(positionedObject, TimeManager.CurrentTime, positionedObject.Instructions);

                //positionedObject.Instructions.AddRange(EmissionSettings.Instructions.BuildInstructionList(positionedObject, TimeManager.CurrentTime));
            }
        }

        public override void Pause(FlatRedBall.Instructions.InstructionList instructions)
        {
            FlatRedBall.Instructions.Pause.EmitterUnpauseInstruction instruction =
                new FlatRedBall.Instructions.Pause.EmitterUnpauseInstruction(this);

            instruction.Stop(this);

            instructions.Add(instruction);
        }

        public void TimedEmit()
        {
            TimedEmit(null);
        }

        /// <summary>
        /// Checks if the emitter is ready to emit, and if so, performs an emit. This method will only emit if 
        /// TimedEmission is set to true. Emitters which have TimedEmission set to true will typically have TimedEmit
        /// called every frame.
        /// </summary>
        /// <param name="spriteList"></param>
        public void TimedEmit(SpriteList spriteList)
        {

            if (IsReadyForTimedEmission)
            {
                Emit(spriteList);
                mLastTimeEmitted = TimeManager.CurrentTime;
            }

        }


        public void SimulatePriorEmissions(float secondsToSimulate)
        {

            //Don't worry about emissions that aren't timed
            if (mTimedEmission)
            {
                SpriteList particles = new SpriteList();
                float firstParticleTime = 0;
                Sprite currentparticle;
                float spawnTime;
                float secondsToSimulateThisParticle;

                int numberOfEmissions = (int)(secondsToSimulate / mSecondFrequency);

                for (float i = 0; i < numberOfEmissions; ++i)
                {

                    spawnTime = i * mSecondFrequency;
                    secondsToSimulateThisParticle = secondsToSimulate - spawnTime;

                    if (RemovalEvent == RemovalEventType.Timed && secondsToSimulateThisParticle >= SecondsLasting)
                    {
                        firstParticleTime += frameInterval;
                        continue;
                    }


                    {
                        Emit(particles);

                        //Update the TimeToExecute of each Instruction on a particle as they are created. That way
                        //the ACTUAL spawn time of the particle is taken into account
                        foreach (Instruction instruction in particles.Last.Instructions)
                        {
                            instruction.TimeToExecute += spawnTime;
                        }

                        if (RemovalEvent == RemovalEventType.Timed)
                        {
                            Sprite sprite = particles.Last;

                            DelegateInstruction delegateInstruction = new DelegateInstruction(() =>
                                {
                                    SpriteManager.RemoveSprite(sprite);

                                });

                            // we handle subtracting simulation time below.
                            //delegateInstruction.TimeToExecute = TimeManager.CurrentTime + SecondsLasting - secondsToSimulateThisParticle;
                            delegateInstruction.TimeToExecute = TimeManager.CurrentTime + spawnTime + SecondsLasting;
                            sprite.Instructions.Add(delegateInstruction);

                            // timed removal screws up the emitter list
                            //particles.Last.Instructions.Add( 
                            //                new MethodInstruction<Sprite>(
                            //               particles.Last, // Instance to Game1
                            //               "RemoveSelfFromListsBelongingTo", // Method to call
                            //               new object[0], // Argument List
                            //               TimeManager.CurrentTime + SecondsLasting - lifeSpan)); // When to call

                        }
                    }
                }
                int maxThisFrame;
                int removedModifier = 1;

                var numberOfIterations =
                    secondsToSimulate / frameInterval - firstParticleTime / frameInterval;

                var frameIntervalSquaredDividedBy2 = (frameInterval * frameInterval) / 2;

                for (int i = 0; i < numberOfIterations; ++i)
                {
                    maxThisFrame = removedModifier + (int)((i * frameInterval) / mSecondFrequency);
                    for (int j = 0; j < particles.Count && j < maxThisFrame; ++j)
                    {
                        currentparticle = particles[j];

                        currentparticle.TimedActivity(frameInterval, frameIntervalSquaredDividedBy2, 0);

                        //Check for instructions and execute when needed
                        if (currentparticle.Instructions.Count > 0 &&
                            currentparticle.Instructions[0].TimeToExecute <= (i * frameInterval))
                        {
                            currentparticle.Instructions[0].Execute();
                            currentparticle.Instructions.RemoveAt(0);
                        }

                        //Check for out of screen
                        if (RemovalEvent == RemovalEventType.OutOfScreen)
                        {
                            if (!SpriteManager.Camera.IsSpriteInView(currentparticle))
                            {
                                SpriteManager.RemoveSprite(currentparticle);
                                --j;
                                --removedModifier;
                            }
                        }

                        //Check for Alpha0
                        else if (RemovalEvent == RemovalEventType.Alpha0 &&
                                    currentparticle.Alpha <= 0)
                        {
                            SpriteManager.RemoveSprite(currentparticle);
                            --j;
                            --removedModifier;
                        }

                    }
                }

                foreach (Sprite sprite in particles)
                {
                    foreach (Instruction instruction in sprite.Instructions)
                    {
                        instruction.TimeToExecute -= secondsToSimulate;
                    }
                }
            }
        }



        #endregion

        #region Public Static Methods
        public static RemovalEventType TranslateRemovalEvent(string removalEvent)
        {
            switch (removalEvent)
            {
                case "None":
                case null:
                case "":
                    return RemovalEventType.None;
                case "OutOfScreen":
                    return RemovalEventType.OutOfScreen;
                case "Fadeout":
                case "Alpha0":
                    return RemovalEventType.Alpha0;
                case "Timed":
                    return RemovalEventType.Timed;
                default:
                    return RemovalEventType.None;
            }

        }

        public static string TranslateRemovalEvent(RemovalEventType removalEvent)
        {
            switch (removalEvent)
            {
                case RemovalEventType.None:
                    return "None";
                case RemovalEventType.OutOfScreen:
                    return "OutOfScreen";
                case RemovalEventType.Alpha0:
                    return "Fadeout";
                case RemovalEventType.Timed:
                    return "Timed";
                default:
                    return "None";
            }

        }

        #endregion

        #region Protected Methods



        #endregion

        #endregion

        #region IEquatable<Emitter> Members

        bool IEquatable<Emitter>.Equals(Emitter other)
        {
            return this == other;
        }

        #endregion
    }
}