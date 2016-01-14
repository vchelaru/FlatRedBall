using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Elements;
using FlatRedBall;

namespace GlueViewUnitTests
{
    [TestFixture]
    public class IVisibleTests
    {
        EntitySave mEntitySave;

        ElementRuntime mElementRuntime;

        [TestFixtureSetUp]
        public void Initialize()
        {
            OverallInitializer.Initialize();

            CreateEntity();

            CreateElementRuntime();
        }

        private void CreateElementRuntime()
        {
            mElementRuntime = new ElementRuntime(mEntitySave, null, null, null, null);
        }

        private void CreateEntity()
        {
            mEntitySave = new EntitySave();
            mEntitySave.Name = "IVisibleTestEntity";
            mEntitySave.ImplementsIVisible = true;
            ObjectFinder.Self.GlueProject.Entities.Add(mEntitySave);

            CustomVariable customVariable = new CustomVariable();
            customVariable.Name = "Visible";
            customVariable.Type = "bool";
            mEntitySave.CustomVariables.Add(customVariable);

            NamedObjectSave nos = new NamedObjectSave();
            nos.InstanceName = "SpriteObject";
            nos.SourceType = SourceType.FlatRedBallType;
            nos.SourceClassType = "Sprite";
            mEntitySave.NamedObjects.Add(nos);

        }

        [Test]
        public void TestRegularVisibility()
        {
            CustomVariable visibleVariable = mEntitySave.GetCustomVariable("Visible");

            mElementRuntime.SetCustomVariable(visibleVariable, mElementRuntime.AssociatedIElement, false, false);

            
            ElementRuntime containedElementRuntime = mElementRuntime.ContainedElements[0] as ElementRuntime;

            Sprite sprite = containedElementRuntime.DirectObjectReference as Sprite;

            if (sprite.AbsoluteVisible)
            {
                throw new Exception("Sprite is visible, but it is part of an element runtime that isn't, so it shouldn't be");
            }


            sprite.Visible = false;
            mElementRuntime.SetCustomVariable(visibleVariable, mElementRuntime.AssociatedIElement, true, false);

            if (sprite.Visible)
            {
                throw new Exception("The Sprite should still have a relative Visibility of true, even though the parent is false");
            }

            sprite.Visible = true;
            if (!sprite.AbsoluteVisible)
            {
                throw new Exception("The Sprite should be visible now!");
            }
        }
        
    }
}
