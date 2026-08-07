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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall;
using GlueControl.Dtos;
using GlueControl.Runtime;

namespace GlueControl
{
    internal class CommandReplayLogic
    {
        public static List<object> GetDtosToReplayFor(string elementNameGlue)
        {
            var element = GlueControl.Managers.ObjectFinder.Self.GetElement(elementNameGlue);
            HashSet<string> allElementNames = new HashSet<string>();
            allElementNames.Add(elementNameGlue);

            if (element != null)
            {
                var baseElements = GlueControl.Managers.ObjectFinder.Self.GetAllBaseElementsRecursively(element);
                foreach (var baseElement in baseElements)
                {
                    allElementNames.Add(baseElement.Name);
                }
            }

            List<object> dtosToReplay = CommandReceiver.GlobalGlueToGameCommands
                .Where(item =>
                {
                    if (item is Dtos.AddObjectDto addObjectDtoReplay)
                    {
                        return allElementNames.Contains(addObjectDtoReplay.ElementNameGlue);
                    }
                    else if (item is Dtos.GlueVariableSetData glueVariableSetDataRerun)
                    {
                        return allElementNames.Contains(glueVariableSetDataRerun.ElementNameGlue);
                    }
                    else if (item is RemoveObjectDto removeObjectDtoRerun)
                    {
                        // We'll loop through the individuals inside this dto, so for now assume true
                        return true;
                    }
                    return false;
                }).ToList();

            return dtosToReplay;
        }
        public static void ApplyEditorCommandsToNewElement(PositionedObject newEntity, string elementNameGlue)
        {
            var element = GlueControl.Managers.ObjectFinder.Self.GetElement(elementNameGlue);
            HashSet<string> allElementNames = new HashSet<string>();
            allElementNames.Add(elementNameGlue);

            if (element != null)
            {
                var baseElements = GlueControl.Managers.ObjectFinder.Self.GetAllBaseElementsRecursively(element);
                foreach (var baseElement in baseElements)
                {
                    allElementNames.Add(baseElement.Name);
                }
            }



            List<object> dtosToReplay = GetDtosToReplayFor(elementNameGlue);

            for (int i = 0; i < dtosToReplay.Count; i++)
            {
                var dto = dtosToReplay[i];
                if (dto is Dtos.AddObjectDto addObjectDtoRerun)
                {
                    InstanceLogic.Self.HandleCreateInstanceCommandFromGlue(addObjectDtoRerun, newEntity);
                }
                else if (dto is Dtos.GlueVariableSetData glueVariableSetDataRerun)
                {
                    // do the variables set on the entity level first, then later the ones set at screen level since
                    // screen level should always be applied later
                    GlueControl.Editing.VariableAssignmentLogic.SetVariable(glueVariableSetDataRerun, newEntity);
                }
                else if (dto is RemoveObjectDto removeObjectDtoRerun)
                {
                    RemoveObjectDtoResponse response = new RemoveObjectDtoResponse();

                    for (int j = 0; j < removeObjectDtoRerun.ObjectNames.Count; j++)
                    {
                        var shouldRerun = allElementNames.Contains(removeObjectDtoRerun.ElementNamesGlue[j]);

                        if (shouldRerun)
                        {
                            var objectName = removeObjectDtoRerun.ObjectNames[j];
                            var removeObjectElement = removeObjectDtoRerun.ElementNamesGlue[j];
                            InstanceLogic.Self.HandleDeleteObject(newEntity, removeObjectElement, objectName, response);
                        }
                    }
                }
            }
        }

    }
}
