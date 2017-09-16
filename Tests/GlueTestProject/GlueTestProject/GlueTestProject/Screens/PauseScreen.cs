using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;

using System.Linq;

using FlatRedBall.Math.Geometry;
using FlatRedBall.Math.Splines;

using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;
using FlatRedBall.Localization;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using GlueTestProject.Entities;
#endif

namespace GlueTestProject.Screens
{
	public partial class PauseScreen
	{

		void CustomInitialize()
		{

            // Let's make sure simply pausing and playing sounds works
#if !IOS
            // I could not get this to play.  I posted about it here:
            // http://community.monogame.net/t/error-while-playing-soundeffect/1605
            SoundEffectInstanceFile.Play();
#endif


#if !ANDROID && !IOS
            if (SoundEffectInstanceFile.State != Microsoft.Xna.Framework.Audio.SoundState.Playing)
            {
                throw new Exception("Playing a sound doesn't put it in the Playing state");
            }
#endif
            SoundEffectInstanceFile.Pause();

			#if !ANDROID && !IOS
            if (SoundEffectInstanceFile.State != Microsoft.Xna.Framework.Audio.SoundState.Paused)
            {
                throw new Exception("Pausing a sound doesn't put it in the Paused state");
            }
			#endif

#if !IOS

            SoundEffectInstanceFile.Play();
#endif

			#if !IOS && !ANDROID
            // This fails on iOS MonoGame - not a FRB failure.
            if (SoundEffectInstanceFile.State != Microsoft.Xna.Framework.Audio.SoundState.Playing)
            {
                throw new Exception("Playing a sound doesn't put it in the Playing state");
            }
#endif

            // Turns out that on XNA 4 PC the 
            // state doesn't immediately get set
            // to stopped when calling stop.  We just
            // have to deal with that I guess
            //SoundEffectInstanceFile.Stop(true);
            //if (SoundEffectInstanceFile.State != Microsoft.Xna.Framework.Audio.SoundState.Stopped)
            //{
            //    throw new Exception("Stopping a sound doesn't put it in the Stopped state");
            //}



            // Set some initial values
            this.CircleInstance.XVelocity = 1;
            this.AxisAlignedRectangleInstance.XVelocity = 1;
            this.SpriteInstance.XVelocity = 1;

#if !IOS
            // See above on why this doesn't work
            this.PausingEntityInstance.PlaySound();
#endif

            this.PauseThisScreen();

            // This used to be checked in CustomActivity but the sound
            // could actually finish playing before Activity gets called.
            // Bringing the test here means there's less code to run and it's
            // way less likely that the sound will finish playing.
            // Actually, it could be stopped too, so let's allow that:

            // I had to increase the sleep time as sometimes the sound effect doesn't get enough time to stop
            const int timeToSleep = 126;
#if WINDOWS_8

            new System.Threading.ManualResetEvent(false).WaitOne(timeToSleep);
#else
            System.Threading.Thread.Sleep(timeToSleep);
#endif
#if !ANDROID && !IOS
            if (PausingEntityInstance.IsSoundPaused == false || PausingEntityInstance.IsSoundStopped)
            {
                throw new Exception("Pausing an entity doesn't pause its sounds. The sleep time may need to be increased if this is hit...");
            }
			#endif

            float interpolationTime = .5f;

            this.InterpolateToState(VariableState.Before, VariableState.After,
                interpolationTime,
                FlatRedBall.Glue.StateInterpolation.InterpolationType.Exponential,
                FlatRedBall.Glue.StateInterpolation.Easing.Out);

            InterpolationEntityInstance.InterpolateToState(
                InterpolationEntity.VariableState.Small,
                InterpolationEntity.VariableState.Big,
                interpolationTime, FlatRedBall.Glue.StateInterpolation.InterpolationType.Exponential,
                FlatRedBall.Glue.StateInterpolation.Easing.Out);

            if (CircleInstance.XVelocity != 0)
            {
                throw new Exception("Circle XVelocity needs to be 0.");
            }
            if (AxisAlignedRectangleInstance.XVelocity != 0)
            {
                throw new Exception("AxisAlignedRectangleInstance XVelocity needs to be 0.");
            }
            if (SpriteInstance.XVelocity != 0)
            {
                throw new Exception("SpriteInstance XVelocity needs to be 0.");
            }
		}

        bool mHasPerformedCheck = false;
		void CustomActivity(bool firstTimeCalled)
		{


            if (this.ActivityCallCount > 45 && !mHasPerformedCheck)
            {
                mHasPerformedCheck = true;
                if (InterpolationEntityInstance.CurrentState != InterpolationEntity.VariableState.Big)
                {
                    throw new Exception("Interpolation should work when paused, but it isn't");
                }
                // Not sure if this is something we should allow or not...
                //if (this.CurrentState != VariableState.After)
                //{
                //    throw new Exception("Interpolation isn't working right....or is it?");
                //}
            }

            if (this.ActivityCallCount > 46)
            {
                // unpause and make sure all works
                UnpauseThisScreen();

                if (CircleInstance.XVelocity == 0)
                {
                    throw new Exception("Circle XVelocity needs to be restored when unpausing.");
                }
                if (AxisAlignedRectangleInstance.XVelocity == 0)
                {
                    throw new Exception("AxisAlignedRectangleInstance XVelocity needs to be restored when unpausing.");
                }
                if (SpriteInstance.XVelocity == 0)
                {
                    throw new Exception("SpriteInstance XVelocity needs to be restored when unpausing.");
                }

#if !IOS
                // This fails on iOS MonoGame - not a FRB failure.
                if (PausingEntityInstance.IsSoundPaused)
                {
                    throw new Exception("Unpausing an entity doesn't unpause its sound");
                }
#endif
                this.IsActivityFinished = true;
            }

		}

		void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
