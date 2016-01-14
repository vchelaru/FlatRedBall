using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall.Graphics.Animation;
using FlatRedBall;

namespace NonGraphicalTests.Graphics.Animation
{
    [TestFixture]
    public class IAnimationChainAnimatableTests
    {
        [Test]
        public void TestContainsChainNameExtensionMethod()
        {
            // Setup
            var nullAnimatable = (IAnimationChainAnimatable)null;
            
            var sprite = new Sprite();
            sprite.AnimationChains = new AnimationChainList();
            sprite.AnimationChains.Add(new AnimationChain { Name = "ValidChain" });

            // This causes me to break in Visual Studio
            //Assert.Throws<ArgumentNullException>(delegate { nullAnimatable.ContainsChainName("test"); });
            bool wasThrown = false;
            try
            {
                nullAnimatable.ContainsChainName("test"); 
            }
            catch (ArgumentException)
            {
                wasThrown = true; ;
            }
            Assert.IsTrue(wasThrown);

            Assert.IsTrue(sprite.ContainsChainName("ValidChain"));
            Assert.IsFalse(sprite.ContainsChainName("InvalidChain"));
        }
    }
}
