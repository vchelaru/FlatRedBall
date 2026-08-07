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


using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Screens;
using GlueControl.Models;
using System;
using Microsoft.Xna.Framework;

namespace GlueControl.Screens
{
    class EntityViewingScreen : Screen
    {
        #region Fields/properties

        public IDestroyable CurrentEntity { get; set; }

        public static string GameElementTypeToCreate { get; set; }
        public static NamedObjectSave InstanceToSelect { get; set; }

        bool isViewingAbstractEntity;

        System.Collections.Generic.List<System.Collections.IList> FactoryLists = new System.Collections.Generic.List<System.Collections.IList>();
        System.Collections.Generic.List<System.Collections.IList> ListsNeedingManualUpdates = new System.Collections.Generic.List<System.Collections.IList>();

        #endregion

        public EntityViewingScreen() : base(nameof(EntityViewingScreen))
        {

        }

        public override void Initialize(bool addToManagers)
        {
            base.Initialize(addToManagers);

            CreateFactoryLists();

            // do this before add to managers so that the events are +='ed on the factories.S
            BeforeCustomInitialize?.Invoke();

            if (addToManagers)
            {
                AddToManagers();
            }
        }

        private void CreateFactoryLists()
        {
            foreach (var factory in EditorTest1.Performance.FactoryManager.GetAllFactories())
            {
                var factoryType = factory.GetType();
                var createNewMethod = factoryType.GetMethod("CreateNew", new Type[] { typeof(Microsoft.Xna.Framework.Vector3) });
                var genericType = createNewMethod.ReturnType;

                var listType = typeof(FlatRedBall.Math.PositionedObjectList<>).MakeGenericType(genericType);

                var listInstance = System.Activator.CreateInstance(listType) as System.Collections.IList;

                factory.ListsToAddTo.Add(listInstance);
                FactoryLists.Add(listInstance);

                var glueElementName = CommandReceiver.GameElementTypeToGlueElement(genericType.FullName);
                var element = Managers.ObjectFinder.Self.GetEntitySave(glueElementName);

                if (element?.IsManuallyUpdated == true)
                {
                    ListsNeedingManualUpdates.Add(listInstance);
                }
            }
        }

        public override void ActivityEditMode()
        {
            base.ActivityEditMode();

            var camera = Camera.Main;
            if (isViewingAbstractEntity)
            {
                Editing.EditorVisuals.Text(
                    $"The entity {EntityViewingScreen.GameElementTypeToCreate} is abstract so it cannot be previewed.\nSelect a derived entity type to view it.",
                    new Vector3(camera.X, camera.Y, 0));
            }

            try
            {
                for (int i = 0; i < SpriteManager.ManagedPositionedObjects.Count; i++)
                {
                    PositionedObject item = FlatRedBall.SpriteManager.ManagedPositionedObjects[i];
                    if (item is FlatRedBall.Entities.IEntity entity)
                    {
                        entity.ActivityEditMode();
                    }
                }
                foreach (var list in ListsNeedingManualUpdates)
                {
                    foreach (var item in list)
                    {
                        (item as FlatRedBall.Entities.IEntity)?.ActivityEditMode();
                    }

                }
                base.ActivityEditMode();
            }
            catch (Exception e)
            {
                Editing.EditorVisuals.Text(
                    $"Error in edit mode for entity {CurrentEntity?.GetType().Name}\n{e}\n{e.InnerException}",
                    new Vector3(camera.X, camera.Y, 0));
            }
        }

        public override void AddToManagers()
        {
            base.AddToManagers();

            if (!string.IsNullOrEmpty(EntityViewingScreen.GameElementTypeToCreate))
            {
                var entityType = this.GetType().Assembly.GetType(EntityViewingScreen.GameElementTypeToCreate);
                isViewingAbstractEntity = entityType?.IsAbstract == true;

                GlueControl.Editing.EditingManager.Self.ElementEditingMode = GlueControl.Editing.ElementEditingMode.EditingEntity;

                Camera.Main.X = 0;
                Camera.Main.Y = 0;
                Camera.Main.Detach();

                if (!isViewingAbstractEntity)
                {

                    //var instance = ownerType.GetConstructor(new System.Type[0]).Invoke(new object[0]) as IDestroyable;
                    var instance = GlueControl.InstanceLogic.Self.CreateEntity(EntityViewingScreen.GameElementTypeToCreate, isMainEntityInScreen: true) as IDestroyable;
                    CurrentEntity = instance;
                    var instanceAsPositionedObject = (PositionedObject)instance;
                    instanceAsPositionedObject.Velocity = Microsoft.Xna.Framework.Vector3.Zero;
                    instanceAsPositionedObject.Acceleration = Microsoft.Xna.Framework.Vector3.Zero;


                    GlueControl.Editing.EditingManager.Self.Select(InstanceToSelect);
                }
            }
        }

        public override void Destroy()
        {
            GlueControl.InstanceLogic.Self.DestroyDynamicallyAddedInstances();

            CurrentEntity?.Destroy();

            // there could be entities which normally would end up in a screen list (like bullets or particles) which do not have a matching list here. Therefore, we should
            // destroy all destroyables:
            for (int i = SpriteManager.ManagedPositionedObjects.Count - 1; i > -1; i--)
            {
                var item = SpriteManager.ManagedPositionedObjects[i] as IDestroyable;
                item?.Destroy();
            }

            // There could be objects that aren't destroyed 
            foreach (var list in this.FactoryLists)
            {
                for (int i = list.Count - 1; i > -1; i--)
                {
                    var item = list[i] as IDestroyable;
                    item?.Destroy();
                }
            }

            foreach (var factory in EditorTest1.Performance.FactoryManager.GetAllFactories())
            {
                factory.Destroy();
            }


            base.Destroy();
        }
    }
}
