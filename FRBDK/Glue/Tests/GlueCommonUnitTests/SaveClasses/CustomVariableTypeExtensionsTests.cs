using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.SaveClasses;
using GlueCommonUnitTests.Parsing;
using Microsoft.Xna.Framework;

namespace GlueCommonUnitTests.SaveClasses;

// Reads the shared static TypeResolutionCore.Self / AvailableAssetTypesCore.Self / ObjectFinderCore.Self /
// PluginManagerCore.Self,
// so this can't run concurrently with any other test class that swaps one out - hence the shared
// collection (see ObjectFinderCoreCollection in NamedObjectSaveElementExtensionsTests.cs).
[Collection(nameof(ObjectFinderCoreCollection))]
public class CustomVariableTypeExtensionsTests
{
    enum TestEnum { First = 0, Second = 1, Third = 2 }

    readonly FakeTypeResolutionCore _typeResolution = new();
    readonly FakeAvailableAssetTypesCore _availableAssetTypes = new();
    readonly FakeObjectFinderCore _finder = new();
    readonly FakePluginManagerCore _plugins = new();

    // HasAccompanyingVelocityConsideringTunneling reads InstructionManager's velocity table, which the
    // engine only fills in Initialize() (Glue calls it from MainGlueWindow). Once per process is enough.
    static readonly Lazy<bool> _instructionManagerInitialized = new(() =>
    {
        FlatRedBall.Instructions.InstructionManager.Initialize();
        return true;
    });

    public CustomVariableTypeExtensionsTests()
    {
        _ = _instructionManagerInitialized.Value;
        TypeResolutionCore.Self = _typeResolution;
        AvailableAssetTypesCore.Self = _availableAssetTypes;
        ObjectFinderCore.Self = _finder;
        PluginManagerCore.Self = _plugins;
        _finder.GlueProject = new GlueProjectSave { FileVersion = (int)GlueProjectSave.GluxVersions.VariantsInsteadOfTypes };
    }

    #region GetIsEnumeration / GetRuntimeType / GetIsAnimationChain

    [Fact]
    public void GetIsEnumeration_EmptyType_ReturnsFalse()
    {
        Assert.False(new CustomVariable { Type = "" }.GetIsEnumeration());
    }

    [Fact]
    public void GetIsEnumeration_UnresolvedType_ReturnsFalse()
    {
        Assert.False(new CustomVariable { Type = "Nope" }.GetIsEnumeration());
    }

    [Fact]
    public void GetIsEnumeration_EnumType_ReturnsTrue()
    {
        _typeResolution.AddType("TestEnum", typeof(TestEnum));

        Assert.True(new CustomVariable { Type = "TestEnum" }.GetIsEnumeration());
    }

    [Fact]
    public void GetIsEnumeration_NonEnumType_ReturnsFalse()
    {
        _typeResolution.AddType("int", typeof(int));

        Assert.False(new CustomVariable { Type = "int" }.GetIsEnumeration());
    }

    [Fact]
    public void GetRuntimeType_VariableState_ReturnsNull()
    {
        _typeResolution.AddType("int", typeof(int));
        _finder.StateSaveCategoryResolver = (_, _) => (true, null);

        Assert.Null(new CustomVariable { Type = "int" }.GetRuntimeType());
    }

    [Fact]
    public void GetRuntimeType_NoOverride_ResolvesType()
    {
        _typeResolution.AddType("int", typeof(int));

        Assert.Equal(typeof(int), new CustomVariable { Type = "int" }.GetRuntimeType());
    }

    [Fact]
    public void GetRuntimeType_WithOverride_ResolvesOverridingPropertyType()
    {
        _typeResolution.AddType("int", typeof(int));
        _typeResolution.AddType("float", typeof(float));

        var variable = new CustomVariable { Type = "int", OverridingPropertyType = "float" };

        Assert.Equal(typeof(float), variable.GetRuntimeType());
    }

    [Fact]
    public void GetIsAnimationChain_StringCurrentChainName_ReturnsTrue()
    {
        _typeResolution.AddType("string", typeof(string));

        var variable = new CustomVariable { Type = "string", SourceObjectProperty = "CurrentChainName" };

        Assert.True(variable.GetIsAnimationChain());
    }

    [Fact]
    public void GetIsAnimationChain_StringOtherProperty_ReturnsFalse()
    {
        _typeResolution.AddType("string", typeof(string));

        var variable = new CustomVariable { Type = "string", SourceObjectProperty = "Text" };

        Assert.False(variable.GetIsAnimationChain());
    }

    #endregion

    #region GetIsFile / GetIsObjectType

    [Theory]
    [InlineData("string")]
    [InlineData("String")]
    [InlineData(null)]
    [InlineData("int")]
    public void GetIsFile_NonFileTypeName_ReturnsFalse(string typeName)
    {
        Assert.False(CustomVariableTypeExtensions.GetIsFile(typeName));
    }

    [Theory]
    [InlineData("Texture2D")]
    [InlineData("Microsoft.Xna.Framework.Graphics.Texture2D")]
    [InlineData("AnimationChainList")]
    [InlineData("BitmapFont")]
    [InlineData("ShapeCollection")]
    [InlineData("Scene")]
    public void GetIsFile_BuiltInFileTypeName_ReturnsTrue(string typeName)
    {
        Assert.True(CustomVariableTypeExtensions.GetIsFile(typeName));
    }

    [Fact]
    public void GetIsFile_AssetTypeWithExtensionMatchingRuntimeTypeName_ReturnsTrue()
    {
        _availableAssetTypes.AddAssetType(new AssetTypeInfo { Extension = "tmx", QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "TileMap" } });

        Assert.True(CustomVariableTypeExtensions.GetIsFile("TileMap"));
    }

    [Fact]
    public void GetIsFile_AssetTypeWithExtensionMatchingQualifiedType_ReturnsTrue()
    {
        _availableAssetTypes.AddAssetType(new AssetTypeInfo
        {
            Extension = "tmx",
            QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "Tiled.TileMap" }
        });

        Assert.True(CustomVariableTypeExtensions.GetIsFile("Tiled.TileMap"));
    }

    [Fact]
    public void GetIsFile_AssetTypeWithoutExtension_ReturnsFalse()
    {
        _availableAssetTypes.AddAssetType(new AssetTypeInfo { Extension = "", QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "Circle" } });

        Assert.False(CustomVariableTypeExtensions.GetIsFile("Circle"));
    }

    [Fact]
    public void GetIsFile_RuntimeType_UsesTypeName()
    {
        Assert.True(CustomVariableTypeExtensions.GetIsFile(typeof(Microsoft.Xna.Framework.Graphics.Texture2D)));
        Assert.False(CustomVariableTypeExtensions.GetIsFile(typeof(string)));
        Assert.False(CustomVariableTypeExtensions.GetIsFile((Type)null));
    }

    [Fact]
    public void GetIsFile_Variable_PrefersOverridingPropertyType()
    {
        var variable = new CustomVariable { Type = "int", OverridingPropertyType = "Texture2D" };

        Assert.True(variable.GetIsFile());
    }

    [Fact]
    public void GetIsFile_Variable_FallsBackToType()
    {
        Assert.True(new CustomVariable { Type = "Texture2D" }.GetIsFile());
        Assert.False(new CustomVariable { Type = "int" }.GetIsFile());
    }

    [Fact]
    public void GetIsObjectType_NullTypeString_ReturnsFalse()
    {
        Assert.False(CustomVariableTypeExtensions.GetIsObjectType((string)null));
    }

    [Fact]
    public void GetIsObjectType_AssetTypeCanBeObject_ReturnsTrue()
    {
        _availableAssetTypes.AddAssetType(new AssetTypeInfo { CanBeObject = true, QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "Sprite" } });

        Assert.True(CustomVariableTypeExtensions.GetIsObjectType("Sprite"));
        Assert.True(new CustomVariable { Type = "Sprite" }.GetIsObjectType());
    }

    [Fact]
    public void GetIsObjectType_AssetTypeCannotBeObject_ReturnsFalse()
    {
        _availableAssetTypes.AddAssetType(new AssetTypeInfo { CanBeObject = false, QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "Sprite" } });

        Assert.False(CustomVariableTypeExtensions.GetIsObjectType("Sprite"));
    }

    #endregion

    #region GetIsVariableState / GetIsBaseElementType / GetEntityNameDefiningThisTypeCategory

    [Fact]
    public void GetIsVariableState_RoutesThroughObjectFinderCore()
    {
        var category = new StateSaveCategory { Name = "Cat" };
        var variable = new CustomVariable { Type = "Cat" };
        _finder.StateSaveCategoryResolver = (cv, _) => cv == variable ? (true, category) : (false, null);

        Assert.True(variable.GetIsVariableState());
        Assert.False(new CustomVariable { Type = "Cat" }.GetIsVariableState());

        var (isState, foundCategory) = variable.GetIsVariableStateAndCategory();
        Assert.True(isState);
        Assert.Same(category, foundCategory);
    }

    [Fact]
    public void GetIsBaseElementType_EntityVariant_ReturnsEntity()
    {
        var entity = new EntitySave { Name = "Entities\\Enemy" };
        _finder.GlueProject.Entities.Add(entity);

        Assert.True(CustomVariableTypeExtensions.GetIsBaseElementType("Entities.EnemyVariant", out var element));
        Assert.Same(entity, element);
    }

    [Fact]
    public void GetIsBaseElementType_OldFileVersion_UsesTypeSuffix()
    {
        _finder.GlueProject.FileVersion = (int)GlueProjectSave.GluxVersions.VariantsInsteadOfTypes - 1;
        var entity = new EntitySave { Name = "Entities\\Enemy" };
        _finder.GlueProject.Entities.Add(entity);

        Assert.True(CustomVariableTypeExtensions.GetIsBaseElementType("Entities.EnemyType", out var element));
        Assert.Same(entity, element);
        Assert.False(CustomVariableTypeExtensions.GetIsBaseElementType("Entities.EnemyVariant", out _));
    }

    [Fact]
    public void GetIsBaseElementType_ScreenVariant_ReturnsScreen()
    {
        var screen = new ScreenSave { Name = "Screens\\Level1" };
        _finder.GlueProject.Screens.Add(screen);

        Assert.True(new CustomVariable { Type = "Screens.Level1Variant" }.GetIsBaseElementType(out var element));
        Assert.Same(screen, element);
    }

    [Fact]
    public void GetIsBaseElementType_NoMatchingElement_ReturnsFalse()
    {
        Assert.False(new CustomVariable { Type = "Entities.MissingVariant" }.GetIsBaseElementType());
    }

    [Fact]
    public void GetIsBaseElementType_SuppressBaseTypeGeneration_ReturnsFalse()
    {
        _finder.GlueProject.SuppressBaseTypeGeneration = true;
        _finder.GlueProject.Entities.Add(new EntitySave { Name = "Entities\\Enemy" });

        Assert.False(CustomVariableTypeExtensions.GetIsBaseElementType("Entities.EnemyVariant", out _));
    }

    [Fact]
    public void GetIsBaseElementType_TypeWithoutDot_ReturnsFalse()
    {
        Assert.False(CustomVariableTypeExtensions.GetIsBaseElementType("int", out _));
    }

    [Fact]
    public void GetEntityNameDefiningThisTypeCategory_EntityType_ReturnsBackslashName()
    {
        var variable = new CustomVariable { Type = "Entities.Enemies.Boss.Category" };

        Assert.Equal("Entities\\Enemies\\Boss", variable.GetEntityNameDefiningThisTypeCategory());
    }

    [Fact]
    public void GetEntityNameDefiningThisTypeCategory_NonEntityType_ReturnsNull()
    {
        Assert.Null(new CustomVariable { Type = "int" }.GetEntityNameDefiningThisTypeCategory());
    }

    #endregion

    #region Defaults

    [Fact]
    public void GetDefaultValueAccordingToType_String_ReturnsEmptyString()
    {
        _typeResolution.AddType("string", typeof(string));

        Assert.Equal("", CustomVariableTypeExtensions.GetDefaultValueAccordingToType("string"));
    }

    [Fact]
    public void GetDefaultValueAccordingToType_UnresolvedVariableState_ReturnsEmptyString()
    {
        Assert.Equal("", CustomVariableTypeExtensions.GetDefaultValueAccordingToType("VariableState"));
    }

    [Fact]
    public void GetDefaultValueAccordingToType_File_ReturnsEmptyString()
    {
        Assert.Equal("", CustomVariableTypeExtensions.GetDefaultValueAccordingToType("Texture2D"));
    }

    [Fact]
    public void GetDefaultValueAccordingToType_Color_ReturnsEmptyString()
    {
        _typeResolution.AddType("Color", typeof(Color));

        Assert.Equal("", CustomVariableTypeExtensions.GetDefaultValueAccordingToType("Color"));
    }

    [Fact]
    public void GetDefaultValueAccordingToType_Primitives_ReturnTypedZero()
    {
        _typeResolution.AddType("byte", typeof(byte));
        _typeResolution.AddType("short", typeof(short));
        _typeResolution.AddType("int", typeof(int));
        _typeResolution.AddType("long", typeof(long));
        _typeResolution.AddType("char", typeof(char));
        _typeResolution.AddType("float", typeof(float));
        _typeResolution.AddType("double", typeof(double));
        _typeResolution.AddType("bool", typeof(bool));
        _typeResolution.AddType("bool?", typeof(bool?));

        Assert.Equal((byte)0, CustomVariableTypeExtensions.GetDefaultValueAccordingToType("byte"));
        Assert.Equal((short)0, CustomVariableTypeExtensions.GetDefaultValueAccordingToType("short"));
        Assert.Equal(0, CustomVariableTypeExtensions.GetDefaultValueAccordingToType("int"));
        Assert.Equal(0L, CustomVariableTypeExtensions.GetDefaultValueAccordingToType("long"));
        Assert.Equal(' ', CustomVariableTypeExtensions.GetDefaultValueAccordingToType("char"));
        Assert.Equal(0.0f, CustomVariableTypeExtensions.GetDefaultValueAccordingToType("float"));
        Assert.Equal(0.0, CustomVariableTypeExtensions.GetDefaultValueAccordingToType("double"));
        Assert.Equal(false, CustomVariableTypeExtensions.GetDefaultValueAccordingToType("bool"));
        Assert.Null(CustomVariableTypeExtensions.GetDefaultValueAccordingToType("bool?"));
    }

    [Fact]
    public void GetDefaultValueAccordingToType_UnknownType_ReturnsEmptyString()
    {
        Assert.Equal("", CustomVariableTypeExtensions.GetDefaultValueAccordingToType("Whatever"));
    }

    [Fact]
    public void SetDefaultValueAccordingToType_FileVariable_SetsEmptyString()
    {
        var variable = new CustomVariable { Type = "Texture2D", DefaultValue = "old" };

        variable.SetDefaultValueAccordingToType("Texture2D");

        Assert.Equal("", variable.DefaultValue);
    }

    [Fact]
    public void SetDefaultValueAccordingToType_Int_SetsZero()
    {
        _typeResolution.AddType("int", typeof(int));
        var variable = new CustomVariable { Type = "int", DefaultValue = "old" };

        variable.SetDefaultValueAccordingToType("int");

        Assert.Equal(0, variable.DefaultValue);
    }

    #endregion

    #region FixEnumerationTypes / ConvertEnumerationValuesToInts

    [Fact]
    public void FixEnumerationTypes_IntDefault_ConvertsToEnumValue()
    {
        _typeResolution.AddType("TestEnum", typeof(TestEnum));
        var variable = new CustomVariable { Type = "TestEnum", DefaultValue = 2 };

        variable.FixEnumerationTypes();

        Assert.Equal(TestEnum.Third, variable.DefaultValue);
    }

    [Fact]
    public void FixEnumerationTypes_LongDefault_ConvertsToEnumValue()
    {
        _typeResolution.AddType("TestEnum", typeof(TestEnum));
        var variable = new CustomVariable { Type = "TestEnum", DefaultValue = 1L };

        variable.FixEnumerationTypes();

        Assert.Equal(TestEnum.Second, variable.DefaultValue);
    }

    [Fact]
    public void FixEnumerationTypes_AlreadyEnum_Unchanged()
    {
        _typeResolution.AddType("TestEnum", typeof(TestEnum));
        var variable = new CustomVariable { Type = "TestEnum", DefaultValue = TestEnum.Second };

        variable.FixEnumerationTypes();

        Assert.Equal(TestEnum.Second, variable.DefaultValue);
    }

    [Fact]
    public void FixEnumerationTypes_UndefinedIntValue_LeavesDefaultUnchanged()
    {
        _typeResolution.AddType("TestEnum", typeof(TestEnum));
        var variable = new CustomVariable { Type = "TestEnum", DefaultValue = 99 };

        variable.FixEnumerationTypes();

        Assert.Equal(99, variable.DefaultValue);
    }

    [Fact]
    public void FixEnumerationTypes_NonEnum_Unchanged()
    {
        _typeResolution.AddType("int", typeof(int));
        var variable = new CustomVariable { Type = "int", DefaultValue = 5 };

        variable.FixEnumerationTypes();

        Assert.Equal(5, variable.DefaultValue);
    }

    [Fact]
    public void ConvertEnumerationValuesToInts_EnumDefaultAndProperties_BecomeInts()
    {
        var variable = new CustomVariable { DefaultValue = TestEnum.Third };
        variable.Properties.Add(new PropertySave { Name = "P", Value = TestEnum.Second });
        variable.Properties.Add(new PropertySave { Name = "Q", Value = "text" });

        variable.ConvertEnumerationValuesToInts();

        Assert.Equal(2, variable.DefaultValue);
        Assert.Equal(1, variable.Properties[0].Value);
        Assert.Equal("text", variable.Properties[1].Value);
    }

    [Fact]
    public void ConvertEnumerationValuesToInts_NonEnumDefault_LeavesPropertiesAlone()
    {
        var variable = new CustomVariable { DefaultValue = 3 };
        variable.Properties.Add(new PropertySave { Name = "P", Value = TestEnum.Second });

        variable.ConvertEnumerationValuesToInts();

        Assert.Equal(TestEnum.Second, variable.Properties[0].Value);
    }

    #endregion

    #region FixAllTypes

    [Fact]
    public void FixAllTypes_EnumIntDefault_ConvertsToEnumValue()
    {
        _typeResolution.AddType("TestEnum", typeof(TestEnum));
        var variable = new CustomVariable { Type = "TestEnum", DefaultValue = 2 };

        variable.FixAllTypes();

        Assert.Equal(TestEnum.Third, variable.DefaultValue);
    }

    [Fact]
    public void FixAllTypes_IntDefaultOnFloatType_ConvertsToFloat()
    {
        _typeResolution.AddType("float", typeof(float));
        var variable = new CustomVariable { Type = "float", DefaultValue = 3 };

        variable.FixAllTypes();

        Assert.Equal(3f, variable.DefaultValue);
    }

    [Fact]
    public void FixAllTypes_OverridingPropertyType_WinsOverType()
    {
        _typeResolution.AddType("float", typeof(float));
        var variable = new CustomVariable { Type = "float", OverridingPropertyType = "int", DefaultValue = 3L };

        variable.FixAllTypes();

        Assert.Equal(3, variable.DefaultValue);
    }

    [Fact]
    public void FixAllTypes_NullDefault_Untouched()
    {
        _typeResolution.AddType("float", typeof(float));
        var variable = new CustomVariable { Type = "float", DefaultValue = null };

        variable.FixAllTypes();

        Assert.Null(variable.DefaultValue);
    }

    [Fact]
    public void FixAllTypes_PreferredDisplayerName_AsksPluginSeam()
    {
        var variable = new CustomVariable
        {
            Type = "float",
            VariableDefinition = new VariableDefinition { PreferredDisplayerName = "SliderDisplay" },
        };

        variable.FixAllTypes();

        Assert.Same(variable, Assert.Single(_plugins.DisplayerRequests));
    }

    [Fact]
    public void FixAllTypes_NoPreferredDisplayerName_SkipsPluginSeam()
    {
        var withEmptyName = new CustomVariable
        {
            Type = "float",
            VariableDefinition = new VariableDefinition { PreferredDisplayerName = "" },
        };
        var withoutDefinition = new CustomVariable { Type = "float" };

        withEmptyName.FixAllTypes();
        withoutDefinition.FixAllTypes();

        Assert.Empty(_plugins.DisplayerRequests);
    }

    #endregion

    #region GetDefiningCustomVariable / GetIsTunneling / CustomVariableToString

    [Fact]
    public void GetDefiningCustomVariable_Null_Throws()
    {
        CustomVariable variable = null;

        Assert.Throws<ArgumentNullException>(() => variable.GetDefiningCustomVariable());
    }

    [Fact]
    public void GetDefiningCustomVariable_NotDefinedByBase_ReturnsSelf()
    {
        var variable = new CustomVariable { Name = "X", DefinedByBase = false };

        Assert.Same(variable, variable.GetDefiningCustomVariable());
    }

    [Fact]
    public void GetDefiningCustomVariable_DefinedByBase_ReturnsBaseVariable()
    {
        var baseVariable = new CustomVariable { Name = "X" };
        var baseEntity = new EntitySave { Name = "Entities\\Base" };
        baseEntity.CustomVariables.Add(baseVariable);

        var derivedVariable = new CustomVariable { Name = "X", DefinedByBase = true };
        var derivedEntity = new EntitySave { Name = "Entities\\Derived", BaseEntity = "Entities\\Base" };
        derivedEntity.CustomVariables.Add(derivedVariable);

        _finder.GlueProject.Entities.Add(baseEntity);
        _finder.GlueProject.Entities.Add(derivedEntity);
        _finder.SetContainer(derivedVariable, derivedEntity);
        _finder.SetContainer(baseVariable, baseEntity);

        Assert.Same(baseVariable, derivedVariable.GetDefiningCustomVariable());
    }

    [Fact]
    public void GetDefiningCustomVariable_DefinedByBaseWithoutContainer_ReturnsNull()
    {
        var variable = new CustomVariable { Name = "X", DefinedByBase = true };

        Assert.Null(variable.GetDefiningCustomVariable());
    }

    [Fact]
    public void GetIsTunneling_RequiresSourceObjectAndProperty()
    {
        Assert.True(new CustomVariable { SourceObject = "Sprite", SourceObjectProperty = "X" }.GetIsTunneling());
        Assert.False(new CustomVariable { SourceObject = "Sprite" }.GetIsTunneling());
        Assert.False(new CustomVariable { SourceObjectProperty = "X" }.GetIsTunneling());
    }

    [Fact]
    public void CustomVariableToString_Uncontained_SaysUncontained()
    {
        var variable = new CustomVariable { Type = "int", Name = "X", DefaultValue = 3 };

        Assert.Equal("int X = 3 (Uncontained)", CustomVariableTypeExtensions.CustomVariableToString(variable));
    }

    [Fact]
    public void CustomVariableToString_Tunneled_IncludesSourceObjectAndContainer()
    {
        var entity = new EntitySave { Name = "Entities\\Enemy" };
        var variable = new CustomVariable { Type = "float", Name = "X", SourceObject = "Sprite", DefaultValue = 1f };
        _finder.SetContainer(variable, entity);

        Assert.Equal("float Sprite.X = 1 in " + entity, CustomVariableTypeExtensions.CustomVariableToString(variable));
    }

    #endregion
    #region HasAccompanyingVelocityConsideringTunneling

    [Fact]
    public void HasAccompanyingVelocity_HasAccompanyingVelocityProperty_ReturnsTrue()
    {
        var variable = new CustomVariable { Name = "X", HasAccompanyingVelocityProperty = true };

        Assert.True(variable.HasAccompanyingVelocityConsideringTunneling(new EntitySave()));
    }

    [Fact]
    public void HasAccompanyingVelocity_NotTunneled_ReturnsFalse()
    {
        var variable = new CustomVariable { Name = "Health" };

        Assert.False(variable.HasAccompanyingVelocityConsideringTunneling(new EntitySave(), maxDepth: 1));
    }

    [Fact]
    public void HasAccompanyingVelocity_TunneledButMaxDepthZero_ReturnsFalse()
    {
        var entity = new EntitySave();
        entity.NamedObjects.Add(new NamedObjectSave { InstanceName = "SpriteInstance", SourceType = SourceType.FlatRedBallType });
        var variable = new CustomVariable { Name = "SpriteX", SourceObject = "SpriteInstance", SourceObjectProperty = "X" };

        Assert.False(variable.HasAccompanyingVelocityConsideringTunneling(entity, maxDepth: 0));
    }

    [Fact]
    public void HasAccompanyingVelocity_TunneledToMissingObject_ReturnsFalse()
    {
        var variable = new CustomVariable { Name = "SpriteX", SourceObject = "Gone", SourceObjectProperty = "X" };

        Assert.False(variable.HasAccompanyingVelocityConsideringTunneling(new EntitySave(), maxDepth: 1));
    }

    [Theory]
    [InlineData("X", true)]
    [InlineData("RotationZ", true)]
    [InlineData("Visible", false)]
    public void HasAccompanyingVelocity_TunneledToFrbTypeProperty_UsesFrbVelocityTable(string property, bool expected)
    {
        var entity = new EntitySave();
        entity.NamedObjects.Add(new NamedObjectSave { InstanceName = "SpriteInstance", SourceType = SourceType.FlatRedBallType });
        var variable = new CustomVariable { Name = "Tunneled", SourceObject = "SpriteInstance", SourceObjectProperty = property };

        Assert.Equal(expected, variable.HasAccompanyingVelocityConsideringTunneling(entity, maxDepth: 1));
    }

    [Fact]
    public void HasAccompanyingVelocity_TunneledToEntityVariableWithFrbVelocityName_ReturnsTrue()
    {
        var inner = new EntitySave { Name = "Entities\\Inner" };
        inner.CustomVariables.Add(new CustomVariable { Name = "X" });
        _finder.GlueProject.Entities.Add(inner);
        var outer = new EntitySave { Name = "Entities\\Outer" };
        outer.NamedObjects.Add(new NamedObjectSave { InstanceName = "InnerInstance", SourceType = SourceType.Entity, SourceClassType = "Entities\\Inner" });
        var variable = new CustomVariable { Name = "Tunneled", SourceObject = "InnerInstance", SourceObjectProperty = "X" };

        Assert.True(variable.HasAccompanyingVelocityConsideringTunneling(outer, maxDepth: 1));
    }

    [Fact]
    public void HasAccompanyingVelocity_TunneledToEntityVariableWithOwnVelocity_RecursesOneLevel()
    {
        var inner = new EntitySave { Name = "Entities\\Inner" };
        inner.CustomVariables.Add(new CustomVariable { Name = "Speed", HasAccompanyingVelocityProperty = true });
        _finder.GlueProject.Entities.Add(inner);
        var outer = new EntitySave { Name = "Entities\\Outer" };
        outer.NamedObjects.Add(new NamedObjectSave { InstanceName = "InnerInstance", SourceType = SourceType.Entity, SourceClassType = "Entities\\Inner" });
        var variable = new CustomVariable { Name = "Tunneled", SourceObject = "InnerInstance", SourceObjectProperty = "Speed" };

        Assert.True(variable.HasAccompanyingVelocityConsideringTunneling(outer, maxDepth: 1));
    }

    [Fact]
    public void HasAccompanyingVelocity_TunneledToEntityVariableWithoutVelocity_ReturnsFalse()
    {
        var inner = new EntitySave { Name = "Entities\\Inner" };
        inner.CustomVariables.Add(new CustomVariable { Name = "Speed" });
        _finder.GlueProject.Entities.Add(inner);
        var outer = new EntitySave { Name = "Entities\\Outer" };
        outer.NamedObjects.Add(new NamedObjectSave { InstanceName = "InnerInstance", SourceType = SourceType.Entity, SourceClassType = "Entities\\Inner" });
        var variable = new CustomVariable { Name = "Tunneled", SourceObject = "InnerInstance", SourceObjectProperty = "Speed" };

        Assert.False(variable.HasAccompanyingVelocityConsideringTunneling(outer, maxDepth: 1));
    }

    [Theory]
    [InlineData("Y", true)]
    [InlineData("Visible", false)]
    public void HasAccompanyingVelocity_TunneledToEntityPropertyWithNoVariable_UsesFrbVelocityTable(string property, bool expected)
    {
        var inner = new EntitySave { Name = "Entities\\Inner" };
        _finder.GlueProject.Entities.Add(inner);
        var outer = new EntitySave { Name = "Entities\\Outer" };
        outer.NamedObjects.Add(new NamedObjectSave { InstanceName = "InnerInstance", SourceType = SourceType.Entity, SourceClassType = "Entities\\Inner" });
        var variable = new CustomVariable { Name = "Tunneled", SourceObject = "InnerInstance", SourceObjectProperty = property };

        Assert.Equal(expected, variable.HasAccompanyingVelocityConsideringTunneling(outer, maxDepth: 1));
    }

    [Fact]
    public void HasAccompanyingVelocity_TunneledToUnknownEntity_ReturnsFalse()
    {
        var outer = new EntitySave { Name = "Entities\\Outer" };
        outer.NamedObjects.Add(new NamedObjectSave { InstanceName = "InnerInstance", SourceType = SourceType.Entity, SourceClassType = "Entities\\Gone" });
        var variable = new CustomVariable { Name = "Tunneled", SourceObject = "InnerInstance", SourceObjectProperty = "X" };

        Assert.False(variable.HasAccompanyingVelocityConsideringTunneling(outer, maxDepth: 1));
    }

    #endregion

    #region GetIsSourceFile

    [Fact]
    public void GetIsSourceFile_NoSourceObject_ReturnsFalse()
    {
        var variable = new CustomVariable { Name = "Health", SourceObjectProperty = "SourceFile" };

        Assert.False(variable.GetIsSourceFile(new EntitySave()));
    }

    [Fact]
    public void GetIsSourceFile_SourceObjectNotFound_ReturnsFalse()
    {
        var variable = new CustomVariable { Name = "Tunneled", SourceObject = "Gone", SourceObjectProperty = "SourceFile" };

        Assert.False(variable.GetIsSourceFile(new EntitySave()));
    }

    [Fact]
    public void GetIsSourceFile_FrbTypeSourceFileProperty_ReturnsTrue()
    {
        var entity = new EntitySave();
        entity.NamedObjects.Add(new NamedObjectSave { InstanceName = "SpriteInstance", SourceType = SourceType.FlatRedBallType });
        var variable = new CustomVariable { Name = "Tunneled", SourceObject = "SpriteInstance", SourceObjectProperty = "SourceFile" };

        Assert.True(variable.GetIsSourceFile(entity));
    }

    [Fact]
    public void GetIsSourceFile_FrbTypeOtherProperty_ReturnsFalse()
    {
        var entity = new EntitySave();
        entity.NamedObjects.Add(new NamedObjectSave { InstanceName = "SpriteInstance", SourceType = SourceType.FlatRedBallType });
        var variable = new CustomVariable { Name = "Tunneled", SourceObject = "SpriteInstance", SourceObjectProperty = "X" };

        Assert.False(variable.GetIsSourceFile(entity));
    }

    [Fact]
    public void GetIsSourceFile_EntitySourceFileProperty_ReturnsFalse()
    {
        var entity = new EntitySave();
        entity.NamedObjects.Add(new NamedObjectSave { InstanceName = "InnerInstance", SourceType = SourceType.Entity });
        var variable = new CustomVariable { Name = "Tunneled", SourceObject = "InnerInstance", SourceObjectProperty = "SourceFile" };

        Assert.False(variable.GetIsSourceFile(entity));
    }

    #endregion
}
