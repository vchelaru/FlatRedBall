using FlatRedBall.Glue.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.PlatformerPlugin.ViewModels;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;

namespace FlatRedBall.PlatformerPlugin.Generators
{
    public class EntityCodeGenerator : ElementComponentCodeGenerator
    {

        public override CodeLocation CodeLocation
        {
            get
            {
                return CodeLocation.AfterStandardGenerated;
            }
        }

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
        {
            ///////////////Early Out//////////////////////
            if (GetIfIsPlatformer(element) == false || GetIfInheritsFromPlatformer(element))
            {
                return codeBlock;
            }
            /////////////End Early Out////////////////////



            codeBlock.Line("#region Platformer Fields");

            codeBlock.Line("public bool IsPlatformingEnabled = true;");


            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// See property for information.");
            codeBlock.Line("/// </summary>");
            codeBlock.Line("bool mIsOnGround = false;");

            codeBlock.Line("bool mCanContinueToApplyJumpToHold = false;");



            codeBlock.Line("private float lastNonZeroPlatformerHorizontalMaxSpeed = 0;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// Whether the character has hit its head on a solid");
            codeBlock.Line("/// collision this frame. This typically occurs when the");
            codeBlock.Line("/// character is moving up in the air. It is used to prevent");
            codeBlock.Line("/// upward velocity from being applied while the player is");
            codeBlock.Line("/// holding down the jump button.");
            codeBlock.Line("/// </summary>");
            codeBlock.Line("bool mHitHead = false;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The current slope that the character is standing or walking on in degrees relative");
            codeBlock.Line("/// to the direction that the character is facing. In other words, if the charater is");
            codeBlock.Line("/// walking uphill to the right (positive slope), if the character turns around the value");
            codeBlock.Line("/// will be negative.");
            codeBlock.Line("/// </summary>");
            codeBlock.Line("float currentSlope = 0;");

            codeBlock.Line("bool mHasDoubleJumped = false;");
            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// Whether the character is in the air and has double-jumped.");
            codeBlock.Line("/// This is used to determine which movement variables are active,");
            codeBlock.Line("/// effectively preventing multiple double-jumps.");
            codeBlock.Line("/// </summary>");
            codeBlock.Line("public bool HasDoubleJumped");
            codeBlock.Line("{");
            codeBlock.Line("    get => mHasDoubleJumped;");
            codeBlock.Line("    set { mHasDoubleJumped = value; DetermineMovementValues(); }");
            codeBlock.Line("}");



            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The time when the jump button was last pushed. This is used to");
            codeBlock.Line("/// determine if upward velocity should be applied while the user");
            codeBlock.Line("/// holds the jump button down.");
            codeBlock.Line("/// </summary>");
            codeBlock.Line("double mTimeJumpPushed = double.NegativeInfinity;");


            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// See property for information.");
            codeBlock.Line("/// </summary>");
            codeBlock.Line("DataTypes.PlatformerValues mCurrentMovement;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// See property for information.");
            codeBlock.Line("/// </summary>");
            codeBlock.Line("HorizontalDirection mDirectionFacing;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// See property for information.");
            codeBlock.Line("/// </summary>");
            codeBlock.Line("MovementType mMovementType;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The last time collision checks were performed. Time values uniquely");
            codeBlock.Line("/// identify a game frame, so this is used to store whether collisions have");
            codeBlock.Line("/// been tested this frame or not. This is used to determine whether collision");
            codeBlock.Line("/// variables should be reset or not when a collision method is called, as");
            codeBlock.Line("/// multiple collisions (such as vs. solid and vs. cloud) may occur in one frame.");
            codeBlock.Line("/// </summary>");
            codeBlock.Line("double mLastCollisionTime = -1;");
            codeBlock.Line("#endregion");

            codeBlock.Line("public Microsoft.Xna.Framework.Vector3 PositionBeforeLastPlatformerCollision;");


            codeBlock.Line("#region Platformer Properties");


            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The MovementValues which were active when the user last jumped.");
            codeBlock.Line("/// These are used to determine the upward velocity to apply while");
            codeBlock.Line("/// the user holds the jump button.");
            codeBlock.Line("/// </summary>");
            codeBlock.Line("public DataTypes.PlatformerValues ValuesJumpedWith { get; set; }");

            codeBlock.Line("public bool WasOnGroundLastFrame{ get; private set; }");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// Whether the platformer object is forcefully ignoring cloud collision.");
            codeBlock.Line("/// Setting this to true will disable all cloud collision. Note that cloud collision");
            codeBlock.Line("/// may still be temporarily ignored due to internal logic, such as when holding down");
            codeBlock.Line("/// and pressing jump, but if so that will not be reflected in this property.");
            codeBlock.Line("/// </summary>"); 
            codeBlock.Line("public bool ForceIgnoreCloudCollision{ get; set; }");

            codeBlock.Property("public FlatRedBall.Input.IInputDevice", "InputDevice")
                .Line("get;")
                .Line("private set;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// Returns the current time, considering whether a Screen is active. ");
            codeBlock.Line("/// This is used to control how long a user can hold the jump button during");
            codeBlock.Line("/// a jump to apply upward velocity.");
            codeBlock.Line("/// </summary>");
            codeBlock.Property("double", "CurrentTime")
                .Get()
                    .If("FlatRedBall.Screens.ScreenManager.CurrentScreen != null")
                        .Line("return FlatRedBall.Screens.ScreenManager.CurrentScreen.PauseAdjustedCurrentTime;")
                    .End()
                    .Else()
                        .Line("return FlatRedBall.TimeManager.CurrentTime;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The current movement variables used for horizontal movement and jumping.");
            codeBlock.Line("/// These automatically get set according to the default platformer logic and should");
            codeBlock.Line("/// not be manually adjusted.");
            codeBlock.Line("/// </summary>");
            codeBlock.Property("protected DataTypes.PlatformerValues", "CurrentMovement")
                .Get()
                    .Line("return mCurrentMovement;");

            codeBlock.Line("public string CurrentMovementName => CurrentMovement?.Name;");

            codeBlock.Line("public float MaxAbsoluteXVelocity => CurrentMovement?.MaxSpeedX ?? 0;");
            codeBlock.Line("public float MaxAbsoluteYVelocity => CurrentMovement?.MaxFallSpeed ?? 0;");

        codeBlock.Line("/// <summary>");
            codeBlock.Line("/// Which direction the character is facing. This can be explicity set in code, but may get overridden by the current InputDevice.");
            codeBlock.Line("/// </summary>");
            codeBlock.Property("public HorizontalDirection", "DirectionFacing")
                .Get()
                    .Line("return mDirectionFacing;").End()
                .Set()
                    .Line("mDirectionFacing = value;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The input object which controls whether the jump was pressed.");
            codeBlock.Line("/// Common examples include a button or keyboard key.");
            codeBlock.Line("/// </summary>");
            codeBlock.AutoProperty("public FlatRedBall.Input.IPressableInput", "JumpInput");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The input object which controls the horizontal movement of the character.");
            codeBlock.Line("/// Common examples include a d-pad, analog stick, or keyboard keys.");
            codeBlock.Line("/// </summary>");
            codeBlock.AutoProperty("public FlatRedBall.Input.I1DInput", "HorizontalInput");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The input object which controls vertical input such as moving on ladders or falling through cloud collision.");
            codeBlock.Line("/// -1 represents full down, 0 is neutral, +1 is full up.");

            codeBlock.Line("/// </summary>");
            codeBlock.AutoProperty("public FlatRedBall.Input.I1DInput", "VerticalInput");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The ratio that the horizontal input is being held.");
            codeBlock.Line("/// -1 represents full left, 0 is neutral, +1 is full right.");
            codeBlock.Line("/// </summary>");
            codeBlock.Property("protected virtual float", "HorizontalRatio")
                .Get()
                    .If("!InputEnabled")
                        .Line("return 0;")
                    .End()
                    .Else()
                        .Line("return HorizontalInput.Value;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// Whether the character is on the ground. This is false");
            codeBlock.Line("/// if the character has jumped or walked off of the edge");
            codeBlock.Line("/// of a platform.");
            codeBlock.Line("/// </summary>");
            codeBlock.Property("public bool", "IsOnGround")
                .Get()
                    .Line("return mIsOnGround;");


            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The current movement type. This is set by the default platformer logic and");
            codeBlock.Line("/// is used to assign the mCurrentMovement variable.");
            codeBlock.Line("/// </summary>");
            codeBlock.Property("public MovementType", "CurrentMovementType")
                .Get()
                    .Line("return mMovementType;")
                .End()
                .Set()
                    .Line("mMovementType = value;")
                    .Line("UpdateCurrentMovement();");
                    


            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// Whether input is read to control the movement of the character.");
            codeBlock.Line("/// This can be turned off if the player should not be able to control");
            codeBlock.Line("/// the character.");
            codeBlock.Line("/// </summary>");
            codeBlock.AutoProperty("public bool", "InputEnabled");

            codeBlock.Line("float groundHorizontalVelocity = 0;");
            codeBlock.Line("public float GroundHorizontalVelocity => groundHorizontalVelocity;");

            // This can be used to add anything else here without the complexity of CodeBlock calls
            codeBlock.Line(
@"

            /// <summary>
            /// Stores the value that the entity must fall down to before cloud collision is re-enabled.
            /// If this value is null, then the player should perform normal cloud collision.
            /// When the entity falls through a
            /// cloud (by pressing down direction + jump), then this value is set to a non-null value. 
            /// </summary>
            private float? cloudCollisionFallThroughY = null;

            public float? TopOfLadderY { get; set; }

");




            codeBlock.Line("#endregion");

            codeBlock.Line(
@"
        /// <summary>
        /// Action for when the character executes a jump.
        /// </summary>
        public System.Action JumpAction;

        /// <summary>
        /// Action for when the character lands from a jump.
        /// </summary>
        public System.Action LandedAction;

");



            if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.ICollidableHasItemsCollidedAgainst)
            {
                codeBlock.Line("public HashSet<string> GroundCollidedAgainst { get; private set;} = new HashSet<string>();");
            }


            return codeBlock;
        }

        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, IElement element)
        {
            ///////////////////Early Out///////////////////////////////
            if(!GetIfIsPlatformer(element) || GetIfInheritsFromPlatformer(element))
            {
                return codeBlock;
            }
            /////////////////End Early Out/////////////////////////////

            codeBlock.Line(
@"
            // this provides default controls for the platformer using either keyboad or 360. Can be overridden in custom code:
            this.InitializeInput();

            // needed to figure out corner collisions
            KeepTrackOfReal = true;

            BeforeGroundMovementSet += (newValue) => 
            {
                if(mGroundMovement != null && mGroundMovement == ValuesJumpedWith && IsOnGround)
                {
                    ValuesJumpedWith = newValue;
                }
            };

            BeforeAirMovementSet += (newValue) => 
            {
                if(mAirMovement != null && mAirMovement == ValuesJumpedWith)
                {
                    ValuesJumpedWith = newValue;
                }
            };

            BeforeAfterDoubleJumpSet += (newValue) =>  
            {
                if(mAfterDoubleJump != null && mAfterDoubleJump == ValuesJumpedWith)
                {
                    ValuesJumpedWith = newValue;
                }
            };
            
            AfterGroundMovementSet += (not, used) => UpdateCurrentMovement();
            AfterAirMovementSet += (not, used) => UpdateCurrentMovement();
            AfterAfterDoubleJumpSet += (not, used) => UpdateCurrentMovement();
");
            return codeBlock;
        }

        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, IElement element)
        {
            ///////////////////Early Out///////////////////////////////
            if(!GetIfIsPlatformer(element))
            {
                return codeBlock;
            }
            /////////////////End Early Out/////////////////////////////

            codeBlock.Line("CurrentMovementType = MovementType.Ground;");
            return codeBlock;
        }

        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, IElement element)
        {
            ///////////////////Early Out///////////////////////////////
            if(!GetIfIsPlatformer(element) || GetIfInheritsFromPlatformer(element))
            {
                return codeBlock;
            }
            /////////////////End Early Out/////////////////////////////


            #region Huge Code Block

            codeBlock.Line(
@"

        private void UpdateCurrentMovement()
        {
            if(IsPlatformingEnabled == false)
            {
                return;
            }

            if(mCurrentMovement ?.MaxSpeedX > 0)
            {
                lastNonZeroPlatformerHorizontalMaxSpeed = mCurrentMovement.MaxSpeedX;
            }

            switch (mMovementType)
            {
                case MovementType.Ground:
                    mCurrentMovement = GroundMovement;
                    break;
                case MovementType.Air:
                    mCurrentMovement = AirMovement;
                    break;
                case MovementType.AfterDoubleJump:

                    // The user could have double-jumped into a set of movement values that no longer support double jump.
                    // For example, double jump is supported in water, but once the user moves out of water, AfterDoubleJump
                    // might be set to null
                    if(AfterDoubleJump == null)
                    {
                        mCurrentMovement = AirMovement;
                    }
                    else
                    {
                        mCurrentMovement = AfterDoubleJump;
                    }

                    break;
            }

            if(CurrentMovement != null)
            {
                if(CurrentMovement.CanClimb)
                {
                    this.YAcceleration = 0;
                }
                else
                {
                    this.YAcceleration = -CurrentMovement.Gravity;
                }

                if(!CurrentMovement.UsesAcceleration)
                {
                    this.XAcceleration = 0;
                }
            }
        }


        #region Platformer Methods


        partial void CustomInitializePlatformerInput();


        public void InitializePlatformerInput(FlatRedBall.Input.IInputDevice inputDevice)
        {
            this.JumpInput = inputDevice.DefaultPrimaryActionInput;
            this.HorizontalInput = inputDevice.DefaultHorizontalInput;
            this.VerticalInput = inputDevice.DefaultVerticalInput;
            this.InputDevice = inputDevice;

            InputEnabled = true;
            CustomInitializePlatformerInput();
        }


        /// <summary>
        /// Reads all input and applies the read-in values to control
        /// velocity and character state.
        /// </summary>
        private void ApplyInput()
        {
#if DEBUG
            if(InputDevice == null)
            {
                throw new NullReferenceException(""The InputDevice must be set before activity is performed on this entity. This can be set in Glue or manually in code"");
            }
#endif

            ApplyHorizontalInput();

            ApplyClimbingInput();

            ApplyJumpInput();
        }

        /// <summary>
        /// Applies the horizontal input to control horizontal movement and state.
        /// </summary>
        private void ApplyHorizontalInput()
        {
            float horizontalRatio = HorizontalRatio;

#if DEBUG
            // Vic asks - TopDown doesn't crash here. Should platformer?
            if(CurrentMovement == null)
            {
                throw new InvalidOperationException(""You must set CurrentMovement variable (can be done in Glue)"");
            }

#endif
            if(horizontalRatio > 0)
            {
                mDirectionFacing = HorizontalDirection.Right;
            }
            else if(horizontalRatio < 0)
            {
                mDirectionFacing = HorizontalDirection.Left;
            }

            var maxSpeed = CurrentMovement.MaxSpeedX;

            var walkingUphill = (currentSlope > 0 && currentSlope < 90);

            if (CurrentMovement.UphillStopSpeedSlope != CurrentMovement.UphillFullSpeedSlope &&
                currentSlope >= (float)CurrentMovement.UphillFullSpeedSlope &&
                // make sure actually walking uphill:
                walkingUphill)
            {
                if ( currentSlope >= (float)CurrentMovement.UphillStopSpeedSlope)
                {
                    maxSpeed *= 0;
                }
                else
                {
                    var interpolationValue =
                        1 - (currentSlope - (float)CurrentMovement.UphillFullSpeedSlope) /
                        (float)(CurrentMovement.UphillStopSpeedSlope - CurrentMovement.UphillFullSpeedSlope);

                    maxSpeed *= interpolationValue;
                }
            }

            if ((this.CurrentMovement.AccelerationTimeX <= 0 && this.CurrentMovement.IsUsingCustomDeceleration == false)|| this.CurrentMovement.UsesAcceleration == false)
            {
                this.XVelocity = groundHorizontalVelocity + horizontalRatio * maxSpeed;
            }
            else
            {
                var desiredSpeed = groundHorizontalVelocity + horizontalRatio * maxSpeed;

                const float epsilon = .001f;

                var isSpeedingUp = 
                    (Math.Abs(XVelocity - groundHorizontalVelocity) < epsilon && Math.Abs(desiredSpeed - groundHorizontalVelocity) > epsilon) ||
                    ((desiredSpeed > 0 && XVelocity < desiredSpeed && XVelocity > 0) ||
                    (desiredSpeed < 0 && XVelocity > desiredSpeed && XVelocity < 0));
                
                var absoluteValueVelocityDifference = System.Math.Abs(desiredSpeed - XVelocity);
                
                float accelerationMagnitude = 0;
                
                if(isSpeedingUp && CurrentMovement.AccelerationTimeX != 0)
                {
                    accelerationMagnitude = maxSpeed / CurrentMovement.AccelerationTimeX;
                }
                else
                {
                     
                    if(System.Math.Abs(XVelocity) > this.CurrentMovement.MaxSpeedX && this.CurrentMovement.IsUsingCustomDeceleration)
                    {
                        accelerationMagnitude = this.CurrentMovement.CustomDecelerationValue;
                    }
                    // if slowing down and max speed is 0, use the last max speed
                    else if(maxSpeed == 0)
                    {
                        accelerationMagnitude = lastNonZeroPlatformerHorizontalMaxSpeed / CurrentMovement.DecelerationTimeX;
                    }
                    else
                    {
                        accelerationMagnitude = maxSpeed / CurrentMovement.DecelerationTimeX;
                    }
                }
                
                var perFrameVelocityChange = accelerationMagnitude * TimeManager.SecondDifference;
                
                if(perFrameVelocityChange > absoluteValueVelocityDifference)
                {
                    // make sure we don't overshoot:
                    accelerationMagnitude = absoluteValueVelocityDifference * (1 / TimeManager.SecondDifference);
                }
                
                this.XAcceleration = accelerationMagnitude * System.Math.Sign(desiredSpeed - XVelocity);
            }
            groundHorizontalVelocity = 0;
        }

        private void ApplyClimbingInput()
        {
            if(CurrentMovement.CanClimb)
            {
                var verticalInputValue = VerticalInput?.Value ?? 0;
                this.YVelocity = verticalInputValue * CurrentMovement.MaxClimbingSpeed;

                if(this.Y > TopOfLadderY)
                {
                    this.Y = TopOfLadderY.Value;
                    if(this.YVelocity > 0)
                    {
                        this.YVelocity = 0;
                    }
                }

            }

        }


        /// <summary>
        /// Applies the jump input to control vertical velocity and state.
        /// </summary>
        private void ApplyJumpInput()
        {
			bool jumpPushed = JumpInput.WasJustPressed && InputEnabled;
			bool jumpDown = JumpInput.IsDown && InputEnabled;

            if(jumpPushed && mIsOnGround && VerticalInput?.Value < -.5 && CurrentMovement.CanFallThroughCloudPlatforms && CurrentMovement.CloudFallThroughDistance > 0)
            {
                // try falling through the ground
                cloudCollisionFallThroughY = this.Y - CurrentMovement.CloudFallThroughDistance;
            }
            // Test for jumping up
            else if (jumpPushed && // Did the player push the jump button
                CurrentMovement.JumpVelocity > 0 &&
                (
                    mIsOnGround || 
                    CurrentMovement.CanClimb ||
                    AfterDoubleJump == null || 
				    (AfterDoubleJump != null && mHasDoubleJumped == false) ||
				    (AfterDoubleJump != null && AfterDoubleJump.JumpVelocity > 0)

				)
                
            )
            {
                cloudCollisionFallThroughY = null;

                mTimeJumpPushed = CurrentTime;
                this.YVelocity = CurrentMovement.JumpVelocity;
                ValuesJumpedWith = CurrentMovement;

                mCanContinueToApplyJumpToHold = true;

                if (JumpAction != null)
                {
                    JumpAction();
                }

                if (CurrentMovementType == MovementType.Air)
                {
                    // November 26, 2021
                    // We used to check if AfterDoubleJump 
                    // is null, and if so, throw an exception.
                    // In the past if the player wanted to jump
                    // in the air, a double jump was needed. But
                    // Now we want to support wall jumping, which means
                    // that the user can jump off walls and not have a double
                    // jump. Removing this error.
                    //if(AfterDoubleJump == null)
                    //{
                    //    throw new InvalidOperationException(""The player is attempting to perform a double-jump, "" +
                    //        ""but the AfterDoubleJump variable is not set. If you are using glue, select this entity and change the After Double Jump variable."");
                    //}
                    mHasDoubleJumped = true ;
                }
                if(CurrentMovementType == MovementType.Ground && CurrentMovement.CanClimb)
                {
                    // the user jumped off a vine. Force the user into air mode:
                    CurrentMovementType = MovementType.Air;
                }
            }


            double secondsSincePush = CurrentTime - mTimeJumpPushed;

            // This needs to be done before checking if the user can continue to apply jump to hold
            if (ValuesJumpedWith != null && ValuesJumpedWith.JumpApplyByButtonHold &&
				(!JumpInput.IsDown || mHitHead)
                )
            {
                mCanContinueToApplyJumpToHold = false;
            }

            if (ValuesJumpedWith != null && 
                mCanContinueToApplyJumpToHold &&
                secondsSincePush < ValuesJumpedWith.JumpApplyLength &&
				(ValuesJumpedWith.JumpApplyByButtonHold == true && JumpInput.IsDown)
                )
            {
                this.YVelocity = ValuesJumpedWith.JumpVelocity;
            }
            else
            {
                mCanContinueToApplyJumpToHold = false;
            }

            this.YVelocity = System.Math.Max(-CurrentMovement.MaxFallSpeed, this.YVelocity);
        }


        /// <summary>
        /// Assigns the current movement values based off of whether the user is on ground and has double-jumped or not.
        /// This is called automatically, but it can be overridden in derived classes to perform custom assignment of 
        /// movement types.
        /// </summary>
        protected virtual void DetermineMovementValues()
        {
            if (mIsOnGround)
            {
                mHasDoubleJumped = false;
                if (CurrentMovementType == MovementType.Air ||
                    CurrentMovementType == MovementType.AfterDoubleJump)
                {
                    CurrentMovementType = MovementType.Ground;
                }
            }
            else
            {
                if (CurrentMovementType == MovementType.Ground && !CurrentMovement.CanClimb)
                {
                    CurrentMovementType = MovementType.Air;
                }

            }

            if (CurrentMovementType == MovementType.Air && mHasDoubleJumped)
            {
                CurrentMovementType = MovementType.AfterDoubleJump;
            }
            // User may have set HasDoubleJumped to false
            else if (CurrentMovementType == MovementType.AfterDoubleJump && !mHasDoubleJumped)
            {
                CurrentMovementType = MovementType.Air;
            }

        }

        #endregion
");
            #endregion

            #region Collision Methods

            codeBlock.Line(
                @"

        /// <summary>
        /// Performs a standard solid collision against an ICollidable.
        /// </summary>
        public bool CollideAgainst(FlatRedBall.Math.Geometry.ICollidable collidable, bool isCloudCollision = false)
        {
            return CollideAgainst(collidable.Collision, isCloudCollision);
        }

        /// <summary>
        /// Performs a standard solid collision against a ShapeCollection.
        /// </summary>
        /// <param name=""shapeCollection""></param>
        public bool CollideAgainst(FlatRedBall.Math.Geometry.ShapeCollection shapeCollection)
        {
            return CollideAgainst(shapeCollection, false);
        }

        public bool CollideAgainst(FlatRedBall.Math.Geometry.AxisAlignedRectangle rectangle, bool isCloudCollision = false)
        {
            return CollideAgainst(() =>
            {
                var collided = rectangle.CollideAgainstBounce(this.Collision, 1, 0, 0);
                return (collided, rectangle);
            }, isCloudCollision);
        }

        /// <summary>
        /// Performs a solid or cloud collision against a ShapeCollection.
        /// </summary>
        /// <param name=""shapeCollection"">The ShapeCollection to collide against.</param>
        /// <param name=""isCloudCollision"">Whether to perform solid or cloud collisions.</param>
        public bool CollideAgainst(FlatRedBall.Math.Geometry.ShapeCollection shapeCollection, bool isCloudCollision)
        {
            return CollideAgainst(() =>
            {
                var collided = shapeCollection.CollideAgainstBounce(this.Collision, 1, 0, 0);
                PositionedObject lastCollided = null;
                if (shapeCollection.LastCollisionAxisAlignedRectangles.Count > 0) lastCollided = shapeCollection.LastCollisionAxisAlignedRectangles[0];
                if (shapeCollection.LastCollisionCircles.Count > 0) lastCollided = shapeCollection.LastCollisionCircles[0];
                if (shapeCollection.LastCollisionPolygons.Count > 0) lastCollided = shapeCollection.LastCollisionPolygons[0];
                // do we care about other shapes?
                return (collided, lastCollided);
            }, isCloudCollision);
        }

        /// <summary>
        /// Executes the collisionFunction to determine if a collision occurred, and if so, reacts
        /// to the collision by modifying the state of the object and raising appropriate events.
        /// This is useful for situations where custom collisions are needed, but then the standard
        /// behavior is desired if a collision occurs.
        /// </summary>
        /// <param name=""collisionFunction"">The collision function to execute.</param>
        /// <param name=""isCloudCollision"">Whether to perform cloud collision (only check when moving down)</param>
        public bool CollideAgainst(System.Func<(bool, PositionedObject)> collisionFunction, bool isCloudCollision, string objectName = null)
        {
            Microsoft.Xna.Framework.Vector3 positionBeforeCollision = this.Position;
            Microsoft.Xna.Framework.Vector3 velocityBeforeCollision = this.Velocity;

            float lastY = this.Y;

            bool isFirstCollisionOfTheFrame = FlatRedBall.TimeManager.CurrentTime != mLastCollisionTime;

            if (isFirstCollisionOfTheFrame)
            {
                // This was set here, but if it is, custom collision (called on non-active collision relationships) won't update 
                // this value and have it be set. Instead, we reset this after applying it
                //groundHorizontalVelocity = 0;
                WasOnGroundLastFrame = mIsOnGround;
                mLastCollisionTime = FlatRedBall.TimeManager.CurrentTime;
                PositionBeforeLastPlatformerCollision = this.Position;
                mIsOnGround = false;
                mHitHead = false;
            }

            if(cloudCollisionFallThroughY != null && this.Y < cloudCollisionFallThroughY)
            {
                cloudCollisionFallThroughY = null;
            }

            bool canCheckCollision = true;

            if(isCloudCollision)
            {
                // need to be moving down...
                // Update September 10, 2021
                // But what if we're standing 
                // on a platform that is moving
                // upward? This moving platform may
                // be a cloud collision, and we should
                // still perform collision. Therefore, we 
                // should probably do cloud collision if...
                // * We are on the ground OR moving downward 
                //    --and--
                // * Not ignoring fallthrough
                //canCheckCollision = velocityBeforeCollision.Y < 0 &&
                canCheckCollision = (velocityBeforeCollision.Y < 0 || WasOnGroundLastFrame) &&
                    // and not ignoring fallthrough
                    cloudCollisionFallThroughY == null;

                if(canCheckCollision)
                {
                    if(WasOnGroundLastFrame == false &&  VerticalInput?.Value < -.5 && CurrentMovement.CanFallThroughCloudPlatforms)
                    {
                        // User is in the air, holding 'down', and the current movement allows the user to fall through clouds
                        canCheckCollision = false;
                    }
                }

                if(ForceIgnoreCloudCollision)
                {
                    canCheckCollision = false;
                }
            }

            bool toReturn = false;

            if (canCheckCollision)
            {

                (bool didCollide, PositionedObject objectCollidedAgainst) = collisionFunction();
                if (didCollide)
                {
                    toReturn = true;

                    // make sure that we've been moved up, and that we're falling
                    bool shouldApplyCollision = true;
                    if (isCloudCollision)
                    {
                        var yReposition = this.Y - positionBeforeCollision.Y;
                        if (yReposition <= 0)
                        {
                            shouldApplyCollision = false;
                        }
                        else
                        {
                            // rewind 1 frame and see if the gaps are bigger than the reposition
                            var thisYDistanceMovedThisFrame = (TimeManager.SecondDifference * velocityBeforeCollision.Y);
                            var otherYDistanceMovedThisFrame = 0f;
                            if (objectCollidedAgainst != null)
                            {
                                otherYDistanceMovedThisFrame = (TimeManager.SecondDifference * objectCollidedAgainst.TopParent.YVelocity);
                            }

                            shouldApplyCollision = yReposition < (-thisYDistanceMovedThisFrame + otherYDistanceMovedThisFrame);
                        }
                    }

                    if (shouldApplyCollision)
                    {

                        if (this.Y > lastY)
                        {
                            if (!WasOnGroundLastFrame && LandedAction != null)
                            {
                                LandedAction();
                            }
                            mIsOnGround = true;");

            if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.ICollidableHasItemsCollidedAgainst)
            {
                codeBlock.Line(
@"
                    if(!string.IsNullOrEmpty(objectCollidedAgainst?.Name ?? objectName))
                    {
                        GroundCollidedAgainst.Add(objectCollidedAgainst?.Name ?? objectName);
                    }
");
            }


            codeBlock.Line(
@"

                            groundHorizontalVelocity = objectCollidedAgainst?.TopParent.XVelocity ?? 0;
                        }
                        if (this.Y < lastY)
                        {
                            mHitHead = true;
                        }
                        if(isCloudCollision)
                        {
                            this.X = positionBeforeCollision.X;
                            this.Velocity.X = velocityBeforeCollision.X;
                        }
                    }
                    else
                    {
                        Position = positionBeforeCollision;
                        Velocity = velocityBeforeCollision;
                    }
                }
            }


            // If a platformer object has multiple parts, one collision may move one set of shapes, but not the other
            // shapes. Need to force that update
            if(toReturn)
            {
                this.ForceUpdateDependenciesDeep();
            }

            return toReturn;
        }
"


                );


            bool hasTiledPlugin =
                Glue.Plugins.PluginManager.AllPluginContainers.Any(item => item.Name == "Tiled Plugin");

            if (hasTiledPlugin)
            {
                
                codeBlock.Line(@"


        (bool, PositionedObject) DoCollisionWithShapeCollectionConsideringCornerLanding(FlatRedBall.TileCollisions.TileShapeCollection shapeCollection, bool isCloudCollision)
        {
            var positionBefore = this.Position;
            var velocityBefore = this.Velocity;

            var didCollideInternal = shapeCollection.CollideAgainstSolid(this);
            if(isCloudCollision == false && didCollideInternal && velocityBefore.Y < 0)
            {
                var repositionDirection = this.Position - positionBefore;
                if(repositionDirection.X != 0)
                {
                    var positionAfter = this.Position;
                    var velocityAfter = this.Velocity;

                    // undo the last frame to figure out where the Y was for the player last frame:

                    var didRedoWithUpward = false;
                    var didTryCollision = true;

                    var didAnyRedoWithoutUpward = false;

                    for(int i = 0; i < shapeCollection.LastCollisionAxisAlignedRectangles.Count; i++)
                    {
                        var rectangle = shapeCollection.LastCollisionAxisAlignedRectangles[i];

                        var canRepositionHorizontally =
                            (rectangle.RepositionDirections & FlatRedBall.Math.Geometry.RepositionDirections.Left) == FlatRedBall.Math.Geometry.RepositionDirections.Left ||
                            (rectangle.RepositionDirections & FlatRedBall.Math.Geometry.RepositionDirections.Right) == FlatRedBall.Math.Geometry.RepositionDirections.Right;

                        if ((rectangle.RepositionDirections & FlatRedBall.Math.Geometry.RepositionDirections.Up) == FlatRedBall.Math.Geometry.RepositionDirections.Up &&
                            canRepositionHorizontally)
                        {
                            // Player was repositioned horizontally by a shape which can also reposition upward.
                            // Did they get pushed off a ledge?

                            // to test this, move the player back to original position, force an upward collision, and see if the resulting upward
                            // collision is <= the position before.
                            this.Position = positionBefore;
                            this.Velocity = velocityBefore;
                            this.ForceUpdateDependenciesDeep();

                            var oldReposition = rectangle.RepositionDirections;

                            rectangle.RepositionDirections = FlatRedBall.Math.Geometry.RepositionDirections.Up;

                            this.Collision.CollideAgainstBounce(rectangle, 0, 1, 0);
                            didTryCollision = true;
                            rectangle.RepositionDirections = oldReposition;

                            if(this.Y <= LastPosition.Y)
                            {
                                didRedoWithUpward = true;
                                //break;
                            }
                            else
                            {
                                didAnyRedoWithoutUpward = true;
                            }

                        }
                        else if(rectangle.RepositionDirections != FlatRedBall.Math.Geometry.RepositionDirections.None)
                        {
                            didAnyRedoWithoutUpward = true;
                            break;
                        }
                    }


                    if(didAnyRedoWithoutUpward)
                    {
                        this.Position = positionAfter;
                        this.Velocity = velocityAfter;
                    }

                    else if(!didRedoWithUpward && didTryCollision)
                    {
                        this.Position = positionAfter;
                        this.Velocity = velocityAfter;
                        this.ForceUpdateDependenciesDeep();
                    }
                }
            }
            return (didCollideInternal, null);
        }

        public bool CollideAgainst(FlatRedBall.TileCollisions.TileShapeCollection shapeCollection, bool isCloudCollision = false)
        {
            var positionBefore = this.Position;
            var velocityBefore = this.Velocity;


            var collided = CollideAgainst(() => DoCollisionWithShapeCollectionConsideringCornerLanding(shapeCollection, isCloudCollision), isCloudCollision, shapeCollection.Name);


            if(collided)
            {
                currentSlope = 0;
            }

            var wasMovedHorizontally = this.X != positionBefore.X;

            var wasSlowedByPolygons = wasMovedHorizontally && shapeCollection.LastCollisionPolygons.Count != 0;

            if(wasSlowedByPolygons)
            {
                var repositionVector = new Microsoft.Xna.Framework.Vector2(0, 1);
                foreach(var rect in this.Collision.AxisAlignedRectangles)
                {
                    if(rect.LastMoveCollisionReposition.X != 0)
                    {
                        repositionVector = rect.LastMoveCollisionReposition;
                        break;
                    }
                }
                var shouldPreserve = DetermineIfHorizontalVelocityShouldBePreserved(velocityBefore.X, shapeCollection, repositionVector);

                if(shouldPreserve)
                {
                    // This was an attempt to fix snagging...
                    // The problem is that when a rectangle collides
                    // against a polygon, the point on the polygon may
                    // be the one that defines the reposition direction.
                    // This means a rectangle could be sitting on a slope
                    // but still have a perfectly vertical reposition. I tried
                    // to fix this by only getting reposition vectors from the polygon
                    // but that caused the platformer to hop in place in some situations.

                    //float maxYMap = float.NegativeInfinity;
                    //for (int i = 0; i < shapeCollection.LastCollisionPolygons.Count; i++)
                    //{
                    //    var polygon = shapeCollection.LastCollisionPolygons[i];
                    //    for (int j = 0; j < polygon.Points.Count; j++)
                    //    {
                    //        maxYMap = Math.Max(maxYMap, polygon.AbsolutePointPosition(j).Y);
                    //    }
                    //}
                    //for(int i = 0; i < shapeCollection.LastCollisionAxisAlignedRectangles.Count; i++)
                    //{
                    //    var rectangle = shapeCollection.LastCollisionAxisAlignedRectangles[i];
                    //    maxYMap = Math.Max(maxYMap, rectangle.Y + rectangle.ScaleY);
                    //}


                    //float maxCollisionOffset = 0;
                    //foreach(var rectangle in this.Collision.AxisAlignedRectangles)
                    //{
                    //    maxCollisionOffset = -rectangle.RelativeY + rectangle.ScaleY;
                    //}

                    //float maxYAfterReposition = maxCollisionOffset + maxYMap;

                    // keep the velocity and the position:
                    var xDifference = positionBefore.X - this.Position.X;

                    var tangent = new Microsoft.Xna.Framework.Vector2(repositionVector.Y, -repositionVector.X);

                    currentSlope = Microsoft.Xna.Framework.MathHelper.ToDegrees( (float) System.Math.Atan2(tangent.Y, tangent.X));

                    if(DirectionFacing == HorizontalDirection.Left)
                    {
                        currentSlope *= -1;
                    }

                    var multiplier = xDifference / tangent.X;

                    this.Velocity.X = velocityBefore.X;
                    this.Position.X = positionBefore.X;
                    this.Position.Y += multiplier * tangent.Y;
                    //this.Position.Y = Math.Min(this.Position.Y, maxYAfterReposition);
                    this.ForceUpdateDependenciesDeep();
                }
            }
            return collided;
        }

        
        private bool DetermineIfHorizontalVelocityShouldBePreserved(float oldHorizontalVelocity, FlatRedBall.TileCollisions.TileShapeCollection shapeCollection, 
            Microsoft.Xna.Framework.Vector2 repositionVector)
        {
            const float maxSlope = 80; // degrees
            var maxSlopeInRadians = Microsoft.Xna.Framework.MathHelper.ToRadians(maxSlope);
            // The reposition is the normal of the slope, so it's the X
            // That is, on a slope like this:
            // \
            //  \
            //   \
            //    \
            //     \
            // If the slope ^^ is nearly 90, then the X will be nearly 1. To get that, we will do the sin of the slope

            var maxRepositionDirectionX = System.Math.Sin(maxSlopeInRadians);

            bool collidedWithSlopeGreaterThanMax = repositionVector.Y <= 0;

            if(collidedWithSlopeGreaterThanMax == false)
            {
                if(repositionVector.X != 0 || repositionVector.Y != 0)
                {
                    var normalized = Microsoft.Xna.Framework.Vector2.Normalize(repositionVector);

                    if(normalized.X > maxRepositionDirectionX || normalized.X < -maxRepositionDirectionX)
                    {
                        collidedWithSlopeGreaterThanMax = true;
                    }

                }
            }
            var shouldBePreserved = collidedWithSlopeGreaterThanMax == false;

            return shouldBePreserved;
        }


        public bool CollideAgainst(FlatRedBall.TileCollisions.TileShapeCollection shapeCollection, FlatRedBall.Math.Geometry.AxisAlignedRectangle thisCollision, bool isCloudCollision = false)
        {
            return CollideAgainst(() =>
            {
                var didCollide = shapeCollection.CollideAgainstSolid(thisCollision);
                return (didCollide, null);
            }, isCloudCollision, shapeCollection.Name);
        }

");
            }


            #endregion

            return base.GenerateAdditionalMethods(codeBlock, element);
        }

        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, IElement element)
        {
            ///////////////////Early Out///////////////////////////////
            if(!GetIfIsPlatformer(element) || GetIfInheritsFromPlatformer(element))
            {
                return codeBlock;
            }
            /////////////////End Early Out/////////////////////////////

            var ifBlock = codeBlock.If("IsPlatformingEnabled");
            ifBlock.Line("ApplyInput();");
            ifBlock.Line("DetermineMovementValues();");

            return base.GenerateActivity(codeBlock, element);
        }

        public static bool GetIfIsPlatformer(IElement element)
        {
            return element.Properties
                .GetValue<bool>(nameof(PlatformerEntityViewModel.IsPlatformer));
        }

        public static bool GetIfInheritsFromPlatformer(IElement element)
        {
            if(string.IsNullOrEmpty(element.BaseElement))
            {
                return false;
            }

            var allBase = ObjectFinder.Self.GetAllBaseElementsRecursively(element);

            return allBase.Any(GetIfIsPlatformer);
        }

    }
}
