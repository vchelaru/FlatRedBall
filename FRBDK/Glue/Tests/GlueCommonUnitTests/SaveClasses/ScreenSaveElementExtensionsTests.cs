using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.SaveClasses;

// InheritsFrom, GetAllBaseScreens and GetReferencedFileSaveRecursively read the shared static ObjectFinderCore.Self, so this can't run
// concurrently with any other test class that swaps it out - hence the shared collection (see
// ObjectFinderCoreCollection in NamedObjectSaveElementExtensionsTests.cs).
[Collection(nameof(ObjectFinderCoreCollection))]
public class ScreenSaveElementExtensionsTests
{
    readonly FakeObjectFinderCore _finder = new();

    public ScreenSaveElementExtensionsTests()
    {
        ObjectFinderCore.Self = _finder;
    }

    ScreenSave AddScreen(string name, string? baseScreen = null)
    {
        var screen = new ScreenSave { Name = name, BaseScreen = baseScreen };
        _finder.AddElement(name, screen);
        return screen;
    }

    [Fact]
    public void InheritsFrom_DirectBase_ReturnsTrue()
    {
        AddScreen("Screens\\Base");
        var derived = AddScreen("Screens\\Derived", "Screens\\Base");

        Assert.True(derived.InheritsFrom("Screens\\Base"));
    }

    [Fact]
    public void InheritsFrom_TransitiveBase_ReturnsTrue()
    {
        AddScreen("Screens\\Root");
        AddScreen("Screens\\Middle", "Screens\\Root");
        var leaf = AddScreen("Screens\\Leaf", "Screens\\Middle");

        Assert.True(leaf.InheritsFrom("Screens\\Root"));
    }

    [Fact]
    public void InheritsFrom_BaseNotFoundByFinder_ReturnsFalse()
    {
        var derived = new ScreenSave { Name = "Screens\\Derived", BaseScreen = "Screens\\Missing" };

        Assert.False(derived.InheritsFrom("Screens\\Other"));
    }

    [Fact]
    public void InheritsFrom_BaseNotFoundByFinder_StillMatchesDirectBaseName()
    {
        var derived = new ScreenSave { Name = "Screens\\Derived", BaseScreen = "Screens\\Missing" };

        Assert.True(derived.InheritsFrom("Screens\\Missing"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void InheritsFrom_NoBase_ReturnsFalse(string? baseScreen)
    {
        var screen = new ScreenSave { Name = "Screens\\Alone", BaseScreen = baseScreen };

        Assert.False(screen.InheritsFrom("Screens\\Base"));
    }

    [Fact]
    public void InheritsFrom_UnrelatedScreen_ReturnsFalse()
    {
        AddScreen("Screens\\Base");
        var derived = AddScreen("Screens\\Derived", "Screens\\Base");

        Assert.False(derived.InheritsFrom("Screens\\Unrelated"));
    }

    [Fact]
    public void GetAllBaseScreens_DirectBase_ReturnsIt()
    {
        var baseScreen = AddScreen("Screens\\Base");
        var derived = AddScreen("Screens\\Derived", "Screens\\Base");

        var result = derived.GetAllBaseScreens();

        Assert.Equal(new[] { baseScreen }, result);
    }

    [Fact]
    public void GetAllBaseScreens_TransitiveBase_ReturnsNearestFirst()
    {
        var root = AddScreen("Screens\\Root");
        var middle = AddScreen("Screens\\Middle", "Screens\\Root");
        var leaf = AddScreen("Screens\\Leaf", "Screens\\Middle");

        var result = leaf.GetAllBaseScreens();

        Assert.Equal(new[] { middle, root }, result);
    }

    [Fact]
    public void GetAllBaseScreens_BaseNotFoundByFinder_ReturnsEmpty()
    {
        var derived = new ScreenSave { Name = "Screens\\Derived", BaseScreen = "Screens\\Missing" };

        Assert.Empty(derived.GetAllBaseScreens());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetAllBaseScreens_NoBase_ReturnsEmpty(string? baseScreen)
    {
        var screen = new ScreenSave { Name = "Screens\\Alone", BaseScreen = baseScreen };

        Assert.Empty(screen.GetAllBaseScreens());
    }

    [Fact]
    public void GetAllBaseScreens_ListOverload_AppendsToExistingList()
    {
        var baseScreen = AddScreen("Screens\\Base");
        var derived = AddScreen("Screens\\Derived", "Screens\\Base");
        var existing = new ScreenSave { Name = "Screens\\Existing" };
        var list = new List<ScreenSave> { existing };

        derived.GetAllBaseScreens(list);

        Assert.Equal(new[] { existing, baseScreen }, list);
    }

    [Fact]
    public void GetReferencedFileSaveRecursively_FoundOnInstance_ReturnsIt()
    {
        var rfs = new ReferencedFileSave { Name = "Screens/Level1/Map.tmx" };
        var screen = new ScreenSave { Name = "Screens\\Level1" };
        screen.ReferencedFiles.Add(rfs);

        Assert.Same(rfs, screen.GetReferencedFileSaveRecursively("Screens/Level1/Map.tmx"));
    }

    [Fact]
    public void GetReferencedFileSaveRecursively_FoundOnBaseScreen_ReturnsIt()
    {
        var rfs = new ReferencedFileSave { Name = "Screens/BaseLevel/Map.tmx" };
        var baseScreen = AddScreen("Screens\\BaseLevel");
        baseScreen.ReferencedFiles.Add(rfs);
        var derived = AddScreen("Screens\\Level1", "Screens\\BaseLevel");

        Assert.Same(rfs, derived.GetReferencedFileSaveRecursively("Screens/BaseLevel/Map.tmx"));
    }

    [Fact]
    public void GetReferencedFileSaveRecursively_BaseNotFoundByFinder_ReturnsNull()
    {
        var derived = new ScreenSave { Name = "Screens\\Level1", BaseScreen = "Screens\\Missing" };

        Assert.Null(derived.GetReferencedFileSaveRecursively("Screens/Missing/Map.tmx"));
    }
}
