using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Math;
using NUnit.Framework;

namespace NonGraphicalTests.Graphics.Particle
{
    [TestFixture]
    public class EmitterTests
    {
        

        [Test]
        public void TestTimedRemoval()
        {
            Emitter emitter = new Emitter();
            emitter.RemovalEvent = Emitter.RemovalEventType.Timed;
            emitter.SecondsLasting = 1000;

            SpriteList sprites = new SpriteList(); ;
            emitter.Emit(sprites);
            SpriteManager.RemoveSprite(sprites[0]);


            if (SpriteManager.NumberOfTimedRemovalObjects != 0)
            {
                throw new Exception("Timed removal is not being cleared out when removing particles");
            }




        }
    }
}
