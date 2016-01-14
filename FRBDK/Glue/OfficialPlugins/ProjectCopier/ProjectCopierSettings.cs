using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue;
using System.Xml.Serialization;

namespace OfficialPlugins.ProjectCopier
{
    public enum CopyingDetermination 
    {
        SourceVsDestinationDates,
        SinceLastCopy

    }

    public class ProjectCopierSettings
    {
        public string DestinationFolder { get; set; }

        /// <summary>
        /// Specifies the root folder which will be copied relative to the .sln file.
        /// </summary>
        public string RelativeSourceFolder { get; set; }

        [XmlIgnore]
        public string EffectiveSourceFolder
        {
            get
            {
                bool isRelative = 
                    string.IsNullOrEmpty(RelativeSourceFolder) ||
                    FileManager.IsRelative(RelativeSourceFolder);

                if(isRelative)
                {
                    return FileManager.RemoveDotDotSlash(ProjectManager.ProjectRootDirectory + RelativeSourceFolder);
                }
                else
                {
                    return RelativeSourceFolder;
                }

            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    RelativeSourceFolder = null;
                }
                else
                {
                    RelativeSourceFolder = FileManager.MakeRelative(value, ProjectManager.ProjectRootDirectory);
                }

            }
        }

        [XmlIgnore]
        public CopyingDetermination CopyingDetermination
        {
            get;
            set;
        }

        public static ProjectCopierSettings Load(string fileName)
        {
            return FileManager.XmlDeserialize<ProjectCopierSettings>(fileName);
        }

        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this, fileName);

        }
    }
}
