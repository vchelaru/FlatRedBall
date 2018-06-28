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

using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;
using FlatRedBall.Localization;
using GlueTestProject.TestFramework;
using FlatRedBall.Content.AnimationChain;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using GlueTestProject.Entities;
using Microsoft.Xna.Framework.Media;
#endif

namespace GlueTestProject.Screens
{
	public partial class GlobalContentScreen
	{
        Song mMenuSongReference;
        EmitterList mEmitterList;
        int mEmitterListStartingCount;

		void CustomInitialize()
		{
            mMenuSongReference = MenuSong;
            mEmitterList = BackgroundEmitters;
            mEmitterListStartingCount = BackgroundEmitters.Count;

            TestAchxReloading();
		}

        private void TestAchxReloading()
        {
            var animationChain = GlobalContent.EmptyAnimationForReload;
            animationChain.Count.ShouldBe(0);

            animationChain.Add(new AnimationChain());
            animationChain.Count.ShouldBe(1);

            GlobalContent.Reload(GlobalContent.EmptyAnimationForReload);

            var reloaded = GlobalContent.EmptyAnimationForReload;
            reloaded.Count.ShouldBe(0);
        }

        void CustomActivity(bool firstTimeCalled)
		{

            if (!firstTimeCalled)
            {
                IsActivityFinished = true;
            }
		}

		void CustomDestroy()
		{
#if !MONOGAME

            if (GlobalContentEntity.burnOrb == null)
            {
                throw new Exception(
                    "The GlobalContentEntity was never loaded, which means the screne that contains it (FileScreen) was never created. Did the file order change?");

            }

            if (GlobalContentEntity.burnOrb.IsDisposed)
            {
                throw new Exception("Entities with GlobalContent are getting their content destroyed by Screens.");
            }

            if (mMenuSongReference.IsDisposed)
            {
                throw new Exception("Screen using GlobalContent are disposing their songs - they shouldn't");
            }
#endif
            if (mEmitterList.Count == 0)
            {
                throw new Exception("Emitter list is being cleared out when it shouldn't be.");
            }
            if (mEmitterList.Count != mEmitterListStartingCount)
            {
                throw new Exception("EmitterList loaded from .emix in global content file screen is being modified when removing objects from it.");
            }
		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
