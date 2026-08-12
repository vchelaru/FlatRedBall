using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using Xunit;

namespace GlueUnitTests.CodeGeneration;

/// <summary>
/// FRB2's FindByName-style generator: typed accessors over the object tree FlatRedBall2's own
/// GlueScreen/GlueEntity already builds from JSON at runtime. Part of #2037.
/// </summary>
public class Frb2CodeGeneratorTests
{
    public Frb2CodeGeneratorTests()
    {
        GlueTestBootstrap.EnsureInitialized();
    }

    static NamedObjectSave Nos(string instanceName, string sourceClassType, SourceType sourceType = SourceType.FlatRedBallType) =>
        new() { InstanceName = instanceName, SourceClassType = sourceClassType, SourceType = sourceType };

    [Fact]
    public void ResolveFrb2TypeName_ReturnsTheMappedFrb2Type_ForAKnownType()
    {
        var nos = Nos("PlayerSprite", "FlatRedBall.Sprite");

        Assert.Equal("FlatRedBall2.Rendering.Sprite", Frb2CodeGenerator.ResolveFrb2TypeName(nos));
    }

    [Fact]
    public void ResolveFrb2TypeName_ReturnsNull_ForAList()
    {
        // FlatRedBall.Math.PositionedObjectList<T> has no FRB2 equivalent yet - see
        // FlatRedBall2/src/Glue/GlueTypeMap.cs.
        var nos = Nos("BearEntityList", "FlatRedBall.Math.PositionedObjectList<T>");

        Assert.Null(Frb2CodeGenerator.ResolveFrb2TypeName(nos));
    }

    [Fact]
    public void ResolveFrb2TypeName_ReturnsNull_ForATypeThisGeneratorDoesNotKnow()
    {
        // FRB2's own GlueTypeMap does not build a LayeredTileMap yet either - generating a typed
        // field for it would name a type that cannot compile against anything real.
        var nos = Nos("Map", "FlatRedBall.TileGraphics.LayeredTileMap");

        Assert.Null(Frb2CodeGenerator.ResolveFrb2TypeName(nos));
    }

    [Fact]
    public void ResolveFrb2TypeName_ReturnsGlueEntity_ForAnElementReference()
    {
        // FRB2's GlueProject.CreateEntity always constructs the shared GlueEntity type for a nested
        // entity, never a generated subclass, regardless of which entity is referenced.
        var nos = Nos("Enemy1", @"Entities\Enemy");

        Assert.Equal("FlatRedBall2.Glue.GlueEntity", Frb2CodeGenerator.ResolveFrb2TypeName(nos));
    }

    [Fact]
    public void ResolveFrb2TypeName_ReturnsNull_ForAnEmptySourceClassType()
    {
        var nos = Nos("Thing", "");

        Assert.Null(Frb2CodeGenerator.ResolveFrb2TypeName(nos));
    }

    [Fact]
    public void GenerateGeneratedFileContents_StartsWithTheAuthorshipMarker()
    {
        var screen = new ScreenSave { Name = "Screens\\MainMenu" };

        string contents = Frb2CodeGenerator.GenerateGeneratedFileContents(screen, "MyGame.Screens");

        Assert.StartsWith("//Code for Screens\\MainMenu", contents);
    }

    [Fact]
    public void GenerateGeneratedFileContents_DerivesFromGlueScreen_ForAScreen()
    {
        var screen = new ScreenSave { Name = "Screens\\MainMenu" };

        string contents = Frb2CodeGenerator.GenerateGeneratedFileContents(screen, "MyGame.Screens");

        Assert.Contains("public partial class MainMenu : FlatRedBall2.Glue.GlueScreen", contents);
    }

    [Fact]
    public void GenerateGeneratedFileContents_DerivesFromGlueEntity_ForAnEntity()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };

        string contents = Frb2CodeGenerator.GenerateGeneratedFileContents(entity, "MyGame.Entities");

        Assert.Contains("public partial class Player : FlatRedBall2.Glue.GlueEntity", contents);
    }

    [Fact]
    public void GenerateGeneratedFileContents_EmitsATypedPropertyAndLookup_ForAMappedObject()
    {
        var screen = new ScreenSave { Name = "Screens\\MainMenu" };
        screen.NamedObjects.Add(Nos("PlayerSprite", "FlatRedBall.Sprite"));

        string contents = Frb2CodeGenerator.GenerateGeneratedFileContents(screen, "MyGame.Screens");

        Assert.Contains("public FlatRedBall2.Rendering.Sprite PlayerSprite { get; private set; }", contents);
        Assert.Contains("PlayerSprite = (FlatRedBall2.Rendering.Sprite)Objects[\"PlayerSprite\"];", contents);
    }

    [Fact]
    public void GenerateGeneratedFileContents_SkipsAPropertyButStillNotesIt_ForAnUnmappedObject()
    {
        var screen = new ScreenSave { Name = "Screens\\MainMenu" };
        screen.NamedObjects.Add(Nos("Map", "FlatRedBall.TileGraphics.LayeredTileMap"));

        string contents = Frb2CodeGenerator.GenerateGeneratedFileContents(screen, "MyGame.Screens");

        Assert.DoesNotContain("LayeredTileMap Map", contents);
        Assert.Contains("Objects[\"Map\"]", contents);
    }

    [Fact]
    public void GenerateGeneratedFileContents_CallsBaseThenTheCustomHook_InCustomInitialize()
    {
        var screen = new ScreenSave { Name = "Screens\\MainMenu" };

        string contents = Frb2CodeGenerator.GenerateGeneratedFileContents(screen, "MyGame.Screens");

        int baseCallIndex = contents.IndexOf("base.CustomInitialize();");
        int hookCallIndex = contents.IndexOf("CustomInitializeAfterObjects();");
        Assert.True(baseCallIndex >= 0 && hookCallIndex > baseCallIndex);
        Assert.Contains("partial void CustomInitializeAfterObjects();", contents);
    }

    [Fact]
    public void GenerateCustomCodeContents_DeclaresTheEmptyPartialHook()
    {
        var screen = new ScreenSave { Name = "Screens\\MainMenu" };

        string contents = Frb2CodeGenerator.GenerateCustomCodeContents(screen, "MyGame.Screens");

        Assert.Contains("partial class MainMenu", contents);
        Assert.Contains("partial void CustomInitializeAfterObjects()", contents);
    }
}
