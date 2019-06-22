using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using NUnit.Framework;

namespace UnitTests.SaveClasses
{
    [TestFixture]
    public class NameVerifierTests
    {
        

        [Test]
        public void TestNamedObjectSave()
        {
            EntitySave entitySave = new EntitySave();

            NamedObjectSave nos = new NamedObjectSave();

            string whyNot;
            NameVerifier.IsNamedObjectNameValid("Parent", nos, out whyNot);
            if (string.IsNullOrEmpty(whyNot))
            {
                throw new Exception("Parent should not be  avalid name for a PositionedObject, but Glue allows it");
            }
        }

        [Test]
        public void TestReferencedFileSave()
        {
            EntitySave entitySave = new EntitySave();

            ReferencedFileSave rfs = new ReferencedFileSave();
            rfs.DestroyOnUnload = false;

            rfs.Name = "File.png";
            entitySave.ReferencedFiles.Add(rfs);

            string whyNot;
            NameVerifier.IsReferencedFileNameValid("File", null, null, entitySave, out whyNot);
            if(string.IsNullOrEmpty(whyNot))
            {
                throw new Exception("Same-named files shouldn't be allowed, but the NameVerifier allows it.");
            }

            NameVerifier.IsReferencedFileNameValid("File.wav", null, null, entitySave, out whyNot);
            if (string.IsNullOrEmpty(whyNot))
            {
                throw new Exception("Same-named files shouldn't be allowed, but the NameVerifier allows it.");
            }

            NameVerifier.IsReferencedFileNameValid("Folder/File.wav", null, null, entitySave, out whyNot);
            if (string.IsNullOrEmpty(whyNot))
            {
                throw new Exception("Same-named files shouldn't be allowed, but the NameVerifier allows it.");
            }

            NameVerifier.IsReferencedFileNameValid("Folder/File", null, null, entitySave, out whyNot);
            if (string.IsNullOrEmpty(whyNot))
            {
                throw new Exception("Same-named files shouldn't be allowed, but the NameVerifier allows it.");
            }

            NameVerifier.IsReferencedFileNameValid("if", null, null, entitySave, out whyNot);
            if (string.IsNullOrEmpty(whyNot))
            {
                throw new Exception("'if' is a reserved keyword and should not be allowed as a file name");
            }

        }

        [Test]
        public void TestStateCategorySave()
        {
            string whyNot;

            NameVerifier.IsStateCategoryNameValid("Color", out whyNot);
            if (string.IsNullOrEmpty(whyNot))
            {
                throw new Exception("Glue shouldn't allow naming a state category reserved names like Color");
            }

            NameVerifier.IsStateCategoryNameValid("Camera", out whyNot);
            if (string.IsNullOrEmpty(whyNot))
            {
                throw new Exception("Glue shouldn't allow naming a state category reserved names like Camera");
            }


        }

        [Test]
        public void TestStateSave()
        {
            EntitySave baseEntity = new EntitySave();
            baseEntity.Name = "BaseForStateSaveNameTest";
            ObjectFinder.Self.GlueProject.Entities.Add(baseEntity);

            EntitySave derivedEntity = new EntitySave();
            derivedEntity.Name = "DerivedForStateSaveNameTest";
            ObjectFinder.Self.GlueProject.Entities.Add(derivedEntity);

            derivedEntity.BaseEntity = baseEntity.Name;

            StateSave stateSave = new StateSave();
            stateSave.Name = "NameOfState";
            baseEntity.States.Add(stateSave);

            string whyItIsntValid;
            NameVerifier.IsStateNameValid("NameOfState", baseEntity, null, null, out whyItIsntValid);

            if (string.IsNullOrEmpty(whyItIsntValid))
            {
                throw new Exception("Name verifier should not allow duplicate names");
            }

            NameVerifier.IsStateNameValid("NameOfState", derivedEntity, null, null, out whyItIsntValid);
            if (string.IsNullOrEmpty(whyItIsntValid))
            {
                throw new Exception("Name verifier should not allow derived to duplicate state names");
            }

            baseEntity.StateCategoryList.Add(new StateSaveCategory() { Name = "Category1", SharesVariablesWithOtherCategories = false });
            baseEntity.StateCategoryList[0].States.Add(new StateSave() { Name = "StateInCategory" });

            NameVerifier.IsStateNameValid("StateInCategory", baseEntity, baseEntity.StateCategoryList[0], null, out whyItIsntValid);

            if(string.IsNullOrEmpty(whyItIsntValid))
            {
                throw new Exception("Categories should not allow multiple states with the same name in them");
            }
        }
    }
}
