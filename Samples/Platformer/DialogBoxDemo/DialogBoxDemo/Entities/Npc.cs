using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Math.Geometry;

namespace DialogBoxDemo.Entities
{
    public partial class Npc
    {
        AnimationController animationController;

        /// <summary>
        /// Initialization logic which is execute only one time for this Entity (unless the Entity is pooled).
        /// This method is called when the Entity is added to managers. Entities which are instantiated but not
        /// added to managers will not have this method called.
        /// </summary>
        private void CustomInitialize()
        {
            InitializePlatformerInput(new InputDeviceBase());

            animationController = new AnimationController(SpriteInstance);
            var idleLayer = new AnimationLayer();
            idleLayer.EveryFrameAction = () =>
            {
                return "CharacterIdle" + DirectionFacing;
            };
            animationController.Layers.Add(idleLayer);
        }

        private void CustomActivity()
        {
            animationController.Activity();


        }

        private void CustomDestroy()
        {


        }

        private static void CustomLoadStaticContent(string contentManagerName)
        {


        }
    }
}
