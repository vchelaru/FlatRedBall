using System.Reflection;
using EngineUnitTests.TestSupport;
using FlatRedBall.Screens;
using Shouldly;

namespace EngineUnitTests.Screens;

// Pins #2366/#2372: Glue-generated code registers a factory per screen so a Native AOT build never reaches a
// trimmed constructor through Assembly.GetType + Activator. Registered names must win over reflection, and
// names nobody registered (older projects, non-AOT games) must keep resolving through reflection.
[Collection(nameof(EngineStatefulTestCollection))]
public class ScreenFactoryRegistryTests : IDisposable
{
    // Each test registers names nobody else uses: the registry is process-wide and has no public reset.
    public class ReflectionOnlyScreen : Screen
    {
    }

    public class ReflectionOrFactoryScreen : Screen
    {
    }

    // ScreenManager.LoadScreen needs a live GuiManager, so these tests pin the lookup it and
    // Screen.StartAsyncLoad both go through instead of the whole load.
    static readonly MethodInfo GetScreenFactoryMethod =
        typeof(ScreenManager).GetMethod("GetScreenFactory", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("ScreenManager.GetScreenFactory not found - has it been renamed?");

    static readonly MethodInfo QualifyScreenNameMethod =
        typeof(ScreenManager).GetMethod("QualifyScreenName", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("ScreenManager.QualifyScreenName not found - has it been renamed?");

    public ScreenFactoryRegistryTests()
    {
        EngineTestBootstrap.EnsureInitialized();
    }

    // MainAssembly is process-wide and null until a game calls ScreenManager.Start.
    public void Dispose()
    {
        SetMainAssembly(null);
    }

    // Returns what ScreenManager would create for the name, or null when the name is unknown.
    static Screen? CreateScreen(string name)
    {
        var factory = (Func<Screen?>?)GetScreenFactoryMethod.Invoke(null, new object[] { name });
        return factory?.Invoke();
    }

    [Fact]
    public void GetScreenFactory_ShouldCreateScreenFromFactory_WhenNameHasNoReflectableType()
    {
        // A trimmed AOT build has no type to find by name; only the factory knows how to build it.
        var created = new Screen();
        ScreenManager.RegisterScreenFactory("RegistryTests.NoSuchType", () => created);
        SetMainAssembly(null);

        CreateScreen("RegistryTests.NoSuchType").ShouldBeSameAs(created);
    }

    [Fact]
    public void GetScreenFactory_ShouldPreferFactory_WhenNameAlsoResolvesThroughReflection()
    {
        var created = new Screen();
        ScreenManager.RegisterScreenFactory(typeof(ReflectionOrFactoryScreen).FullName!, () => created);

        // Point the reflection fallback at this assembly so the type is findable; the factory must still win.
        SetMainAssembly(typeof(ReflectionOrFactoryScreen).Assembly);

        CreateScreen(typeof(ReflectionOrFactoryScreen).FullName!).ShouldBeSameAs(created);
    }

    [Fact]
    public void GetScreenFactory_ShouldFallBackToReflection_WhenNameIsNotRegistered()
    {
        SetMainAssembly(typeof(ReflectionOnlyScreen).Assembly);

        CreateScreen(typeof(ReflectionOnlyScreen).FullName!).ShouldBeOfType<ReflectionOnlyScreen>();
    }

    [Fact]
    public void GetScreenFactory_ShouldReturnNull_WhenNameIsNeitherRegisteredNorReflectable()
    {
        SetMainAssembly(typeof(ReflectionOnlyScreen).Assembly);

        GetScreenFactoryMethod.Invoke(null, new object[] { "RegistryTests.NeverRegistered" }).ShouldBeNull();
    }

    [Fact]
    public void RegisterScreenFactory_ShouldReject_EmptyNameOrNullFactory()
    {
        Should.Throw<ArgumentException>(() => ScreenManager.RegisterScreenFactory("", () => new Screen()));
        Should.Throw<ArgumentNullException>(() => ScreenManager.RegisterScreenFactory("RegistryTests.NullFactory", null!));
    }

    [Fact]
    public void QualifyScreenName_ShouldFindRegisteredScreenInAnotherNamespace_ByUnqualifiedName()
    {
        // Screens in folders live in different namespaces than the screen that is ending (Oct 2024 change),
        // so the unqualified name has to be matched against the registry before any reflection scan.
        ScreenManager.RegisterScreenFactory("RegistryTests.Folder.UnqualifiedTarget", () => new Screen());

        Qualify("UnqualifiedTarget", "RegistryTests.Other").ShouldBe("RegistryTests.Folder.UnqualifiedTarget");
    }

    [Fact]
    public void QualifyScreenName_ShouldPreferScreenInSameNamespace_WhenRegistered()
    {
        ScreenManager.RegisterScreenFactory("RegistryTests.Same.Duplicate", () => new Screen());
        ScreenManager.RegisterScreenFactory("RegistryTests.Elsewhere.Duplicate", () => new Screen());

        Qualify("Duplicate", "RegistryTests.Same").ShouldBe("RegistryTests.Same.Duplicate");
    }

    [Fact]
    public void QualifyScreenName_ShouldFallBackToReflection_WhenNotRegistered()
    {
        var assembly = typeof(ReflectionOnlyScreen).Assembly;

        Qualify(nameof(ReflectionOnlyScreen), "SomewhereElse", assembly).ShouldBe(typeof(ReflectionOnlyScreen).FullName);
    }

    [Fact]
    public void StartAsyncLoad_ShouldCreateScreenFromFactory_WhenNameHasNoReflectableType()
    {
        var created = new Screen();
        ScreenManager.RegisterScreenFactory("RegistryTests.AsyncNoSuchType", () => created);
        SetMainAssembly(null);

        var loader = new Screen();
        loader.StartAsyncLoad("RegistryTests.AsyncNoSuchType");

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (loader.AsyncLoadingState != AsyncLoadingState.Done && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(10);
        }

        loader.AsyncLoadingState.ShouldBe(AsyncLoadingState.Done);
        typeof(Screen).GetField("mNextScreenToLoadAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(loader).ShouldBeSameAs(created);
    }

    static string Qualify(string unqualified, string currentNamespace, Assembly? assembly = null)
    {
        return (string)QualifyScreenNameMethod.Invoke(null,
            new object?[] { unqualified, currentNamespace, assembly ?? typeof(ScreenFactoryRegistryTests).Assembly })!;
    }

    static void SetMainAssembly(Assembly? assembly)
    {
        typeof(ScreenManager).GetProperty(nameof(ScreenManager.MainAssembly))!.SetValue(null, assembly);
    }
}
