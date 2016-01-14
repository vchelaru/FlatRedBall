using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Gui;

namespace GlueViewUnitTests.EntityControlPlugin
{
    [TestFixture]
    public class ResizingTests
    {
        Circle mCircle;

        [TestFixtureSetUp]
        public void Initialize()
        {
            mCircle = new Circle();
        }
    }
}
