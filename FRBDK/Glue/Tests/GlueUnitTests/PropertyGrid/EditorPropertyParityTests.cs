using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using FlatRedBall.Glue;
using FlatRedBall.Glue.EditorProperties;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Services;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.PropertyGrid;

/// <summary>
/// The file and variable properties moved out of the WinForms displayers into <see cref="ReferencedFileSavePropertyModel"/>
/// and <see cref="CustomVariablePropertyModel"/>. PropertyGridParity.golden.txt is what the displayers exposed before
/// the move (name | type | read-only | values offered for text), for <see cref="EditorPropertyFixtures"/>. The grid
/// and the model must both still match it.
/// </summary>
public class EditorPropertyParityTests : IDisposable
{
    /// <summary>The grid shows the model's ProjectSpecificFiles as a list with its own editor, under this name.</summary>
    const string ProjectSpecificFilesInGrid = "Project Specific Files";

    readonly GlueProjectSave originalProject;
    readonly EditorPropertyFixtures.Fixture fixture;

    public EditorPropertyParityTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        originalProject = ObjectFinder.Self.GlueProject;
        fixture = EditorPropertyFixtures.Create();
        ObjectFinder.Self.GlueProject = fixture.Project;
    }

    public void Dispose()
    {
        ObjectFinder.Self.GlueProject = originalProject;
    }

    [Fact]
    public void Grid_ShouldShowWhatItShowedBeforeTheModel()
    {
        foreach (var (key, grid) in GridSections())
        {
            Compare(key, grid);
        }
    }

    [Fact]
    public void Model_ShouldListWhatTheGridShowedBeforeTheModel()
    {
        foreach (var (key, model) in ModelSections())
        {
            Compare(key, model.Select(line => line.StartsWith(nameof(ReferencedFileSave.ProjectSpecificFiles) + " |")
                ? $"{ProjectSpecificFilesInGrid} | {typeof(ProjectSpecificFileDisplayer)} | editable | "
                : line).ToList());
        }
    }

    [Fact]
    public void Model_ShouldOfferTheValuesTheGridOffers()
    {
        // Variable types depend on what the whole process has loaded, so the golden file can't pin them; the grid
        // and the model must still agree.
        var grid = GridSections().ToDictionary(item => item.key, item => item.lines);
        foreach (var (key, model) in ModelSections())
        {
            var gridOptions = grid[key].Where(line => line.Split(" | ")[3].Length > 0).ToList();
            var modelOptions = model.Where(line => line.Split(" | ")[3].Length > 0).ToList();
            modelOptions.ShouldBe(gridOptions, key);
        }
    }

    [Fact]
    public void Model_ShouldSetFilePropertiesOnTheFile()
    {
        var rfs = fixture.Files.Single(item => item.Key == "global csv").File;
        var properties = Builder.Get<ReferencedFileSavePropertyModel>().GetProperties(rfs);

        properties.Single(item => item.Name == nameof(ReferencedFileSave.UniformRowType)).SetValue("string[]");
        properties.Single(item => item.Name == nameof(ReferencedFileSave.CreatesDictionary)).SetValue(false);

        rfs.UniformRowType.ShouldBe("string[]");
        rfs.CreatesDictionary.ShouldBeFalse();
        properties.Single(item => item.Name == nameof(ReferencedFileSave.UniformRowType)).GetValue().ShouldBe("string[]");
    }

    [Fact]
    public void Model_ShouldSetANewVariablesTypeAndDefaultValue()
    {
        var (variable, element) = fixture.Variables.Where(item => item.Key == "new float").Select(item => (item.Variable, item.Element)).Single();
        var properties = Builder.Get<CustomVariablePropertyModel>().GetProperties(variable, element);

        properties.Single(item => item.Name == nameof(CustomVariable.DefaultValue)).SetValue("7.5");
        variable.DefaultValue.ShouldBe(7.5f);

        properties.Single(item => item.Name == nameof(CustomVariable.Type)).SetValue("int");
        variable.Type.ShouldBe("int");
    }

    void Compare(string key, IReadOnlyList<string> actual)
    {
        var expected = Golden()[key];
        // Variable types depend on what the process has loaded (see Model_ShouldOfferTheValuesTheGridOffers).
        static string WithoutTypeOptions(string line) => line.StartsWith("Type | System.String | editable") ? "Type | System.String | editable" : line;
        actual.Select(WithoutTypeOptions).ToList().ShouldBe(expected.Select(WithoutTypeOptions).ToList(), key);
    }

    IEnumerable<(string key, List<string> lines)> GridSections()
    {
        foreach (var file in fixture.Files)
        {
            var displayer = new ReferencedFileSavePropertyGridDisplayer(Builder.Get<ReferencedFileSavePropertyModel>()) { Instance = file.File };
            displayer.RefreshOnTimer = false;
            yield return ("file: " + file.Key, Describe(displayer));
        }
        foreach (var variable in fixture.Variables)
        {
            var displayer = new CustomVariablePropertyGridDisplayer(Builder.Get<CustomVariablePropertyModel>())
            {
                CurrentElement = variable.Element,
                RefreshOnTimer = false,
            };
            displayer.Instance = variable.Variable;
            yield return ("variable: " + variable.Key, Describe(displayer));
        }
    }

    IEnumerable<(string key, List<string> lines)> ModelSections()
    {
        foreach (var file in fixture.Files)
        {
            yield return ("file: " + file.Key, Describe(Builder.Get<ReferencedFileSavePropertyModel>().GetProperties(file.File)));
        }
        foreach (var variable in fixture.Variables)
        {
            yield return ("variable: " + variable.Key,
                Describe(Builder.Get<CustomVariablePropertyModel>().GetProperties(variable.Variable, variable.Element)));
        }
    }

    static Dictionary<string, List<string>> Golden()
    {
        var path = Path.Combine(RepoPaths.FrbRoot, "FRBDK", "Glue", "Tests", "GlueUnitTests", "PropertyGrid", "PropertyGridParity.golden.txt");
        var sections = new Dictionary<string, List<string>>();
        List<string>? current = null;
        foreach (var line in File.ReadAllLines(path))
        {
            if (line.StartsWith("# "))
            {
                current = new List<string>();
                sections[line.Substring(2)] = current;
            }
            else if (line.Length > 0)
            {
                current!.Add(line);
            }
        }
        return sections;
    }

    static List<string> Describe(PropertyGridDisplayer displayer) =>
        ((ICustomTypeDescriptor)displayer).GetProperties(Array.Empty<Attribute>()).Cast<PropertyDescriptor>()
            .Select(item => Line(item.Name, item.PropertyType, item.IsReadOnly, GridOptions(item)))
            .OrderBy(item => item, StringComparer.Ordinal).ToList();

    static List<string> Describe(IReadOnlyList<EditorProperty> properties) =>
        properties.Select(item => Line(item.Name, item.Type, item.IsReadOnly,
                item.Type == typeof(string) ? item.Options?.Invoke() : null))
            .OrderBy(item => item, StringComparer.Ordinal).ToList();

    static string Line(string name, Type type, bool isReadOnly, IEnumerable<string>? options) =>
        $"{name} | {type} | {(isReadOnly ? "read-only" : "editable")} | {string.Join("; ", options ?? Array.Empty<string>())}";

    static IEnumerable<string>? GridOptions(PropertyDescriptor descriptor)
    {
        var converter = descriptor.Converter;
        if (descriptor.PropertyType != typeof(string) || converter == null ||
            !converter.GetStandardValuesSupported() || !converter.GetStandardValuesExclusive())
        {
            return null;
        }
        return converter.GetStandardValues()!.Cast<object>().Select(item => item?.ToString() ?? "");
    }
}
