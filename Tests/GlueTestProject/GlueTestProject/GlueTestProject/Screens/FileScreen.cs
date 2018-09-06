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

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using GlueTestProject.Entities;
using FlatRedBall.Content.AnimationChain;
#endif

namespace GlueTestProject.Screens
{
	public partial class FileScreen
	{
        bool hasHadAnyParticles = false;
		void CustomInitialize()
		{
            if (!ShapeManager.VisibleCircles.Contains(FileScreen.ShapeCollectionFile.Circles[0]))
            {
                throw new Exception("Static files that are part of Screens should be added to managers, but they're not.");
            }

            if (GetFile("ShapeCollectionFile") != ShapeCollectionFile)
            {
                throw new Exception("GetFile is not properly returning file references");
            }

            // We're going to grab content from here without actually calling LoadStaticContent
            FileReferencingEntity.ContentManagerName = this.ContentManagerName;
            Scene scene = FileReferencingEntity.SceneLoadedOnlyWhenReferenced;

            this.CurrentSceneFileSettingCategoryState = SceneFileSettingCategory.SetSceneFile1;
            if (this.SceneInstanceThatIsAssigned != SceneFile1)
            {
                throw new Exception("State assignment isn't working properly");
            }
            if (SpriteManager.AutomaticallyUpdatedSprites.Contains(this.SceneInstanceThatIsAssigned.Sprites[0]) == false)
            {
                throw new Exception("Setting a SourceFile does not seem to be calling AddToManagers");
            }

            if (SpriteManager.AutomaticallyUpdatedSprites.Contains(NotAddedToManagers.Sprites[0]))
            {
                throw new Exception("The NotAddedToManagers Sprite is being added to the engine, and it shouldn't be");
            }

            // Some platforms (like Android) lazy-load instead of async load content in GlobalContent if
            // GlobalContnt is set to load async.
            // Therefore, let's force access on some files:
            var bearFromContent = GlobalContent.BearContentPipeline;


            // let's test loading .achx files using the manual .achx parser. This allows PC to act the same
            // as iOS/Android:
            var old = AnimationChainListSave.ManualDeserialization;
            AnimationChainListSave.ManualDeserialization = true;
            const string tempContentManager = "TempContentManager";
            var animationEntity = new Entities.AnimationChainEntity(tempContentManager);
            if(animationEntity.SpriteObjectAnimationChains.Count == 0)
            {
                throw new Exception("Animation chains are not loading properly through manaul XML deserialization");
            }
            animationEntity.Destroy();
            FlatRedBallServices.Unload(tempContentManager);
            AnimationChainListSave.ManualDeserialization = old;

		}

		void CustomActivity(bool firstTimeCalled)
		{
            if(SpriteManager.ParticleCount > 0)
            {
                hasHadAnyParticles = true;
            }
            if(this.ActivityCallCount >= 40)
            {
                if (SpriteManager.ParticleCount == 0)
                {
                    throw new Exception($"The emitter file should be emitting, but it is not. Has had any particles: {hasHadAnyParticles}");
                }

                IsActivityFinished = true;
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
