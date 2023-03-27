using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using GlueSaveClasses;

//using System.Windows.Forms;

using System.Collections;
using Newtonsoft.Json;

namespace FlatRedBall.Glue.SaveClasses
{
    #region ResolutionValues Class
    public class ResolutionValues
    {
        public string Name;
        public int Width;
        public int Height;
    }
    #endregion

    public class GlueBookmark
    {
        public string Name { get; set; }
        public string ImageSource { get; set; }
    }

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
            HasFormsObject = 1, // Not sure if this is exact, but it should be maybe around here. This will make old projects work. It's before 4.
            AddedGeneratedGame1 = 2,
            ListsHaveAssociateWithFactoryBool = 3,
            GumGueHasGetAnimation = 4,
            // Nope, this isn't v4, I checked and it's actually version 14
            //GumHasMIsLayoutSuspendedPublic = 4,
            CsvInheritanceSupport = 5,
            IPositionedSizedObjectInEngine = 5,
            NugetPackageInCsproj = 6,
            // Actually the TileShapeCollection had a CollideAgainstClosest, but the ShapeManager didn't get it until v 17+, so moving this down:
            //ShapeManagerCollideAgainstClosest = 6,
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
            GumHasMIsLayoutSuspendedPublic = 14, // not exact, but close enough. Was added March 3, 2022
            SpriteHasUseAnimationTextureFlip = 15,
            RemoveIsScrollableEntityList = 16,
            // Not exact, but close enough to help address issues:
            HasGetGridLine = 17,
            // also not exact, but close enough:
            HasScreenManagerAfterScreenDestroyed = 17,
            ScreenManagerHasPersistentPolygons = 17,
            ShapeManagerCollideAgainstClosest = 17,

            SpriteHasTolerateMissingAnimations = 18,
            AnimationLayerHasName = 19,
            IPlatformer = 19,
            GumDefaults2 = 20,
            IStackableInEngine = 21,
            ICollidableHasItemsCollidedAgainst = 22,
            CollisionRelationshipManualPhysics = 23,
            GumSupportsStackSpacing = 24,
            CollisionRelationshipsSupportMoveSoft = 25,
            GeneratedCameraSetupFile = 26,
            ShapeCollectionHasMaxAxisAlignedRectanglesRadiusX = 27,
            AutoNameCollisionListsAsSingle = 28,
            GumTextObjectsUpdateTextWith0ChildDepth = 29,
            HasFrameworkElementManager = 30,
            HasGumSkiaElements = 31,
            ITiledTileMetadataInFrb = 32,
            DamageableHasHealth = 33,
            HasGame1GenerateEarly = 34,
            ICollidableHasObjectsCollidedAgainst = 35,
            HasIRepeatPressableInput = 36,
            AllTiledFilesGenerated = 37
        }

        #endregion

        #region Versions

        public const int LatestVersion = (int)GluxVersions.AllTiledFilesGenerated;

        public int FileVersion { get; set; }

        #endregion

        #region Camera Fields
        // April 2017 - adding replacement for these, eventually should get removed:
        public bool In2D = true;
        public bool RunFullscreen = false;


        public bool SetResolution = false;
        public int ResolutionWidth = 800;
        public int ResolutionHeight = 600;

        public bool SetOrthogonalResolution = false;
        public int OrthogonalWidth = 800;
        public int OrthogonalHeight = 600;


        #endregion

        #region Screens / Entities

        public List<ScreenSave> Screens = new List<ScreenSave>();

        public List<EntitySave> Entities = new List<EntitySave>();

        public List<GlueElementFileReference> ScreenReferences { get; set; } = new List<GlueElementFileReference>();
        public List<GlueElementFileReference> EntityReferences { get; set; } = new List<GlueElementFileReference>();

        #endregion

        #region Global Files

        public List<ReferencedFileSave> GlobalFiles = new List<ReferencedFileSave>();

        // Even though these never appear in the saved json on disk, they need to be serialized
        // so that the clone can be properly created, so it an't be ignored.
        // [JsonIgnore]
        public List<ReferencedFileSave> GlobalFileWildcards = new List<ReferencedFileSave>();

        public GlobalContentSettingsSave GlobalContentSettingsSave = new GlobalContentSettingsSave();

        #endregion

        #region Bookmarks

        public List<GlueBookmark> Bookmarks { get; set; } = new List<GlueBookmark>();


        #endregion

        #region Fields / Properties

        public GluxPluginData PluginData
        {
            get;
            set;
        } = new GluxPluginData();

        public List<PropertySave> Properties = new List<PropertySave>();
        public bool ShouldSerializeProperties()
        {
            return Properties != null && Properties.Count != 0;
        }



        public List<string> SyncedProjects = new List<string>();

        public string StartUpScreen;

        public List<ResolutionValues> ResolutionPresets = new List<ResolutionValues>();


        public PerformanceSettingsSave PerformanceSettingsSave = new PerformanceSettingsSave();

        public List<string> IgnoredDirectories = new List<string>();


        public List<CustomClassSave> CustomClasses = new List<CustomClassSave>();

        public bool ApplyToFixedResolutionPlatforms = true;


        public string ExternallyBuiltFileDirectory = "../../";


        public string CustomGameClass;

        // This is a new class added in early 2017 to give more control over cameras.
        // But we don't want to automatically update all projects to this.
        // If this is null, then Glue knows to use the old settings. If this is not null,
        // Glue knows to use the new settings. Eventually we'll just "new" this right here, but
        // in the meantime, Glue code will new it in ProjectLoader.
        public DisplaySettings DisplaySettings { get; set; }

        public List<DisplaySettings> AllDisplaySettings { get; set; }
        = new List<DisplaySettings>();

        // As of March 18, 2022, this has been removed. It's very buggy and it should probably get removed from Glue altogether.
        // Old projects will still use it, but not new ones.
        public bool UsesTranslation { get; set; }

        /// <summary>
        /// Whether to generate the base Type class for all base screens and entities. If false, these will not be generated.
        /// </summary>
        public bool SuppressBaseTypeGeneration { get; set; }

        #endregion

        #region Methods

        public List<ReferencedFileSave> GetAllReferencedFiles()
        {
            List<ReferencedFileSave> allFiles = new List<ReferencedFileSave>();

            for (int i = 0; i < Entities.Count; i++)
            {
                EntitySave entitySave = Entities[i];

                for (int j = 0; j < entitySave.ReferencedFiles.Count; j++)
                {
                    ReferencedFileSave rfs = entitySave.ReferencedFiles[j];
                    allFiles.Add(rfs);
                }
            }

            for (int i = 0; i < Screens.Count; i++)
            {
                ScreenSave screenSave = Screens[i];

                for (int j = 0; j < screenSave.ReferencedFiles.Count; j++)
                {
                    ReferencedFileSave rfs = screenSave.ReferencedFiles[j];
                    allFiles.Add(rfs);
                }
            }

            for (int i = 0; i < GlobalFiles.Count; i++)
            {
                ReferencedFileSave rfs = GlobalFiles[i];
                allFiles.Add(rfs);
            }

            return allFiles;

        }

        public GlueElement GetElement(string elementName)
        {
            GlueElement retval;

            retval = GetScreenSave(elementName);

            if (retval == null)
            {
                retval = GetEntitySave(elementName);
            }

            return retval;
        }

        public ScreenSave GetScreenSave(string screenName)
        {
            if (!string.IsNullOrEmpty(screenName))
            {
                // We don't know what project is using the Glue classes, and it may prefer
                // forward slashes or back slashes.  Therefore we should tolerate either when
                // making comparisons
                screenName = screenName.Replace('/', '\\');

                for (int i = 0; i < Screens.Count; i++)
                {
                    var screenSave = Screens[i];

                    if (screenSave.Name.Replace('/', '\\') == screenName)
                    {
                        return screenSave;
                    }
                }
            }
            return null;
        }

        public EntitySave GetEntitySave(string entityName)
        {
            if (!string.IsNullOrEmpty(entityName))
            {
                // We don't know what project is using the Glue classes, and it may prefer
                // forward slashes or back slashes.  Therefore we should tolerate either when
                // making comparisons
                entityName = entityName.Replace('/', '\\');

                for (int i = 0; i < Entities.Count; i++)
                {
                    EntitySave entitySave = Entities[i];

                    if (entitySave.Name.Replace('/', '\\') == entityName)
                    {
                        return entitySave;
                    }
                }
            }
            return null;
        }

        public CustomClassSave GetCustomClass(string customClassName)
        {
            foreach (CustomClassSave ccs in CustomClasses)
            {
                if (ccs.Name == customClassName)
                {
                    return ccs;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the CustomClassSave referencing the argument file name - the file name will be
        /// the name of a ReferencedFileSave and not absolute.
        /// </summary>
        /// <param name="fileName">The file name, such as GlobalContent/File.csv</param>
        /// <returns>The found CustomClassSave, or null if no matches are found.</returns>
        public CustomClassSave GetCustomClassReferencingFile(string fileName)
        {

            foreach (CustomClassSave ccs in CustomClasses)
            {
                if (ccs.CsvFilesUsingThis.Contains(fileName))
                {
                    return ccs;
                }
            }

            return null;
        }

        public void FixBackSlashSourcefiles()
        {
            List<string> names = new List<string>();
            foreach (EntitySave entitySave in Entities)
            {
                foreach (NamedObjectSave nos in entitySave.NamedObjects)
                {
                    if (nos.SourceFile != null)
                    {
                        nos.SourceFile = nos.SourceFile.Replace('\\', '/');
                    }
                }
            }


            foreach (ScreenSave screenSave in Screens)
            {
                foreach (NamedObjectSave nos in screenSave.NamedObjects)
                {
                    if (nos.SourceFile != null)
                    {
                        nos.SourceFile = nos.SourceFile.Replace('\\', '/');
                    }
                }
            }
        }

        private void FixReferencedFileSaveBackSlashes()
        {
            foreach (EntitySave entitySave in Entities)
            {
                List<ReferencedFileSave> rfsList = entitySave.ReferencedFiles;

                FixReferencedFileBackSlash(rfsList);
            }

            foreach (ScreenSave screenSave in Screens)
            {
                List<ReferencedFileSave> rfsList = screenSave.ReferencedFiles;

                FixReferencedFileBackSlash(rfsList);
            }

            FixReferencedFileBackSlash(GlobalFiles);
        }

        public void FixReferencedFileBackSlash(List<ReferencedFileSave> rfsList)
        {
            System.Threading.Tasks.Parallel.ForEach(rfsList, (rfs) =>

            //foreach (ReferencedFileSave rfs in rfsList)
            {
                rfs.SetNameNoCall(rfs?.Name.Replace("\\", "/"));
            });
        }

        public void SortScreens()
        {
            Screens.Sort(CompareScreenSaves);
        }

        private static int CompareScreenSaves(ScreenSave ss1, ScreenSave ss2)
        {
            return ss1.Name.CompareTo(ss2.Name);
        }

        public void UpdateIfTranslationIsUsed()
        {
            // Update March 18, 2022
            // This is set to false because
            // there is no good control over
            // localization. There can be lots
            // of string variables in Glue, and
            // when a localization DB is added, the
            // string translation is unexpected, and 
            // very difficult to track. Instead, we should
            // just let it happen at the code and Gum level.
            UsesTranslation = false;
            if(this.FileVersion < (int)GluxVersions.RemoveAutoLocalizationOfVariables)
            {
                List<ReferencedFileSave> allReferencedFiles = GetAllReferencedFiles();
                foreach (ReferencedFileSave rfs in allReferencedFiles)
                {
                    if (rfs.IsDatabaseForLocalizing)
                    {
                        UsesTranslation = true;
                        break;
                    }
                }
            }
        }

        public GlueElement GetElementContaining(CustomVariable customVariable)
        {
            foreach (EntitySave entitySave in Entities)
            {
                if (entitySave.CustomVariables.Contains(customVariable))
                {
                    return entitySave;
                }
            }


            foreach (ScreenSave screenSave in Screens)
            {
                if (screenSave.CustomVariables.Contains(customVariable))
                {
                    return screenSave;
                }
            }

            return null;

        }

        public GlueElement GetElementContaining(StateSave stateSave)
        {
            foreach (EntitySave entitySave in Entities)
            {
                foreach (StateSave containedStateSave in entitySave.AllStates)
                {
                    if (containedStateSave == stateSave)
                    {
                        return entitySave;
                    }
                }
            }


            foreach (ScreenSave screenSave in Screens)
            {
                foreach (StateSave containedStateSave in screenSave.AllStates)
                {
                    if (containedStateSave == stateSave)
                    {
                        return screenSave;
                    }
                }
            }

            return null;


        }

        #endregion
    }
}
