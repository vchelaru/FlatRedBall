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
using System.Xml.Serialization;
using Newtonsoft.Json;
using FlatRedBall.IO;

namespace GlueControl.Models
{
    public abstract class GlueElement : IFileReferencer, INamedObjectContainer
    {

        public List<ReferencedFileSave> ReferencedFiles
        {
            get;
            set;
        }

        public bool UseGlobalContent
        {
            get;
            set;
        }

        public ReferencedFileSave GetReferencedFileSave(string fileName)
        {
            return IElementExtensionMethods.GetReferencedFileSave(this, fileName);
        }

        public List<CustomVariable> CustomVariables
        {
            get;
            set;
        }

        public bool IsOnOwnLayer
        {
            get;
            set;
        }

        [XmlIgnore]
        [JsonIgnore]
        public string ClassName
        {
            get
            {
                return FileManager.RemovePath(Name);
            }
        }

        public string Name
        {
            get;
            set;
        }

        public abstract string BaseElement { get; }

        [XmlIgnore]
        [JsonIgnore]
        public abstract string BaseObject
        {
            get; set;
        }

        [XmlIgnore]
        [JsonIgnore]
        public int VerificationIndex
        {
            get;
            set;
        }

        public IEnumerable<NamedObjectSave> AllNamedObjects
        {
            get
            {
                foreach (NamedObjectSave nos in NamedObjects)
                {
                    yield return nos;

                    foreach (NamedObjectSave containedNos in nos.ContainedObjects)
                    {
                        yield return containedNos;
                    }
                }
            }
        }


        public List<NamedObjectSave> NamedObjects
        {
            get;
            set;
        } = new List<NamedObjectSave>();

        public List<PropertySave> Properties
        {
            get;
            set;
        } = new List<PropertySave>();

        public CustomVariable GetCustomVariable(string customVariableName)
        {
            foreach (var customVariable in CustomVariables)
            {
                if (customVariable.Name == customVariableName)
                {
                    return customVariable;
                }
            }
            return null;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class EntitySave : GlueElement
    {
        public override string BaseElement => BaseEntity;

        string mBaseEntity;
        public string BaseEntity
        {
            get { return mBaseEntity; }
            set
            {
                if (value == "<NONE>")
                {
                    mBaseEntity = "";
                }
                else
                {
                    mBaseEntity = value;
                }

            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public override string BaseObject
        {
            get { return mBaseEntity; }
            set { mBaseEntity = value; }
        }

        [XmlIgnore]
        [JsonIgnore]
        public bool IsManuallyUpdated
        {
            get
            {
                return Properties.GetValue<bool>(nameof(IsManuallyUpdated));
            }
            set
            {
                Properties.SetValue(nameof(IsManuallyUpdated), value);
            }
        }
    }

    public class ScreenSave : GlueElement
    {
        string mBaseScreen;

        public string BaseScreen
        {
            get { return mBaseScreen; }
            set
            {
                if (value == "<NONE>")
                {
                    mBaseScreen = "";
                }
                else
                {
                    mBaseScreen = value;
                }
            }
        }

        public override string BaseElement => BaseScreen;

        public override string BaseObject
        {
            get { return mBaseScreen; }
            set { mBaseScreen = value; }
        }

    }
}