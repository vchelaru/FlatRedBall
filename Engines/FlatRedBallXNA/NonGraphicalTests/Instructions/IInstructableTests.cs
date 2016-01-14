using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall;
using FlatRedBall.Instructions;

namespace NonGraphicalTests.Instructions
{
    [TestFixture]
    public class IInstructableTests
    {
        [Test]
        public void TestFluentInterface()
        {
            PositionedObject positionedObject = new PositionedObject();

            // Make sure it doesn't crash:
            positionedObject.Set("XVelocity").To(4.3f).After(1.1f);            

        }
    }
}
