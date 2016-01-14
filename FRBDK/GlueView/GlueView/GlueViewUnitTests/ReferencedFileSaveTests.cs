using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;

namespace GlueViewUnitTests
{
    [TestFixture]
    public class ReferencedFileSaveTests
    {
        EntitySave mBaseEntity;
        EntitySave mDerivedEntity;


        [TestFixtureSetUp]
        public void Initialize()
        {

            // Couldn't run tests here because it requires FRB to be initialized.  
            OverallInitializer.Initialize();

            mBaseEntity = new EntitySave();
            mBaseEntity.Name = "ReferencedFileSaveTestsBaseEntity";

            ReferencedFileSave rfs = new ReferencedFileSave();


            ObjectFinder.Self.GlueProject.Entities.Add(mBaseEntity);

            mDerivedEntity = new EntitySave();
            mDerivedEntity.Name = "ReferencedFileSaveTestsDerivedEntity";
            mDerivedEntity.BaseEntity = mBaseEntity.Name;
            ObjectFinder.Self.GlueProject.Entities.Add(mDerivedEntity);

        }


        [Test]
        public void Test()
        {


        }
    }
}
