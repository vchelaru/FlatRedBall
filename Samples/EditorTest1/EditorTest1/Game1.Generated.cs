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

using System.Linq;
namespace EditorTest1
{
    public partial class Game1
    {
        #if DEBUG
        GlueCommunication.GameConnectionManager gameConnectionManager;
        #endif
        #if DEBUG
        GlueControl.GlueControlManager glueControlManager;
        #endif
        partial void GeneratedInitializeEarly () 
        {
        }
        partial void GeneratedInitialize () 
        {
            global::EditorTest1.GlobalContent.Initialize();
            System.AppDomain currentDomain = System.AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += (s, e) =>
            {
                // Get just the name of assmebly
                // Aseembly name excluding version and other metadata
                string name = e.Name.Contains(", ") ? e.Name.Substring(0, e.Name.IndexOf(", ")) : e.Name;
            
                if (name == "Newtonsoft.Json")
                {
                    // Load whatever version available
                    return System.Reflection.Assembly.Load(name);
                }
            
                return null;
            };
            
            #if DEBUG
            gameConnectionManager = new GlueCommunication.GameConnectionManager(8846);
            this.Exiting += (not, used) => gameConnectionManager.Dispose();
            #endif
            var args = System.Environment.GetCommandLineArgs();
            bool? changeResize = null;
            var resizeArgs = args.FirstOrDefault(item => item.StartsWith("AllowWindowResizing="));
            if (!string.IsNullOrEmpty(resizeArgs))
            {
                var afterEqual = resizeArgs.Split('=')[1];
                changeResize = bool.Parse(afterEqual);
            }
            if (changeResize != null)
            {
                CameraSetup.Data.AllowWindowResizing = changeResize.Value;
            }
            CameraSetup.SetupCamera(FlatRedBall.Camera.Main, graphics);
            #if WEB
            global::FlatRedBall.FlatRedBallServices.ForceClientSizeUpdates();
            #endif
            #if GameCanStartInEditMode || REFERENCES_FRB_SOURCE
            var isInEditMode = args.FirstOrDefault(item => item.StartsWith("IsInEditMode="));
            if (!string.IsNullOrEmpty(isInEditMode))
            {
                //I started working on this, but decided to drop it because it's more complicated than simply setting
                //IsNextScreenInEditMode. The code that handles SetEditMode dto needs to run, which is a bigger refator. 
                //I'll keep this code in here for now, and return later when I'm ready to do a bigger refactor. 
                //var afterEqual = isInEditMode.Split('=')[1];
                //FlatRedBall.Screens.ScreenManager.IsNextScreenInEditMode = bool.Parse(afterEqual);
                //this.IsMouseVisible = true;
            }
            #endif
            #if DEBUG
            glueControlManager = new GlueControl.GlueControlManager(8846)
            {
                GameConnectionManager = gameConnectionManager,
            };
            FlatRedBall.FlatRedBallServices.GraphicsOptions.SizeOrOrientationChanged += (not, used) =>
            {
                if (FlatRedBall.Screens.ScreenManager.IsInEditMode)
                {
                    GlueControl.Editing.CameraLogic.UpdateCameraToZoomLevel(zoomAroundCursorPosition: false);
                }
                GlueControl.Editing.CameraLogic.PushZoomLevelToEditor();
            }
            ;
            #endif
            FlatRedBall.Screens.ScreenManager.AfterScreenDestroyed += (screen) =>
            {
                GlueControl.Editing.EditorVisuals.DestroyContainedObjects();
            }
            ;
            System.Type startScreenType = typeof(global::EditorTest1.Screens.GameScreen);
            var commandLineArgs = System.Environment.GetCommandLineArgs();
            if (commandLineArgs.Length > 0)
            {
                var thisAssembly = this.GetType().Assembly;
                // see if any of these are screens:
                foreach (var item in commandLineArgs)
                {
                    var type = thisAssembly.GetType(item);
                    if (type != null)
                    {
                        startScreenType = type;
                        break;
                    }
                }
            }
            if (startScreenType != null)
            {
                FlatRedBall.Screens.ScreenManager.Start(startScreenType);
            }
            // This value is used for parallax. If the game doesn't change its resolution, this this code should solve parallax with zooming cameras.
            global::FlatRedBall.TileGraphics.MapDrawableBatch.NativeCameraWidth = global::EditorTest1.CameraSetup.Data.ResolutionWidth;
            global::FlatRedBall.TileGraphics.MapDrawableBatch.NativeCameraHeight = global::EditorTest1.CameraSetup.Data.ResolutionHeight;
        }
        partial void GeneratedUpdate (Microsoft.Xna.Framework.GameTime gameTime) 
        {
        }
        partial void GeneratedDrawEarly (Microsoft.Xna.Framework.GameTime gameTime) 
        {
        }
        partial void GeneratedDraw (Microsoft.Xna.Framework.GameTime gameTime) 
        {
        }
    }
}
