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

﻿using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Screens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueControl.Editing
{
    public static class MoveObjectToContainerLogic
    {
        internal static bool TryMoveObjectToContainer(string objectName, string containerName, ElementEditingMode elementEditingMode)
        {
            // first we have to get the object
            var allAvailableObjects =
                SelectionLogic.GetAvailableObjects(elementEditingMode);
            var instanceToMove = allAvailableObjects.FirstOrDefault(item => item.Name == objectName);

            if (instanceToMove != null)
            {
                // If the container name is not null, check the screen for a matching container (list, shape collection)
                if (!string.IsNullOrWhiteSpace(containerName))
                {
                    var container = GetContainerByName(containerName);

                    // if the container has been found, add the object to the container according to the container type and return true
                    if (container != null)
                    {
                        if (container is ShapeCollection asShapeCollection)
                        {
                            if (instanceToMove is AxisAlignedRectangle aaRect)
                            {
                                if (!asShapeCollection.AxisAlignedRectangles.Contains(aaRect))
                                {
                                    asShapeCollection.AxisAlignedRectangles.Add(aaRect);
                                }
                                return true;
                            }
                            else if (instanceToMove is Circle circle)
                            {
                                if (!asShapeCollection.Circles.Contains(circle))
                                {
                                    asShapeCollection.Circles.Add(circle);
                                }
                                return true;
                            }
                            else if (instanceToMove is Polygon polygon)
                            {
                                if (!asShapeCollection.Polygons.Contains(polygon))
                                {
                                    asShapeCollection.Polygons.Add(polygon);
                                }
                                return true;
                            }
                            return false;
                        }
                        else if (container is IList asIList) // PositionedObjectLists are ILists
                        {
                            // this could fail, so try/catch
                            try
                            {
                                if (!asIList.Contains(instanceToMove))
                                {
                                    asIList.Add(instanceToMove);
                                }
                                return true;
                            }
                            catch
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        // If the container is not found, return false
                        return false;
                    }
                }
                else
                {
                    // for now return false...
                    return false;

                    // If the container is null, check the listsbelongto to see if there is a list with the matching name

                    // If found, remove it
                    // If not, return false
                }
            }

            return false;
        }

        private static object GetContainerByName(string containerName)
        {
            var currentScreen = ScreenManager.CurrentScreen;

            object instance = null;

            instance = InstanceLogic.Self.ShapeCollectionsAddedAtRuntime.FirstOrDefault(item => item.Name == containerName);

            if (instance == null)
            {
                instance = InstanceLogic.Self.ListsAddedAtRuntime.FirstOrDefault(item => (item as FlatRedBall.Utilities.INameable).Name == containerName);
            }

            if (instance == null)
            {
                currentScreen.GetInstance(containerName, currentScreen, out string throwaway, out instance);
            }


            return instance;
        }
    }
}