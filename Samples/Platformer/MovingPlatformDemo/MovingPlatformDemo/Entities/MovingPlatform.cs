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
using System.Threading.Tasks;

namespace MovingPlatformDemo.Entities
{
    public partial class MovingPlatform
    {
        bool isDestroyed = false;
        /// <summary>
        /// Initialization logic which is execute only one time for this Entity (unless the Entity is pooled).
        /// This method is called when the Entity is added to managers. Entities which are instantiated but not
        /// added to managers will not have this method called.
        /// </summary>
        private void CustomInitialize()
        {
            StartMoving();

        }

        private async void StartMoving()
        {
            async Task AccelerateFor(float acceleration, float seconds)
            {
                XAcceleration = acceleration;
                await TimeManager.DelaySeconds(seconds);
            }

            while(!isDestroyed)
            {
                await AccelerateFor(30, 1);
                await AccelerateFor(0, 2);
                await AccelerateFor(-30, 2);
                await AccelerateFor(0, 2);
                await AccelerateFor(30, 1);
            }
        }

        internal void StoreBasePosition()
        {
        }

        private void CustomActivity()
        {

        }

        private void CustomDestroy()
        {
            isDestroyed = true;

        }

        private static void CustomLoadStaticContent(string contentManagerName)
        {


        }
    }
}
