using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System.ComponentModel;
using FlatRedBall.Glue.Controls;
using System.Drawing.Design;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using Glue.IO;
using System.IO;
using FlatRedBall.Content.Particle;
using FlatRedBall.Glue.Elements;

namespace FlatRedBall.Glue.FormHelpers.PropertyGrids
{
    #region ProjectSpecificFileDisplayer class
    public class ProjectSpecificFileDisplayer : ICustomTypeDescriptor
    {
        public ProjectSpecificFileCollection Instance
        {
            get;
            set;
        }


        public PropertyDescriptorCollection GetProperties()
        {
            var pds = new PropertyDescriptorCollection(null);

            for (var i = 0; i < Instance.Count; i++)
            {
                PropertyDescriptor pd = new ProjectSpecificFileCollectionPropertyDescriptor(Instance, i);

                pds.Add(pd);
            }

            return pds;
        }

        public override string ToString()
        {
            if (Instance.Count == 0)
            {
                return "No project-specific files";
            }
            else if (Instance.Count == 1)
            {
                return "1 Project Specific File";
            }
            else
            {
                return Instance.Count + " Project Specific Files";
            }
        }

        #region The other Unimportant ICustomDescriptor stuff

        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }



        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return GetProperties();
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }
        #endregion
    }

    #endregion

    #region ProjectSpecificFileCollectionPropertyDescriptor
    public class ProjectSpecificFileCollectionPropertyDescriptor : PropertyDescriptor
    {
        private ProjectSpecificFileCollection collection = null;
        private int index = -1;

        public ProjectSpecificFileCollectionPropertyDescriptor(ProjectSpecificFileCollection coll, int idx)
            : base("#" + idx.ToString(), null)
        {
            this.collection = coll;
            this.index = idx;
        }

        public override AttributeCollection Attributes
        {
            get
            {
                return new AttributeCollection(null);
            }
        }

        public override bool CanResetValue(object component)
        {
            return true;
        }

        public override Type ComponentType
        {
            get
            {
                return this.collection.GetType();
            }
        }

        public override string DisplayName
        {
            get
            {
                ProjectSpecificFile projectSpecificFile = this.collection[index];
                return projectSpecificFile.ProjectName;
            }
        }

        public override string Description
        {
            get
            {
                ProjectSpecificFile projectSpecificFile = this.collection[index];
                StringBuilder sb = new StringBuilder();
                sb.Append(projectSpecificFile.FilePath);

                return sb.ToString();
            }
        }

        public override object GetValue(object component)
        {
            return collection[index];
        }

        public override bool IsReadOnly
        {
            get { return true; }
        }

        public override string Name
        {
            get { return "#" + index.ToString(); }
        }

        public override Type PropertyType
        {
            get { return collection[index].GetType(); }
        }

        public override void ResetValue(object component) { }

        public override bool ShouldSerializeValue(object component)
        {
            return true;
        }

        public override void SetValue(object component, object value)
        {
            // this.collection[index] = value;

        }
    }
    #endregion


    #region ProjectSpecificFileCollectionConverter
    internal class ProjectSpecificFileCollectionConverter : ExpandableObjectConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is ProjectSpecificFileCollection)
            {
                var collection = (ProjectSpecificFileCollection)value;
                return collection.Count.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
    #endregion

    public class ReferencedFileSavePropertyGridDisplayer : PropertyGridDisplayer
    {
        ProjectSpecificFileDisplayer mProjectSpecificFileDisplayer = new ProjectSpecificFileDisplayer();

        public override object Instance
        {
            get
            {
                return base.Instance;
            }
            set
            {

                mProjectSpecificFileDisplayer.Instance = ((ReferencedFileSave)value).ProjectSpecificFiles;

                UpdateIncludedAndExcluded(value as ReferencedFileSave);



                base.Instance = value;


                UpdatePropertyGridMembers();

            }
        }

        private void UpdatePropertyGridMembers()
        {

            var member = GetPropertyGridMember("BuildTool");
            if (member != null)
            {
                var availableTools = new AvailableBuildTools();
                availableTools.ShowNewApplication = false;

                ReferencedFileSave instance = ((ReferencedFileSave)Instance);
                var extension = FileManager.GetExtension(instance.Name);

                availableTools.ShowNoneOption = Elements.AvailableAssetTypes.Self.AllAssetTypes
                    .Any(item => item.Extension == extension && string.IsNullOrEmpty(item.CustomBuildToolName));


                if (instance != null && !string.IsNullOrEmpty(instance.SourceFile))
                {

                    availableTools.SourceFileExtensionRestriction = FileManager.GetExtension(instance.SourceFile);
                }
                member.TypeConverter = availableTools;
            }
        }

        ProjectSpecificFileDisplayer GetProjectSpecificFileMember()
        {
            return mProjectSpecificFileDisplayer;
        }

        void SetProjectSpecificFiles(object sender, MemberChangeArgs args)
        {
            // do nothing
        }

        object GetImageWidth()
        {
            return ImageHeader.GetDimensions(
                    ProjectManager.MakeAbsolute(((ReferencedFileSave)Instance).Name)).Width;
        }

        object GetImageHeight()
        {
            return ImageHeader.GetDimensions(
                    ProjectManager.MakeAbsolute(((ReferencedFileSave)Instance).Name)).Height;
        }

        object GetEquilibriumParticleCount()
        {
            string fileName = ProjectManager.MakeAbsolute(((ReferencedFileSave)Instance).Name);
            if (File.Exists(fileName))
            {
                EmitterSaveList esl = EmitterSaveList.FromFile(fileName);

                return esl.GetEquilibriumParticleCount();
            }
            else
            {
                return 0;
            }
        }

        object GetBurstParticleCount()
        {
            EmitterSaveList esl = EmitterSaveList.FromFile(ProjectManager.MakeAbsolute(((ReferencedFileSave)Instance).Name));
            return esl.GetBurstParticleCount();
        }

        private void UpdateIncludedAndExcluded(ReferencedFileSave instance)
        {
            ResetToDefault();

            AssetTypeInfo ati = instance.GetAssetTypeInfo();


            ExcludeMember("InvalidFileNameCharacters");
            ExcludeMember("ToStringDelegate");
            ExcludeMember("Properties");

            ContainerType containerType = instance.GetContainerType();

            if (containerType == ContainerType.Entity)
            {
                // We do this because this is set automatically if the Entity is unique
                ExcludeMember("IsSharedStatic");

                // We do this because whether the objects from a file are manually updated or not
                // should be up to the entity, not the source file.
                ExcludeMember("IsManuallyUpdated");
            }

            if (containerType == ContainerType.Entity || ati == null || ati.QualifiedRuntimeTypeName.QualifiedType != "Microsoft.Xna.Framework.Media.Song")
            {
                ExcludeMember("DestroyOnUnload");
            }

            #region Extension-based additions/removals
            string extension = FileManager.GetExtension(instance.Name);

            if (extension != "csv" && !instance.TreatAsCsv)
            {
                ExcludeMember("CreatesDictionary");
                ExcludeMember("IsDatabaseForLocalizing");
                ExcludeMember("UniformRowType");
            }
            else
            {
                IncludeMember("UniformRowType", typeof(ReferencedFileSave), new AvailablePrimitiveTypeArraysStringConverter());
            }

            if ((extension != "txt" && extension != "csv") ||
                (extension == "txt" && instance.TreatAsCsv == false)
                )
            {
                ExcludeMember("CsvDelimiter");
            }

            if (extension != "txt")
            {
                ExcludeMember("TreatAsCsv");
            }

            if (extension == "png")
            {
                Attribute[] fileDetailsCategoryAttribute = new Attribute[]{
                    new CategoryAttribute("File Details")};
                IncludeMember("ImageWidth", typeof(int), null, GetImageWidth, null, fileDetailsCategoryAttribute);
                IncludeMember("ImageHeight", typeof(int), null, GetImageHeight, null, fileDetailsCategoryAttribute);
            }

            if (extension == "emix")
            {
                Attribute[] fileDetailsCategoryAttribute = new Attribute[]{
                    new CategoryAttribute("File Details")};

                IncludeMember("EquilibriumParticleCount", typeof(float), null, GetEquilibriumParticleCount, null, fileDetailsCategoryAttribute);
                IncludeMember("BurstParticleCount", typeof(float), null, GetBurstParticleCount, null, fileDetailsCategoryAttribute);
                
            }

            #endregion

            AddProjectSpecificFileMember();


            if (!instance.LoadedAtRuntime)
            {
                ExcludeMember("IsSharedStatic");
                ExcludeMember("IsManuallyUpdated");
                ExcludeMember("LoadedOnlyWhenReferenced");
                ExcludeMember("HasPublicProperty");
                ExcludeMember("InstanceName");
                ExcludeMember("IncludeDirectoryRelativeToContainer");
            }

            if (ati == null || string.IsNullOrEmpty(ati.MakeManuallyUpdatedMethod))
            {
                ExcludeMember("IsManuallyUpdated");
            }

            if (!instance.GetCanUseContentPipeline())
            {
                ExcludeMember("UseContentPipeline");
            }
            if (!instance.UseContentPipeline || ati.QualifiedRuntimeTypeName.QualifiedType != "Microsoft.Xna.Framework.Graphics.Texture2D")
            {
                ExcludeMember("TextureFormat");
            }

            IncludeMember("OpensWith", typeof(ReferencedFileSave), new AvailableApplicationsStringConverters());

            bool shouldShowRuntimeType = instance.LoadedAtRuntime;
            if (shouldShowRuntimeType)
            {
                IncludeMember("RuntimeType", typeof(ReferencedFileSave), new AvailableRuntimeTypeForExtensionConverter() { Extension = extension });
            }
            else
            {
                ExcludeMember("RuntimeType");
            }

            if (string.IsNullOrEmpty(instance.SourceFile))
            {
                ExcludeMember("SourceFile");
                ExcludeMember("BuildTool");
                ExcludeMember("AdditionalArguments");
                ExcludeMember("ConditionalCompilationSymbols");
            }
            else
            {

            }
        }

        private void AddProjectSpecificFileMember()
        {
            ExcludeMember("ProjectSpecificFiles");

            Attribute[] attributes = new Attribute[]
            {
                new EditorAttribute(typeof(ProjectSpecificFileCollectionEditor), typeof(UITypeEditor))

            };

            IncludeMember("Project Specific Files", typeof(ProjectSpecificFileDisplayer),
                SetProjectSpecificFiles, GetProjectSpecificFileMember, new ProjectSpecificFileCollectionConverter(), attributes);
        }



    }
}
