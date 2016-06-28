using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using EditorObjects.SaveClasses;
using FlatRedBall.IO;
using System.IO;
using EditorObjects.Collections;
using FlatRedBall.IO.Csv;

namespace FlatRedBall.Glue.SaveClasses
{
    #region FileProgramAssociation

    public class FileProgramAssociations
    {
        public string Extension;
        public string DefaultProgram;

 
    }

    #endregion

    public class ProjectFileGlueFilePair
    {
        public string GlueFileName { get; set; }
        public string GameProjectFileName { get; set; }
    }

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
        public string[] RecentFiles = new string[5];

        public List<string> BuildTools = new List<string>();

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

        public bool AutoCopyFilesOnChange { get; set; }

        public ExternalSeparatingList<BuildToolAssociation> BuildToolAssociations = new ExternalSeparatingList<BuildToolAssociation>();

        [XmlIgnore]
        public static bool StopSavesAndLoads { get; set; }

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

            Associations.ReAddExternals();
            BuildToolAssociations.ReAddExternals();
        }

        public void LoadExternalBuildToolsFromCsv(string csvFileName)
        {
            List<BuildToolAssociation> externals = new List<BuildToolAssociation>();

            CsvFileManager.CsvDeserializeList(typeof(BuildToolAssociation), csvFileName, externals);

            BuildToolAssociations.AddExternalRange(externals);
        }
	}
}
