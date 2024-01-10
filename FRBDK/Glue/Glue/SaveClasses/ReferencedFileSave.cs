using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Xml.Serialization;
using FlatRedBall.IO;
using FlatRedBall.Content;
//using FlatRedBall.Gui;
//using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using FlatRedBall.Glue.Interfaces;
using SourceReferencingFile = FlatRedBall.Glue.Content.SourceReferencingFile;
using Newtonsoft.Json;

namespace FlatRedBall.Glue.SaveClasses
{
    #region ProjectSpecificFileCollection Class
    public class ProjectSpecificFileCollection : CollectionBase
    {
        public void Add(ProjectSpecificFile projectSpecificFile)
        {
            List.Add(projectSpecificFile);
        }

        public void Remove(ProjectSpecificFile projectSpecificFile)
        {
            List.Remove(projectSpecificFile);
        }

        public int Count
        {
            get
            {
                return this.List.Count;
            }
        }

        public ProjectSpecificFile this[int index]
        {
            get { return (ProjectSpecificFile)this.List[index]; }
        }

    }

    #endregion
    
    #region ProjectSpecificFileConverter
    internal class ProjectSpecificFileConverter : TypeConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context,
                             System.Globalization.CultureInfo culture,
                             object value, Type destType)
        {
            if (destType == typeof(string) && value is ProjectSpecificFile)
            {
                var emp = (ProjectSpecificFile)value;
                return emp.File?.FullPath;
            }
            return base.ConvertTo(context, culture, value, destType);
        }
    }
    #endregion

    #region ProjectSpecificFile
    [TypeConverter(typeof(ProjectSpecificFileConverter))]
    public class ProjectSpecificFile
    {
        [Obsolete("This was sometimes used to mean the ID like Android, and sometimes name. ID can be misleading because multiple projects can have the same ID, so we shouldn't use this. Use ProjectName instead")]
        public string ProjectId
        {
            get { return ProjectName; }
            set { ProjectName = value; }
        }

        public string ProjectName
        {
            get; set;
        }

        [Obsolete("Use File to handle equality consistently")]
        public string FilePath 
        {
            get => File?.FullPath;
            set => File = value;
        }

        // Need to do this since it doesn't have a no-arg constructor and...should it?
        [XmlIgnore]
        [JsonIgnore]
        public FilePath File
        {
            get; set;
        }

        public string Display
        {
            get
            {
                return File + " (" + ProjectName + ")";
            }
        }

        public override string ToString()
        {
            return File + " (" + ProjectName + ")";
        }
    }
    #endregion

    #region Enums

    public enum AvailableDelimiters
    {
        Comma,
        Tab,
        Pipe
    }

    public static class AvailableDelimiterExtensions
    {
        public static char ToChar(this AvailableDelimiters delimiter)
        {
            switch (delimiter)
            {
                case AvailableDelimiters.Comma:
                    return ',';
                case AvailableDelimiters.Tab:
                    return '\t';
                case AvailableDelimiters.Pipe:
                    return '|';
            }
            throw new ArgumentException();
        }
    }


    #endregion

    #region Delegates

    public delegate string ReferencedFileSaveToString(ReferencedFileSave rfs);

    #endregion

    public class ReferencedFileSave : IPropertyListContainer
    {
        #region Fields

        [XmlIgnore]
        [JsonIgnore]
        public static ReferencedFileSaveToString ToStringDelegate;

        [XmlIgnore]
        [JsonIgnore]
        public static char[] InvalidFileNameCharacters = new char[]{
            '\\',
            '/',
            ':',
            '*',
            '?',
            '"',
            '<',
            '>',
            '|'};



        string mName;
        private List<ProjectSpecificFile> _projectSpecificFiles = new List<ProjectSpecificFile>();

        //private string mLayerOn;


        #endregion

        #region Properties

        /// <summary>
        /// A cached FilePath. This will remain null unless other code sets it (such as Glue).
        /// This gets nulled out whenever Name is set.
        /// </summary>
        [JsonIgnore]
        [XmlIgnore]
        public FilePath FilePath;

        [XmlIgnore]
        [JsonIgnore]
        public string CachedInstanceName;

        // Have Name be first so it JSON serializes first:
        /// <summary>
        /// The name of the file name, relative to the Content folder.
        /// </summary>
        public string Name
        {
            get => mName;
            set
            {
                if (!String.IsNullOrEmpty(value) && value.Replace("\\", "/").StartsWith("content/", StringComparison.OrdinalIgnoreCase))
                    value = value.Substring("content/".Length);

                mName = value;

                FilePath = null;
                CachedInstanceName = null;
            }
        }

        public List<PropertySave> Properties
        {
            get;
            set;
        } = new List<PropertySave>();
        public bool ShouldSerializeProperties()
        {
            return Properties != null && Properties.Count != 0;
        }


        [Category("Memory and Performance"), 
        Description("Whether the object created by this file will be a manually-updated object.  For example, Scenes which do not move can be made manually updated to improve runtime performance."),
        DefaultValue(false)]
        public bool IsManuallyUpdated
        {
            get;
            set;
        }

        [Category("Memory and Performance")]
        public bool IsSharedStatic
        {
            get;
            set;
        }

        bool mIncludeDirectoryRelativeToContainer;
        [CategoryAttribute("Access"), DefaultValue(false)]
        public bool IncludeDirectoryRelativeToContainer
        {
            get => mIncludeDirectoryRelativeToContainer;
            set
            {
                mIncludeDirectoryRelativeToContainer = value;
                CachedInstanceName = null;
            }
        }
        
        // Moved to Properties June 9, 2019
        // Eventually will want to mark this as XmlIgnore
        [Category("Memory and Performance"), DefaultValue(false)]
        public bool LoadedOnlyWhenReferenced
        {
            get
            {
                return Properties.GetValue<bool>(nameof(LoadedOnlyWhenReferenced));
            }
            set
            {
                Properties.SetValue(nameof(LoadedOnlyWhenReferenced), value);
            }
        }

        // converted to properties on June 9, 2019
        // Update 6/22/2019 - cannot ever convert this
        // to a property because the default value is true
        // so there may be old projects that don't set the value
        // in the XML. 
        [Category("Destroy"), DefaultValue(true)]
        public bool DestroyOnUnload
        {
            get;
            set;
        }


        public string Summary
        {
            get;
            set;
        }

        // December 21, 2011
        // Why do we need this property?
        // I don't think it's used anywhere.
        //[Browsable(false)]
        //public string ContentProjectParent
        //{
        //    get;
        //    set;
        //}

        [CategoryAttribute("Access"), DefaultValue(false)]
        public bool HasPublicProperty
        {
            get;
            set;
        }



        [DefaultValue("<DEFAULT>")]
        public string OpensWith
        {
            get;
            set;
        }

        [CategoryAttribute("Code"), DefaultValue(true)]
        public bool LoadedAtRuntime
        {
            get;
            set;
        }

        [CategoryAttribute("CSV"), DefaultValue(false)]
        public bool CreatesDictionary
        {
            get;
            set;
        }

        [CategoryAttribute("CSV"), DefaultValue(AvailableDelimiters.Comma)]
        public AvailableDelimiters CsvDelimiter
        {
            get;
            set;
        }

        [CategoryAttribute("CSV"), DefaultValue(false)]
        public bool TreatAsCsv
        {
            get;
            set;
        }

        [XmlIgnore]
        [JsonIgnore]
        [Browsable(false)]
        public List<SourceReferencingFile> SourceFileCache
        {
            get;
            // Setter is made public so extension methods can access it
            set;
        } = new List<SourceReferencingFile>();

        [Category("Build")]
        public string SourceFile
        {
            get;
            set;
        }
        public bool ShouldSerializeSourceFile()
        {
            return !string.IsNullOrEmpty(SourceFile);
        }


        [Category("Build")]
        public string BuildTool
        {
            get;
            set;
        }
        public bool ShouldSerializeBuildTool()
        {
            return !string.IsNullOrEmpty(BuildTool);
        }

        [Category("Build")]
        public string AdditionalArguments
        {
            get;
            set;
        }
        public bool ShouldSerializeAdditionalArguments()
        {
            return !string.IsNullOrEmpty(AdditionalArguments);
        }

        //[Category("Build")]
        //public string BuildTool
        //{
        //    get;
        //    set;
        //}

        [XmlIgnore]
        [JsonIgnore]
        public bool IsDatabaseForLocalizing
        {
            get
            {
                return Properties.GetValue<bool>(nameof(IsDatabaseForLocalizing));
            }
            set
            {
                Properties.SetValue(nameof(IsDatabaseForLocalizing), value);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public bool UseContentPipeline
        {
            get
            {
                return Properties.ContainsValue("UseContentPipeline") && ((bool)Properties.GetValue("UseContentPipeline"));
            }
            set
            {
                Properties.SetValue("UseContentPipeline", value);
            }
        }

        //[XmlIgnore]
        //[JsonIgnore]
        //public TextureProcessorOutputFormat TextureFormat
        //{
        //    get
        //    {
        //        return Properties.GetValue<TextureProcessorOutputFormat>("TextureFormat");
        //    }
        //    set
        //    {
        //        Properties.SetValue("TextureFormat", value);
        //    }
        //}

        //[XmlIgnore]
        //public bool GenerateMipmaps

        [XmlIgnore]
        [JsonIgnore]
        [CategoryAttribute("CSV")]
        public string UniformRowType
        {
            get
            {
                if (Properties.ContainsValue("UniformRowType"))
                {
                    return (string)Properties.GetValue("UniformRowType");
                }
                else
                {
                    return null;
                }
            }

            set
            {
                string valueToSet = value;
                if (valueToSet == "<NONE>")
                {
                    valueToSet = null;
                }
                Properties.SetValue("UniformRowType", valueToSet);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        [Browsable(false)]
        public bool IsCsvOrTreatedAsCsv
        {
            get
            {
                return TreatAsCsv || FileManager.GetExtension(this.Name) == "csv";
            }
        }

        public List<ProjectSpecificFile> ProjectSpecificFiles
        {
            get { return _projectSpecificFiles; }
            set { _projectSpecificFiles = value; }
        }
        public bool ShouldSerializeProjectSpecificFiles()
        {
            return ProjectSpecificFiles != null && ProjectSpecificFiles.Count != 0;
        }

        [CategoryAttribute("Build")]
        public string ConditionalCompilationSymbols
        {
            get;
            set;
        }
        public bool ShouldSerializeConditionalCompilationSymbols()
        {
            return !string.IsNullOrEmpty(ConditionalCompilationSymbols);
        }

        [CategoryAttribute("Code")]
        public string RuntimeType
        {
            get;
            set;
        }

        [CategoryAttribute("Code"), DefaultValue(true)]
        public bool AddToManagers
        {
            get;
            set;
        }

        public List<string> ProjectsToExcludeFrom
        {
            get;
            set;
        }

        public bool IsCreatedByWildcard { get; set; }

        #endregion

        #region Methods

        #region Constructor

        public ReferencedFileSave()
        {
            // Even though we have a DefaultValue, must do this:
            // https://stackoverflow.com/questions/5882164/xml-serialization-and-defaultvalue-related-problem-in-c-sharp
            DestroyOnUnload = true;
            ProjectsToExcludeFrom = new List<string>();
            AddToManagers = true;

            CsvDelimiter = AvailableDelimiters.Comma;
            SourceFileCache = new List<SourceReferencingFile>();
            LoadedAtRuntime = true;
            OpensWith = "<DEFAULT>";

            // September 12, 2012
            // IsSharedStatic used 
            // to default to false.
            // It would be set to true
            // on Entities and false on
            // Screen, but I think it was
            // set to false on Screens just
            // as a holdover from the very early
            // days.  Files need to be static for
            // Entities and GlobalContent.  They could
            // go either way on Screens, but we're going
            // to default them to static for consistency and
            // for the generated GetFile method.
            IsSharedStatic = true;
        }

        #endregion

        #region Public Methods

        public ReferencedFileSave Clone()
        {
            ReferencedFileSave clonedRfs = (ReferencedFileSave)this.MemberwiseClone();

            clonedRfs.ProjectsToExcludeFrom = new List<string>();
            clonedRfs.ProjectsToExcludeFrom.AddRange(this.ProjectsToExcludeFrom);

            return clonedRfs;
        }

        public void SetNameNoCall(string newName)
        {
            mName = newName;
        }

        public override string ToString()
        {
            if (ToStringDelegate != null)
            {
                return ToStringDelegate(this);
            }
            else
            {
                return mName;
            }
        }
        
        #endregion
        
        #endregion
    }

}
