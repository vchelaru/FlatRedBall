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
using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Math.Geometry;

namespace GlueControl.Runtime
{
    public class DynamicEntity : PositionedObject, IDestroyable, ICollidable
    {
        public string EditModeType { get; set; }

        public HashSet<string> ItemsCollidedAgainst { get; private set; } = new HashSet<string>();
        public HashSet<string> LastFrameItemsCollidedAgainst { get; private set; } = new HashSet<string>();

        public HashSet<object> ObjectsCollidedAgainst { get; private set; } = new HashSet<object>();
        public HashSet<object> LastFrameObjectsCollidedAgainst { get; private set; } = new HashSet<object>();

        public ShapeCollection Collision
        {
            get; private set;
        } = new ShapeCollection();

        public void Destroy()
        {
            // needs to loop through children and destroy?
            RemoveSelfFromListsBelongingTo();


            for (int i = Children.Count - 1; i > -1; i--)
            {
                var child = Children[i];

                if (child is IDestroyable destroyable)
                {
                    destroyable.Destroy();
                }
                else if (child is Circle circle)
                {
                    ShapeManager.Remove(circle);
                }
                else if (child is Polygon polygon)
                {
                    ShapeManager.Remove(polygon);
                }
                else if (child is AxisAlignedRectangle rectangle)
                {
                    ShapeManager.Remove(rectangle);
                }
                else if (child is Line line)
                {
                    ShapeManager.Remove(line);
                }
                else if (child is Sprite sprite)
                {
                    SpriteManager.RemoveSprite(sprite);
                }
                else if (child is PositionedObject positionedObject)
                {
                    positionedObject.RemoveSelfFromListsBelongingTo();
                }
            }
        }
    }
}