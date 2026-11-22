using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SaveClasses.Helpers;
using FlatRedBall.Instructions;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.CodeGeneration;

// A tunneled variable (CircleX -> Circle.X) gets an `if (Circle.Parent != null) RelativeX else X` branch in
// InterpolateBetween. The else used to be closed on a different condition than the one that opened it, so it
// stayed open and every later variable's assignment landed inside it: CircleY was only interpolated when the
// circle had no parent.
[Collection(nameof(TaskManagerSequentialCollection))]
public class StateInterpolationRelativeBranchTests
{
    public StateInterpolationRelativeBranchTests()
    {
        GlueTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public void AssignValuesUsingStartingValues_ClosesTheRelativeElse_BeforeTheNextVariable()
    {
        string generated;
        using (new InitializedInstructionManagerScope())
        {
            var glueProject = new GlueProjectSave { FileVersion = GlueProjectSave.LatestVersion };
            ObjectFinder.Self.GlueProject = glueProject;

            var entity = new EntitySave { Name = @"Entities\TwoTunneledVariables" };
            glueProject.Entities.Add(entity);
            entity.NamedObjects.Add(new NamedObjectSave
            {
                InstanceName = "Circle",
                SourceType = SourceType.FlatRedBallType,
                SourceClassType = "Circle",
            });
            entity.CustomVariables.Add(new CustomVariable { Name = "CircleX", Type = "float", SourceObject = "Circle", SourceObjectProperty = "X" });
            entity.CustomVariables.Add(new CustomVariable { Name = "CircleY", Type = "float", SourceObject = "Circle", SourceObjectProperty = "Y" });

            var characteristics = new Dictionary<string, InterpolationCharacteristic>
            {
                ["CircleX"] = InterpolationCharacteristic.CanInterpolate,
                ["CircleY"] = InterpolationCharacteristic.CanInterpolate,
            };

            ICodeBlock codeBlock = new CodeDocument();
            typeof(StateCodeGenerator)
                .GetMethod("AssignValuesUsingStartingValues", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, new object[] { entity, codeBlock, characteristics });
            generated = codeBlock.ToString();
        }

        // Premise: the relative branch is written at all. Without it this test proves nothing.
        generated.ShouldContain("Circle.RelativeX", Case.Sensitive);

        var lines = generated.Split('\n');
        string Indentation(string startsWith) =>
            lines.Single(line => line.TrimStart().StartsWith(startsWith)).TakeWhile(char.IsWhiteSpace).Aggregate("", (s, c) => s + c);

        Indentation("if (setCircleY)").ShouldBe(Indentation("if (setCircleX)"),
            $"CircleY's block is nested inside CircleX's else:\n{generated}");
    }

    /// <summary>
    /// Fills InstructionManager's absolute/relative table (X -> RelativeX), which codegen reads and Glue.exe
    /// fills at startup, and empties it again afterwards. The table is process-wide and Initialize can't run
    /// twice, so leaving it filled would change the text every later codegen test generates.
    /// </summary>
    sealed class InitializedInstructionManagerScope : IDisposable
    {
        readonly List<IList> _toClear = new();
        readonly List<IDictionary> _dictionariesToClear = new();

        public InitializedInstructionManagerScope()
        {
            if (InstructionManager.GetRelativeForAbsolute("X") != null)
            {
                return;
            }

            foreach (var field in typeof(InstructionManager).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                switch (field.GetValue(null))
                {
                    case IList list when list.Count == 0:
                        _toClear.Add(list);
                        break;
                    case IDictionary dictionary when dictionary.Count == 0:
                        _dictionariesToClear.Add(dictionary);
                        break;
                }
            }

            InstructionManager.Initialize();
        }

        public void Dispose()
        {
            _toClear.ForEach(list => list.Clear());
            _dictionariesToClear.ForEach(dictionary => dictionary.Clear());
        }
    }
}
