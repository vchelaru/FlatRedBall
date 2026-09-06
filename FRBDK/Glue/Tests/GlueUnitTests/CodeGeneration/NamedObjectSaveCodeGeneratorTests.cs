using System.Collections.Generic;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using GlueFormsCore.Managers;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.CodeGeneration;

// Regression coverage for #1770: a derived (variant) entity's LayeredTileMap object, exposed from its
// base with InstantiatedByBase = true, could have its SourceFile changed in the property grid, but the
// override was silently ignored - the derived class never re-ran its instantiation code, so only the
// base's own file was ever loaded. See ShouldReinstantiateDespiteInstantiatedByBase's doc comment in
// NamedObjectSaveCodeGenerator.cs for the fix.
// Assigns ObjectFinder.Self.GlueProject, which is process-wide, so it runs in the sequential collection.
[Collection(nameof(TaskManagerSequentialCollection))]
public class NamedObjectSaveCodeGeneratorTests
{
    public NamedObjectSaveCodeGeneratorTests()
    {
        GlueTestBootstrap.EnsureInitialized();
    }

    private static (EntitySave baseEntity, EntitySave derivedEntity) CreateBaseAndDerivedEntities()
    {
        var glueProject = new GlueProjectSave();
        ObjectFinder.Self.GlueProject = glueProject;

        var baseEntity = new EntitySave { Name = "Entities\\BaseEntity" };
        var derivedEntity = new EntitySave { Name = "Entities\\DerivedEntity", BaseEntity = baseEntity.Name };

        glueProject.Entities.Add(baseEntity);
        glueProject.Entities.Add(derivedEntity);

        return (baseEntity, derivedEntity);
    }

    private static NamedObjectSave CreateFileSourcedNamedObject(string sourceFile, bool instantiatedByBase)
    {
        return new NamedObjectSave
        {
            InstanceName = "Map",
            SourceType = SourceType.File,
            SourceFile = sourceFile,
            InstantiatedByBase = instantiatedByBase
        };
    }

    [Fact]
    public void ShouldReinstantiateDespiteInstantiatedByBase_ShouldReturnTrue_WhenDerivedSourceFileDiffersFromBase()
    {
        var (baseEntity, derivedEntity) = CreateBaseAndDerivedEntities();

        baseEntity.NamedObjects.Add(CreateFileSourcedNamedObject("Content/Base.tmx", instantiatedByBase: false));
        var derivedNos = CreateFileSourcedNamedObject("Content/Derived.tmx", instantiatedByBase: true);
        derivedEntity.NamedObjects.Add(derivedNos);

        NamedObjectSaveCodeGenerator.ShouldReinstantiateDespiteInstantiatedByBase(derivedNos, derivedEntity)
            .ShouldBeTrue();
    }

    [Fact]
    public void ShouldReinstantiateDespiteInstantiatedByBase_ShouldReturnFalse_WhenDerivedSourceFileMatchesBase()
    {
        var (baseEntity, derivedEntity) = CreateBaseAndDerivedEntities();

        baseEntity.NamedObjects.Add(CreateFileSourcedNamedObject("Content/Base.tmx", instantiatedByBase: false));
        var derivedNos = CreateFileSourcedNamedObject("Content/Base.tmx", instantiatedByBase: true);
        derivedEntity.NamedObjects.Add(derivedNos);

        NamedObjectSaveCodeGenerator.ShouldReinstantiateDespiteInstantiatedByBase(derivedNos, derivedEntity)
            .ShouldBeFalse();
    }

    [Fact]
    public void ShouldReinstantiateDespiteInstantiatedByBase_ShouldReturnFalse_WhenSourceTypeIsNotFile()
    {
        var (baseEntity, derivedEntity) = CreateBaseAndDerivedEntities();

        var baseNos = new NamedObjectSave
        {
            InstanceName = "Map",
            SourceType = SourceType.FlatRedBallType,
            InstantiatedByBase = false
        };
        baseEntity.NamedObjects.Add(baseNos);

        var derivedNos = new NamedObjectSave
        {
            InstanceName = "Map",
            SourceType = SourceType.FlatRedBallType,
            InstantiatedByBase = true
        };
        derivedEntity.NamedObjects.Add(derivedNos);

        NamedObjectSaveCodeGenerator.ShouldReinstantiateDespiteInstantiatedByBase(derivedNos, derivedEntity)
            .ShouldBeFalse();
    }

    [Fact]
    public void ShouldReinstantiateDespiteInstantiatedByBase_ShouldReturnFalse_WhenThereIsNoBaseElement()
    {
        var glueProject = new GlueProjectSave();
        ObjectFinder.Self.GlueProject = glueProject;

        var entity = new EntitySave { Name = "Entities\\StandaloneEntity" };
        glueProject.Entities.Add(entity);

        var nos = CreateFileSourcedNamedObject("Content/Base.tmx", instantiatedByBase: true);
        entity.NamedObjects.Add(nos);

        NamedObjectSaveCodeGenerator.ShouldReinstantiateDespiteInstantiatedByBase(nos, entity)
            .ShouldBeFalse();
    }

    /// <summary>
    /// Builds a base/derived pair where both entities own a real ReferencedFileSave, since
    /// GenerateInstantiationOrAssignment resolves the file through
    /// EntitySave.GetReferencedFileSave(SourceFile) and emits nothing at all when that lookup misses -
    /// an assertion against generated text is silently vacuous without it.
    /// </summary>
    private static (EntitySave baseEntity, EntitySave derivedEntity, NamedObjectSave derivedNos)
        CreateExposedInDerivedTileMapPair(string baseSourceFile, string derivedSourceFile)
    {
        var (baseEntity, derivedEntity) = CreateBaseAndDerivedEntities();

        var baseNos = CreateFileSourcedNamedObject(baseSourceFile, instantiatedByBase: false);
        baseNos.SourceName = "Entire File (LayeredTileMap)";
        baseNos.ExposedInDerived = true;
        baseEntity.NamedObjects.Add(baseNos);
        baseEntity.ReferencedFiles.Add(new ReferencedFileSave { Name = baseSourceFile });

        var derivedNos = CreateFileSourcedNamedObject(derivedSourceFile, instantiatedByBase: true);
        derivedNos.SourceName = "Entire File (LayeredTileMap)";
        derivedEntity.NamedObjects.Add(derivedNos);
        derivedEntity.ReferencedFiles.Add(new ReferencedFileSave { Name = derivedSourceFile });

        return (baseEntity, derivedEntity, derivedNos);
    }

    private static string GenerateInitializeCodeFor(NamedObjectSave namedObject, EntitySave container)
    {
        NamedObjectSaveCodeGenerator.ReusableEntireFileRfses ??= new Dictionary<string, string>();

        var codeBlock = new CodeBlockBase();
        NamedObjectSaveCodeGenerator.WriteCodeForNamedObjectInitialize(
            namedObject, container, codeBlock, overridingContainerName: null);
        return codeBlock.ToString();
    }

    /// <summary>
    /// The predicate tests above pin the decision; this pins the generated text that decision is
    /// supposed to produce, so mis-wiring the predicate into WriteCodeForNamedObjectInitialize still
    /// fails a test.
    /// </summary>
    [Fact]
    public void WriteCodeForNamedObjectInitialize_ShouldAssignFromDerivedsOwnFile_WhenDerivedSourceFileDiffersFromBase()
    {
        var (_, derivedEntity, derivedNos) =
            CreateExposedInDerivedTileMapPair("Content/BaseMap.tmx", "Content/DerivedMap.tmx");

        var generatedCode = GenerateInitializeCodeFor(derivedNos, derivedEntity);

        generatedCode.ShouldContain("Map = DerivedMap;");
        generatedCode.ShouldNotContain("BaseMap");
    }

    /// <summary>
    /// The unchanged InstantiatedByBase path: a derived object still pointing at the base's file must
    /// keep deferring to the base's single instantiation rather than emitting a duplicate one.
    /// </summary>
    [Fact]
    public void WriteCodeForNamedObjectInitialize_ShouldGenerateNoInstantiation_WhenDerivedSourceFileMatchesBase()
    {
        var (_, derivedEntity, derivedNos) =
            CreateExposedInDerivedTileMapPair("Content/BaseMap.tmx", "Content/BaseMap.tmx");

        var generatedCode = GenerateInitializeCodeFor(derivedNos, derivedEntity);

        generatedCode.ShouldNotContain("Map = ");
    }

    /// <summary>
    /// The whole reported path, with no hand-set InstantiatedByBase: InheritanceManager.UpdateFromBaseType
    /// clones the base's ExposedInDerived object into the variant (the real production cloning, which is
    /// what sets InstantiatedByBase), the variant is then pointed at its own .tmx the way dropping a file
    /// on the object does, and the generated code has to load that file rather than the base's.
    ///
    /// showPopupAboutObjectErrors: false - the true default reaches DialogService and would block the run
    /// on a real modal dialog.
    /// </summary>
    [Fact]
    public void UpdateFromBaseType_ThenChangingDerivedSourceFile_ShouldGenerateCodeLoadingTheDerivedsOwnFile()
    {
        var (baseEntity, derivedEntity) = CreateBaseAndDerivedEntities();

        var baseNos = CreateFileSourcedNamedObject("Content/BaseMap.tmx", instantiatedByBase: false);
        baseNos.SourceName = "Entire File (LayeredTileMap)";
        baseNos.ExposedInDerived = true;
        baseEntity.NamedObjects.Add(baseNos);
        baseEntity.ReferencedFiles.Add(new ReferencedFileSave { Name = "Content/BaseMap.tmx" });

        InheritanceManager.Self.UpdateFromBaseType(derivedEntity, showPopupAboutObjectErrors: false);

        var derivedNos = derivedEntity.NamedObjects.ShouldHaveSingleItem();
        // Pins the premise the rest of this class's tests assume rather than restating it by hand: the
        // clone really does arrive instantiated by the base, which is why a SourceFile override on it was
        // dead data before the fix.
        derivedNos.InstantiatedByBase.ShouldBeTrue();
        // "BaseMap.tmx", not the "Content/..." that was assigned: NamedObjectSave's SourceFile setter
        // strips a leading "content/" segment, so both entities' stored values are already relative to
        // the content root by the time ShouldReinstantiateDespiteInstantiatedByBase compares them.
        derivedNos.SourceFile.ShouldBe("BaseMap.tmx");

        derivedNos.SourceFile = "Content/DerivedMap.tmx";
        derivedEntity.ReferencedFiles.Add(new ReferencedFileSave { Name = "Content/DerivedMap.tmx" });

        var generatedCode = GenerateInitializeCodeFor(derivedNos, derivedEntity);

        generatedCode.ShouldContain("Map = DerivedMap;");
        generatedCode.ShouldNotContain("BaseMap");
    }
}
