using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;

using FlatRedBall.Math.Geometry;
using FlatRedBall.Math.Splines;
using BitmapFont = FlatRedBall.Graphics.BitmapFont;
using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;
using GlueTestProject.DataTypes;
#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using FlatRedBall.Screens;
#elif FRB_MDX
using Keys = Microsoft.DirectX.DirectInput.Key;


#endif



namespace GlueTestProject.Entities
{
    public enum MovementType
    {
        Ground,
        Air,
        AfterDoubleJump
    }

    public enum HorizontalDirection
    {
        Left,
        Right
    }

	public partial class PlatformerCharacterBase
    {
        #region Fields

        bool mIsOnGround = false;
        bool mHitHead = false;

        bool mHasDoubleJumped = false;

        double mTimeJumpPushed = double.NegativeInfinity;
        MovementValues mValuesJumpedWith;

        MovementValues mCurrentMovement;
        HorizontalDirection mDirectionFacing;

        MovementType mMovementType;

        double mLastCollisionTime = -1;
        #endregion

        #region Properties

        double CurrentTime
        {
            get
            {
                if (ScreenManager.CurrentScreen != null)
                {
                    return ScreenManager.CurrentScreen.PauseAdjustedCurrentTime;
                }
                else
                {
                    return TimeManager.CurrentTime;
                }
            }
        }

        MovementValues CurrentMovement
        {
            get
            {
                return mCurrentMovement;
            }
        }

        HorizontalDirection DirectionFacing
        {
            get
            {
                return mDirectionFacing;
            }
        }

		public FlatRedBall.Input.IPressableInput JumpInput
		{
			get;
			set;
		}
        public FlatRedBall.Input.I1DInput HorizontalInput
        {
            get;
            set;
        }
	    protected virtual float HorizontalRatio
        {
            get
            {
                if(!InputEnabled)
                {
                    return 0;
                }
                else
                {
                    return HorizontalInput.Value;
                }
            }

        }

        public bool IsOnGround
        {
            get
            {
                return mIsOnGround;
            }
        }

        public MovementType CurrentMovementType
        {
            get
            {
                return mMovementType;
            }
            set
            {
                mMovementType = value;

                switch (mMovementType)
                {
                    case MovementType.Ground:
                        mCurrentMovement = GroundMovement;
                        break;
                    case MovementType.Air:
                        mCurrentMovement = AirMovement;
                        break;
                    case MovementType.AfterDoubleJump:
                        mCurrentMovement = AfterDoubleJump;
                        break;
                }
            }
        }

        public bool InputEnabled
        {
            get;
            set;
        }

        #endregion

        private void CustomInitialize()
		{
			if (FlatRedBall.Input.InputManager.Xbox360GamePads [0].IsConnected)
			{
				this.JumpInput = 
					FlatRedBall.Input.InputManager.Xbox360GamePads [0].GetButton (FlatRedBall.Input.Xbox360GamePad.Button.A);

                this.HorizontalInput =
                    FlatRedBall.Input.InputManager.Xbox360GamePads[0].LeftStick.Horizontal;
			}
			else
			{
				this.JumpInput = 
					FlatRedBall.Input.InputManager.Keyboard.GetKey (Keys.Space);

                this.HorizontalInput =
                    FlatRedBall.Input.InputManager.Keyboard.Get1DInput(Keys.Left, Keys.Right);
			}

            InputEnabled = true;

            CurrentMovementType = MovementType.Ground;

            YAcceleration = this.CurrentMovement.Gravity;

		}

		private void CustomActivity()
		{
            InputActivity();

		}

        private void InputActivity()
        {
            HorizontalMovementActivity();

            JumpVelocity();
        }

        private void JumpVelocity()
        {
			bool jumpPushed = JumpInput.WasJustPressed && InputEnabled;
			bool jumpDown = JumpInput.IsDown && InputEnabled;


            if (jumpPushed && 
                CurrentMovement.JumpVelocity > 0 &&
                (mIsOnGround || AfterDoubleJump == null || (AfterDoubleJump != null && mHasDoubleJumped == false))
                
                )
            {

                mTimeJumpPushed = CurrentTime;
                this.YVelocity = CurrentMovement.JumpVelocity;
                mValuesJumpedWith = CurrentMovement;

                if (CurrentMovementType == MovementType.Air)
                {
                    mHasDoubleJumped = true ;
                }
            }

            double secondsSincePush = CurrentTime - mTimeJumpPushed;

            if (mValuesJumpedWith != null && 
                secondsSincePush < mValuesJumpedWith.JumpApplyLength &&
				(mValuesJumpedWith.JumpApplyByButtonHold == false || JumpInput.IsDown)
                )
            {
                this.YVelocity = mValuesJumpedWith.JumpVelocity;

            }

            if (mValuesJumpedWith != null && mValuesJumpedWith.JumpApplyByButtonHold &&
				(!JumpInput.IsDown || mHitHead)
                )
            {
                mValuesJumpedWith = null;
            }

            this.YVelocity = Math.Max(-CurrentMovement.MaxFallSpeed, this.YVelocity);
        }


        private void HorizontalMovementActivity()
        {
            float horizontalRatio = HorizontalRatio;


            if(horizontalRatio > 0)
            {
                mDirectionFacing = HorizontalDirection.Right;
            }
            else if(horizontalRatio < 0)
            {
                mDirectionFacing = HorizontalDirection.Left;
            }

            if (this.CurrentMovement.AccelerationTimeX <= 0)
            {
                this.XVelocity = horizontalRatio * CurrentMovement.MaxSpeedX;
            }
            else
            {
                float acceleration = CurrentMovement.MaxSpeedX / CurrentMovement.AccelerationTimeX;

                float sign = Math.Sign(horizontalRatio);
                float magnitude = Math.Abs(horizontalRatio);

                if(sign == 0)
                {
                    sign = -Math.Sign(XVelocity);
                    magnitude = 1;
                }

                if (XVelocity == 0 || sign == Math.Sign(XVelocity))
                {
                    this.XAcceleration = sign * magnitude * CurrentMovement.MaxSpeedX / CurrentMovement.AccelerationTimeX;
                }
                else
                {
                    float accelerationValue = magnitude * CurrentMovement.MaxSpeedX / CurrentMovement.DecelerationTimeX;


                    if (Math.Abs(XVelocity) < accelerationValue * TimeManager.SecondDifference)
                    {
                        this.XAcceleration = 0;
                        this.XVelocity = 0;
                    }
                    else
                    {

                        // slowing down
                        this.XAcceleration = sign * accelerationValue;
                    }

                }

                XVelocity = Math.Min(XVelocity, CurrentMovement.MaxSpeedX);
                XVelocity = Math.Max(XVelocity, -CurrentMovement.MaxSpeedX);
            }
        }

		private void CustomDestroy()
		{


		}

        private static void CustomLoadStaticContent(string contentManagerName)
        {


        }

        public void CollideAgainst(ShapeCollection shapeCollection)
        {
            CollideAgainst(shapeCollection, false);
        }

        public void CollideAgainst(ShapeCollection shapeCollection, bool isCloudCollision)
        {
            CollideAgainst(() => shapeCollection.CollideAgainstBounceWithoutSnag(this.Collision, 0), isCloudCollision);
        }

        public void CollideAgainst(Func<bool> collisionFunction, bool isCloudCollision)
        {
            Vector3 positionBeforeCollision = this.Position;
            Vector3 velocityBeforeCollision = this.Velocity;

            float lastY = this.Y;

            bool isFirstCollisionOfTheFrame = TimeManager.CurrentTime != mLastCollisionTime;

            if (isFirstCollisionOfTheFrame)
            {
                mLastCollisionTime = TimeManager.CurrentTime;
                mIsOnGround = false;
                mHitHead = false;
            }

            if(isCloudCollision == false || velocityBeforeCollision.Y < 0)
            {

                if (collisionFunction())
                {
                    // make sure that we've been moved up, and that we're falling
                    bool shouldApplyCollision = true;
                    if (isCloudCollision)
                    {
                        if (this.Y <= positionBeforeCollision.Y)
                        {
                            shouldApplyCollision = false;
                        }
                    }

                    if (shouldApplyCollision)
                    {

                        if (this.Y > lastY)
                        {
                            mIsOnGround = true;
                        }
                        if (this.Y < lastY)
                        {
                            mHitHead = true;
                        }
                    }
                    else
                    {
                        Position = positionBeforeCollision;
                        Velocity = velocityBeforeCollision;
                    }
                }
            }
        }


        public virtual void DetermineMovementValues()
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
                if (CurrentMovementType == MovementType.Ground)
                {
                    CurrentMovementType = MovementType.Air;
                }

            }

            if (CurrentMovementType == MovementType.Air && mHasDoubleJumped)
            {
                CurrentMovementType = MovementType.AfterDoubleJump;
            }
        


        }

        public virtual void SetAnimations(AnimationChainList animations)
        {
            if(this.MainSprite != null)
            {
                string chainToSet = GetChainToSet();

                if(!string.IsNullOrEmpty(chainToSet))
                {
                    bool differs = MainSprite.CurrentChainName == null ||
                        MainSprite.CurrentChainName != chainToSet;

                    if(differs)
                    {
                        this.MainSprite.SetAnimationChain(animations[chainToSet]);
                    }
                }
            }
        }

        protected virtual string GetChainToSet()
        {
            string chainToSet = null;

            if (IsOnGround == false)
            {
                if (mDirectionFacing == HorizontalDirection.Right)
                {
                    if (this.YVelocity > 0)
                    {
                        chainToSet = "JumpRight";
                    }
                    else
                    {
                        chainToSet = "FallRight";
                    }
                }
                else
                {
                    if (this.YVelocity > 0)
                    {
                        chainToSet = "JumpLeft";
                    }
                    else
                    {
                        chainToSet = "FallLeft";
                    }
                }

            }
            else if (HorizontalRatio != 0)
            {
                if (mDirectionFacing == HorizontalDirection.Right)
                {
                    chainToSet = "WalkRight";
                }
                else
                {
                    chainToSet = "WalkLeft";
                }
            }
            else
            {
                if (mDirectionFacing == HorizontalDirection.Right)
                {
                    chainToSet = "StandRight";
                }
                else
                {
                    chainToSet = "StandLeft";
                }
            }
            return chainToSet;
        }

	}
}
