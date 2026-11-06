using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.CodeGeneration;

/// <summary>
/// Pins the text Glue generates for an entity's AddToManagers and AddToManagersBottomUp, for a spread of
/// inheritance shapes, so a change to the order objects are added to the engine shows up as a diff.
///
/// Why this matters: objects sharing a Z are drawn in the order they were added, and a derived entity's
/// generated AddToManagers adds its own named objects *before* calling base.AddToManagers, so base objects
/// are added (and draw) last. Games depend on that today. These tests make no claim the order is desirable,
/// only that it does not change by accident.
///
/// Each scenario has a snapshot under Snapshots/AddToManagers. A diff means the generated order changed:
/// if that was intended, regenerate with the environment variable UPDATE_ADDTOMANAGERS_SNAPSHOTS=1 and review
/// the diff in the PR. The "ResolvedAddOrder" test cases walk the base.AddToManagers call chain and pin the
/// order objects are actually added in, which is the thing games observe.
/// </summary>
// Replaces ObjectFinder.Self.GlueProject, so it runs in the sequential collection.
[Collection(nameof(TaskManagerSequentialCollection))]
public class AddToManagersOrderPinningTests : IDisposable
{
    private const string UpdateVariable = "UPDATE_ADDTOMANAGERS_SNAPSHOTS";

    private readonly GlueProjectSave _originalGlueProject;

    public AddToManagersOrderPinningTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalGlueProject = ObjectFinder.Self.GlueProject;
    }

    public void Dispose()
    {
        ObjectFinder.Self.GlueProject = _originalGlueProject;
    }

    #region Builders

    private static GlueProjectSave NewProject()
    {
        var project = new GlueProjectSave { FileVersion = GlueProjectSave.LatestVersion };
        ObjectFinder.Self.GlueProject = project;
        return project;
    }

    private static EntitySave AddEntity(GlueProjectSave project, string name, string baseName = null)
    {
        var entity = new EntitySave { Name = "Entities\\" + name };
        if (baseName != null)
        {
            entity.BaseEntity = "Entities\\" + baseName;
        }
        project.Entities.Add(entity);
        return entity;
    }

    private static NamedObjectSave AddSprite(GlueElement element, string name, float? z = null, string layer = null)
    {
        var nos = new NamedObjectSave
        {
            InstanceName = name,
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "FlatRedBall.Sprite",
            LayerOn = layer
        };
        if (z != null)
        {
            nos.SetVariable("Z", z.Value);
        }
        element.NamedObjects.Add(nos);
        return nos;
    }

    private static NamedObjectSave AddText(GlueElement element, string name, float? z = null)
    {
        var nos = new NamedObjectSave
        {
            InstanceName = name,
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "FlatRedBall.Graphics.Text"
        };
        if (z != null)
        {
            nos.SetVariable("Z", z.Value);
        }
        element.NamedObjects.Add(nos);
        return nos;
    }

    private static NamedObjectSave AddLayer(GlueElement element, string name)
    {
        var nos = new NamedObjectSave
        {
            InstanceName = name,
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "FlatRedBall.Graphics.Layer"
        };
        element.NamedObjects.Add(nos);
        return nos;
    }

    private static NamedObjectSave AddEntityInstance(GlueElement element, string name, string entityName, float? z = null)
    {
        var nos = new NamedObjectSave
        {
            InstanceName = name,
            SourceType = SourceType.Entity,
            SourceClassType = "Entities\\" + entityName,
            SourceName = "Entity: " + entityName
        };
        if (z != null)
        {
            nos.SetVariable("Z", z.Value);
        }
        element.NamedObjects.Add(nos);
        return nos;
    }

    private static NamedObjectSave AddEmptyList(GlueElement element, string name, string entityName)
    {
        var nos = new NamedObjectSave
        {
            InstanceName = name,
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "PositionedObjectList<T>",
            SourceClassGenericType = "Entities\\" + entityName
        };
        element.NamedObjects.Add(nos);
        return nos;
    }

    #endregion

    #region Generation

    private static string GenerateFor(GlueElement element)
    {
        var addToManagers = new CodeDocument();
        CodeWriter.GenerateAddToManagers(element, addToManagers);

        var bottomUp = new CodeDocument();
        CodeWriter.GenerateAddToManagersBottomUp(bottomUp, element);

        // Per-instance variables such as Z are assigned here, not in AddToManagers, and BottomUp reaches it.
        var assign = new CodeDocument();
        CodeWriter.GenerateAssignCustomVariables(assign, element);

        return Normalize(addToManagers.ToString())
            + "\n// ---- AddToManagersBottomUp ----\n" + Normalize(bottomUp.ToString())
            + "\n// ---- AssignCustomVariables ----\n" + Normalize(assign.ToString());
    }

    private static string Normalize(string text) =>
        text.Replace("\r\n", "\n").TrimEnd() + "\n";

    private static string GenerateAll(params GlueElement[] elements)
    {
        var builder = new StringBuilder();
        foreach (var element in elements)
        {
            builder.Append("//==== ").Append(element.Name).AppendLine(" ====");
            builder.Append(GenerateFor(element));
            builder.AppendLine();
        }
        return Normalize(builder.ToString());
    }

    /// <summary>
    /// Flattens the AddToManagers call chain the way the runtime executes it: each generated method is walked
    /// top to bottom, and its base.AddToManagers / base.AddToManagersBottomUp call is replaced by the base
    /// class's lines. Returns one "[Class.Method] line" entry per line that adds, attaches, or positions
    /// something, in execution order. The virtual dispatch from base.AddToManagers into the derived
    /// AddToManagersBottomUp override is followed too, since that is where the base's call lands.
    /// </summary>
    private static List<string> ResolveRuntimeOrder(GlueElement leaf)
    {
        var result = new List<string>();
        Expand(leaf, "AddToManagers", leaf, result);
        return result;
    }

    private static void Expand(GlueElement current, string method, GlueElement leaf, List<string> result)
    {
        var body = new CodeDocument();
        if (method == "AddToManagers")
        {
            CodeWriter.GenerateAddToManagers(current, body);
        }
        else if (method == "AssignCustomVariables")
        {
            CodeWriter.GenerateAssignCustomVariables(body, current);
        }
        else
        {
            CodeWriter.GenerateAddToManagersBottomUp(body, current);
        }

        var shortName = current.Name.Substring(current.Name.LastIndexOf('\\') + 1);

        foreach (var rawLine in Normalize(body.ToString()).Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line == "{" || line == "}" || line.StartsWith("public ") || line.StartsWith("//"))
            {
                continue;
            }

            if (line.StartsWith("base.AddToManagers(") || line.StartsWith("base.AddToManagersBottomUp(") ||
                line.StartsWith("base.AssignCustomVariables("))
            {
                var baseElement = ObjectFinder.Self.GetBaseElement(current);
                if (baseElement != null)
                {
                    var baseMethod = line.StartsWith("base.AddToManagersBottomUp(") ? "AddToManagersBottomUp"
                        : line.StartsWith("base.AssignCustomVariables(") ? "AssignCustomVariables"
                        : "AddToManagers";
                    Expand(baseElement, baseMethod, leaf, result);
                }
                continue;
            }

            if (line == "AssignCustomVariables(false);")
            {
                // Virtual call: lands on the leaf's override, whose first line calls base.
                Expand(leaf, "AssignCustomVariables", leaf, result);
                continue;
            }

            if (line == "AddToManagersBottomUp(layerToAddTo);" || line == "AddToManagersBottomUp();")
            {
                // Virtual call: lands on the most-derived override, which is the leaf's.
                Expand(leaf, "AddToManagersBottomUp", leaf, result);
                continue;
            }

            result.Add($"[{shortName}.{method}] {line}");
        }
    }

    #endregion

    #region Snapshot plumbing

    private static string SnapshotPath(string name) =>
        Path.Combine(RepoPaths.FrbRoot, "FRBDK", "Glue", "Tests", "GlueUnitTests", "CodeGeneration",
            "Snapshots", "AddToManagers", name + ".txt");

    private static void ShouldMatchSnapshot(string name, string actual)
    {
        actual = Normalize(actual);
        var path = SnapshotPath(name);

        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, actual);
            return;
        }

        File.Exists(path).ShouldBeTrue(
            $"Missing snapshot {path}. Run once with {UpdateVariable}=1 to create it, then review it.");

        var expected = Normalize(File.ReadAllText(path));
        actual.ShouldBe(expected,
            $"Generated AddToManagers code for '{name}' changed. If intended, regenerate with {UpdateVariable}=1.");
    }

    #endregion

    #region Scenarios

    // Each scenario builds a project and returns the leaf element plus every element in its chain, base first.
    private static (GlueElement leaf, GlueElement[] all) Build(string scenario)
    {
        var project = NewProject();

        switch (scenario)
        {
            case "SingleSprite":
            {
                var e = AddEntity(project, "Solo");
                AddSprite(e, "SpriteA");
                return (e, new GlueElement[] { e });
            }
            case "MultipleSpritesNoZ":
            {
                var e = AddEntity(project, "Solo");
                AddSprite(e, "SpriteA");
                AddSprite(e, "SpriteB");
                AddSprite(e, "SpriteC");
                return (e, new GlueElement[] { e });
            }
            case "BaseAndDerivedSprites":
            {
                var b = AddEntity(project, "Base");
                AddSprite(b, "BaseSprite");
                var d = AddEntity(project, "Derived", "Base");
                AddSprite(d, "DerivedSprite");
                return (d, new GlueElement[] { b, d });
            }
            case "BaseAndDerivedSameZ":
            {
                var b = AddEntity(project, "Base");
                AddSprite(b, "BaseSprite", z: 1);
                var d = AddEntity(project, "Derived", "Base");
                AddSprite(d, "DerivedSprite", z: 1);
                return (d, new GlueElement[] { b, d });
            }
            case "BaseAndDerivedDifferentZ":
            {
                var b = AddEntity(project, "Base");
                AddSprite(b, "BaseSprite", z: 5);
                var d = AddEntity(project, "Derived", "Base");
                AddSprite(d, "DerivedSprite", z: -5);
                return (d, new GlueElement[] { b, d });
            }
            case "ManySpritesMixedZOnBothLevels":
            {
                var b = AddEntity(project, "Base");
                AddSprite(b, "BaseA", z: 0);
                AddSprite(b, "BaseB", z: 1);
                AddSprite(b, "BaseC");
                var d = AddEntity(project, "Derived", "Base");
                AddSprite(d, "DerivedA", z: 0);
                AddSprite(d, "DerivedB", z: 1);
                AddSprite(d, "DerivedC");
                return (d, new GlueElement[] { b, d });
            }
            case "ThreeLevelInheritance":
            {
                var a = AddEntity(project, "Grand");
                AddSprite(a, "GrandSprite");
                var b = AddEntity(project, "Parent", "Grand");
                AddSprite(b, "ParentSprite");
                var c = AddEntity(project, "Child", "Parent");
                AddSprite(c, "ChildSprite");
                return (c, new GlueElement[] { a, b, c });
            }
            case "FourLevelInheritance":
            {
                var a = AddEntity(project, "L0");
                AddSprite(a, "S0", z: 0);
                var b = AddEntity(project, "L1", "L0");
                AddSprite(b, "S1", z: 0);
                var c = AddEntity(project, "L2", "L1");
                AddSprite(c, "S2", z: 0);
                var d = AddEntity(project, "L3", "L2");
                AddSprite(d, "S3", z: 0);
                return (d, new GlueElement[] { a, b, c, d });
            }
            case "DerivedWithNoObjects":
            {
                var b = AddEntity(project, "Base");
                AddSprite(b, "BaseSprite");
                var d = AddEntity(project, "Derived", "Base");
                return (d, new GlueElement[] { b, d });
            }
            case "BaseWithNoObjects":
            {
                var b = AddEntity(project, "Base");
                var d = AddEntity(project, "Derived", "Base");
                AddSprite(d, "DerivedSprite");
                return (d, new GlueElement[] { b, d });
            }
            // No shape (AxisAlignedRectangle etc.) scenario on purpose: how shapes are generated depends on which
            // plugins earlier tests in the process registered, so its snapshot differed between a filtered run
            // and the full suite. Sprites, text, layers and entity instances do not have that dependence.
            case "SpritesAndText":
            {
                var b = AddEntity(project, "Base");
                AddText(b, "BaseText", z: 1);
                AddSprite(b, "BaseSprite", z: 1);
                var d = AddEntity(project, "Derived", "Base");
                AddText(d, "DerivedText", z: 1);
                AddSprite(d, "DerivedSprite", z: 1);
                return (d, new GlueElement[] { b, d });
            }
            case "LayersOnBothLevels":
            {
                var b = AddEntity(project, "Base");
                AddLayer(b, "BaseLayer");
                AddSprite(b, "BaseSprite", layer: "BaseLayer");
                var d = AddEntity(project, "Derived", "Base");
                AddLayer(d, "DerivedLayer");
                AddSprite(d, "DerivedSprite", layer: "DerivedLayer");
                return (d, new GlueElement[] { b, d });
            }
            case "DerivedSpriteOnBaseLayer":
            {
                var b = AddEntity(project, "Base");
                AddLayer(b, "BaseLayer");
                AddSprite(b, "BaseSprite", layer: "BaseLayer");
                var d = AddEntity(project, "Derived", "Base");
                AddSprite(d, "DerivedSprite", layer: "BaseLayer");
                return (d, new GlueElement[] { b, d });
            }
            case "EntityInstancesInBothLevels":
            {
                var child = AddEntity(project, "Child");
                var b = AddEntity(project, "Base");
                AddEntityInstance(b, "BaseChild", "Child", z: 1);
                AddSprite(b, "BaseSprite", z: 1);
                var d = AddEntity(project, "Derived", "Base");
                AddEntityInstance(d, "DerivedChild", "Child", z: 1);
                AddSprite(d, "DerivedSprite", z: 1);
                return (d, new GlueElement[] { child, b, d });
            }
            case "PositionedObjectLists":
            {
                var child = AddEntity(project, "Child");
                var b = AddEntity(project, "Base");
                AddEmptyList(b, "BaseList", "Child");
                AddSprite(b, "BaseSprite");
                var d = AddEntity(project, "Derived", "Base");
                AddEmptyList(d, "DerivedList", "Child");
                AddSprite(d, "DerivedSprite");
                return (d, new GlueElement[] { child, b, d });
            }
            case "DerivedExposesBaseSprite":
            {
                var b = AddEntity(project, "Base");
                var baseSprite = AddSprite(b, "BaseSprite", z: 1);
                baseSprite.ExposedInDerived = true;
                var d = AddEntity(project, "Derived", "Base");
                AddSprite(d, "DerivedSprite", z: 1);
                return (d, new GlueElement[] { b, d });
            }
            case "DerivedOverridesBaseSpriteVariable":
            {
                var b = AddEntity(project, "Base");
                var baseSprite = AddSprite(b, "BaseSprite", z: 1);
                baseSprite.ExposedInDerived = true;
                var d = AddEntity(project, "Derived", "Base");
                var overriding = new NamedObjectSave
                {
                    InstanceName = "BaseSprite",
                    SourceType = SourceType.FlatRedBallType,
                    SourceClassType = "FlatRedBall.Sprite",
                    DefinedByBase = true
                };
                overriding.SetVariable("Z", 9f);
                d.NamedObjects.Add(overriding);
                AddSprite(d, "DerivedSprite", z: 1);
                return (d, new GlueElement[] { b, d });
            }
            case "ZOnlyOnDerived":
            {
                var b = AddEntity(project, "Base");
                AddSprite(b, "BaseSprite");
                var d = AddEntity(project, "Derived", "Base");
                AddSprite(d, "DerivedSprite", z: 3);
                return (d, new GlueElement[] { b, d });
            }
            case "ZOnlyOnBase":
            {
                var b = AddEntity(project, "Base");
                AddSprite(b, "BaseSprite", z: 3);
                var d = AddEntity(project, "Derived", "Base");
                AddSprite(d, "DerivedSprite");
                return (d, new GlueElement[] { b, d });
            }
            case "PooledBase":
            {
                var b = AddEntity(project, "Base");
                b.CreatedByOtherEntities = true;
                AddSprite(b, "BaseSprite");
                var d = AddEntity(project, "Derived", "Base");
                d.CreatedByOtherEntities = true;
                AddSprite(d, "DerivedSprite");
                return (d, new GlueElement[] { b, d });
            }
            case "SiblingsShareBase":
            {
                var b = AddEntity(project, "Base");
                AddSprite(b, "BaseSprite");
                var d1 = AddEntity(project, "DerivedOne", "Base");
                AddSprite(d1, "OneSprite");
                var d2 = AddEntity(project, "DerivedTwo", "Base");
                AddSprite(d2, "TwoSprite");
                return (d2, new GlueElement[] { b, d1, d2 });
            }
            case "BaseDeclaredAfterDerivedInProject":
            {
                // List order in the project file must not matter to the generated order.
                var d = AddEntity(project, "Derived", "Base");
                AddSprite(d, "DerivedSprite");
                var b = AddEntity(project, "Base");
                AddSprite(b, "BaseSprite");
                return (d, new GlueElement[] { b, d });
            }
            default:
                throw new ArgumentException(scenario);
        }
    }

    public static IEnumerable<object[]> ScenarioNames() => new[]
    {
        "SingleSprite",
        "MultipleSpritesNoZ",
        "BaseAndDerivedSprites",
        "BaseAndDerivedSameZ",
        "BaseAndDerivedDifferentZ",
        "ManySpritesMixedZOnBothLevels",
        "ThreeLevelInheritance",
        "FourLevelInheritance",
        "DerivedWithNoObjects",
        "BaseWithNoObjects",
        "SpritesAndText",
        "LayersOnBothLevels",
        "DerivedSpriteOnBaseLayer",
        "EntityInstancesInBothLevels",
        "PositionedObjectLists",
        "DerivedExposesBaseSprite",
        "DerivedOverridesBaseSpriteVariable",
        "ZOnlyOnDerived",
        "ZOnlyOnBase",
        "PooledBase",
        "SiblingsShareBase",
        "BaseDeclaredAfterDerivedInProject",
    }.Select(name => new object[] { name });

    #endregion

    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public void GeneratedCode_MatchesSnapshot(string scenario)
    {
        var (_, all) = Build(scenario);

        ShouldMatchSnapshot(scenario, GenerateAll(all));
    }

    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public void ResolvedAddOrder_MatchesSnapshot(string scenario)
    {
        var (leaf, _) = Build(scenario);

        var order = ResolveRuntimeOrder(leaf);

        ShouldMatchSnapshot(scenario + ".ResolvedOrder", string.Join("\n", order));
    }
}
