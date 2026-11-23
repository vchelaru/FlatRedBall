using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using FlatRedBall.Glue.EditorProperties;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Services;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.PropertyGrid;

/// <summary>
/// The object, screen and entity properties moved out of the WinForms displayers into
/// <see cref="NamedObjectSavePropertyModel"/>, <see cref="EntitySavePropertyModel"/> and <see cref="ScreenSavePropertyModel"/>,
/// and the Gum, Collision and DamageDealing additions from the displayer hook to <c>AdjustEntityProperties</c>.
/// ElementPropertyParity.golden.txt is what the displayers (with those plugins' handlers) exposed before the move
/// (name | type | read-only | values offered for text), for <see cref="ElementPropertyFixtures"/>. The grid and the
/// models must both still match it.
/// </summary>
public class ElementPropertyParityTests : IDisposable
{
    /// <summary>Their values include every asset type the process has loaded, so the golden file can't pin them.</summary>
    static readonly string[] ProcessDependentOptions =
    {
        nameof(NamedObjectSave.SourceClassType), nameof(NamedObjectSave.SourceClassGenericType), nameof(EntitySave.BaseEntity),
    };

    const string PluginTestEntityName = "Entities\\PluginParityTestEntity";

    readonly GlueProjectSave originalProject;
    readonly GlueElement originalElement;
    readonly NamedObjectSave originalObject;
    readonly ElementPropertyFixtures.Fixture fixture;

    public ElementPropertyParityTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        originalProject = ObjectFinder.Self.GlueProject;
        originalElement = GlueState.Self.CurrentElement;
        originalObject = GlueState.Self.CurrentNamedObjectSave;
        fixture = ElementPropertyFixtures.Create();
        ObjectFinder.Self.GlueProject = fixture.Project;
    }

    public void Dispose()
    {
        ObjectFinder.Self.GlueProject = originalProject;
        GlueState.Self.CurrentElement = originalElement;
        GlueState.Self.CurrentNamedObjectSave = originalObject;
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
            Compare(key, model);
        }
    }

    [Fact]
    public void Model_ShouldOfferTheValuesTheGridOffers()
    {
        var grid = GridSections().ToDictionary(item => item.key, item => item.lines);
        foreach (var (key, model) in ModelSections())
        {
            model.ShouldBe(grid[key], key);
        }
    }

    [Fact]
    public void Golden_ShouldCoverEverySection()
    {
        ModelSections().Select(item => item.key).OrderBy(item => item).ToList()
            .ShouldBe(Golden().Keys.OrderBy(item => item).ToList());
    }

    [Fact]
    public void Model_ShouldSetObjectPropertiesOnTheObject()
    {
        var item = fixture.Objects.Single(item => item.Key == "screen sprite");
        var properties = ObjectModel().GetProperties(item.Object, item.Element);

        properties.Single(property => property.Name == nameof(NamedObjectSave.LayerOn)).SetValue(AvailableLayersTypeConverter.TopLayerName);
        properties.Single(property => property.Name == nameof(NamedObjectSave.IsZBuffered)).SetValue(true);

        item.Object.LayerOn.ShouldBe(AvailableLayersTypeConverter.TopLayerName);
        item.Object.IsZBuffered.ShouldBeTrue();
    }

    [Fact]
    public void Model_ShouldKeepPluginFlagsInTheEntitysCustomProperties()
    {
        var player = (EntitySave)fixture.Elements.Single(item => item.Key == "icollidable entity").Element;
        var properties = EntityModel(withPlugins: true).GetProperties(player);
        var damageable = properties.Single(item => item.Name == OfficialPlugins.DamageDealingPlugin.MainDamageDealingPlugin.ImplementsIDamageable);

        damageable.GetValue().ShouldBe(false);
        damageable.SetValue(true);

        player.Properties.GetValue(OfficialPlugins.DamageDealingPlugin.MainDamageDealingPlugin.ImplementsIDamageable).ShouldBe(true);
        damageable.GetValue().ShouldBe(true);
        // Implementing IDamageable shows the switch that turns its generated properties off.
        EntityModel(withPlugins: true).GetProperties(player)
            .ShouldContain(item => item.Name == OfficialPlugins.DamageDealingPlugin.MainDamageDealingPlugin.SuppressDamagePropertyCodeGeneration);
    }

    [Fact]
    public void Model_ShouldSetScreenPropertiesAndReadTheNameWithoutItsFolder()
    {
        var level1 = (ScreenSave)fixture.Elements.Single(item => item.Key == "derived screen").Element;
        var properties = ScreenModel().GetProperties(level1);

        properties.Single(item => item.Name == nameof(ScreenSave.BaseScreen)).SetValue("<NONE>");

        // The setter stores <NONE> as no base.
        level1.BaseScreen.ShouldBeEmpty();
        properties.Single(item => item.Name == nameof(ScreenSave.Name)).GetValue().ShouldBe("Level1");
    }

    [Fact]
    public void Model_ShouldShowAnInheritedIVisibleAsReadOnlyTrue()
    {
        var enemy = (EntitySave)fixture.Elements.Single(item => item.Key == "entity").Element;
        var boss = (EntitySave)fixture.Elements.Single(item => item.Key == "derived entity").Element;
        enemy.ImplementsIVisible = true;

        var implementsIVisible = EntityModel(withPlugins: false).GetProperties(boss).Single(item => item.Name == nameof(EntitySave.ImplementsIVisible));

        implementsIVisible.IsReadOnly.ShouldBeTrue();
        implementsIVisible.GetValue().ShouldBe(true);
    }

    [Fact]
    public void EntityModel_ShouldAskRegisteredPluginsForTheirProperties()
    {
        ParityTestPlugin.EnsureRegistered();
        var entity = new EntitySave { Name = PluginTestEntityName };
        fixture.Project.Entities.Add(entity);

        var properties = Builder.Get<EntitySavePropertyModel>().GetProperties(entity);

        properties.ShouldContain(item => item.Name == ParityTestPlugin.FlagName);
    }

    void Compare(string key, IReadOnlyList<string> actual)
    {
        var expected = Golden()[key];
        static string WithoutProcessDependentOptions(string line) =>
            ProcessDependentOptions.Any(name => line.StartsWith(name + " | System.String | editable"))
                ? line.Substring(0, line.LastIndexOf('|') + 1)
                : line;
        actual.Select(WithoutProcessDependentOptions).ToList().ShouldBe(expected.Select(WithoutProcessDependentOptions).ToList(), key);
    }

    static NamedObjectSavePropertyModel ObjectModel() => new(GlueState.Self, AvailableAssetTypes.Self, (_, _) => { });

    static EntitySavePropertyModel EntityModel(bool withPlugins) => new(GlueState.Self, GlueCommands.Self, AvailableAssetTypes.Self,
        withPlugins ? AdjustLikeThePlugins : (_, _) => { });

    static ScreenSavePropertyModel ScreenModel() => new(GlueState.Self, GlueCommands.Self, (_, _) => { });

    /// <summary>What Gum, Collision and DamageDealing add to an entity.</summary>
    static void AdjustLikeThePlugins(EntitySave entity, EditorPropertyListBuilder properties)
    {
        GumPlugin.Managers.GumCollidableManager.HandleEntityProperties(entity, properties);
        OfficialPlugins.CollisionPlugin.Managers.StackableEntityManager.Self.HandleEntityProperties(entity, properties);
        OfficialPlugins.DamageDealingPlugin.MainDamageDealingPlugin.HandleEntityProperties(entity, properties);
    }

    IEnumerable<(string key, List<string> lines)> GridSections()
    {
        foreach (var item in fixture.Objects)
        {
            GlueState.Self.CurrentElement = item.Element;
            GlueState.Self.CurrentNamedObjectSave = item.Object;
            var displayer = new NamedObjectPropertyGridDisplayer(ObjectModel()) { RefreshOnTimer = false };
            displayer.UpdateToState(GlueState.Self);
            yield return ("object: " + item.Key, Describe(displayer));
        }
        GlueState.Self.CurrentNamedObjectSave = null;
        foreach (var item in fixture.Elements)
        {
            yield return ("element: " + item.Key, Describe(ElementDisplayer(item.Element, withPlugins: false)));
        }
        foreach (var item in fixture.Elements.Where(item => item.Element is EntitySave))
        {
            yield return ("element with plugins: " + item.Key, Describe(ElementDisplayer(item.Element, withPlugins: true)));
        }
    }

    IEnumerable<(string key, List<string> lines)> ModelSections()
    {
        foreach (var item in fixture.Objects)
        {
            yield return ("object: " + item.Key, Describe(ObjectModel().GetProperties(item.Object, item.Element)));
        }
        foreach (var item in fixture.Elements)
        {
            yield return ("element: " + item.Key, Describe(ElementProperties(item.Element, withPlugins: false)));
        }
        foreach (var item in fixture.Elements.Where(item => item.Element is EntitySave))
        {
            yield return ("element with plugins: " + item.Key, Describe(ElementProperties(item.Element, withPlugins: true)));
        }
    }

    static PropertyGridDisplayer ElementDisplayer(GlueElement element, bool withPlugins) => element is EntitySave entity
        ? new EntitySavePropertyGridDisplayer(EntityModel(withPlugins)) { RefreshOnTimer = false, Instance = entity }
        : new ScreenSavePropertyGridDisplayer(ScreenModel()) { RefreshOnTimer = false, Instance = element };

    static IReadOnlyList<EditorProperty> ElementProperties(GlueElement element, bool withPlugins) => element is EntitySave entity
        ? EntityModel(withPlugins).GetProperties(entity)
        : ScreenModel().GetProperties((ScreenSave)element);

    static Dictionary<string, List<string>> Golden()
    {
        var path = Path.Combine(RepoPaths.FrbRoot, "FRBDK", "Glue", "Tests", "GlueUnitTests", "PropertyGrid", "ElementPropertyParity.golden.txt");
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
        properties.Select(item => Line(item.Name, item.Type, item.IsReadOnly, item.Type == typeof(string) ? item.Options?.Invoke() : null))
            .OrderBy(item => item, StringComparer.Ordinal).ToList();

    static string Line(string name, Type type, bool isReadOnly, IEnumerable<string>? options) =>
        $"{name} | {type} | {(isReadOnly ? "read-only" : "editable")} | {string.Join("; ", options ?? Array.Empty<string>())}";

    /// <summary>The values the grid's drop-down offers, asked with the row as context as the grid asks.</summary>
    static IEnumerable<string>? GridOptions(PropertyDescriptor descriptor)
    {
        var converter = descriptor.Converter;
        var context = new RowContext(descriptor);
        if (descriptor.PropertyType != typeof(string) || converter == null ||
            !converter.GetStandardValuesSupported(context) || !converter.GetStandardValuesExclusive(context))
        {
            return null;
        }
        return converter.GetStandardValues(context)!.Cast<object>().Select(item => item?.ToString() ?? "").ToList();
    }

    sealed class RowContext : ITypeDescriptorContext
    {
        public RowContext(PropertyDescriptor descriptor) => PropertyDescriptor = descriptor;
        public IContainer? Container => null;
        public object? Instance => null;
        public PropertyDescriptor? PropertyDescriptor { get; }
        public object? GetService(Type serviceType) => null;
        public void OnComponentChanged() { }
        public bool OnComponentChanging() => true;
    }

    /// <summary>Adds a flag to <see cref="PluginTestEntityName"/> only, so registering it changes nothing else in the run.</summary>
    sealed class ParityTestPlugin : PluginBase
    {
        public const string FlagName = "ParityTestFlag";
        static bool registered;

        public static void EnsureRegistered()
        {
            if (!registered)
            {
                registered = true;
                PluginManager.RegisterPlugin(new ParityTestPlugin());
            }
        }

        public override string FriendlyName => "Element property parity test plugin";
        public override Version Version => new(1, 0);
        public override void StartUp()
        {
            AdjustEntityProperties = (entity, properties) =>
            {
                if (entity.Name == PluginTestEntityName)
                {
                    properties.Set(ElementPropertyModel.CustomProperty(entity, FlagName, typeof(bool)));
                }
            };
        }
        public override bool ShutDown(PluginShutDownReason shutDownReason) => true;
    }
}
