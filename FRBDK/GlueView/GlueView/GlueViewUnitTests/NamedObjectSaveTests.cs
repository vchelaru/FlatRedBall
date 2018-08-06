using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue;
using FlatRedBall;

namespace GlueViewUnitTests
{
    [TestFixture]
    public class NamedObjectSaveTests
    {
        EntitySave mEntitySave;

        [TestFixtureSetUp]
        public void Initialize()
        {
            OverallInitializer.Initialize();

            mEntitySave = new EntitySave();
            mEntitySave.Name = "EntitySaveInNamedObjectSaveTests";
        }

        [Test]
        public void TestRelativeAbsoluteConversion()
        {
            NamedObjectSave nos = new NamedObjectSave();
            nos.SourceType = SourceType.FlatRedBallType;
            nos.SourceClassType = "Sprite";
            nos.UpdateCustomProperties();
            nos.InstanceName = "SpriteObject";
            nos.SetPropertyValue("ScaleX", 2.0f);
            nos.SetPropertyValue("X", 4.0f);
            nos.SetPropertyValue("RotationZ", 4.0f);
            nos.SetPropertyValue("RotationZVelocity", 4.0f);

            mEntitySave.NamedObjects.Add(nos);

            CustomVariable customVariable = new CustomVariable();
            customVariable.SourceObject = nos.InstanceName;
            customVariable.SourceObjectProperty = "ScaleY";
            customVariable.DefaultValue = 8.0f;

            mEntitySave.CustomVariables.Add(customVariable);

            ElementRuntime elementRuntime = new ElementRuntime();
            elementRuntime.Initialize(mEntitySave, null, null, null, null);

            Sprite sprite = elementRuntime.ContainedElements[0].DirectObjectReference as Sprite;
            sprite.ForceUpdateDependencies();

            if (elementRuntime.X != 0)
            {
                throw new Exception("NOS variables are being applied to the container instead of just to the NOS");
            }

            if (sprite.X != 4.0f)
            {
                throw new Exception("Absolute values should get set when setting X on objects even though they're attached");
            }

            if (sprite.RotationZ != 4.0f)
            {
                throw new Exception("Absolute values should get set when setting RotationZ on objects even though they're attached");
            }
            if (sprite.RelativeRotationZVelocity != 4.0f)
            {
                throw new Exception("Setting RotationZVelocity should set RelativeRotationZVelocity");
            }
            if (sprite.ScaleX != 2.0f)
            {
                throw new Exception("Scale values aren't properly showing up on Sprites");
            }
            if (sprite.ScaleY != 8.0f)
            {
                throw new Exception("Scale values aren't properly showing up on Sprites");
            }
        }

        [Test]
        public void TestSceneTypes()
        {
            EntitySave entitySave = new EntitySave();
            entitySave.Name = "NosSceneTest";

            NamedObjectSave nos = new NamedObjectSave();
            nos.SourceType = SourceType.FlatRedBallType;
            nos.InstanceName = "NamedScene";
            nos.SourceClassType = "Scene";
            entitySave.NamedObjects.Add(nos);

            ElementRuntime elementRuntime = new ElementRuntime();
            elementRuntime.Initialize(entitySave, null, null, null, null);

            if (elementRuntime.ContainedElements.Count == 0)
            {
                throw new Exception("ElementRuntimes with Scene NOS's should create ElementRuntimes for the Scene NOS");
            }

            if (elementRuntime.ContainedElements[0].DirectObjectReference is Scene == false)
            {
                throw new Exception("Scene NOS's should create Scenes");
            }

            if (((Scene)elementRuntime.ContainedElements[0].DirectObjectReference).Name != nos.InstanceName)
            {
                throw new Exception("Name on Scenes are not being set from NOS's");
            }

        }
    }
}
