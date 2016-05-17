using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

//using System.Windows.Forms;
#if GLUE
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.IO.Zip;
using FlatRedBall.Glue.StandardTypes;
using FlatRedBall.Glue.VSHelpers;
using Microsoft.Build.BuildEngine;
using KellermanSoftware.CompareNetObjects;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins;
#endif
using System.Collections;



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

    public class GlueProjectSave
    {
        #region Fields


        #region Camera Fields
        public bool In2D = true;
        public bool RunFullscreen = false;


        public bool SetResolution = false;
        public int ResolutionWidth = 800;
        public int ResolutionHeight = 600;

        public bool SetOrthogonalResolution = false;
        public int OrthogonalWidth = 800;
        public int OrthogonalHeight = 600;


        #endregion


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


        public List<ScreenSave> Screens = new List<ScreenSave>();

        public List<EntitySave> Entities = new List<EntitySave>();

        public List<ReferencedFileSave> GlobalFiles = new List<ReferencedFileSave>();


        public GlobalContentSettingsSave GlobalContentSettingsSave = new GlobalContentSettingsSave();


        public List<string> SyncedProjects = new List<string>();


        public string StartUpScreen;


        public List<ResolutionValues> ResolutionPresets = new List<ResolutionValues>();


        public PerformanceSettingsSave PerformanceSettingsSave =
            new PerformanceSettingsSave();


        public List<string> IgnoredDirectories = new List<string>();


        public List<CustomClassSave> CustomClasses = new List<CustomClassSave>();


        public List<TranslatedFileSave> TranslatedFiles = new List<TranslatedFileSave>();


        public bool ApplyToFixedResolutionPlatforms = true;


        public string ExternallyBuiltFileDirectory = "../../";


        public string CustomGameClass;


        #endregion

        #region Properties

        [XmlIgnore]
        public bool UsesTranslation
        {
            get;
            set;
        }

        [XmlIgnore]
        public IEnumerator<NamedObjectSave> NamedObjects
        {
            get
            {
                foreach (ScreenSave screenSave in Screens)
                {
                    foreach (NamedObjectSave nos in screenSave.NamedObjects)
                    {
                        yield return nos;
                    }
                }

                foreach (EntitySave entitySave in Entities)
                {
                    foreach (NamedObjectSave nos in entitySave.NamedObjects)
                    {
                        yield return nos;
                    }
                }
            }
        }

        [XmlIgnore]
        public bool GlobalContentHasChanged { get; set; }

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

        public void CreateTranslatedFileSaveIfNecessary(string fileName)
        {
            TranslatedFileSave tfs = GetTranslatedFileSave(fileName);

            if (tfs == null)
            {
                tfs = new TranslatedFileSave();

                tfs.FileName = fileName;

                TranslatedFiles.Add(tfs);
            }
        }

        public TranslatedFileSave GetTranslatedFileSave(string fileName)
        {
            for (int i = 0; i < TranslatedFiles.Count; i++)
            {
                if (TranslatedFiles[i].FileName == fileName)
                {
                    return TranslatedFiles[i];
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
                rfs.SetNameNoCall(rfs.Name.Replace("\\", "/"));
            });
        }

        public void SortScreens()
        {
            Screens.Sort(CompareScreenSaves);
        }

        private static int CompareReferencedFiles(ReferencedFileSave rfs1, ReferencedFileSave rfs2)
        {
            return rfs1.Name.CompareTo(rfs2.Name);
        }

        private static int CompareScreenSaves(ScreenSave ss1, ScreenSave ss2)
        {
            return ss1.Name.CompareTo(ss2.Name);
        }

        public void UpdateIfTranslationIsUsed()
        {
            List<ReferencedFileSave> allReferencedFiles = GetAllReferencedFiles();
            UsesTranslation = false;
            foreach (ReferencedFileSave rfs in allReferencedFiles)
            {
                if (rfs.IsDatabaseForLocalizing)
                {
                    UsesTranslation = true;
                    break;
                }
            }
        }

        // I don't think we're going to use these anymore
        //public IElement GetElementContaining(NamedObjectPropertyOverride propertyOverride)
        //{

        //    foreach (EntitySave entitySave in Entities)
        //    {
        //        foreach (StateSave containedStateSave in entitySave.AllStates)
        //        {
        //            // I don't think we're going to use these anymore

        //            //if (containedStateSave.NamedObjectPropertyOverrides.Contains(propertyOverride))
        //            //{
        //            //    return entitySave;
        //            //}
        //        }

        //    }


        //    foreach (ScreenSave screenSave in Screens)
        //    {
        //        foreach (StateSave containedStateSave in screenSave.AllStates)
        //        {
        //            // I don't think we're going to use these anymore

        //            //if (containedStateSave.NamedObjectPropertyOverrides.Contains(propertyOverride))
        //            //{
        //            //    return screenSave;
        //            //}
        //        }
        //    }

        //    return null;


        //}

        public IElement GetElementContaining(CustomVariable customVariable)
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

        public IElement GetElementContaining(StateSave stateSave)
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
