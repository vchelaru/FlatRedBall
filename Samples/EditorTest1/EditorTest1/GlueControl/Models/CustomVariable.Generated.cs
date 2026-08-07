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
    public class CustomVariable
    {
        string mSourceObject;
        string mSourceObjectProperty;
        object mDefaultValue;

        public string Name
        {
            get;
            set;
        }

        public List<PropertySave> Properties = new List<PropertySave>();

        public object DefaultValue
        {
            get { return mDefaultValue; }
            set
            {
                // We used to do this, but it turns out the user
                // may want to set a value like a Sprite's Texture
                // back to NULL.  Therefore, we allow "<NONE>" and 
                // we just use null in the code generation.
                // UPDATE:  Actually, the user can't choose to not set
                // a CustomVariable - it's always set to something.  And 
                // the user may want to make a variable for setting a Sprite's
                // Texture, but may not want to necessarily override it in the 
                // custom variable.  So we'll handle "NONE" as nothing.
                if (value is string && (string)value == "<NONE>")
                {
                    mDefaultValue = null;
                }
                else
                {
                    mDefaultValue = value;
                }
            }
        }

        public bool SetByDerived
        {
            get;
            set;
        }

        public bool IsShared
        {
            get;
            set;
        }

        public bool DefinedByBase
        {
            get;
            set;
        }


        public string SourceObject
        {
            get { return mSourceObject; }
            set
            {
                mSourceObject = value;

                if (mSourceObject == "<NONE>")
                {
                    mSourceObject = "";
                }
            }

        }

        public string SourceObjectProperty
        {
            get { return mSourceObjectProperty; }
            set
            {
                mSourceObjectProperty = value;
                if (mSourceObjectProperty == "<NONE>")
                {
                    mSourceObjectProperty = "";
                }
            }
        }


    }
}
