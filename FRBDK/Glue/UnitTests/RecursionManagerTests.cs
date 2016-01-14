using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.SaveClasses;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class RecursionManagerTests
    {
        [TestFixtureSetUp]
        public void Initialize()
        {
            OverallInitializer.Initialize();
        }

        [Test]
        public void TestRecursion()
        {
            EntitySave container = new EntitySave();
            container.Name = "TestRecursionContainer";
            ObjectFinder.Self.GlueProject.Entities.Add(container);

            if (RecursionManager.Self.CanContainInstanceOf(container, container.Name) == true)
            {
                throw new Exception("A type cannot contain itself");
            }

        }
    }
}
