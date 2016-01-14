using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using NUnit.Framework;

namespace GlueViewUnitTests
{
    [TestFixture]
    public class InheritanceTests
    {
        #region Fields

        EntitySave mBaseEntity;
        EntitySave mDerivedEntity;

        ElementRuntime mDerivedElementRuntime;

        #endregion

        [TestFixtureSetUp]
        public void Initialize()
        {
            OverallInitializer.Initialize();

            mBaseEntity = new EntitySave();
            mBaseEntity.Name = "BaseEntityInheritanceTests";
            ObjectFinder.Self.GlueProject.Entities.Add(mBaseEntity);

            NamedObjectSave nos = new NamedObjectSave();
            nos.InstanceName = "SpriteInstance";
            nos.SourceType = SourceType.FlatRedBallType;
            nos.SourceClassType = "Sprite";
            nos.SetByDerived = true;
            mBaseEntity.NamedObjects.Add(nos);

            nos = new NamedObjectSave();
            nos.InstanceName = "RectInstance";
            nos.SourceType = SourceType.FlatRedBallType;
            nos.SourceClassType = "AxisAlignedRectangle";
            nos.ExposedInDerived = true;
            mBaseEntity.NamedObjects.Add(nos);

            mDerivedEntity = new EntitySave();
            mDerivedEntity.Name = "DerivedentityInheritanceTests";
            mDerivedEntity.BaseEntity = mBaseEntity.Name;
            mDerivedEntity.UpdateFromBaseType();
            ObjectFinder.Self.GlueProject.Entities.Add(mDerivedEntity);

            mDerivedElementRuntime = new ElementRuntime(mDerivedEntity, null, null, null, null);
        }

        [Test]
        public void TestSetByDerived()
        {
            NamedObjectSave nos = mDerivedEntity.NamedObjects.FirstOrDefault(item => item.FieldName == "SpriteInstance");

            bool shouldCreate = 
                ElementRuntime.ShouldElementRuntimeBeCreatedForNos(nos, mDerivedEntity);
            if(!shouldCreate)
            {
                throw new Exception("NOS's which are defined by base, but their base has SetByDerived should be created.");
            }



            if (mDerivedElementRuntime.ContainedElements.FirstOrDefault(item => item.Name == "SpriteInstance") == null)
            {
                throw new Exception("Objects defined in base and SetByDerived should be created, but are not.");
            }

            // Verify that only 1 AARect has been made
            int count = 0;
            foreach (var item in mDerivedElementRuntime.ContainedElements.Where(item => item.Name == "RectInstance"))
            {
                count++;
            }
            if (count != 1)
            {
                throw new Exception("Only one rectangle should be created, but it looks like more are made");
            }
        }
    }
}
