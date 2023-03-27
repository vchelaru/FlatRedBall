using System;
using System.Collections.Generic;
using System.Text;

namespace GlueControl.Models
{
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
            AddedGeneratedGame1 = 2,
            ListsHaveAssociateWithFactoryBool = 3,
            GumGueHasGetAnimation = 4,
            GumHasMIsLayoutSuspendedPublic = 4,
            HasFormsObject = 4, // Not sure if this is exact, but it should be maybe around here. This will make old projects work
            CsvInheritanceSupport = 5,
            NugetPackageInCsproj = 6,
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
            SpriteHasUseAnimationTextureFlip = 15,
            RemoveIsScrollableEntityList = 16,
            ScreenManagerHasPersistentPolygons = 17,
        }

        #endregion

        #region Versions

        public const int LatestVersion = (int)GluxVersions.RemoveIsScrollableEntityList;

        public int FileVersion { get; set; }

        #endregion

        #region Elements and References

        public List<ScreenSave> Screens = new List<ScreenSave>();
        public List<EntitySave> Entities = new List<EntitySave>();

        public List<GlueElementFileReference> ScreenReferences { get; set; } = new List<GlueElementFileReference>();
        public List<GlueElementFileReference> EntityReferences { get; set; } = new List<GlueElementFileReference>();

        #endregion
    }
}
