#define IncludeSetVariable
// The following #defines come from the version of your GLUJ/GLUX file. For more information see https://docs.flatredball.com/flatredball/glue-reference/glujglux
#define PreVersion
#define HasFormsObject
#define AddedGeneratedGame1
#define ListsHaveAssociateWithFactoryBool
#define GumGueHasGetAnimation
#define CsvInheritanceSupport
#define IPositionedSizedObjectInEngine
#define NugetPackageInCsproj
#define SupportsEditMode
#define SupportsShapeCollectionAddToManagerMakeAutomaticallyUpdated
#define ScreensHaveActivityEditMode
#define SupportsNamedSubcollisions
#define TimeManagerHasDelaySeconds
#define GumTextHasIsBold
#define GlueSavedToJson
#define IEntityInFrb
#define SeparateJsonFilesForElements
#define GumSupportsAchxAnimation
#define StartupInGeneratedGame
#define RemoveAutoLocalizationOfVariables
#define GumHasMIsLayoutSuspendedPublic
#define SpriteHasUseAnimationTextureFlip
#define RemoveIsScrollableEntityList
#define HasGetGridLine
#define HasScreenManagerAfterScreenDestroyed
#define ScreenManagerHasPersistentPolygons
#define ShapeManagerCollideAgainstClosest
#define SpriteHasTolerateMissingAnimations
#define AnimationLayerHasName
#define IPlatformer
#define GumDefaults2
#define IStackableInEngine
#define ICollidableHasItemsCollidedAgainst
#define CollisionRelationshipManualPhysics
#define GumSupportsStackSpacing
#define CollisionRelationshipsSupportMoveSoft
#define GeneratedCameraSetupFile
#define ShapeCollectionHasMaxAxisAlignedRectanglesRadiusX
#define AutoNameCollisionListsAsSingle
#define GumHasIgnoredByParentSize
#define GumTextObjectsUpdateTextWith0ChildDepth
#define HasFrameworkElementManager
#define HasGumSkiaElements
#define ITiledTileMetadataInFrb
#define DamageableHasHealth
#define HasGame1GenerateEarly
#define ICollidableHasObjectsCollidedAgainst
#define HasIRepeatPressableInput
#define AllTiledFilesGenerated
#define RemoveRedundantDerivedData
#define GraphicalUiElementProtectedAnimationProperties
#define GraphicalUiElementINotifyPropertyChanged
#define GumTextObjectsHaveTextOverflowProperties
#define TileShapeCollectionIsICollidable
#define TileShapeCollectionAddToLayerSupportsAutomaticallyUpdated
#define ISongInFrb
#define RendererHasExternalEffectManager
#define SpriteHasSetCollisionFromAnimation
#define HasIGumScreenOwner
#define ScreenIsINameable
#define SpriteManagerHasInsertLayer
#define GumUsesSystemTypes
#define GumCommonCodeReferencing
#define GumTextSupportsBbCode
#define DamageDealingToggles
#define VariantsInsteadOfTypes
#define ITopDownEntity
#define CaseSensitiveLoading
#define ScreensHaveDefaultLayer
#define HasFrbServicesGraphicsDeviceManager
#define ShapeCollectionHasLastCollisionCallDeepCheckCount
#define ScreenHasCancellationToken
#define GameCanStartInEditMode
#define GumHasRenderableCloneLogic
#define ShapeCollectionHasIsPointOnOrInside
#define AudioManagerStopSongTakesBool
#define GraphicalUiElementRemoveFromManagersIsVirtual
#define GumVisualHasRenderTarget
#define GumNineSliceHasAnimate
#define ObsoleteGumDimensionUnitTypes
#define GumHasIRenderTargetTextureReferencer
#define GumHasGueVirtualIsPointInside
#define PositionedNodeHasTag
#define NineSliceHasTilingMiddleSections
#define GumHasFrbRuntimeInterfaces
using EditorTest1;

﻿using System;
using System.Collections.Generic;
using System.Text;

namespace GlueControl.Models
{
    public class GlueProjectSave
    {
        #region Constants

        public const string ScreenExtension = "glsj";
        public const string EntityExtension = "glej";

        #endregion

        #region Glux Version

        // Version 0/1 didn't exist
        // Version 2 introduces a partial game class
        // Version 3 has lists associated with factory
        // This is documented here: https://flatredball.com/documentation/tools/glue-reference/glujglux/
        public enum GluxVersions
        {
            PreVersion = 1,
            AddedGeneratedGame1 = 2,
            ListsHaveAssociateWithFactoryBool = 3,
            GumGueHasGetAnimation = 4,
            GumHasMIsLayoutSuspendedPublic = 4,
            HasFormsObject = 4, // Not sure if this is exact, but it should be maybe around here. This will make old projects work
            CsvInheritanceSupport = 5,
            NugetPackageInCsproj = 6,
            SupportsEditMode = 7,
            SupportsShapeCollectionAddToManagerMakeAutomaticallyUpdated = 7,
            // this was added late summer 2021
            // There should have been another version
            // inbetween 7 and 8, but there wasn't, and 
            // this introduced a problem found late February '22
            // with collision relationship subcollision generation.
            // Therefore, we'll duplicate ScreensHaveActivityEditMode as a
            // file version for supporting named subcollisions.
            ScreensHaveActivityEditMode = 8,
            SupportsNamedSubcollisions = 8,
            GlueSavedToJson = 9,
            IEntityInFrb = 10,
            SeparateJsonFilesForElements = 11,
            // This was added long ago, but a new version 
            // is being created here to not surprise existing
            // games with a double-animation call
            GumSupportsAchxAnimation = 12,
            // Added Feb 28, 2022
            StartupInGeneratedGame = 13,
            RemoveAutoLocalizationOfVariables = 14,
            SpriteHasUseAnimationTextureFlip = 15,
            RemoveIsScrollableEntityList = 16,
            ScreenManagerHasPersistentPolygons = 17,
        }

        #endregion

        #region Versions

        public const int LatestVersion = (int)GluxVersions.RemoveIsScrollableEntityList;

        public int FileVersion { get; set; }

        #endregion

        #region Elements and References

        public List<ScreenSave> Screens = new List<ScreenSave>();
        public List<EntitySave> Entities = new List<EntitySave>();

        public List<GlueElementFileReference> ScreenReferences { get; set; } = new List<GlueElementFileReference>();
        public List<GlueElementFileReference> EntityReferences { get; set; } = new List<GlueElementFileReference>();

        #endregion
    }
}
