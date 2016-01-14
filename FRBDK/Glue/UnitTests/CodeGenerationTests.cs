using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using NUnit.Framework;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue;
using FlatRedBall.IO.Csv;
using Microsoft.Xna.Framework;

namespace UnitTests
{
    [TestFixture]
    public class CodeGenerationTests
    {
        EntitySave mButton;
        EntitySave mButtonList;

        NamedObjectSave mButtonInButtonList;

        [TestFixtureSetUp]
        public void Initialize()
        {
            OverallInitializer.Initialize();

            CreateButton();

            CreateButtonList();

        }

        private void CreateButton()
        {
            mButton = new EntitySave();
            mButton.Name = "ButtonInCodeGenerationTests";
            ObjectFinder.Self.GlueProject.Entities.Add(mButton);
        }

        private void CreateButtonList()
        {
            mButtonList = new EntitySave();
            mButtonList.Name = "ButtonListInCodeGenerationTests";
            ObjectFinder.Self.GlueProject.Entities.Add(mButtonList);

            mButtonInButtonList = new NamedObjectSave();
            mButtonInButtonList.SourceType = SourceType.Entity;
            mButtonInButtonList.SourceClassType = mButton.Name;
            mButtonList.NamedObjects.Add(mButtonInButtonList);
        }

        [Test]
        public void TestStateCodeGeneration()
        {
            ICodeBlock codeBlock = new CodeDocument(0);
            
            mButtonInButtonList.CurrentState = "InvalidState";

            StateCodeGenerator.WriteSetStateOnNamedObject(mButtonInButtonList, codeBlock);
            string result = codeBlock.ToString();
            if (result.Contains(mButtonInButtonList.CurrentState))
            {
                throw new Exception("Code generation for NamedObjects is generating state setting code when states don't really exist");
            }

            // Make sure generation doesn't mess up on a entity with a "" base (instead of null)
            StateCodeGenerator scg = new StateCodeGenerator();
            EntitySave entitySave = new EntitySave();

            entitySave.States.Add(new StateSave());

            entitySave.BaseEntity = "";
            scg.GenerateFields(codeBlock, entitySave);
        }

        [Test]
        public void TestNamespaceGeneration()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("namespace REPLACE_ME.Entities{\r\npublic partial class Stinkbrow");
            CodeWriter.SetClassNameAndNamespace("Project.Entities", "Grunchel", stringBuilder);

            string after = stringBuilder.ToString();
            if (!after.Contains("namespace Project.Entities"))
            {
                throw new Exception("Code generation did not properly replace the namespace");
            }


        }

        [Test]
        public void TestCsvGeneration()
        {

            string nameInCsv = "Name With Spaces and Invalids*~";

            string constName = CsvCodeGenerator.GetConstNameForValue(nameInCsv);

            if (constName.Contains(' '))
            {
                throw new Exception("Const name for CSV is invalid!");
            }

            if (constName.IndexOfAny(NameVerifier.InvalidCharacters) != -1)
            {
                throw new Exception("Const name contains invalid characters!");
            }

            Type type = TypeManager.GetTypeFromString("bool");
            if (type != typeof(bool))
            {
                throw new Exception("TypeManager is not properly returning type from \"bool\"");
            }

            type = TypeManager.GetTypeFromString("System.Boolean");
            if (type != typeof(bool))
            {
                throw new Exception("TypeManager is not properly returning type from \"bool\"");
            }

            type = TypeManager.GetTypeFromString("Boolean");
            if (type != typeof(bool))
            {
                throw new Exception("TypeManager is not properly returning type from \"bool\"");
            }

            RuntimeCsvRepresentation rcr = new RuntimeCsvRepresentation();
            rcr.Headers = new CsvHeader[2];
            rcr.Headers[0].OriginalText = "Variable";
            rcr.Headers[1].OriginalText = "Variable (float)";
            string why = CsvCodeGenerator.GetWhyCsvIsWrong(rcr, false, null);
            if (string.IsNullOrEmpty(why))
            {
                throw new Exception("Glue should be reporting errors on CSVs that have duplicate header names (but of different types)");
            }
        }
    }
}
