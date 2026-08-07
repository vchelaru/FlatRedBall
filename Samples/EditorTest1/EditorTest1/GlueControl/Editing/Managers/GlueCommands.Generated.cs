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
using GlueControl.Dtos;
using GlueControl.Models;
using FlatRedBall.IO;


namespace GlueControl.Managers
{
    internal class GlueCommands : GlueCommandsStateBase
    {
        #region Fields/properties

        public static GlueCommands Self { get; }

        public GluxCommands GluxCommands { get; private set; }

        public GenerateCodeCommands GenerateCodeCommands { get; private set; }

        FilePath GlueProjectFilePath;
        #endregion

        #region  Constructors

        static GlueCommands() => Self = new GlueCommands();

        public GlueCommands()
        {
            GluxCommands = new GluxCommands();
            GenerateCodeCommands = new GenerateCodeCommands();
        }

        #endregion

        public string GetAbsoluteFileName(ReferencedFileSave rfs)
        {
            if (rfs == null)
            {
                throw new ArgumentNullException("rfs", "The argument ReferencedFileSave should not be null");
            }
            var gameDirectory = GlueProjectFilePath.GetDirectoryContainingThis();
            var contentDirectory = gameDirectory + "Content/";
            return contentDirectory + rfs.Name;
        }

        public FilePath GetAbsoluteFilePath(ReferencedFileSave rfs)
        {
            return GetAbsoluteFileName(rfs);
        }

        public void PrintOutput(string output)
        {
            SendMethodCallToGame(nameof(PrintOutput), output);
        }

        public void Undo()
        {
            SendMethodCallToGame(nameof(Undo));
        }

        private Task<object> SendMethodCallToGame(string caller = null, params object[] parameters)
        {
            return base.SendMethodCallToGame(new GlueCommandDto(), caller, parameters);
        }

        // It's async in Glue but we just do non-async here
        //public void LoadProjectAsync(string fileName)
        public void LoadProject(string fileName)
        {
            GlueProjectFilePath = fileName;
            ObjectFinder.Self.GlueProject = GlueProjectSaveExtensions.Load(fileName);

            var project = ObjectFinder.Self.GlueProject;
            foreach (var item in project.Screens)
            {
                item.FixAllTypes();
            }
            foreach (var item in project.Entities)
            {
                item.FixAllTypes();
            }
        }
    }
}
