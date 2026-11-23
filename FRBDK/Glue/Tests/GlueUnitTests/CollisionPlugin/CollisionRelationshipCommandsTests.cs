using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using OfficialPlugins.CollisionPlugin.Controllers;
using OfficialPlugins.CollisionPlugin.Managers;
using OfficialPlugins.CollisionPlugin.ViewModels;
using Shouldly;
using Xunit;

namespace GlueUnitTests.CollisionPlugin;

/// <summary>
/// <see cref="CollisionRelationshipCommands"/> is the logic both the Collision tab (through its view model controller)
/// and GlueCli use. These pin that its settings list matches the view model, and that changing a relationship's
/// object gives the same model through either path.
/// </summary>
[Collection(nameof(TaskManagerSequentialCollection))]
public class CollisionRelationshipCommandsTests : IDisposable
{
    readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    readonly GlueProjectSave _originalGlueProject;
    readonly string _originalRelativeDirectory;
    readonly bool _originalSynchronousMode;
    readonly IUiThreadMarshaller _originalMarshaller;
    readonly string _tempProjectDirectory;

    public CollisionRelationshipCommandsTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        GlueTestBootstrap.EnsureCollisionPluginRegisteredWithPluginManager();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _originalRelativeDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;
        _originalSynchronousMode = TaskManager.SynchronousMode;
        _originalMarshaller = TaskManager.UiThreadMarshaller;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        GlueState.Self.CurrentMainProject = vsProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
        FlatRedBall.IO.FileManager.RelativeDirectory = _tempProjectDirectory + "\\";
        TaskManager.SynchronousMode = true;
        TaskManager.UiThreadMarshaller = new InlineUiThreadMarshaller();
    }

    public void Dispose()
    {
        SetCurrentSelectionForTest(null, null);
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        FlatRedBall.IO.FileManager.RelativeDirectory = _originalRelativeDirectory;
        TaskManager.SynchronousMode = _originalSynchronousMode;
        TaskManager.UiThreadMarshaller = _originalMarshaller;
        try
        {
            Directory.Delete(_tempProjectDirectory, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public void Settings_ShouldMatchTheViewModelsSyncedProperties_ByNameTypeAndDefault()
    {
        var synced = typeof(CollisionRelationshipViewModel).GetProperties()
            .Where(property => property.GetCustomAttribute<SyncedPropertyAttribute>() != null)
            .ToDictionary(property => property.Name);
        var settings = new CollisionRelationshipCommands().Settings;

        settings.Select(setting => setting.Name).OrderBy(name => name).ShouldBe(synced.Keys.OrderBy(name => name));
        foreach (var setting in settings)
        {
            var property = synced[setting.Name];
            setting.Type.ShouldBe(property.PropertyType, setting.Name);
            var defaultValue = property.GetCustomAttribute<DefaultValueAttribute>()?.Value;
            var expected = defaultValue ?? (property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null);
            setting.DefaultValue.ShouldBe(expected, setting.Name);
        }
    }

    [Fact]
    public async Task ChangingTheSecondObject_ShouldRenameAndFillTheSubCollision_ThroughTheTabAndThroughTheCommands()
    {
        var screen = await CreateScreenWithLists();
        var throughTab = await AddRelationship(screen, "ThroughTab");
        var throughCommands = await AddRelationship(screen, "ThroughCommands");

        // The Collision tab: the selected relationship's view model, set as its ComboBox does.
        SetCurrentSelectionForTest(screen, throughTab);
        CollisionRelationshipViewModelController.RefreshViewModel(throughTab);
        CollisionRelationshipViewModelController.ViewModel.SecondCollisionName = "CoinList";

        SetCurrentSelectionForTest(screen, throughCommands);
        new CollisionRelationshipCommands().SetValue(throughCommands, nameof(CollisionRelationshipViewModel.SecondCollisionName), "CoinList");

        throughTab.InstanceName.ShouldBe("PlayerListVsCoinList");
        throughCommands.InstanceName.ShouldBe("PlayerListVsCoinList2");
        foreach (var relationship in new[] { throughTab, throughCommands })
        {
            relationship.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.SecondCollisionName)).ShouldBe("CoinList");
            relationship.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.SecondSubCollisionSelectedItem))
                .ShouldBe(CollisionRelationshipViewModel.EntireObject);
            relationship.SourceClassType.ShouldBe("FlatRedBall.Math.Collision.ListVsListRelationship<Entities.Player, Entities.Coin>");
        }
    }

    /// <summary>
    /// The Collision tab end to end: its Add button, then the object and sub-collision ComboBoxes, each written
    /// through the view model as the UI's bindings do. The expected values are what the tab did before its rules
    /// moved to CollisionRelationshipCommands: a new object that can have a sub-collision gets &lt;Entire Object&gt;
    /// when none was chosen and keeps a chosen one even if the new entity lacks it; an object that cannot have
    /// one clears it; the auto name follows every change.
    /// </summary>
    [Fact]
    public async Task CollisionTab_CreateThenChangeObjects_ShouldKeepSubCollisionsAndNameAsBefore()
    {
        var screen = await CreateScreenWithLists();
        var walls = new NamedObjectSave();
        walls.SetDefaults();
        walls.InstanceName = "Walls";
        walls.SourceType = SourceType.FlatRedBallType;
        walls.SourceClassType = "FlatRedBall.Math.Geometry.ShapeCollection";
        await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(walls, screen, listToAddTo: null, selectNewNos: false,
            performSaveAndGenerateCode: false, updateUi: false);
        SetCurrentSelectionForTest(screen, null);

        // The Add button on PlayerList's Collision tab, for the EnemyList row.
        var relationship = await CollidableNamedObjectController.CreateCollisionRelationshipBetweenObjects("PlayerList", "EnemyList", screen);
        relationship.InstanceName.ShouldBe("PlayerListVsEnemyList");

        SetCurrentSelectionForTest(screen, relationship);
        CollisionRelationshipViewModelController.RefreshViewModel(relationship);
        var viewModel = CollisionRelationshipViewModelController.ViewModel;
        string Stored(string name) => relationship.Properties.GetValue<string>(name);

        viewModel.SecondCollisionName = "CoinList";
        relationship.InstanceName.ShouldBe("PlayerListVsCoinList");
        Stored(nameof(viewModel.FirstSubCollisionSelectedItem)).ShouldBe(CollisionRelationshipViewModel.EntireObject);
        Stored(nameof(viewModel.SecondSubCollisionSelectedItem)).ShouldBe(CollisionRelationshipViewModel.EntireObject);

        viewModel.FirstSubCollisionSelectedItem = "CircleInstance";
        relationship.InstanceName.ShouldBe("PlayerListCircleInstanceVsCoinList");

        // Enemy has no CircleInstance; the tab keeps the chosen sub-collision.
        viewModel.FirstCollisionName = "EnemyList";
        relationship.InstanceName.ShouldBe("EnemyListCircleInstanceVsCoinList");
        Stored(nameof(viewModel.FirstSubCollisionSelectedItem)).ShouldBe("CircleInstance");
        relationship.SourceClassType.ShouldBe("FlatRedBall.Math.Collision.ListVsListRelationship<Entities.Enemy, Entities.Coin>");

        // A ShapeCollection has no sub-collisions.
        viewModel.SecondCollisionName = "Walls";
        relationship.InstanceName.ShouldBe("EnemyListCircleInstanceVsWalls");
        Stored(nameof(viewModel.SecondSubCollisionSelectedItem)).ShouldBeNull();
        relationship.SourceClassType.ShouldBe("FlatRedBall.Math.Collision.ListVsShapeCollectionRelationship<Entities.Enemy>");

        // The tab shows what the model holds.
        viewModel.FirstCollisionName.ShouldBe("EnemyList");
        viewModel.SecondCollisionName.ShouldBe("Walls");
        viewModel.FirstSubCollisionSelectedItem.ShouldBe("CircleInstance");
        viewModel.SecondSubCollisionSelectedItem.ShouldBeNull();

        // The screen as it serializes to its .glsj (this test project has no file to save to).
        var saved = Newtonsoft.Json.JsonConvert.SerializeObject(screen);
        saved.ShouldContain("\"EnemyListCircleInstanceVsWalls\"", Case.Sensitive);
        saved.ShouldNotContain("PlayerListVsEnemyList", Case.Sensitive);
        screen.NamedObjects.Count(item => item.IsCollisionRelationship()).ShouldBe(1);
    }

    async Task<ScreenSave> CreateScreenWithLists()
    {
        foreach (var name in new[] { "Player", "Enemy", "Coin" })
        {
            ObjectFinder.Self.GlueProject.Entities.Add(new EntitySave { Name = "Entities\\" + name, ImplementsICollidable = true });
        }
        var circle = new NamedObjectSave();
        circle.SetDefaults();
        circle.InstanceName = "CircleInstance";
        circle.SourceType = SourceType.FlatRedBallType;
        circle.SourceClassType = "Circle";
        ObjectFinder.Self.GlueProject.Entities[0].NamedObjects.Add(circle);
        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("GameScreen");
        foreach (var name in new[] { "Player", "Enemy", "Coin" })
        {
            var list = new NamedObjectSave();
            list.SetDefaults();
            list.InstanceName = name + "List";
            list.SourceType = SourceType.FlatRedBallType;
            list.SourceClassType = "FlatRedBall.Math.PositionedObjectList<T>";
            list.SourceClassGenericType = "Entities\\" + name;
            await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(list, screen, listToAddTo: null, selectNewNos: false,
                performSaveAndGenerateCode: false, updateUi: false);
        }
        return screen;
    }

    static async Task<NamedObjectSave> AddRelationship(ScreenSave screen, string name)
    {
        var relationship = new NamedObjectSave();
        relationship.SetDefaults();
        relationship.InstanceName = name;
        relationship.SourceType = SourceType.FlatRedBallType;
        relationship.SourceClassType = AssetTypeInfoManager.Self.CollisionRelationshipAti.QualifiedRuntimeTypeName.QualifiedType;
        relationship.Properties.SetValue(nameof(CollisionRelationshipViewModel.IsAutoNameEnabled), true);
        relationship.Properties.SetValue(nameof(CollisionRelationshipViewModel.FirstCollisionName), "PlayerList");
        relationship.Properties.SetValue(nameof(CollisionRelationshipViewModel.SecondCollisionName), "EnemyList");
        await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(relationship, screen, listToAddTo: null, selectNewNos: false,
            performSaveAndGenerateCode: false, updateUi: false);
        return relationship;
    }

    static readonly FieldInfo SnapshotField =
        typeof(GlueState).GetField("snapshot", BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>Selection without the tree view round trip; see CollisionRelationshipErrorRefreshTests.</summary>
    static void SetCurrentSelectionForTest(GlueElement? element, NamedObjectSave? namedObject)
    {
        var snapshot = (GlueStateSnapshot)SnapshotField.GetValue(GlueState.Self)!;
        snapshot.CurrentElement = element;
        snapshot.CurrentNamedObjectSave = namedObject;
    }

    class InlineUiThreadMarshaller : IUiThreadMarshaller
    {
        public void Invoke(Action action) => action();
        public T Invoke<T>(Func<T> func) => func();
        public Task Invoke(Func<Task> func) => func();
        public Task<T> Invoke<T>(Func<Task<T>> func) => func();
        public void BeginInvoke(Action action) => action();
    }
}
