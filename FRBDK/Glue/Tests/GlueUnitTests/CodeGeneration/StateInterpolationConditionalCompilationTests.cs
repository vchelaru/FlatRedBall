using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SaveClasses.Helpers;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.CodeGeneration;

// GitHub issue #2094 - InterpolateBetween's per-variable assignment is wrapped in
// #if <symbol>/#endif when the variable is sourced from a conditionally-compiled-out
// NamedObjectSave (NamedObjectSaveCodeGenerator.AddIfConditionalSymbolIfNecesssary /
// AddEndIfIfNecessary). StateCodeGenerator.AssignValuesUsingStartingValues used to call
// curBlock.End() (closing the enclosing "if (setX)" block) *before* AddEndIfIfNecessary, so
// the "#endif" line landed after the whole if-block instead of before its closing "}" - when
// the symbol is undefined the preprocessor strips that "}" along with the rest of the region,
// producing CS1513 in the generated file.
[Collection(nameof(TaskManagerSequentialCollection))]
public class StateInterpolationConditionalCompilationTests
{
    public StateInterpolationConditionalCompilationTests()
    {
        GlueTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public void AssignValuesUsingStartingValues_KeepsIfBlockClosingBraceOutsideConditionalCompilationRegion()
    {
        var glueProject = new GlueProjectSave { FileVersion = GlueProjectSave.LatestVersion };
        ObjectFinder.Self.GlueProject = glueProject;

        var entity = new EntitySave { Name = @"Entities\ConditionalCompilationEntity" };
        glueProject.Entities.Add(entity);

        var namedObject = new NamedObjectSave
        {
            InstanceName = "ConditionallyCompiledOutCircle",
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "Circle",
            ConditionalCompilationSymbols = "DONT_INCLUDE_ME",
        };
        entity.NamedObjects.Add(namedObject);

        var variable = new CustomVariable
        {
            Name = "ConditionallyCompiledOutCircleX",
            Type = "float",
            SourceObject = "ConditionallyCompiledOutCircle",
            SourceObjectProperty = "X",
        };
        entity.CustomVariables.Add(variable);

        var characteristics = new Dictionary<string, InterpolationCharacteristic>
        {
            ["ConditionallyCompiledOutCircleX"] = InterpolationCharacteristic.CanInterpolate,
        };

        ICodeBlock codeBlock = new CodeDocument();

        // AssignValuesUsingStartingValues is private - it's the exact method the fix lives in, and
        // everything it calls (GetLeftSideOfEquals, AddAssignmentForInterpolationForVariable,
        // NamedObjectSaveCodeGenerator.AddIfConditionalSymbolIfNecesssary/AddEndIfIfNecessary) runs for
        // real; only the upstream "can this variable interpolate" classification is supplied directly
        // instead of being computed, since that classification is unrelated to this bug.
        var method = typeof(StateCodeGenerator).GetMethod(
            "AssignValuesUsingStartingValues", BindingFlags.NonPublic | BindingFlags.Static);
        method.Invoke(null, new object[] { entity, codeBlock, characteristics });

        var generatedSource = codeBlock.ToString();

        generatedSource.ShouldContain("#if DONT_INCLUDE_ME");
        generatedSource.ShouldContain("#endif");

        // Simulate the preprocessor with the symbol undefined - the normal case for anyone who hasn't
        // defined DONT_INCLUDE_ME - by stripping every "#if DONT_INCLUDE_ME"/"#endif" region. If the
        // closing brace of the enclosing "if (setConditionallyCompiledOutCircleX)" block was emitted
        // inside that region, brace counts in what's left go unbalanced, which is exactly CS1513.
        var withSymbolUndefined = StripConditionalRegion(generatedSource, "DONT_INCLUDE_ME");

        var openBraceCount = withSymbolUndefined.Count(c => c == '{');
        var closeBraceCount = withSymbolUndefined.Count(c => c == '}');
        closeBraceCount.ShouldBe(openBraceCount,
            $"Generated code has unbalanced braces once #if DONT_INCLUDE_ME is stripped (CS1513):\n{withSymbolUndefined}");
    }

    static string StripConditionalRegion(string source, string symbol)
    {
        var resultLines = new List<string>();
        var skipping = false;

        foreach (var line in source.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed == $"#if {symbol}")
            {
                skipping = true;
                continue;
            }
            if (skipping && trimmed == "#endif")
            {
                skipping = false;
                continue;
            }
            if (!skipping)
            {
                resultLines.Add(line);
            }
        }

        return string.Join("\n", resultLines);
    }
}
