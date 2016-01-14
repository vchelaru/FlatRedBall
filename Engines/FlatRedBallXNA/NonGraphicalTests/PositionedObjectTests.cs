using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall;

namespace NonGraphicalTests
{
    [TestFixture]
    public class PositionedObjectTests
    {
        [Test]
        public void TestRotations()
        {
            PositionedObject first = new PositionedObject();
            PositionedObject second = new PositionedObject();

            first.RotationZ = 1.7f;

            second.RotationMatrix = first.RotationMatrix;

            if (second.RotationZ != 1.7f)
            {
                throw new Exception("There seems to be a bug with setting rotation matrices when RotationY is set");
            }

            TestRotationY(0, first);
            TestRotationY(.5f, first);
            TestRotationY(1, first);
            TestRotationY(1.5f, first);
            TestRotationY(2, first);
            TestRotationY(2.5f, first);
            TestRotationY(3, first);
            TestRotationY(3.5f, first);
        }


        void TestRotationY(float yValue, PositionedObject positionedObject)
        {

            positionedObject.RotationX = 0;
            positionedObject.RotationY = yValue;
            positionedObject.RotationZ = 0;

            positionedObject.RotationMatrix = positionedObject.RotationMatrix;

            float epsilon = .001f;
            
            if (Math.Abs( positionedObject.RotationY - yValue) > epsilon)
            {
                throw new Exception("There seems to be a bug with setting rotation matrices when RotationY is set");
            }

        }

    }
}
