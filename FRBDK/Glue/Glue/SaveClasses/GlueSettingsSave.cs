using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using EditorObjects.SaveClasses;
using FlatRedBall.IO;
using System.IO;
using EditorObjects.Collections;
using FlatRedBall.IO.Csv;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.LoadRecentFilesPlugin.Views;
using System.Windows;
using Newtonsoft.Json;
using FlatRedBall.Glue.Themes;

namespace FlatRedBall.Glue.SaveClasses
{
    #region FileProgramAssociation

    public class FileProgramAssociations
    {
        public string Extension;
        public string DefaultProgram;

 
    }

    #endregion

    #region ProjectFileGlueFilePair Class
    public class ProjectFileGlueFilePair
    {
        public string GlueFileName { get; set; }
        public string GameProjectFileName { get; set; }

        public override string ToString()
        {
            return $"{GameProjectFileName} ({GlueFileName})";
        }
    }

    #endregion

    #region RecentFileSave

    public class RecentFileSave
    {
        public string FileName { get; set; }
        public bool IsFavorite { get; set; }
        public DateTime LastTimeAccessed { get; set; }

        public string PlatformType { get; set; }

        public override string ToString() => $"{FileName} {LastTimeAccessed}";
    }

    #endregion

    /// <summary>
    /// Global glue settings for the current user, not tied to any particular project.
    /// </summary>
    public class GlueSettingsSave
    {
        #region Fields

        // Vic says: I think this is in its own file - I don't think we're using it
        //public List<string> AvailablePrograms;          
    
        // This is migrating to be project-specific
        // so that users can set up their tools and not
        // have to worry about others in the project having
        // to manually set them up.
        [XmlElementAttribute("Association")]
        public ExternalSeparatingList<FileProgramAssociations> Associations = new ExternalSeparatingList<FileProgramAssociations>();

        public List<RecentFileSave> RecentFileList { get; set; } = new List<RecentFileSave>();

        #endregion

        #region Properties

        public static string SettingsFileName
        {
            get
            {
                if (StopSavesAndLoads)
                    return "";
                
                return FileManager.UserApplicationDataForThisApplication +
                       "settings.xml";
            }
        }

        public static string SettingsFileNameJson => FileManager.UserApplicationDataForThisApplication +
                       "settings.json";

		public string LastProjectFile
		{
			get;
			set;
		}

        public List<ProjectFileGlueFilePair> GlueLocationSpecificLastProjectFiles
        {
            get;
            set;
        } = new List<ProjectFileGlueFilePair>();

        public int WindowLeft { get; set; }
        public int WindowTop { get; set; }
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }
        public int MainSplitterDistance { get; set; }
        public int StoredRecentFiles { get; set; }

        public List<string> TopTabs { get; set; } = new List<string>();
        public List<string> LeftTabs { get; set; } = new List<string>();
        public List<string> CenterTabs { get; set; } = new List<string>();
        public List<string> RightTabs { get; set; } = new List<string>();
        public List<string> BottomTabs { get; set; } = new List<string>();

        public double? LeftTabWidthPixels { get; set; }

        public ExternalSeparatingList<BuildToolAssociation> BuildToolAssociations = new ExternalSeparatingList<BuildToolAssociation>();

        [XmlIgnore] public static bool StopSavesAndLoads { get; set; }

        public List<PropertySave> Properties = new List<PropertySave>();

        public bool IsBookmarksListVisible { get; set; }
        public double BookmarkRowHeight { get; set; }

        public ThemeConfig ThemeConfig { get; set; } = new();

        /// <summary>
        /// XML cannot serialize CultureInfo because it doesn't have a parameterless constructor;
        /// hence, this property is used as a workaround.
        /// </summary>
        public string Culture
        {
            get => CurrentCulture?.TwoLetterISOLanguageName ?? "en";
            set
            {
                if (!String.IsNullOrWhiteSpace(value))
                {
                    CurrentCulture = new CultureInfo(value);
                }
            }
        }
        [XmlIgnore] public CultureInfo CurrentCulture { get; set; }

        public string DefaultNewProjectDirectory { get; set; }

        #endregion

        public void Save()
        {
            if(StopSavesAndLoads) return;

            string directory = FileManager.GetDirectory(SettingsFileName);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Associations.RemoveExternals();
            BuildToolAssociations.RemoveExternals();

            FileManager.XmlSerialize(this, SettingsFileName);

            // for some reason the build tool associations die here, maybe because of the type converters? 
            //var asJson = JsonConvert.SerializeObject(this, Formatting.Indented);
            //FileManager.SaveText(asJson, SettingsFileNameJson);

            Associations.ReAddExternals();
            BuildToolAssociations.ReAddExternals();
        }

        #region Loading-related Methods

        public void FixAllTypes()
        {
            CurrentCulture ??= CultureInfo.InstalledUICulture.TwoLetterISOLanguageName switch
            {
                "fr" => new CultureInfo("fr-FR"),
                "nl" => new CultureInfo("nl-NL"),
                "de" => new CultureInfo("de-DE"),
                _ => new CultureInfo("en-US")
            };

            foreach (var property in Properties)
            {
                FixAllTypes(property);
            }
        }

        private static void FixAllTypes(PropertySave property)
        {
            if (!string.IsNullOrEmpty(property.Type) && property.Value != null)
            {
                object variableValue = property.Value;
                var type = property.Type;

                variableValue = CustomVariableExtensionMethods.FixValue(variableValue, type);

                property.Value = variableValue;
            }
        }

        public void LoadExternalBuildToolsFromCsv(string csvFileName)
        {
            var externals = new List<BuildToolAssociation>();

            CsvFileManager.CsvDeserializeList(typeof(BuildToolAssociation), csvFileName, externals);

            BuildToolAssociations.AddExternalRange(externals);
        }

        #endregion
    }
}
