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
using FlatRedBall.IO;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace GlueControl.Models
{
    public static class GlueProjectSaveExtensions
    {
        public static GlueProjectSave Load(FilePath fileName)
        {
            GlueProjectSave mainGlueProjectSave = null;
            if (fileName.Extension == "glux")
            {
                mainGlueProjectSave = FileManager.XmlDeserialize<GlueProjectSave>(fileName.FullPath);
            }
            else if (fileName.Extension == "gluj")
            {
                // During the conversion, there may still be XML files using version 9, so check for that:
                if (fileName.Exists())
                {
                    var text = System.IO.File.ReadAllText(fileName.FullPath);
                    mainGlueProjectSave = JsonConvert.DeserializeObject<GlueProjectSave>(text);
                }
                else if (System.IO.File.Exists(fileName.RemoveExtension() + ".glux"))
                {
                    mainGlueProjectSave = FileManager.XmlDeserialize<GlueProjectSave>(fileName.RemoveExtension() + ".glux");
                }
            }
            // don't do this in the editor
            //mainGlueProjectSave = mainGlueProjectSave.MarkTags("GLUE");

            var files =
                Directory.GetFiles(fileName.GetDirectoryContainingThis() + @"\");



            foreach (var file in files.Where(item => 
                         item.EndsWith(".generated.glux", StringComparison.OrdinalIgnoreCase) 
                         || item.EndsWith(".generated.gluj", StringComparison.OrdinalIgnoreCase)))
            {
                string withoutExtension = FileManager.RemoveExtension(file);
                string withoutGenerated = FileManager.RemoveExtension(withoutExtension);

                if (withoutGenerated == null) continue;
                var tag = FileManager.GetExtension(withoutGenerated);

                //mainGlueProjectSave.Merge(FileManager.XmlDeserialize<GlueProjectSave>(file)
                //.MarkTags(tag));
                ;
            }

            if (mainGlueProjectSave.FileVersion >= (int)GlueProjectSave.GluxVersions.SeparateJsonFilesForElements)
            {
                LoadReferencedScreensAndEntities(fileName, mainGlueProjectSave);

                // todo - when we eventually support referenced file saves
                //WildcardReferencedFileSaveLogic.LoadWildcardReferencedFiles(fileName, mainGlueProjectSave);
            }

            return mainGlueProjectSave;
        }

        private static void LoadReferencedScreensAndEntities(FilePath glujFilePath, GlueProjectSave main)
        {
            var glueDirectory = glujFilePath.GetDirectoryContainingThis();
            foreach (var screenReference in main.ScreenReferences)
            {
                var path = new FilePath(glueDirectory + screenReference.Name + "." + GlueProjectSave.ScreenExtension);

                if (path.Exists())
                {
                    var fileContents = System.IO.File.ReadAllText(path.FullPath);
                    var deserialized = JsonConvert.DeserializeObject<ScreenSave>(fileContents);

                    main.Screens.Add(deserialized);
                }
            }

            foreach (var entityReference in main.EntityReferences)
            {
                var path = new FilePath(glueDirectory + entityReference.Name + "." + GlueProjectSave.EntityExtension);

                if (path.Exists())
                {
                    var fileContents = System.IO.File.ReadAllText(path.FullPath);
                    var deserialized = JsonConvert.DeserializeObject<EntitySave>(fileContents);

                    main.Entities.Add(deserialized);
                }
            }

            main.ScreenReferences.Clear();
            main.EntityReferences.Clear();
        }


    }
}
