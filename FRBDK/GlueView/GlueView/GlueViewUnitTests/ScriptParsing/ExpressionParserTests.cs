using System;
using System.Collections.Generic;
using GlueViewOfficialPlugins.Scripting;
using CodeTranslator.Parsers;
using FlatRedBall.Glue;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.Parsing;
using NUnit.Framework;
using System.Text;
using FlatRedBall.Utilities;
using FlatRedBall.Math;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Localization;
using Microsoft.Xna.Framework;
using FlatRedBall.Graphics.Particle;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall;
using FlatRedBall.IO.Csv;
using GlueView.Scripting;
using FlatRedBall.Instructions;
using System.Reflection;

namespace GlueViewUnitTests.ScriptParsing
{
    [TestFixture]
    public class ExpressionParserTests
    {
        #region Fields

        ScriptParsingPlugin mPlugin;

        ExpressionParser mExpressionParser;


        ElementRuntime mElementRuntime;
        ElementRuntime mParentElementRuntime;

        StringBuilder mLog = new StringBuilder();

        #region mClassContents 
        string mClassContents =
            @"
    class UnitStatBars
	{
        void OnAfterEnemyInfoSet(object sender, EventArgs e)
        {

            //if(EnemyInfo != null)
            //{

            //if (clamp)
            //{
            //     healthRatio = Math.Min(1, healthRatio);
            // }
            this.HealthStatBar.InterpolateBetween(StatBar.VariableState.Empty, StatBar.VariableState.Full, 0.5f);
            //}
        }
	}

";

        #endregion

        #endregion

        [TestFixtureSetUp]
        public void Initialize()
        {
            OverallInitializer.Initialize();

            mPlugin = new ScriptParsingPlugin();
            mPlugin.StartUpForTests();
            mExpressionParser = new ExpressionParser();
            mExpressionParser.LogStringBuilder = mLog;
            
            CreateElementRuntime("ExpressionParserTestEntity");

            CreateParentElementRuntime();

            List<string> languages = new List<string>();
            languages.Add("ID");
            languages.Add("English");
            languages.Add("Spanish");

            Dictionary<string, string[]> entries = new Dictionary<string, string[]>();

            entries.Add("T_Test", new string[]
            {
                "T_Test",
                "Test in English",
                "Test in Spanish"
            });

            LocalizationManager.AddDatabase(entries, languages);
        }

        void CreateElementRuntime(string name)
        {
            var entitySave = new EntitySave {Name = name};

            ObjectFinder.Self.GlueProject.Entities.Add(entitySave);

            #region Create CustomVariables
            var xVariable = new CustomVariable
                                {
                                    Name = "X", 
                                    Type = "float", 
                                    DefaultValue = 3.0f
                                };
            entitySave.CustomVariables.Add(xVariable);

            var yVariable = new CustomVariable
                                {
                                    Name = "Y", 
                                    Type = "float", 
                                    DefaultValue = 4.0f
                                };
            entitySave.CustomVariables.Add(yVariable);


            var customVariable = new CustomVariable
                                {
                                    Name = "SomeNewVar",
                                    Type = "double",
                                    DefaultValue = 3.333
                                };
            entitySave.CustomVariables.Add(customVariable);

            var csvTypeVAriable = new CustomVariable
                                {
                                    Name = "CsvTypeVariable",
                                    Type = "CsvType.csv",
                                    DefaultValue = null

                                };
            entitySave.CustomVariables.Add(csvTypeVAriable);

            var csvTypeVariable2 = new CustomVariable
                                {
                                    Name = "EnemyInfoVariable",
                                    Type = "EnemyInfo.csv",
                                    DefaultValue = "Imp"

                                };
            entitySave.CustomVariables.Add(csvTypeVariable2);

            var scaleXVariable = new CustomVariable
            {
                Name = "ScaleX",
                Type = "float",
                DefaultValue = 10.0f
            };
            entitySave.CustomVariables.Add(scaleXVariable);


            #endregion

            #region Create the NamedObjectsSave

            NamedObjectSave nos = new NamedObjectSave();
            nos.SourceType = SourceType.FlatRedBallType;
            nos.SourceClassType = "Sprite";
            nos.InstanceName = "SpriteObject";
            nos.UpdateCustomProperties();
            nos.SetPropertyValue("ScaleX", 3.0f);
            entitySave.NamedObjects.Add(nos);


            #endregion

            #region Create the ReferencedFileSaves

            ReferencedFileSave rfs = new ReferencedFileSave();
            rfs.Name = "Content/Entities/ReferencedFileSaveTestsBaseEntity/SceneFile.scnx";
            entitySave.ReferencedFiles.Add(rfs);

            rfs = new ReferencedFileSave();
            rfs.Name = "Content/EnemyInfo.csv";
            rfs.CreatesDictionary = true;

            entitySave.ReferencedFiles.Add(rfs);


            #endregion

            mElementRuntime = new ElementRuntime(entitySave, null, null, null, null)
                                  {
                                      X = (float) xVariable.DefaultValue, 
                                      Y = (float) yVariable.DefaultValue
                                  };


            #region Create the uncategorized states

            var leftX = new InstructionSave
            {
                Member = "X",
                Value = -10,
                Type = "float"
            };




            var rightX = new InstructionSave
            {
                Member = "X",
                Value = 10,
                Type = "float"
            };

            var someNewVarSetLeft = new InstructionSave
            {
                Member = "SomeNewVar",
                Value = -10.0,
                Type = "double"

            };


            var someNewVarSetRight = new InstructionSave
            {
                Member = "SomeNewVar",
                Value = 10.0,
                Type = "double"

            };

            var leftState = new StateSave {Name = "Left"};
            leftState.InstructionSaves.Add(leftX);
            leftState.InstructionSaves.Add(someNewVarSetLeft);

            var rightState = new StateSave {Name = "Right"};
            rightState.InstructionSaves.Add(rightX);
            rightState.InstructionSaves.Add(someNewVarSetRight);


            entitySave.States.Add(leftState);
            entitySave.States.Add(rightState);

            #endregion

            #region Create the categorized states

            StateSaveCategory category = new StateSaveCategory();
            category.SharesVariablesWithOtherCategories = false;

            category.Name = "StateCategory";

            var topY = new InstructionSave
            {
                Member = "Y",
                Value = 10,
                Type = "float"
            };

            var bottomY = new InstructionSave
            {
                Member = "Y",
                Value = 10,
                Type = "float"
            };

            var topState = new StateSave { Name = "Top" };
            topState.InstructionSaves.Add(topY);
            var bottomState = new StateSave { Name = "Bottom" };
            bottomState.InstructionSaves.Add(rightX);

            category.States.Add(topState);
            category.States.Add(bottomState);

            entitySave.StateCategoryList.Add(category);

            #endregion
        }

        void CreateParentElementRuntime()
        {
            EntitySave containerEntity = new EntitySave();
            ObjectFinder.Self.GlueProject.Entities.Add(containerEntity);

            containerEntity.Name = "ContainerEntity";

            NamedObjectSave first = new NamedObjectSave();
            first.SourceType = SourceType.Entity;
            first.SourceClassType = "ExpressionParserTestEntity";
            first.InstanceName = "FirstObject";
            first.UpdateCustomProperties();
            first.SetPropertyValue("CsvTypeVariable", "CsvValue1");

            containerEntity.NamedObjects.Add(first);

            mParentElementRuntime = new ElementRuntime(
                containerEntity, null, null, null, null);
        }

        [Test]
        public void EvaluatePrimitiveOperations()
        {
            object left = 1;
            object right = 1;

            var result = PrimitiveOperationManager.Self.AddObjects(left, right);
            if (result is int == false || (int)result != 2)
            {
                throw new Exception("AddObjects isn't adding integers properly");
            }


            left = 5;
            right = 2;
            result = PrimitiveOperationManager.Self.DivideObjects(left, right);
            if (result is int == false || (int)result != 5/2)
            {
                throw new Exception("AddObjects isn't adding integers properly");
            }

            left = 5;
            right = 2.0f;
            result = PrimitiveOperationManager.Self.DivideObjects(left, right);
            if (result is float == false || (float)result != 5 / 2.0f)
            {
                throw new Exception("AddObjects isn't adding integers properly");
            }
        }



        [Test]
        public void TestCodeParser()
        {
            var parsedClass = new ParsedClass(mClassContents, false);

            Assert.AreEqual(parsedClass.ParsedMethods.Count, 1);

            var parsedMethod = parsedClass.ParsedMethods[0];

            var contents = parsedMethod.MethodContents;

            Assert.True(contents.Contains("this.HealthStatBar.InterpolateBetween(StatBar.VariableState.Empty, StatBar.VariableState.Full, 0.5f);"), 
                "The parsed method contents don't contain a call to InterpolateBetween - there is an error in parsing the method contents");
        }


        [Test]
        public void TestUnaryAndAssignmentOperators()
        {
            CodeContext codeContext = new CodeContext(null);

            string[] lines = new string[] { "int m = 3;", "m++;" };

            mPlugin.ApplyLinesInternal(lines, 0, lines.Length, null, codeContext);

            if ((int)codeContext.VariableStack[0]["m"] != 4)
            {
                throw new Exception("The ++ operator is not working.");
            }

            lines = new string[] { "m=8;" };
            mPlugin.ApplyLinesInternal(lines, 0, lines.Length, null, codeContext);
            if ((int)codeContext.VariableStack[0]["m"] != 8)
            {
                throw new Exception("The += operator is not working.");
            }

            lines = new string[] { "m+=3;" };
            mPlugin.ApplyLinesInternal(lines, 0, lines.Length, null, codeContext);
            if ((int)codeContext.VariableStack[0]["m"] != 11)
            {
                throw new Exception("The += operator is not working.");
            }

            lines = new string[] { "m-=3;" };
            mPlugin.ApplyLinesInternal(lines, 0, lines.Length, null, codeContext);
            if ((int)codeContext.VariableStack[0]["m"] != 8)
            {
                throw new Exception("The += operator is not working.");
            }

            lines = new string[] { "m*=3;" };
            mPlugin.ApplyLinesInternal(lines, 0, lines.Length, null, codeContext);
            if ((int)codeContext.VariableStack[0]["m"] != 24)
            {
                throw new Exception("The += operator is not working.");
            }
        }
        
        [Test]
        public void TestEvaluation()
        {
            object evaluated = mExpressionParser.EvaluateExpression("3.1f", new CodeContext(null));
            if ((evaluated is float) == false)
            {
                throw new Exception("Values with 'f' should be floats");
            }

            TestEvaluation("-16", -16);

            TestEvaluation("1+1", 2);

            TestEvaluation("2.3f - 1", 2.3f - 1);

            TestEvaluation("1 + (2 * 3.0)", 1 + (2 * 3.0));

            TestEvaluation("4/2.0f", 4 / 2.0f);

            TestEvaluation("true || false", true || false);
            TestEvaluation("true && false", true && false);




            // Test full evaluations and verify the results
            TestEvaluation("System.Math.Max(3, 5)", 5);
            TestEvaluation("System.Math.Min(8-1, 8)", 7);

            TestEvaluation("this", mElementRuntime, mElementRuntime);

            TestEvaluation("this.X", mElementRuntime.X, mElementRuntime);

            TestEvaluation("this.SomeNewVar", 3.333, mElementRuntime);

            TestEvaluation("System.Math.Max(this.X, this.Y)", System.Math.Max(mElementRuntime.X, mElementRuntime.Y), mElementRuntime);

            TestEvaluation("float.PositiveInfinity", float.PositiveInfinity);

            TestEvaluation("System.Math.Max(float.PositiveInfinity, 0)", float.PositiveInfinity);
            TestEvaluation("VariableState.Left", mElementRuntime.GetStateRecursively("Left"), mElementRuntime);

            TestEvaluation("new Vector3()", new Vector3());

            TestEvaluation("Color.Red", Color.Red);
            // need this, but first need to be able to reorder cast properly
            TestEvaluation("new Color(.3f, .4f, .5f, .6f)", new Color(.3f, .4f, .5f, .6f));

            TestEvaluation("\"a\" + \"b\"", "ab", null);
            TestEvaluation("1 + \"a\" + 2", "1a2", null);

            TestEvaluation("this.SpriteObject.ScaleX", 3.0f, mElementRuntime);

            EmitterList emitterList = new EmitterList();
            var first = new Emitter();
            first.Name = "First";
            emitterList.Add(first);

            var second = new Emitter();
            second.Name = "Second";
            emitterList.Add(second);

            TestEvaluation("FindByName(\"First\")", emitterList.FindByName("First"), emitterList);

        }

        [Test]
        public void TestAssignment()
        {
            string line = "this.X = -16;";
            string[] lines = new string[] { line };

            IElement element = mElementRuntime.AssociatedIElement;

            var codeContext = new CodeContext(mElementRuntime);
            mPlugin.ApplyLinesInternal(lines, 0, 1, element, codeContext);

            if (mElementRuntime.X != -16)
            {
                throw new Exception("Assignment of states on contained Entities isn't working");
            }
        }

        [Test]
        public void TestMethodCallParser()
        {
            CustomVariable variable = MethodCallParser.GetCustomVariableFromNosOrElement(mParentElementRuntime.ContainedElements[0], "CsvTypeVariable");

            if (variable == null || variable.DefaultValue == null)
            {
                throw new Exception("The MethodCallParser is not properly finding values on NamedObjects");
            }


        }

        public void TestParsedLine()
        {
            var parsedLine = new ParsedLine("this.DoSomething(firstArgument, secondArgument)");

            var match = parsedLine.GetMatchingBracketForBracketAtIndex(1);

            if (match != 5)
            {
                throw new Exception("The matching closing bracket code item index should be 5, but it's " + match);
            }

            parsedLine.ConsolidateMethodContents();

            if (parsedLine.CodeItems.Count != 2)
            {
                throw new Exception("There should only be 2 code items, but there are " + parsedLine.CodeItems.Count);
            }


            parsedLine = new ParsedLine("new Vector3()");

            if (parsedLine[1].CodeType == CodeType.MethodCall)
            {
                throw new Exception("The type should be a constructor, not method call");
            }

            parsedLine = new ParsedLine("x() + y");
            if (parsedLine.CodeItems.Count != 5)
            {
                throw new Exception("There should be 5 CodeItems in the parsed line");
            }

            parsedLine = new ParsedLine("x");
            parsedLine.CombineToExpressions();
            if (parsedLine.Count != 1)
            {
                throw new Exception("Error combining to expressions");
            }

            parsedLine = new ParsedLine("x.y.z");
            parsedLine.CombineToExpressions();
            if (parsedLine.Count != 1)
            {
                throw new Exception("Error combining to expressions");
            }

            parsedLine = new ParsedLine("x.y.z + a.b.c");
            parsedLine.CombineToExpressions();
            if (parsedLine.Count != 3)
            {
                throw new Exception("Error combining to expressions");
            }

            parsedLine = new ParsedLine("x()");
            parsedLine.CombineToExpressions();
            if (parsedLine.Count != 1)
            {
                throw new Exception("Error combining to expressions");
            }

            parsedLine = new ParsedLine("x().y");
            parsedLine.CombineToExpressions();
            if (parsedLine.Count != 1)
            {
                throw new Exception("Error combining to expressions");
            }

            parsedLine = new ParsedLine("x() + y");
            parsedLine.CombineToExpressions();
            if (parsedLine.Count != 3)
            {
                throw new Exception("Error combining to expressions");
            }


            parsedLine = new ParsedLine("x(3+4) + y");
            parsedLine.CombineToExpressions();
            if (parsedLine.Count != 3)
            {
                throw new Exception("Error combining to expressions");
            }

            parsedLine = new ParsedLine("ToolEmitters.FindByName().Emit;");
            parsedLine.ConsolidateMethodContents();
            parsedLine.CombineToExpressions();
            if (parsedLine.Count != 2)
            {
                throw new Exception("Error combining to expressions");
            }
        }

        [Test]
        public void TestMultipleLines()
        {
            string[] lines = new string[2];
            lines[0] = "this.X = 3 ";
            lines[1] = "         + 4;";

            
            mPlugin.ApplyLinesInternal(lines, 0, 2, mElementRuntime.AssociatedIElement, new CodeContext(mElementRuntime));


            Assert.AreEqual(mElementRuntime.X, 7);




        }

        [Test]
        public void TestFileEvaluations()
        {
            string toParse = "SceneFile";
            CodeContext codeContext = new CodeContext(mElementRuntime);

            object result =
                mExpressionParser.EvaluateExpression(toParse, codeContext);

            if (result == null || (result is Scene) == false)
            {
                throw new Exception("References to textures is not working");
            }


            toParse = "GetFile(\"SceneFile\")";

            result =
                mExpressionParser.EvaluateExpression(toParse, new CodeContext(mElementRuntime));

            if (result == null || (result is Scene) == false)
            {
                throw new Exception("References to textures is not working");
            }


            toParse = "EnemyInfo";

            result =
                mExpressionParser.EvaluateExpression(toParse, new CodeContext(mElementRuntime));

            if (result == null || (result is RuntimeCsvRepresentation) == false)
            {
                throw new Exception("References to CSV files is not working");
            }

            toParse = "EnemyInfoVariable";
            result =
                mExpressionParser.EvaluateExpression(toParse, new CodeContext(mElementRuntime));
            if (result == null || (result is CsvEntry) == false)
            {
                throw new Exception("References to CSV variables is not working");
            }


            toParse = "EnemyInfo[\"Orc\"]";
            result =
                mExpressionParser.EvaluateExpression(toParse, new CodeContext(mElementRuntime));

            if (result == null || (result is CsvEntry) == false)
            {
                throw new Exception("Getting item from a CSV dictionary is not working");
            }

            toParse = "EnemyInfo[\"Orc\"].HP";
            result =
                mExpressionParser.EvaluateExpression(toParse, new CodeContext(mElementRuntime));

            if (((int)result) != 16)
            {
                throw new Exception("Getting item from a CSV dictionary is not working");
            }



        }
        
        [Test]
        public void TestStates()
        {

            string stringToParse = "ExpressionParserTestEntity";
            object result =
                mExpressionParser.EvaluateExpression(stringToParse, new CodeContext(mParentElementRuntime));
            if (result != ObjectFinder.Self.GetIElement("ExpressionParserTestEntity"))
            {
                throw new Exception("The returned value for ExpressionParserTestEntity.VariableState.Left does not match the Left state.  Result:\n" +
                result);
            }




            // First we'll do the simple tests of seeing if we can find the states
            string leftVariableStateString = "ExpressionParserTestEntity.VariableState.Left";
            result =
                mExpressionParser.EvaluateExpression(leftVariableStateString, new CodeContext(mParentElementRuntime));
            if (result != mElementRuntime.AssociatedIElement.GetState("Left"))
            {
                throw new Exception("The returned value for ExpressionParserTestEntity.VariableState.Left does not match the Left state.  Result:\n" + 
                result);
            }

            string stateCategory = "ExpressionParserTestEntity.StateCategory";
            result =
                mExpressionParser.EvaluateExpression(stateCategory, new CodeContext(mParentElementRuntime));
            if (result == null || result is StateSaveCategory == false)
            {
                throw new Exception("State Category isn't returning properly");
            }


            string topVariableStateList = "ExpressionParserTestEntity.StateCategory.Top";
            result =
                mExpressionParser.EvaluateExpression(topVariableStateList, new CodeContext(mParentElementRuntime));

            if (result != mElementRuntime.AssociatedIElement.GetState("Top"))
            {
                throw new Exception("The returned variable for " + topVariableStateList + " does not match the Top state");
            }

            topVariableStateList = "StateCategory.Top";
            result =
                mExpressionParser.EvaluateExpression(topVariableStateList, new CodeContext(mElementRuntime));

            if (result != mElementRuntime.AssociatedIElement.GetState("Top"))
            {
                throw new Exception("The returned variable for " + topVariableStateList + " does not match the Top state");
            }
            // Now what happens when we actually set them?



            string line = "this.FirstObject.CurrentState = ExpressionParserTestEntity.VariableState.Left;";
            string[] lines = new string[] { line };
            // prepare the variable so we know it changes
            mParentElementRuntime.ContainedElements[0].RelativeX = 0;
            mPlugin.ApplyLinesInternal(lines, 0, 1, mParentElementRuntime.AssociatedIElement, new CodeContext(mParentElementRuntime));
            mParentElementRuntime.ContainedElements[0].ForceUpdateDependencies();
            if (mParentElementRuntime.ContainedElements[0].X != -10)
            {
                throw new Exception("Assignment of states on contained Entities isn't working");
            }
            
            line = "this.FirstObject.SomeNewVar;";
            lines = new string[] { line };
            // prepare the variable so we know it changes
            var evaluated = mExpressionParser.EvaluateExpression(line, new CodeContext(mParentElementRuntime), ExpressionParseType.Evaluate);
            if ((double)evaluated != -10.0)
            {
                throw new Exception("Getting the value of custom variables set by states isn't working!");
            }
            



            line = "this.FirstObject.CurrentState = ExpressionParserTestEntity.StateCategory.Top;";
            lines = new string[] { line };
            // prepare the variable so we know it changes
            mParentElementRuntime.ContainedElements[0].RelativeY = 0;
            mPlugin.ApplyLinesInternal(lines, 0, 1, mParentElementRuntime.AssociatedIElement, new CodeContext(mParentElementRuntime));
            mParentElementRuntime.ContainedElements[0].ForceUpdateDependencies();
            if (mParentElementRuntime.ContainedElements[0].Y != 10)
            {
                throw new Exception("Assignment of states in categories on contained Entities isn't working");
            }


            Type type;
            mPlugin.GetTypeStringForValue("CurrentStateCategoryState", mElementRuntime, new CodeContext(mElementRuntime), out type);
            if (type != typeof(string))
            {
                throw new Exception("States need to come back as strings, but they're not!");
            }

            line = "this.CurrentStateCategoryState = StateCategory.Top";
            lines = new string[] { line };
            mElementRuntime.Y = 0;
            mPlugin.ApplyLinesInternal(lines, 0, 1, mElementRuntime.AssociatedIElement, new CodeContext(mElementRuntime));
            mElementRuntime.ForceUpdateDependencies();
            if (mElementRuntime.Y != 10)
            {
                throw new Exception("Assignment of categorized states on Entities isn't working");
            }
        
        }

        [Test]
        public void TestInterpolateBetween()
        {
            string logString = mLog.ToString();
            int countBefore = logString.CountOf("Interpolating ExpressionParserTestEntity between Left (State) and Right (State) with value");
            
            var expression = "this.InterpolateBetween(VariableState.Left, VariableState.Right, 0)";
            mExpressionParser.EvaluateExpression(expression, new CodeContext(mElementRuntime));

            var stateSave = mElementRuntime.GetStateRecursively("Left");



            Assert.AreEqual(mElementRuntime.X, -10.0f,
                          "InterpolateBetween code seems to fail - expect to be at " +
                           mElementRuntime.GetStateRecursively("Left").InstructionSaves[0].Value +
                          " but X is instead " + mElementRuntime.X);
        }

        [Test]
        public void TestLocalization()
        {
            LocalizationManager.CurrentLanguage = 1;

            string singleQuotes = "\"Hi\"";

            object evaluated = mExpressionParser.EvaluateExpression(singleQuotes, new CodeContext(null));

            if (((string)evaluated) != "Hi")
            {
                throw new Exception("Evaluating string constants is not working");
            }

            string expression = "LocalizationManager.Translate(\"T_Test\")";

            evaluated = mExpressionParser.EvaluateExpression(expression, new CodeContext(null));
            if (evaluated is string == false)
            {
                throw new Exception("Parsing LocalizationManager.Translate is not returning a string");
            }

            if (evaluated as string != "Test in English")
            {
                throw new Exception("Parsing LocalizationManager.Translate is returning the wrong value");
            }



        }

        [Test]
        public void TestGetTypeForValue()
        {
            Type type;
            
            string result = mPlugin.GetTypeStringForValue(
                "SpriteManager.Camera.OrthogonalWidth", mElementRuntime, new CodeContext(null), out type);

            if (result != typeof(float).FullName)
            {
                throw new Exception("ScriptParser didn't understand SpriteManager.Camera.OrthogonalWidth");
            }
        }

        [Test]
        public void TestAnonymousMethods()
        {
            CodeContext context = new CodeContext(mElementRuntime);
            ExecuteScriptInstruction instruction = new ExecuteScriptInstruction(context, "this.X = 64;");

            instruction.Execute();
            ScriptParsingPlugin.Self.CallUpdate();
            if (mElementRuntime.X != 64)
            {
                throw new Exception("ExecuteScriptInstruction is not positioning properly");
            }

            object result = mExpressionParser.EvaluateExpression("() => this.X = 3 ", context);

            if (result as ExecuteScriptInstruction == null)
            {
                throw new Exception("ExecuteScriptInstruction is not returning a script instruction");
            }

            result = mExpressionParser.EvaluateExpression("() => {this.X = 3;} ", context);

            if (result as ExecuteScriptInstruction == null)
            {
                throw new Exception("ExecuteScriptInstruction is not returning a script instruction");
            }

            string line = "this.Call( () => this.X = -32 ).After(0);";
            string[] lines = new string[] { line };
            mPlugin.ApplyLinesInternal(lines, 0, 1, mElementRuntime.AssociatedIElement, new CodeContext(mElementRuntime));
            if (mElementRuntime.Instructions.Count == 0)
            {
                throw new Exception("Instructions are not being added properly to elements when using the Call method");
            }

            // Forces the instructions to execute
            mElementRuntime.ExecuteInstructions(TimeManager.CurrentTime + 1);
            ScriptParsingPlugin.Self.CallUpdate();

            if (mElementRuntime.Instructions.Count != 0)
            {
                throw new Exception("The instructions are not executing properly");
            }

            if (mElementRuntime.X != -32)
            {
                throw new Exception("The instruction set earlier is not properly being applied");
            }

            // Test just like above, but multi-line
            lines = new string[] {
                "this.Call( ",
                "() => this.X = -128 ).After(0);"
            };

            mPlugin.ApplyLinesInternal(lines, 0, lines.Length, mElementRuntime.AssociatedIElement, new CodeContext(mElementRuntime));
            if (mElementRuntime.Instructions.Count == 0)
            {
                throw new Exception("Instructions are not being added properly to elements when using the Call method");
            }
            // Forces the instructions to execute
            mElementRuntime.ExecuteInstructions(TimeManager.CurrentTime + 1);
            ScriptParsingPlugin.Self.CallUpdate();
            if (mElementRuntime.X != -128)
            {
                throw new Exception("ExecuteScriptInstruction is not positioning properly");
            }

            // Let's test multiline with semicolons
            lines = new string[] {
            "this.Call(",
            "    () => ",
            "    { ",
            "        this.X = 256; ",
            "    }",
            "    ).After(0);"};

            int numberOfLinesInStatement = ScriptParsingPlugin.Self.GetNumberOfLinesInRegularStatement(lines, 0);

            if (numberOfLinesInStatement != 6)
            {
                throw new Exception("The number of lines should be 6, not " + numberOfLinesInStatement);
            }

            mPlugin.ApplyLinesInternal(lines, 0, lines.Length, mElementRuntime.AssociatedIElement, new CodeContext(mElementRuntime));
            if (mElementRuntime.Instructions.Count == 0)
            {
                throw new Exception("Instructions are not being added properly to elements when using the Call method");
            }
            // Forces the instructions to execute
            mElementRuntime.ExecuteInstructions(TimeManager.CurrentTime + 1);
            ScriptParsingPlugin.Self.CallUpdate();
            if (mElementRuntime.X != 256)
            {
                throw new Exception("ExecuteScriptInstruction is not positioning properly");
            }



            lines = new string[] {
            "this.Set(\"X\").To(3.2f).After(1);"};

            
            mPlugin.ApplyLinesInternal(lines, 0, lines.Length, mElementRuntime.AssociatedIElement, new CodeContext(mElementRuntime));
            if (mElementRuntime.Instructions.Count == 0)
            {
                throw new Exception("Instructions are not being added properly to elements when using the Call method");
            }
            // Forces the instructions to execute
            mElementRuntime.ExecuteInstructions(TimeManager.CurrentTime + 3);
            ScriptParsingPlugin.Self.CallUpdate();
            if (mElementRuntime.X != 3.2f)
            {
                throw new Exception("ExecuteScriptInstruction is not positioning properly");
            }
        }

        [Test]
        public void TestReferenceGetting()
        {
            CodeContext codeContext = new CodeContext(null);
            Sprite sprite = new Sprite();
            codeContext.VariableStack[0].Add("spriteObject", sprite);

            var result = mExpressionParser.EvaluateExpression("spriteObject.X", codeContext, ExpressionParseType.GetReference);

            if (result is AssignableReference == false)
            {
                throw new Exception("Evaluation of reference types is not working properly");
            }

            mPlugin.ApplyLinesInternal(new string[]{"spriteObject.X = 2;"}, 0, 1, null, codeContext);

            if (sprite.X != 2.0f)
            {
                throw new Exception("Assignmnt of code context variables isn't working");
            }

            //codeContext.VariableStack[stackDepth][memberName];
            mPlugin.ApplyLinesInternal(new string[] { "int m = 3;"}, 0, 1, null, codeContext);
            result = mExpressionParser.EvaluateExpression("m", codeContext, ExpressionParseType.GetReference);
            if (result is IAssignableReference == false)
            {
                throw new Exception("Getting reference to local variables is not working properly");
            }

            mPlugin.ApplyLinesInternal(new string[] {"m = 4;" }, 0, 1, null, codeContext);
            //codeContext.VariableStack[stackDepth][memberName];
            if ((int)(codeContext.VariableStack[0]["m"]) != 4)
            {
                throw new Exception("Assignmnt of code context variables isn't working");
            }

            result = mExpressionParser.EvaluateExpression("Sprite someSprite;", codeContext, ExpressionParseType.GetReference);

            int stackDepth;
            codeContext.GetVariableInformation("someSprite", out stackDepth);
            if (stackDepth == -1)
            {
                throw new Exception("Declaring variables like int j is not working");
            }
        }

        [Test]
        public void TestConditionalBlocks()
        {
            var type = ConditionalCodeBlock.GetBlockTypeStartingAt(new string[] { "if (true)" }, 0);
            if (type != BlockType.If)
            {
                throw new Exception("If blocks are not being identified as such");
            }

            type = ConditionalCodeBlock.GetBlockTypeStartingAt(new string[] { "while (true)" }, 0);
            if (type != BlockType.While)
            {
                throw new Exception("If blocks are not being identified as such");
            }

            type = ConditionalCodeBlock.GetBlockTypeStartingAt(new string[] { "foreach (var item in someList)" }, 0);
            if (type != BlockType.Foreach)
            {
                throw new Exception("If blocks are not being identified as such");
            }

            type = ConditionalCodeBlock.GetBlockTypeStartingAt(new string[] { "for (int i = 0; i < 10; i++)" }, 0);
            if (type != BlockType.For)
            {
                throw new Exception("If blocks are not being identified as such");
            }


            string[] lines = new string[]
            {
                "int m = 3;",
                "for(int i = 0; i < 10; i++)",
                "{",
                "   m++;",
                "}"
                
            };

            CodeContext codeContext = new CodeContext(null);
            mPlugin.ApplyLinesInternal(lines, 0, lines.Length, null, codeContext);

            if ((int)(codeContext.VariableStack[0]["m"]) != 13)
            {
                throw new Exception("for loops are not working");
            }



            lines = new string[]
            {
                "while( m < 18)",
                "{",
                "   m++;",
                "}"
                
            };

            mPlugin.ApplyLinesInternal(lines, 0, lines.Length, null, codeContext);

            if ((int)(codeContext.VariableStack[0]["m"]) != 18)
            {
                throw new Exception("while-loops are not working");
            }




        }

        [Test]
        public void TestAssigningFields()
        {
            CodeContext codeContext = new CodeContext(null);

            string[] lines = new string[] { "Vector3 vector3 = new Vector3();", "vector3.X = 4;" };
            mPlugin.ApplyLinesInternal(lines, 0, lines.Length, null, codeContext);
            Vector3 vector3 = (Vector3)codeContext.VariableStack[0]["vector3"];
            if (vector3.X != 4.0f)
            {
                throw new Exception("Assigning fields on local structs is not working.");
            }

            lines = new string[] { "Sprite sprite = new Sprite();",};
            mPlugin.ApplyLinesInternal(lines, 0, lines.Length, null, codeContext);

            // I need to fix this eventually:
            //lines = new string[] { "sprite.Position.X = 4;" };
            //mPlugin.ApplyLinesInternal(lines, 0, lines.Length, null, codeContext);

            //Sprite sprite = (Sprite)codeContext.VariableStack[0]["sprite"];
            //if (sprite.X != 4.0f)
            //{
            //    throw new Exception("Assigning fields on local structs is not working.");
            //}


        }

        [Test]
        public void TestTypeMismatch()
        {
            // We use "1" instead of 1.0f so it evaluates to an integer.
            // This should still work and return a valid polygon
            string[] lines = new string[] { "Polygon.CreateRectangle (1, 1);" };

            object evaluated = mExpressionParser.EvaluateExpression("Polygon.CreateRectangle (1, 1);", new CodeContext(null));
            if (evaluated == null || evaluated.GetType() != typeof(FlatRedBall.Math.Geometry.Polygon))
            {
                throw new Exception("Create Polygon is not working when passing integer arguments");
            }
        }

        [Test]
        public void TestEntities()
        {
            var customVariable = mPlugin.CreateCustomVariableFor(
                "float", "SpriteObject.X", "4", mElementRuntime, new CodeContext(mElementRuntime));


            mElementRuntime.ContainedElements[0].RelativeX = 0;
            string[] lines = new string[] { "this.SpriteObject.X = 4;" };
            CodeContext context = new CodeContext(mElementRuntime);
            mPlugin.ApplyLinesInternal(lines, 0, lines.Length, null, context);

            if (((PositionedObject)mElementRuntime.ContainedElements[0].DirectObjectReference).RelativeX != 0)
            {
                throw new Exception("Setting X is setting RelativeX, but it shouldn't when doing it through script");
            }

            mElementRuntime.ContainedElements[0].RelativeX = 0;
            lines = new string[] { "this.SpriteObject.RelativeX = 4;" };
            context = new CodeContext(mElementRuntime);
            mPlugin.ApplyLinesInternal(lines, 0, lines.Length, null, context);

            if ( ((PositionedObject)mElementRuntime.ContainedElements[0].DirectObjectReference).RelativeX != 4.0f)
            {
                throw new Exception("Setting X is setting RelativeX, but it shouldn't when doing it through script");
            }
        }

        private void TestEvaluation(string whatToEvaluate, object expectedValue, object objectToEvaluateOn = null)
        {
            CodeContext codeContext = new CodeContext(objectToEvaluateOn);
            codeContext.VariableStack.Add(new Dictionary<string, object>());
            var evaluated = mExpressionParser.EvaluateExpression(whatToEvaluate, codeContext);

            Assert.IsTrue(evaluated.Equals(expectedValue), "Expected to get value " + expectedValue + " but got " + evaluated + " instead");

        }
    }
}
