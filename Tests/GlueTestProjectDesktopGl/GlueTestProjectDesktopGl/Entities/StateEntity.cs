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
using FlatRedBall.Instructions;
using GlueTestProject.TestFramework;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;


#endif

namespace GlueTestProject.Entities
{
	public partial class StateEntity
	{
        List<double> mValues = new List<double>();
        bool mIsVelocityTesting = false;
        double mTimeCreated;
        bool mHasCurrentAdvancedInterpolationEventBeenRaised = false;

        bool mHasCustomInitializeBeenCalled = false;

		private void CustomInitialize()
		{
            var variableState = new VariableState();
            var fieldThatShouldntExist = variableState.GetType().GetField("CurrentState");
            fieldThatShouldntExist.ShouldBe(null, "because a state should not set itself");

            mHasCustomInitializeBeenCalled = true;

            mTimeCreated = TimeManager.CurrentTime;
#if DEBUG
            // Interpolating using a NaN should throw an exception
            bool hasThrownException = false;
            try
            {
                this.InterpolateBetween(VariableState.First, VariableState.Second, float.NaN);
            }
            catch (Exception)
            {
                hasThrownException = true;
            }
            if (!hasThrownException)
            {
                throw new Exception("Generated code is not throwing an exception when iterpolating using a NaN interpolation value");
            }
#endif

            // Verify that InterpolateBetween propery sets visible on the circle


            RunInterpolateBetweenTests();

            this.CurrentInterpolationCategoryState = InterpolationCategory.Interpolate1;
            this.InterpolateToState(InterpolationCategory.Interpolate2, .1f);

            // Set it to false to be sure that it gets set to true in the event.
            mHasCurrentAdvancedInterpolationEventBeenRaised = false;


            // If this is missing, then the advanced state interpolation plugin is not installed, or the source library is missing :
            this.InterpolateToState(AdvancedInterpolationCategory.Advanced1,
                AdvancedInterpolationCategory.Advanced2, .1, 
                FlatRedBall.Glue.StateInterpolation.InterpolationType.Exponential, 
                FlatRedBall.Glue.StateInterpolation.Easing.Out);

        }

        private void RunInterpolateBetweenTests()
        {
            InterpolateBetween(CircleVisibility.CircleOff, CircleVisibility.CircleOn, 0);
            if (CircleObjectVisible)
            {
                throw new Exception("InterpolateBetween with a value of 0 does not properly set non-interpolatable values to their full values");
            }

            InterpolateBetween(CircleVisibility.CircleOff, CircleVisibility.CircleOn, 1);
            if (CircleObjectVisible == false)
            {
                throw new Exception("InterpolateBetween with a value of 1 does not properly set non-interpolatable values to their full values");
            }

            InterpolateBetween(CircleVisibility.CircleOff, CircleVisibility.CircleOn, .5f);
            if (CircleObjectVisible)
            {
                throw new Exception("InterpolateBetween with a value of .5 does not properly set non-interpolatable values to their full values");
            }

            InterpolateBetween(CircleVisibility.CircleOff, CircleVisibility.CircleOn, 0);
            if (CurrentCircleVisibilityState != CircleVisibility.CircleOff)
            {
                throw new Exception("InterpolateBetween with a value of 0 does not properly set the current state");
            }

            InterpolateBetween(CircleVisibility.CircleOff, CircleVisibility.CircleOn, 1);
            if (CurrentCircleVisibilityState != CircleVisibility.CircleOn)
            {
                throw new Exception("InterpolateBetween with a value of 1 does not properly set the current state");
            }

        }

		private void CustomActivity()
		{
            if (!mHasCustomInitializeBeenCalled)
            {
                throw new Exception("CustomInitialize isn't being called and it should be");
            }

            mValues.Add(LongVariableWithVelocityModifiedByVelocity);

            if (TimeManager.SecondsSince(mTimeCreated) > .5f)
            {
                if (CurrentInterpolationCategoryState != InterpolationCategory.Interpolate2)
                {
                    throw new Exception("States are not properly being set at the end of interpolation");
                }

                if (CurrentAdvancedInterpolationCategoryState != AdvancedInterpolationCategory.Advanced2)
                {
                    throw new Exception("States are not properly being set at the end of advanced interpolation");
                }

                if (mHasCurrentAdvancedInterpolationEventBeenRaised == false)
                {
                    throw new Exception("Events are not being raised when advanced interpolation finishes.");
                }
            }

		}

		private void CustomDestroy()
		{

            if (mIsVelocityTesting)
            {
                double timePassed = TimeManager.CurrentTime - mTimeStarted;

                long accumulation = FlatRedBall.Math.MathFunctions.RoundToLong(this.LongVariableWithVelocityVelocity * timePassed);

                if (System.Math.Abs(accumulation - this.LongVariableWithVelocity) > 1)
                {
                    throw new Exception("Accumulation for decimal values on whole number variables isn't working right");
                }
            }
		}

        private static void CustomLoadStaticContent(string contentManagerName)
        {


        }
        double mTimeStarted;
        public void StartVelocityTesting(float timeToTake)
        {
            mIsVelocityTesting = true;

            this.IntVariableWithVelocityVelocity = 6;

            this.Call(CheckVelocityIsApplying).After(timeToTake);


            this.LongVariableWithVelocityModifiedByVelocity = 1234;

            this.LongVariableWithVelocity = 0;

            if (this.LongVariableWithVelocityModifiedByVelocity > 1)
            {
                throw new Exception("Setting a value does not reset the ModifiedByVelocity value");
            }
            LongVariableWithVelocityVelocity = 3;
            mTimeStarted = TimeManager.CurrentTime;
        }


        void CheckVelocityIsApplying()
        {
            if (this.IntVariableWithVelocity == 0)
            {
                throw new Exception("Velocity is not being applied properly");
            }

        }
	}
}
